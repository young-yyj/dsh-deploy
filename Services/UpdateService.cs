using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using dsh_deploy.Models;
using Timer = System.Threading.Timer;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 更新服务
    /// </summary>
    public class UpdateService : IDisposable
    {
        private readonly LogService _logService;
        private readonly ConfigService _configService;
        private UpdateConfig _config;
        private VersionInfo _versionInfo;
        private Timer? _checkTimer;
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        private bool _disposed;

        public event EventHandler<VersionInfo>? UpdateAvailable;

        public UpdateService(LogService logService, ConfigService configService)
        {
            _logService = logService;
            _configService = configService;
            _config = new UpdateConfig();
            _versionInfo = new VersionInfo();
        }

        /// <summary>
        /// 版本信息
        /// </summary>
        public VersionInfo VersionInfo => _versionInfo;

        /// <summary>
        /// 更新配置
        /// </summary>
        public void UpdateConfig(UpdateConfig config)
        {
            _config = config;
            
            if (_config.Enabled)
            {
                StartAutoCheck();
            }
            else
            {
                StopAutoCheck();
            }
        }

        /// <summary>
        /// 开始自动检查
        /// </summary>
        public void StartAutoCheck()
        {
            _checkTimer?.Dispose();
            _checkTimer = new Timer(async _ => await CheckForUpdatesAsync(), null, 0, _config.CheckInterval);
            _logService.Info($"自动更新检查已启动，间隔：{_config.CheckInterval / 1000}秒");
        }

        /// <summary>
        /// 停止自动检查
        /// </summary>
        public void StopAutoCheck()
        {
            _checkTimer?.Dispose();
            _checkTimer = null;
            _logService.Info("自动更新检查已停止");
        }

        /// <summary>
        /// 检查更新
        /// </summary>
        public async Task<VersionInfo> CheckForUpdatesAsync()
        {
            try
            {
                _logService.Info("正在检查更新...");

                // 获取当前版本
                var currentVersion = await GetCurrentVersionAsync();
                _versionInfo.CurrentVersion = currentVersion;

                // 获取最新版本
                var latestVersion = await GetLatestVersionAsync();
                _versionInfo.LatestVersion = latestVersion;
                _versionInfo.LastCheckTime = DateTime.Now;

                // 比较版本
                _versionInfo.HasUpdate = _versionInfo.CompareVersions() < 0;

                if (_versionInfo.HasUpdate)
                {
                    _logService.Info($"发现新版本：{latestVersion}（当前：{currentVersion}）");
                    
                    if (_config.NotifyUser)
                    {
                        UpdateAvailable?.Invoke(this, _versionInfo);
                    }
                }
                else
                {
                    _logService.Info($"当前已是最新版本：{currentVersion}");
                }

                return _versionInfo;
            }
            catch (Exception ex)
            {
                _logService.Error($"检查更新失败: {ex.Message}");
                return _versionInfo;
            }
        }

        /// <summary>
        /// 获取当前版本
        /// </summary>
        private async Task<string> GetCurrentVersionAsync()
        {
            Process? process = null;
            try
            {
                // 安全验证命令
                if (!SecurityService.IsCommandSafe("dsh"))
                {
                    _logService.Warn("获取当前版本失败: 不安全的命令");
                    return "unknown";
                }

                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dsh",
                        Arguments = "--version",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                return output.Trim();
            }
            catch (Exception ex)
            {
                _logService.Warn($"获取当前版本失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                return "unknown";
            }
            finally
            {
                process?.Dispose();
            }
        }

        /// <summary>
        /// 获取最新版本
        /// </summary>
        private async Task<string> GetLatestVersionAsync()
        {
            try
            {
                // 安全验证URL
                if (!SecurityService.IsUrlSafe(_config.UpdateUrl))
                {
                    _logService.Warn($"获取最新版本失败: 不安全的URL '{_config.UpdateUrl}'");
                    return "unknown";
                }
                
                var response = await _httpClient.GetStringAsync(_config.UpdateUrl);
                using var doc = JsonDocument.Parse(response);
                
                if (doc.RootElement.TryGetProperty("dist-tags", out var distTags) &&
                    distTags.TryGetProperty("latest", out var latest))
                {
                    return latest.GetString() ?? "unknown";
                }

                return "unknown";
            }
            catch (Exception ex)
            {
                _logService.Warn($"获取最新版本失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                return "unknown";
            }
        }

        /// <summary>
        /// 执行更新
        /// </summary>
        public async Task<bool> PerformUpdateAsync()
        {
            Process? process = null;
            try
            {
                // 安全验证命令
                if (!SecurityService.IsCommandSafe("npm"))
                {
                    _logService.Error("执行更新失败: 不安全的命令");
                    return false;
                }

                _logService.Info("正在执行更新...");

                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "npm",
                        Arguments = "update -g @deepseek-ai/dsh",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logService.Info("更新成功");
                    await CheckForUpdatesAsync();
                    return true;
                }
                else
                {
                    _logService.Error($"更新失败: {SecurityService.SanitizeLogMessage(error)}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"执行更新失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                return false;
            }
            finally
            {
                process?.Dispose();
            }
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
                    // 注意：不释放静态HttpClient，避免影响其他使用者
                }
                _disposed = true;
            }
        }
    }
}
