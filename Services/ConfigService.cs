using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 配置服务 - 负责配置文件的读写
    /// </summary>
    public class ConfigService
    {
        private readonly LogService _logService;
        private readonly string _configDirectory;
        private readonly string _configFilePath;
        private AppConfig _currentConfig;

        public ConfigService(LogService logService)
        {
            _logService = logService;
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dsh");
            _configFilePath = Path.Combine(_configDirectory, "wpf-config.json");
            _currentConfig = new AppConfig();

            EnsureConfigDirectory();
        }

        /// <summary>
        /// 当前配置
        /// </summary>
        public AppConfig Current => _currentConfig;

        /// <summary>
        /// 加载配置
        /// </summary>
        /// <returns>配置对象</returns>
        public async Task<AppConfig> LoadAsync()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = await File.ReadAllTextAsync(_configFilePath, Encoding.UTF8);
                    _currentConfig = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    _logService.Info($"配置已加载: {_configFilePath}");
                }
                else
                {
                    _currentConfig = new AppConfig();
                    await SaveAsync();
                    _logService.Info("使用默认配置");
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"加载配置失败: {ex.Message}");
                _currentConfig = new AppConfig();
            }

            return _currentConfig;
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        public async Task SaveAsync()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                var json = JsonSerializer.Serialize(_currentConfig, options);
                await File.WriteAllTextAsync(_configFilePath, json, Encoding.UTF8);
                _logService.Info("配置已保存");
            }
            catch (Exception ex)
            {
                _logService.Error($"保存配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 更新配置
        /// </summary>
        /// <param name="action">更新操作</param>
        public async Task UpdateAsync(Action<AppConfig> action)
        {
            action(_currentConfig);
            await SaveAsync();
        }

        /// <summary>
        /// 重置配置
        /// </summary>
        public async Task ResetAsync()
        {
            _currentConfig = new AppConfig();
            await SaveAsync();
            _logService.Info("配置已重置为默认值");
        }

        /// <summary>
        /// 备份配置
        /// </summary>
        public async Task<string> BackupAsync()
        {
            try
            {
                var backupPath = Path.Combine(_configDirectory, $"wpf-config.backup.{DateTime.Now:yyyyMMddHHmmss}.json");
                if (File.Exists(_configFilePath))
                {
                    await Task.Run(() => File.Copy(_configFilePath, backupPath, true));
                    _logService.Info($"配置已备份到: {backupPath}");
                    return backupPath;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logService.Error($"备份配置失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确保配置目录存在
        /// </summary>
        private void EnsureConfigDirectory()
        {
            try
            {
                if (!Directory.Exists(_configDirectory))
                {
                    Directory.CreateDirectory(_configDirectory);
                }
            }
            catch (Exception ex)
            {
                _logService.Error($"创建配置目录失败: {ex.Message}");
            }
        }
    }
}
