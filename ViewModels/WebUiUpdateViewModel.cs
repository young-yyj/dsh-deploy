using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dsh_deploy.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace dsh_deploy.ViewModels
{
    /// <summary>
    /// dsh-web-ui 升级窗口 ViewModel
    /// </summary>
    public class WebUiUpdateViewModel : ViewModelBase
    {
        private readonly IDshService _dshService;
        private readonly WebUiUpdateService _updateService;
        private readonly LogService _logService;
        private readonly Dispatcher _dispatcher;
        private CancellationTokenSource? _upgradeCts;

        private string _installedVersion = "检测中...";
        private string _latestVersion = "检测中...";
        private string _statusText = "就绪";
        private string _statusColor = "#9E9E9E";
        private bool _isBusy;
        private bool _isUpgrading;

        private const int MaxOutputLines = 500;

        public WebUiUpdateViewModel()
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _dshService = ServiceLocator.Instance.Get<IDshService>();
            _updateService = ServiceLocator.Instance.Get<WebUiUpdateService>();
            _logService = _dshService.LogService;

            CheckUpdateCommand = new AsyncRelayCommand(CheckUpdateAsync, () => !IsBusy);
            UpgradeCommand = new AsyncRelayCommand(UpgradeAsync, () => !IsBusy);
            CancelCommand = new RelayCommand(CancelUpgrade, () => IsUpgrading);
            CloseCommand = new RelayCommand(Close);

            // 初始加载本地安装版本
            _ = LoadInstalledVersionAsync();
        }

        #region 属性

        /// <summary>
        /// 当前安装版本
        /// </summary>
        public string InstalledVersion
        {
            get => _installedVersion;
            set => SetProperty(ref _installedVersion, value);
        }

        /// <summary>
        /// 最新版本
        /// </summary>
        public string LatestVersion
        {
            get => _latestVersion;
            set => SetProperty(ref _latestVersion, value);
        }

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StatusColor
        {
            get => _statusColor;
            set => SetProperty(ref _statusColor, value);
        }

        /// <summary>
        /// 是否忙（检查或升级中）
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 是否正在升级
        /// </summary>
        public bool IsUpgrading
        {
            get => _isUpgrading;
            private set
            {
                if (SetProperty(ref _isUpgrading, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 实时输出集合
        /// </summary>
        public ObservableCollection<string> Output { get; } = new();

        /// <summary>
        /// 所属窗口（由窗口代码设置）
        /// </summary>
        public Window? Window { get; set; }

        #endregion

        #region 命令

        public ICommand CheckUpdateCommand { get; }
        public ICommand UpgradeCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand CloseCommand { get; }

        #endregion

        #region 命令实现

        private async Task LoadInstalledVersionAsync()
        {
            InstalledVersion = await _updateService.GetInstalledVersionAsync() ?? "未安装";
        }

        private async Task CheckUpdateAsync()
        {
            IsBusy = true;
            StatusText = "正在检查更新...";
            StatusColor = "#FF9800";
            try
            {
                InstalledVersion = await _updateService.GetInstalledVersionAsync() ?? "未安装";

                if (InstalledVersion == "未安装")
                {
                    LatestVersion = "-";
                    StatusText = "dsh-web-ui 未安装";
                    StatusColor = "#F44336";
                    return;
                }

                var latest = await _updateService.GetLatestVersionAsync();
                LatestVersion = latest ?? "获取失败";

                if (latest == null)
                {
                    StatusText = "获取最新版本失败";
                    StatusColor = "#F44336";
                }
                else if (CompareVersions(InstalledVersion, latest) < 0)
                {
                    StatusText = $"发现新版本 {latest}";
                    StatusColor = "#FF9800";
                }
                else
                {
                    StatusText = "已是最新版本";
                    StatusColor = "#4CAF50";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task UpgradeAsync()
        {
            IsBusy = true;
            IsUpgrading = true;
            StatusText = "预检中...";
            StatusColor = "#FF9800";
            _upgradeCts = new CancellationTokenSource();
            try
            {
                AppendOutput("=== 开始升级 dsh-web-ui ===");

                // 1. 预检：是否已安装
                if (!await _updateService.IsInstalledAsync())
                {
                    StatusText = "升级失败";
                    StatusColor = "#F44336";
                    AppendOutput($"dsh-web-ui 未安装，路径不存在: {_updateService.ProfilePath}");
                    MessageBox.Show(
                        $"dsh-web-ui 未安装，路径不存在：\n{_updateService.ProfilePath}",
                        "升级失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 2. 预检：pnpm 是否可用
                AppendOutput("检查 pnpm...");
                if (!await _updateService.IsPnpmAvailableAsync())
                {
                    StatusText = "升级失败";
                    StatusColor = "#F44336";
                    AppendOutput("未检测到 pnpm");
                    MessageBox.Show(
                        "未检测到 pnpm，请先安装：\n\nnpm install -g pnpm",
                        "升级失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 3. 记录旧版本
                var oldVersion = await _updateService.GetInstalledVersionAsync() ?? "未知";
                InstalledVersion = oldVersion;
                AppendOutput($"当前版本: {oldVersion}");

                // 4. DSH 运行中 → 确认后停止
                var status = await _dshService.RefreshStatusAsync(forceRefresh: true);
                var dshProcesses = await _dshService.GetProcessesAsync();
                if (status.IsOnline || dshProcesses.Count > 0)
                {
                    var confirm = MessageBox.Show(
                        "升级需要关闭 DSH，期间 Web UI 与任务看板等将中断。\n\n是否停止 DSH 并继续升级？",
                        "关闭 DSH", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (confirm != MessageBoxResult.Yes)
                    {
                        StatusText = "已取消：DSH 仍在运行";
                        StatusColor = "#9E9E9E";
                        AppendOutput("已取消：DSH 未停止");
                        return;
                    }

                    AppendOutput("正在停止 DSH...");
                    await _dshService.StopServiceAsync();
                    await Task.Delay(1000);
                    await _dshService.RefreshStatusAsync(forceRefresh: true);
                    AppendOutput("DSH 已停止");
                }

                // 5. 执行升级
                StatusText = "正在升级...";
                AppendOutput("执行 pnpm update --latest（镜像源）...");
                var result = await _updateService.UpdateAsync(
                    new Progress<string>(AppendOutput), _upgradeCts.Token);

                if (result.Cancelled)
                {
                    StatusText = "升级已取消";
                    StatusColor = "#9E9E9E";
                    AppendOutput("升级已取消");
                    return;
                }

                if (!result.Success)
                {
                    StatusText = "升级失败";
                    StatusColor = "#F44336";
                    AppendOutput($"升级失败: {result.Message}");
                    MessageBox.Show(
                        BuildErrorMessage(result.Message),
                        "升级失败", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 6. 读取新版本
                var newVersion = await _updateService.GetInstalledVersionAsync() ?? "未知";
                InstalledVersion = newVersion;
                StatusText = "升级完成";
                StatusColor = "#4CAF50";
                AppendOutput($"升级完成: {oldVersion} → {newVersion}");

                // 7. 询问是否重启 DSH
                var restart = MessageBox.Show(
                    $"dsh-web-ui 升级完成（{oldVersion} → {newVersion}）。\n\n是否立即重启 DSH？",
                    "升级完成", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (restart == MessageBoxResult.Yes)
                {
                    AppendOutput("正在重启 DSH...");
                    await _dshService.StartServiceAsync();
                    AppendOutput("DSH 已启动");
                }
                else
                {
                    AppendOutput("DSH 未重启，升级将在下次启动后生效");
                }
            }
            finally
            {
                _upgradeCts?.Dispose();
                _upgradeCts = null;
                IsUpgrading = false;
                IsBusy = false;
            }
        }

        /// <summary>
        /// 取消升级（终止 pnpm 进程树）
        /// </summary>
        public void CancelUpgrade()
        {
            _upgradeCts?.Cancel();
            AppendOutput("正在取消升级...");
        }

        private void Close()
        {
            Window?.Close();
        }

        #endregion

        #region 私有方法

        private void AppendOutput(string line)
        {
            _dispatcher.Invoke(() =>
            {
                Output.Add(line);
                while (Output.Count > MaxOutputLines)
                {
                    Output.RemoveAt(0);
                }
            });
            _logService.Info(SecurityService.SanitizeLogMessage(line), "web-ui升级");
        }

        /// <summary>
        /// 根据错误输出构建用户提示（对照升级指南错误表）
        /// </summary>
        private static string BuildErrorMessage(string message)
        {
            if (message.Contains("EPERM", StringComparison.OrdinalIgnoreCase))
            {
                return "文件被锁定：DSH 可能未完全关闭。\n请停止 DSH 后重试。\n\n详细信息：\n" + message;
            }

            if (message.Contains("Timeout", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("ETIMEDOUT", StringComparison.OrdinalIgnoreCase))
            {
                return "下载超时：网络较慢或镜像源不可达。\n可检查网络，或在配置中更换镜像源。\n\n详细信息：\n" + message;
            }

            if (message.Contains("error (23)", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("ECONNRESET", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("ECONNREFUSED", StringComparison.OrdinalIgnoreCase))
            {
                return "网络连接问题：请检查网络后重试。\n\n详细信息：\n" + message;
            }

            return "升级失败，请查看下方输出框获取详细信息。\n\n" + message;
        }

        /// <summary>
        /// 版本比较（解析失败时降级为字符串比较）
        /// </summary>
        private static int CompareVersions(string a, string b)
        {
            try
            {
                return new Version(a).CompareTo(new Version(b));
            }
            catch
            {
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            }
        }

        #endregion
    }
}
