using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 日志服务 - 负责日志记录和管理
    /// </summary>
    public class LogService
    {
        private readonly ObservableCollection<LogEntry> _logs = new();
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly LogLevel _minLogLevel;
        private readonly Dispatcher _dispatcher;
        private readonly object _lock = new();
        private const int MaxLogEntries = 1000;

        public LogService(Dispatcher dispatcher, LogLevel minLogLevel = LogLevel.INFO)
        {
            _dispatcher = dispatcher;
            _minLogLevel = minLogLevel;
            _logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dsh", "logs");
            _logFilePath = Path.Combine(_logDirectory, "dsh-wpf.log");

            EnsureLogDirectory();
        }

        /// <summary>
        /// 日志集合（用于绑定UI）
        /// </summary>
        public ObservableCollection<LogEntry> Logs => _logs;

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="level">日志级别</param>
        /// <param name="message">日志消息</param>
        /// <param name="source">来源</param>
        public void Log(LogLevel level, string message, string source = "")
        {
            if (level < _minLogLevel) return;

            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source
            };

            // 添加到内存集合
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _logs.Add(entry);
                    if (_logs.Count > MaxLogEntries)
                    {
                        _logs.RemoveAt(0);
                    }
                }
            });

            // 写入文件
            WriteToFile(entry);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public void Debug(string message, string source = "")
            => Log(LogLevel.DEBUG, message, source);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public void Info(string message, string source = "")
            => Log(LogLevel.INFO, message, source);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void Warn(string message, string source = "")
            => Log(LogLevel.WARN, message, source);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void Error(string message, string source = "")
            => Log(LogLevel.ERROR, message, source);

        /// <summary>
        /// 记录错误日志（带异常）
        /// </summary>
        public void Error(string message, Exception ex, string source = "")
            => Log(LogLevel.ERROR, $"{message}: {ex.Message}", source);

        /// <summary>
        /// 清空日志
        /// </summary>
        public void Clear()
        {
            _dispatcher.Invoke(() =>
            {
                lock (_lock)
                {
                    _logs.Clear();
                }
            });
        }

        /// <summary>
        /// 导出日志
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public async Task ExportLogsAsync(string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("DSH Deploy 日志导出");
                sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(new string('=', 50));
                sb.AppendLine();

                foreach (var log in _logs)
                {
                    sb.AppendLine(log.FullDisplay);
                }

                await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8);
                Info($"日志已导出到: {filePath}");
            }
            catch (Exception ex)
            {
                Error($"导出日志失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 确保日志目录存在
        /// </summary>
        private void EnsureLogDirectory()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch (Exception ex)
            {
                // 如果无法创建日志目录，静默继续
                System.Diagnostics.Debug.WriteLine($"创建日志目录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 写入日志到文件
        /// </summary>
        private void WriteToFile(LogEntry entry)
        {
            try
            {
                var logLine = entry.FullDisplay + Environment.NewLine;
                File.AppendAllText(_logFilePath, logLine, Encoding.UTF8);
            }
            catch
            {
                // 日志写入失败，静默继续
            }
        }
    }
}
