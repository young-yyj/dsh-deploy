using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using dsh_deploy.Models;
using dsh_deploy.Services;

namespace dsh_deploy.ViewModels
{
    /// <summary>
    /// 主窗口ViewModel
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDshService _dshService;
        private readonly Dispatcher _dispatcher;
        private TrayService? _trayService;

        private ServiceStatus _serviceStatus;
        private DashboardData _dashboardData;
        private string _statusText = "正在初始化...";
        private string _statusColor = "#9E9E9E";
        private bool _isServiceRunning;
        private string _webUrl = "http://127.0.0.1:3080";
        private int _port = 3080;
        private string _processInfo = string.Empty;
        private DateTime _lastUpdateTime;

        // 拆分的ViewModel
        private ServiceControlViewModel? _serviceControlViewModel;
        private LogViewModel? _logViewModel;
        private DashboardViewModel? _dashboardViewModel;

        public MainViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _dshService = ServiceLocator.Instance.Get<IDshService>();
            _serviceStatus = new ServiceStatus();
            _dashboardData = new DashboardData();

            // 初始化拆分的ViewModel
            _serviceControlViewModel = new ServiceControlViewModel(_dshService);
            _logViewModel = new LogViewModel(_dshService.LogService);
            _dashboardViewModel = new DashboardViewModel(ServiceLocator.Instance.Get<DashboardService>());

            // 初始化命令
            OpenWebCommand = new RelayCommand(OpenWebInterface);
            RefreshCommand = new AsyncRelayCommand(RefreshStatusAsync);
            ExitCommand = new RelayCommand(ExitApplication);
            MinimizeToTrayCommand = new RelayCommand(MinimizeToTray);
            RunDiagnosticsCommand = new AsyncRelayCommand(RunDiagnosticsAsync);

            // 订阅状态变化
            _dshService.StatusChanged += OnStatusChanged;

            // 初始化
            InitializeAsync();
        }

        /// <summary>
        /// 初始化托盘服务
        /// </summary>
        /// <param name="mainWindow">主窗口</param>
        public void InitializeTray(Window mainWindow)
        {
            _trayService = new TrayService(_dshService.LogService);
            _trayService.Initialize(mainWindow);
        }

        /// <summary>
        /// 初始化仪表盘服务
        /// </summary>
        public void InitializeDashboard()
        {
            _dashboardViewModel?.StartAutoRefresh(5);
        }

        #region 属性

        /// <summary>
        /// 服务状态
        /// </summary>
        public ServiceStatus ServiceStatus
        {
            get => _serviceStatus;
            set => SetProperty(ref _serviceStatus, value);
        }

        /// <summary>
        /// 仪表盘数据
        /// </summary>
        public DashboardData DashboardData
        {
            get => _dashboardData;
            set => SetProperty(ref _dashboardData, value);
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
        /// 服务是否运行中
        /// </summary>
        public bool IsServiceRunning
        {
            get => _isServiceRunning;
            set => SetProperty(ref _isServiceRunning, value);
        }

        /// <summary>
        /// Web URL
        /// </summary>
        public string WebUrl
        {
            get => _webUrl;
            set => SetProperty(ref _webUrl, value);
        }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        /// <summary>
        /// 进程信息
        /// </summary>
        public string ProcessInfo
        {
            get => _processInfo;
            set => SetProperty(ref _processInfo, value);
        }

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdateTime
        {
            get => _lastUpdateTime;
            set => SetProperty(ref _lastUpdateTime, value);
        }

        /// <summary>
        /// DSH服务
        /// </summary>
        public IDshService DshService => _dshService;

        /// <summary>
        /// 服务控制ViewModel
        /// </summary>
        public ServiceControlViewModel ServiceControl => _serviceControlViewModel!;

        /// <summary>
        /// 日志ViewModel
        /// </summary>
        public LogViewModel Log => _logViewModel!;

        /// <summary>
        /// 仪表盘ViewModel
        /// </summary>
        public DashboardViewModel Dashboard => _dashboardViewModel!;

        #endregion

        #region 命令

        public ICommand OpenWebCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand MinimizeToTrayCommand { get; }
        public ICommand RunDiagnosticsCommand { get; }

        #endregion

        #region 命令实现

        private void OpenWebInterface()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _webUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _dshService.LogService.Error($"打开Web界面失败: {ex.Message}");
            }
        }

        private async Task RefreshStatusAsync()
        {
            await _dshService.RefreshStatusAsync(forceRefresh: true);
        }

        private void ExitApplication()
        {
            _trayService?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }

        private void MinimizeToTray()
        {
            _trayService?.HideMainWindow();
        }

        #endregion

        #region 私有方法

        private async void InitializeAsync()
        {
            try
            {
                await _dshService.InitializeAsync();
                
                // 更新配置
                WebUrl = _dshService.ConfigService.Current.WebUrl;
                Port = _dshService.ConfigService.Current.Port;

                // 启动状态定时器
                _dshService.StartStatusTimer();

                // 初始化仪表盘
                InitializeDashboard();
            }
            catch (Exception ex)
            {
                _dshService.LogService.Error($"初始化失败: {ex.Message}");
            }
        }

        private async Task RunDiagnosticsAsync()
        {
            _dshService.LogService.Info("开始系统诊断...");
            
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("=== DSH Deploy Manager 系统诊断报告 ===");
            diagnostics.AppendLine($"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            diagnostics.AppendLine();

            // 检查Node.js
            diagnostics.AppendLine("【Node.js 环境】");
            try
            {
                var nodeVersion = await Task.Run(() =>
                {
                    var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "node",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    });
                    process?.WaitForExit(5000);
                    return process?.StandardOutput.ReadToEnd()?.Trim() ?? "未安装";
                });
                diagnostics.AppendLine($"  版本：{nodeVersion}");
            }
            catch
            {
                diagnostics.AppendLine("  状态：未安装或无法访问");
            }
            diagnostics.AppendLine();

            // 检查DSH
            diagnostics.AppendLine("【DSH 服务】");
            diagnostics.AppendLine($"  状态：{ServiceStatus.StateText}");
            diagnostics.AppendLine($"  端口：{Port}");
            diagnostics.AppendLine($"  进程：{ProcessInfo ?? "无"}");
            diagnostics.AppendLine();

            // 检查端口
            diagnostics.AppendLine("【端口状态】");
            var portStatus = await _dshService.PortService.CheckPortAsync(Port);
            diagnostics.AppendLine($"  端口 {Port}：{(portStatus.IsInUse ? "被占用" : "可用")}");
            if (portStatus.IsInUse)
            {
                diagnostics.AppendLine($"  占用进程：{portStatus.ProcessName} (PID: {portStatus.ProcessId})");
            }
            diagnostics.AppendLine();

            // 检查健康状态
            if (_dshService.HealthCheckService != null)
            {
                diagnostics.AppendLine("【健康检查】");
                diagnostics.AppendLine($"  状态：{_dshService.HealthCheckService.HealthStatus.StateText}");
                diagnostics.AppendLine($"  连续失败：{_dshService.HealthCheckService.HealthStatus.ConsecutiveFailures}次");
                diagnostics.AppendLine();
            }

            // 检查更新
            if (_dshService.UpdateService != null)
            {
                diagnostics.AppendLine("【版本信息】");
                diagnostics.AppendLine($"  当前版本：{_dshService.UpdateService.VersionInfo.CurrentVersion}");
                diagnostics.AppendLine($"  最新版本：{_dshService.UpdateService.VersionInfo.LatestVersion}");
                diagnostics.AppendLine($"  有更新：{(_dshService.UpdateService.VersionInfo.HasUpdate ? "是" : "否")}");
                diagnostics.AppendLine();
            }

            diagnostics.AppendLine("=== 诊断完成 ===");

            _dshService.LogService.Info(diagnostics.ToString());
            _dshService.LogService.Info("系统诊断完成，详细报告已输出到日志");
        }

        private void OnStatusChanged(object? sender, ServiceStatus status)
        {
            _dispatcher.Invoke(() =>
            {
                ServiceStatus = status;
                StatusText = status.StateText;
                StatusColor = status.StateColor;
                IsServiceRunning = status.IsOnline;
                LastUpdateTime = status.LastUpdated;

                // 更新进程信息
                if (status.ProcessId.HasValue)
                {
                    ProcessInfo = $"PID: {status.ProcessId} | {status.ProcessName}";
                }
                else
                {
                    ProcessInfo = string.Empty;
                }
            });
        }

        #endregion
    }
}
