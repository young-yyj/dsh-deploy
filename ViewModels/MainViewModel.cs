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
        private readonly DshService _dshService;
        private readonly Dispatcher _dispatcher;

        private ServiceStatus _serviceStatus;
        private string _statusText = "正在初始化...";
        private string _statusColor = "#9E9E9E";
        private bool _isServiceRunning;
        private bool _isStarting;
        private bool _isStopping;
        private string _webUrl = "http://127.0.0.1:3080";
        private int _port = 3080;
        private string _processInfo = string.Empty;
        private DateTime _lastUpdateTime;

        public MainViewModel(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _dshService = new DshService(dispatcher);
            _serviceStatus = new ServiceStatus();

            // 初始化命令
            StartCommand = new AsyncRelayCommand(StartServiceAsync, CanStartService);
            StopCommand = new AsyncRelayCommand(StopServiceAsync, CanStopService);
            RestartCommand = new AsyncRelayCommand(RestartServiceAsync, CanRestartService);
            OpenWebCommand = new RelayCommand(OpenWebInterface);
            RefreshCommand = new AsyncRelayCommand(RefreshStatusAsync);
            ClearPortCommand = new AsyncRelayCommand(ClearPortConflictAsync);
            ClearLogCommand = new RelayCommand(ClearLogs);
            ExitCommand = new RelayCommand(ExitApplication);

            // 订阅状态变化
            _dshService.StatusChanged += OnStatusChanged;

            // 初始化
            InitializeAsync();
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
        /// 是否正在启动
        /// </summary>
        public bool IsStarting
        {
            get => _isStarting;
            set => SetProperty(ref _isStarting, value);
        }

        /// <summary>
        /// 是否正在停止
        /// </summary>
        public bool IsStopping
        {
            get => _isStopping;
            set => SetProperty(ref _isStopping, value);
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
        /// 日志集合
        /// </summary>
        public ObservableCollection<LogEntry> Logs => _dshService.LogService.Logs;

        /// <summary>
        /// DSH服务
        /// </summary>
        public DshService DshService => _dshService;

        #endregion

        #region 命令

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand OpenWebCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ClearPortCommand { get; }
        public ICommand ClearLogCommand { get; }
        public ICommand ExitCommand { get; }

        #endregion

        #region 命令实现

        private async Task StartServiceAsync()
        {
            IsStarting = true;
            try
            {
                await _dshService.StartServiceAsync();
            }
            finally
            {
                IsStarting = false;
            }
        }

        private bool CanStartService()
        {
            return !IsStarting && !IsStopping && !IsServiceRunning;
        }

        private async Task StopServiceAsync()
        {
            IsStopping = true;
            try
            {
                await _dshService.StopServiceAsync();
            }
            finally
            {
                IsStopping = false;
            }
        }

        private bool CanStopService()
        {
            return !IsStarting && !IsStopping && IsServiceRunning;
        }

        private async Task RestartServiceAsync()
        {
            IsStarting = true;
            IsStopping = true;
            try
            {
                await _dshService.RestartServiceAsync();
            }
            finally
            {
                IsStarting = false;
                IsStopping = false;
            }
        }

        private bool CanRestartService()
        {
            return !IsStarting && !IsStopping;
        }

        private void OpenWebInterface()
        {
            _dshService.OpenWebInterface();
        }

        private async Task RefreshStatusAsync()
        {
            await _dshService.RefreshStatusAsync(forceRefresh: true);
        }

        private async Task ClearPortConflictAsync()
        {
            await _dshService.ClearPortConflictAsync();
        }

        private void ClearLogs()
        {
            _dshService.LogService.Clear();
        }

        private void ExitApplication()
        {
            Application.Current.Shutdown();
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
            }
            catch (Exception ex)
            {
                _dshService.LogService.Error($"初始化失败: {ex.Message}");
            }
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
