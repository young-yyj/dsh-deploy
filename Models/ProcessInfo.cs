using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 进程信息
    /// </summary>
    public class ProcessInfo
    {
        /// <summary>
        /// 进程ID
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// 进程名称
        /// </summary>
        public string ProcessName { get; set; } = string.Empty;

        /// <summary>
        /// 命令行
        /// </summary>
        public string CommandLine { get; set; } = string.Empty;

        /// <summary>
        /// 进程路径
        /// </summary>
        public string? ExecutablePath { get; set; }

        /// <summary>
        /// 启动时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 内存使用（字节）
        /// </summary>
        public long MemoryUsage { get; set; }

        /// <summary>
        /// CPU使用率
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// 是否是DSH进程
        /// </summary>
        public bool IsDshProcess { get; set; }

        /// <summary>
        /// 是否是Node.js进程
        /// </summary>
        public bool IsNodeProcess => ProcessName?.ToLower() == "node.exe";

        /// <summary>
        /// 内存使用显示文本
        /// </summary>
        public string MemoryDisplay => MemoryUsage switch
        {
            >= 1073741824 => $"{MemoryUsage / 1073741824.0:F2} GB",
            >= 1048576 => $"{MemoryUsage / 1048576.0:F2} MB",
            >= 1024 => $"{MemoryUsage / 1024.0:F2} KB",
            _ => $"{MemoryUsage} B"
        };

        /// <summary>
        /// 运行时间
        /// </summary>
        public string RunningTime
        {
            get
            {
                if (!StartTime.HasValue) return "未知";
                var span = DateTime.Now - StartTime.Value;
                if (span.TotalDays >= 1) return $"{(int)span.TotalDays}天{span.Hours}小时";
                if (span.TotalHours >= 1) return $"{(int)span.TotalHours}小时{span.Minutes}分钟";
                if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes}分钟";
                return "刚刚启动";
            }
        }
    }
}
