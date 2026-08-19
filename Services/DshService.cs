using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// DSH服务 - 核心服务，整合端口、进程、配置管理
    /// </summary>
    public class DshService
    {
        private readonly LogService _logService;
        private readonly PortService _portService;
        private readonly ProcessService _processService;
        private readonly ConfigService _configService;
        private readonly Dispatcher _dispatcher;
        private CrashRecoveryService? _crashRecoveryService;
        private UpdateService? _updateService;
        private HealthCheckService? _healthCheckService;

        private ServiceStatus _currentStatus;
        private DispatcherTimer? _statusTimer;
        private DateTime _lastStatusCheck = DateTime.MinValue;
        private const int StatusCacheSeconds = 5;

        public DshService(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _logService = new LogService(dispatcher);
            _portService = new PortService(_logService);
            _processService = new ProcessService(_logService);
            _configService = new ConfigService(_logService);
            _currentStatus = new ServiceStatus();

            InitializeStatusTimer();
        }

        /// <summary>
        /// 日志服务
        /// </summary>
        public LogService LogService => _logService;

        /// <summary>
        /// 配置服务
        /// </summary>
        public ConfigService ConfigService => _configService;

        /// <summary>
        /// 端口服务
        /// </summary>
        public PortService PortService => _portService;

        /// <summary>
        /// 崩溃恢复服务
        /// </summary>
        public CrashRecoveryService? CrashRecoveryService => _crashRecoveryService;

        /// <summary>
        /// 更新服务
        /// </summary>
        public UpdateService? UpdateService => _updateService;

        /// <summary>
        /// 健康检查服务
        /// </summary>
        public HealthCheckService? HealthCheckService => _healthCheckService;

        /// <summary>
        /// 当前状态
        /// </summary>
        public ServiceStatus CurrentStatus => _currentStatus;

        /// <summary>
        /// 状态变化事件
        /// </summary>
        public event EventHandler<ServiceStatus>? StatusChanged;

        /// <summary>
        /// 初始化服务
        /// </summary>
        public async Task InitializeAsync()
        {
            _logService.Info("正在初始化DSH服务...");
            
            await _configService.LoadAsync();
            
            // 初始化崩溃恢复服务
            _crashRecoveryService = new CrashRecoveryService(_logService, this, _dispatcher);
            _crashRecoveryService.UpdateConfig(_configService.Current.CrashRecovery);
            
            // 初始化更新服务
            _updateService = new UpdateService(_logService, _configService);
            _updateService.UpdateConfig(_configService.Current.AutoUpdate);
            
            // 初始化健康检查服务
            _healthCheckService = new HealthCheckService(_logService, this);
            _healthCheckService.UpdateConfig(_configService.Current.HealthCheck);
            
            await RefreshStatusAsync();
            
            _logService.Info("DSH服务初始化完成");
        }

        /// <summary>
        /// 刷新状态（使用缓存）
        /// </summary>
        public async Task<ServiceStatus> RefreshStatusAsync(bool forceRefresh = false)
        {
            // 检查缓存
            if (!forceRefresh && 
                _lastStatusCheck != DateTime.MinValue &&
                (DateTime.Now - _lastStatusCheck).TotalSeconds < StatusCacheSeconds)
            {
                return _currentStatus;
            }

            var status = await GetStatusInternalAsync();
            
            // 更新缓存
            _currentStatus = status;
            _lastStatusCheck = DateTime.Now;

            // 触发状态变化事件
            StatusChanged?.Invoke(this, status);

            return status;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public async Task<bool> StartServiceAsync()
        {
            _logService.Info("正在启动DSH服务...");
            
            // 检查端口
            var port = _configService.Current.Port;
            var portInfo = await _portService.CheckPortAsync(port);

            if (portInfo.IsInUse)
            {
                // 检查是否是DSH进程
                var dshProcesses = await _processService.GetDshProcessesAsync();
                var isDshProcess = dshProcesses.Any(p => p.ProcessId == portInfo.ProcessId);

                if (!isDshProcess)
                {
                    _logService.Warn($"端口 {port} 被其他进程占用");
                    _currentStatus = new ServiceStatus
                    {
                        State = ServiceState.PortConflict,
                        Port = port,
                        ProcessId = portInfo.ProcessId,
                        ProcessName = portInfo.ProcessName,
                        Message = $"端口被 {portInfo.ProcessName} 占用"
                    };
                    StatusChanged?.Invoke(this, _currentStatus);
                    return false;
                }
            }

            // 启动服务
            var success = await _processService.StartDshServiceAsync(
                _configService.Current.DshCommand,
                _configService.Current.DshArgs);

            if (success)
            {
                // 等待服务启动
                await Task.Delay(2000);
                await RefreshStatusAsync(forceRefresh: true);
            }

            return success;
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public async Task<bool> StopServiceAsync()
        {
            _logService.Info("正在停止DSH服务...");
            
            var stoppedCount = await _processService.StopAllDshProcessesAsync();
            _logService.Info($"已停止 {stoppedCount} 个进程");

            await RefreshStatusAsync(forceRefresh: true);
            return stoppedCount > 0;
        }

        /// <summary>
        /// 重启服务
        /// </summary>
        public async Task<bool> RestartServiceAsync()
        {
            _logService.Info("正在重启DSH服务...");
            
            await StopServiceAsync();
            await Task.Delay(1000);
            return await StartServiceAsync();
        }

        /// <summary>
        /// 清理端口冲突
        /// </summary>
        public async Task<bool> ClearPortConflictAsync()
        {
            var port = _configService.Current.Port;
            var portInfo = await _portService.CheckPortAsync(port);

            if (!portInfo.IsInUse || !portInfo.ProcessId.HasValue)
            {
                return true;
            }

            _logService.Info($"正在清理端口 {port} 冲突 (PID: {portInfo.ProcessId})...");
            
            var success = await _processService.StopProcessAsync(portInfo.ProcessId.Value);
            
            if (success)
            {
                await RefreshStatusAsync(forceRefresh: true);
            }

            return success;
        }

        /// <summary>
        /// 打开Web界面
        /// </summary>
        public void OpenWebInterface()
        {
            try
            {
                var url = _configService.Current.WebUrl;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                _logService.Info($"已打开Web界面: {url}");
            }
            catch (Exception ex)
            {
                _logService.Error($"打开Web界面失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取进程列表
        /// </summary>
        public async Task<List<ProcessInfo>> GetProcessesAsync()
        {
            return await _processService.GetDshProcessesAsync();
        }

        /// <summary>
        /// 启动状态定时器
        /// </summary>
        public void StartStatusTimer()
        {
            _statusTimer?.Start();
        }

        /// <summary>
        /// 停止状态定时器
        /// </summary>
        public void StopStatusTimer()
        {
            _statusTimer?.Stop();
        }

        /// <summary>
        /// 内部状态获取
        /// </summary>
        private async Task<ServiceStatus> GetStatusInternalAsync()
        {
            var port = _configService.Current.Port;
            var status = new ServiceStatus { Port = port };

            try
            {
                // 快速检查端口
                var portInfo = await _portService.CheckPortAsync(port, quickCheck: true);

                if (!portInfo.IsInUse)
                {
                    // 端口未占用，检查是否有DSH进程在运行（可能正在启动或使用其他端口）
                    var dshProcesses = await _processService.GetDshProcessesAsync();
                    status.State = dshProcesses.Count > 0 ? ServiceState.Starting : ServiceState.Stopped;
                    status.Message = dshProcesses.Count > 0 ? "服务正在启动..." : "服务已停止";
                    
                    if (dshProcesses.Count > 0)
                    {
                        status.ProcessId = dshProcesses[0].ProcessId;
                        status.ProcessName = dshProcesses[0].ProcessName;
                    }
                }
                else
                {
                    // 端口被占用，获取详细信息
                    var portDetails = await _portService.CheckPortAsync(port);
                    
                    // 获取DSH进程列表
                    var dshProcesses = await _processService.GetDshProcessesAsync();
                    
                    // 检查占用端口的进程是否是DSH进程
                    var isDshProcess = dshProcesses.Any(p => p.ProcessId == portDetails.ProcessId);
                    
                    if (isDshProcess)
                    {
                        // 端口被DSH进程占用，服务正常运行
                        status.State = ServiceState.Running;
                        status.ProcessId = portDetails.ProcessId;
                        status.ProcessName = portDetails.ProcessName;
                        status.Message = "服务运行中";
                    }
                    else if (dshProcesses.Count > 0)
                    {
                        // 有DSH进程但端口被其他进程占用
                        status.State = ServiceState.PortConflict;
                        status.ProcessId = portDetails.ProcessId;
                        status.ProcessName = portDetails.ProcessName;
                        status.Message = $"端口被 {portDetails.ProcessName} 占用，但检测到DSH进程";
                    }
                    else
                    {
                        // 端口被其他进程占用，没有DSH进程
                        status.State = ServiceState.PortConflict;
                        status.ProcessId = portDetails.ProcessId;
                        status.ProcessName = portDetails.ProcessName;
                        status.Message = $"端口被 {portDetails.ProcessName} 占用";
                    }
                }
            }
            catch (Exception ex)
            {
                status.State = ServiceState.Error;
                status.Message = $"状态检查失败: {ex.Message}";
                _logService.Error($"状态检查失败: {ex.Message}");
            }

            return status;
        }

        /// <summary>
        /// 初始化状态定时器
        /// </summary>
        private void InitializeStatusTimer()
        {
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_configService.Current.StatusCheckInterval)
            };
            _statusTimer.Tick += async (s, e) =>
            {
                await RefreshStatusAsync(forceRefresh: true);
            };
        }
    }
}
