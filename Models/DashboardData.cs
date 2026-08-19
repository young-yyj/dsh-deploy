using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 仪表盘数据
    /// </summary>
    public class DashboardData
    {
        /// <summary>
        /// 服务状态
        /// </summary>
        public ServiceStatus ServiceStatus { get; set; } = new ServiceStatus();

        /// <summary>
        /// 健康状态
        /// </summary>
        public HealthStatus HealthStatus { get; set; } = new HealthStatus();

        /// <summary>
        /// 进程信息
        /// </summary>
        public ProcessInfo? ProcessInfo { get; set; }

        /// <summary>
        /// 运行时长
        /// </summary>
        public string Uptime { get; set; } = "未运行";

        /// <summary>
        /// CPU使用率
        /// </summary>
        public double CpuUsage { get; set; }

        /// <summary>
        /// 内存使用量（MB）
        /// </summary>
        public double MemoryUsage { get; set; }

        /// <summary>
        /// 端口状态
        /// </summary>
        public string PortStatus { get; set; } = "未知";

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// 日志统计
        /// </summary>
        public LogStatistics LogStatistics { get; set; } = new LogStatistics();

        /// <summary>
        /// 更新统计
        /// </summary>
        public UpdateStatistics UpdateStatistics { get; set; } = new UpdateStatistics();
    }

    /// <summary>
    /// 日志统计
    /// </summary>
    public class LogStatistics
    {
        /// <summary>
        /// 总日志数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 错误数
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 警告数
        /// </summary>
        public int WarningCount { get; set; }

        /// <summary>
        /// 信息数
        /// </summary>
        public int InfoCount { get; set; }

        /// <summary>
        /// 调试数
        /// </summary>
        public int DebugCount { get; set; }
    }

    /// <summary>
    /// 更新统计
    /// </summary>
    public class UpdateStatistics
    {
        /// <summary>
        /// 当前版本
        /// </summary>
        public string CurrentVersion { get; set; } = "未知";

        /// <summary>
        /// 最新版本
        /// </summary>
        public string LatestVersion { get; set; } = "未知";

        /// <summary>
        /// 是否有更新
        /// </summary>
        public bool HasUpdate { get; set; }

        /// <summary>
        /// 最后检查时间
        /// </summary>
        public DateTime LastCheckTime { get; set; }
    }
}
