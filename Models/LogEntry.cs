using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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
        public SolidColorBrush LevelColor => Level switch
        {
            LogLevel.DEBUG => Brushes.Gray,
            LogLevel.INFO => Brushes.Black,
            LogLevel.WARN => Brushes.Orange,
            LogLevel.ERROR => Brushes.Red,
            _ => Brushes.Black
        };

        /// <summary>
        /// 完整显示文本
        /// </summary>
        public string FullDisplay => $"[{TimestampDisplay}] [{LevelDisplay}] {Message}";
    }
}
