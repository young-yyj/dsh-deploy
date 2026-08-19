using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Threading;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 仪表盘服务
    /// </summary>
    public class DashboardService : IDisposable
    {
        private readonly LogService _logService;
        private readonly DshService _dshService;
        private readonly Dispatcher _dispatcher;
        private DashboardData _dashboardData;
        private DispatcherTimer? _refreshTimer;
        private bool _disposed;

        public event EventHandler<DashboardData>? DataUpdated;

        public DashboardService(LogService logService, DshService dshService, Dispatcher dispatcher)
        {
            _logService = logService;
            _dshService = dshService;
            _dispatcher = dispatcher;
            _dashboardData = new DashboardData();

            // 监听服务状态变化
            _dshService.StatusChanged += OnStatusChanged;
        }

        /// <summary>
        /// 仪表盘数据
        /// </summary>
        public DashboardData DashboardData => _dashboardData;

        /// <summary>
        /// 启动自动刷新
        /// </summary>
        public void StartAutoRefresh(int intervalSeconds = 5)
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
            }
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(intervalSeconds)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshAsync();
            _refreshTimer.Start();
            _logService.Info($"仪表盘自动刷新已启动，间隔：{intervalSeconds}秒");
        }

        /// <summary>
        /// 停止自动刷新
        /// </summary>
        public void StopAutoRefresh()
        {
            if (_refreshTimer != null)
            {
                _refreshTimer.Stop();
                _refreshTimer = null;
            }
            _logService.Info("仪表盘自动刷新已停止");
        }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public async Task<DashboardData> RefreshAsync()
        {
            try
            {
                // 更新服务状态
                _dashboardData.ServiceStatus = _dshService.CurrentStatus;

                // 更新健康状态
                if (_dshService.HealthCheckService != null)
                {
                    _dashboardData.HealthStatus = _dshService.HealthCheckService.HealthStatus;
                }

                // 更新进程信息
                var processes = await _dshService.GetProcessesAsync();
                if (processes.Count > 0)
                {
                    _dashboardData.ProcessInfo = processes[0];
                    _dashboardData.Uptime = processes[0].RunningTime;
                    _dashboardData.MemoryUsage = processes[0].MemoryUsage / 1024.0 / 1024.0;
                }
                else
                {
                    _dashboardData.ProcessInfo = null;
                    _dashboardData.Uptime = "未运行";
                    _dashboardData.MemoryUsage = 0;
                }

                // 更新端口状态
                _dashboardData.PortStatus = _dashboardData.ServiceStatus.State switch
                {
                    ServiceState.Running => "正常监听",
                    ServiceState.PortConflict => "冲突",
                    _ => "未监听"
                };

                // 更新日志统计
                UpdateLogStatistics();

                // 更新版本信息
                if (_dshService.UpdateService != null)
                {
                    _dashboardData.UpdateStatistics = new UpdateStatistics
                    {
                        CurrentVersion = _dshService.UpdateService.VersionInfo.CurrentVersion,
                        LatestVersion = _dshService.UpdateService.VersionInfo.LatestVersion,
                        HasUpdate = _dshService.UpdateService.VersionInfo.HasUpdate,
                        LastCheckTime = _dshService.UpdateService.VersionInfo.LastCheckTime
                    };
                }

                // 更新CPU使用率
                UpdateCpuUsage();

                _dashboardData.LastUpdated = DateTime.Now;

                // 触发数据更新事件
                DataUpdated?.Invoke(this, _dashboardData);

                return _dashboardData;
            }
            catch (Exception ex)
            {
                _logService.Error($"刷新仪表盘数据失败: {ex.Message}");
                return _dashboardData;
            }
        }

        /// <summary>
        /// 更新日志统计
        /// </summary>
        private void UpdateLogStatistics()
        {
            var logs = _logService.Logs;
            _dashboardData.LogStatistics = new LogStatistics
            {
                TotalCount = logs.Count,
                ErrorCount = logs.Count(l => l.Level == LogLevel.ERROR),
                WarningCount = logs.Count(l => l.Level == LogLevel.WARN),
                InfoCount = logs.Count(l => l.Level == LogLevel.INFO),
                DebugCount = logs.Count(l => l.Level == LogLevel.DEBUG)
            };
        }

        /// <summary>
        /// 更新CPU使用率
        /// </summary>
        private void UpdateCpuUsage()
        {
            try
            {
                if (_dashboardData.ProcessInfo != null)
                {
                    var process = Process.GetProcessById(_dashboardData.ProcessInfo.ProcessId);
                    _dashboardData.CpuUsage = process.TotalProcessorTime.TotalMilliseconds / 
                        Environment.ProcessorCount / 
                        process.StartTime.Subtract(DateTime.Now).TotalMilliseconds * 100;
                }
                else
                {
                    _dashboardData.CpuUsage = 0;
                }
            }
            catch
            {
                _dashboardData.CpuUsage = 0;
            }
        }

        /// <summary>
        /// 服务状态变化事件处理
        /// </summary>
        private async void OnStatusChanged(object? sender, ServiceStatus status)
        {
            await RefreshAsync();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopAutoRefresh();
                    _dshService.StatusChanged -= OnStatusChanged;
                }
                _disposed = true;
            }
        }
    }
}
