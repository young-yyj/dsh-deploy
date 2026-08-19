using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        DEBUG,
        INFO,
        WARN,
        ERROR
    }

    /// <summary>
    /// 日志条目
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.INFO;

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 来源
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// 时间戳显示文本
        /// </summary>
        public string TimestampDisplay => Timestamp.ToString("HH:mm:ss");

        /// <summary>
        /// 级别显示文本
        /// </summary>
        public string LevelDisplay => Level switch
        {
            LogLevel.DEBUG => "调试",
            LogLevel.INFO => "信息",
            LogLevel.WARN => "警告",
            LogLevel.ERROR => "错误",
            _ => "未知"
        };

        /// <summary>
        /// 级别颜色
        /// </summary>
        public System.Windows.Media.SolidColorBrush LevelColor => Level switch
        {
            LogLevel.DEBUG => System.Windows.Media.Brushes.Gray,
            LogLevel.INFO => System.Windows.Media.Brushes.Black,
            LogLevel.WARN => System.Windows.Media.Brushes.Orange,
            LogLevel.ERROR => System.Windows.Media.Brushes.Red,
            _ => System.Windows.Media.Brushes.Black
        };

        /// <summary>
        /// 完整显示文本
        /// </summary>
        public string FullDisplay => $"[{TimestampDisplay}] [{LevelDisplay}] {Message}";
    }
}
