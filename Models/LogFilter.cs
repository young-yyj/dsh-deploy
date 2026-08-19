using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 日志过滤器
    /// </summary>
    public class LogFilter
    {
        /// <summary>
        /// 最小日志级别
        /// </summary>
        public LogLevel MinLevel { get; set; } = LogLevel.DEBUG;

        /// <summary>
        /// 关键词过滤
        /// </summary>
        public string Keyword { get; set; } = string.Empty;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// 是否匹配日志条目
        /// </summary>
        public bool IsMatch(LogEntry entry)
        {
            // 检查日志级别
            if (entry.Level < MinLevel)
                return false;

            // 检查关键词
            if (!string.IsNullOrEmpty(Keyword) && 
                !entry.Message.Contains(Keyword, StringComparison.OrdinalIgnoreCase))
                return false;

            // 检查时间范围
            if (StartTime.HasValue && entry.Timestamp < StartTime.Value)
                return false;

            if (EndTime.HasValue && entry.Timestamp > EndTime.Value)
                return false;

            return true;
        }

        /// <summary>
        /// 克隆过滤器
        /// </summary>
        public LogFilter Clone()
        {
            return new LogFilter
            {
                MinLevel = MinLevel,
                Keyword = Keyword,
                StartTime = StartTime,
                EndTime = EndTime
            };
        }
    }
}
