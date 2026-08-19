using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using dsh_deploy.Models;
using Timer = System.Threading.Timer;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 健康检查服务
    /// </summary>
    public class HealthCheckService : IDisposable
    {
        private readonly LogService _logService;
        private readonly DshService _dshService;
        private HealthCheckConfig _config;
        private HealthStatus _healthStatus;
        private Timer? _checkTimer;
        private HttpClient? _httpClient;
        private bool _disposed;

        public event EventHandler<HealthStatus>? HealthStatusChanged;
        public event EventHandler? ServiceUnhealthy;

        public HealthCheckService(LogService logService, DshService dshService)
        {
            _logService = logService;
            _dshService = dshService;
            _config = new HealthCheckConfig();
            _healthStatus = new HealthStatus();
        }

        /// <summary>
        /// 健康状态
        /// </summary>
        public HealthStatus HealthStatus => _healthStatus;

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(HealthCheckConfig config)
        {
            _config = config;
            
            if (_config.Enabled)
            {
                StartChecking();
            }
            else
            {
                StopChecking();
            }
        }

        /// <summary>
        /// 开始检查
        /// </summary>
        public void StartChecking()
        {
            _checkTimer?.Dispose();
            _checkTimer = new Timer(async _ => await CheckHealthAsync(), null, 0, _config.Interval);
            _logService.Info($"健康检查已启动，间隔：{_config.Interval / 1000}秒");
        }

        /// <summary>
        /// 停止检查
        /// </summary>
        public void StopChecking()
        {
            _checkTimer?.Dispose();
            _checkTimer = null;
            _logService.Info("健康检查已停止");
        }

        /// <summary>
        /// 执行健康检查
        /// </summary>
        public async Task<HealthStatus> CheckHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _healthStatus.State = HealthState.Checking;
                _healthStatus.LastCheckTime = DateTime.Now;

                _httpClient ??= new HttpClient();
                _httpClient.Timeout = TimeSpan.FromMilliseconds(_config.Timeout);

                var response = await _httpClient.GetAsync(_config.HealthUrl);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    // 健康
                    _healthStatus.State = HealthState.Healthy;
                    _healthStatus.ConsecutiveFailures = 0;
                    _healthStatus.LastResponseTime = stopwatch.ElapsedMilliseconds;
                    _healthStatus.LastError = string.Empty;
                    
                    _logService.Debug($"健康检查成功，响应时间：{stopwatch.ElapsedMilliseconds}ms");
                }
                else
                {
                    // 不健康
                    HandleFailure($"HTTP {response.StatusCode}");
                }
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                HandleFailure("请求超时");
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                HandleFailure($"连接失败: {ex.Message}");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                HandleFailure($"未知错误: {ex.Message}");
            }

            HealthStatusChanged?.Invoke(this, _healthStatus);
            return _healthStatus;
        }

        /// <summary>
        /// 处理失败
        /// </summary>
        private async void HandleFailure(string error)
        {
            _healthStatus.State = HealthState.Unhealthy;
            _healthStatus.ConsecutiveFailures++;
            _healthStatus.LastError = error;

            _logService.Warn($"健康检查失败（第{_healthStatus.ConsecutiveFailures}次）：{error}");

            // 检查是否达到不健康阈值
            if (_healthStatus.ConsecutiveFailures >= _config.UnhealthyThreshold)
            {
                _logService.Error($"服务不健康，连续失败{_healthStatus.ConsecutiveFailures}次");
                
                ServiceUnhealthy?.Invoke(this, EventArgs.Empty);

                // 自动重启
                if (_config.AutoRestart)
                {
                    _logService.Info("正在自动重启服务...");
                    await _dshService.RestartServiceAsync();
                }
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void Reset()
        {
            _healthStatus = new HealthStatus();
            _logService.Info("健康检查状态已重置");
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
                    _checkTimer?.Dispose();
                    _httpClient?.Dispose();
                }
                _disposed = true;
            }
        }
    }
}
