using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using dsh_deploy.Models;
using Timer = System.Threading.Timer;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 崩溃恢复服务
    /// </summary>
    public class CrashRecoveryService : IDisposable
    {
        private readonly LogService _logService;
        private readonly DshService _dshService;
        private readonly Dispatcher _dispatcher;
        private CrashRecoveryConfig _config;

        private int _retryCount;
        private DateTime _lastSuccessTime;
        private Timer? _resetTimer;
        private CancellationTokenSource? _cts;
        private bool _isRecovering;
        private bool _disposed;

        public CrashRecoveryService(LogService logService, DshService dshService, Dispatcher dispatcher)
        {
            _logService = logService;
            _dshService = dshService;
            _dispatcher = dispatcher;
            _config = new CrashRecoveryConfig();
            _retryCount = 0;
            _lastSuccessTime = DateTime.Now;

            // 监听服务状态变化
            _dshService.StatusChanged += OnStatusChanged;
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(CrashRecoveryConfig config)
        {
            _config = config;
            _logService.Info($"崩溃恢复配置已更新：策略={config.Strategy}, 最大重试={config.MaxRetries}");
        }

        /// <summary>
        /// 服务状态变化事件处理
        /// </summary>
        private void OnStatusChanged(object? sender, ServiceStatus status)
        {
            if (!_config.Enabled) return;

            // 使用Task.Run避免async void
            _ = Task.Run(async () =>
            {
                try
                {
                    if (status.State == ServiceState.Running)
                    {
                        // 服务正常运行，记录成功时间
                        _lastSuccessTime = DateTime.Now;
                        
                        // 启动重置定时器
                        StartResetTimer();
                    }
                    else if (status.State == ServiceState.Error || status.State == ServiceState.Stopped)
                    {
                        // 检测到服务异常，触发恢复
                        await HandleServiceFailureAsync(status);
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error($"崩溃恢复事件处理失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 处理服务失败
        /// </summary>
        private async Task HandleServiceFailureAsync(ServiceStatus status)
        {
            if (_isRecovering) return;
            if (_retryCount >= _config.MaxRetries)
            {
                _logService.Warn($"崩溃恢复：已达到最大重试次数({_config.MaxRetries})，停止恢复");
                return;
            }

            _isRecovering = true;
            _cts = new CancellationTokenSource();

            try
            {
                _logService.Warn($"崩溃恢复：检测到服务异常（{status.State}），准备恢复...");

                // 计算延迟
                var delay = _config.CalculateDelay(_retryCount);
                
                if (delay > 0)
                {
                    _logService.Info($"崩溃恢复：等待 {delay}ms 后重试（第 {_retryCount + 1} 次）");
                    await Task.Delay(delay, _cts.Token);
                }

                // 执行重启
                _logService.Info($"崩溃恢复：正在重启服务（第 {_retryCount + 1} 次）...");
                var success = await _dshService.RestartServiceAsync();

                if (success)
                {
                    _retryCount++;
                    _logService.Info($"崩溃恢复：服务重启成功（第 {_retryCount} 次）");
                }
                else
                {
                    _retryCount++;
                    _logService.Error($"崩溃恢复：服务重启失败（第 {_retryCount} 次）");
                }
            }
            catch (OperationCanceledException)
            {
                _logService.Info("崩溃恢复：恢复操作已取消");
            }
            catch (Exception ex)
            {
                _logService.Error($"崩溃恢复：恢复过程中发生错误: {ex.Message}");
            }
            finally
            {
                _isRecovering = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 启动重置定时器
        /// </summary>
        private void StartResetTimer()
        {
            _resetTimer?.Dispose();
            _resetTimer = new Timer(ResetRetryCount, null, _config.ResetAfter, Timeout.Infinite);
        }

        /// <summary>
        /// 重置重试计数
        /// </summary>
        private void ResetRetryCount(object? state)
        {
            if (_retryCount > 0)
            {
                _logService.Info($"崩溃恢复：服务已稳定运行 {_config.ResetAfter / 1000} 秒，重置重试计数");
                _retryCount = 0;
            }
        }

        /// <summary>
        /// 取消当前恢复操作
        /// </summary>
        public void CancelRecovery()
        {
            _cts?.Cancel();
            _logService.Info("崩溃恢复：已取消当前恢复操作");
        }

        /// <summary>
        /// 重置恢复状态
        /// </summary>
        public void Reset()
        {
            _retryCount = 0;
            _isRecovering = false;
            _cts?.Cancel();
            _logService.Info("崩溃恢复：已重置恢复状态");
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
                    _resetTimer?.Dispose();
                    _cts?.Cancel();
                    _cts?.Dispose();
                    _dshService.StatusChanged -= OnStatusChanged;
                }
                _disposed = true;
            }
        }
    }
}
