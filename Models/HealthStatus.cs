using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 健康状态枚举
    /// </summary>
    public enum HealthState
    {
        /// <summary>
        /// 健康
        /// </summary>
        Healthy,

        /// <summary>
        /// 不健康
        /// </summary>
        Unhealthy,

        /// <summary>
        /// 检查中
        /// </summary>
        Checking,

        /// <summary>
        /// 未知
        /// </summary>
        Unknown
    }

    /// <summary>
    /// 健康状态
    /// </summary>
    public class HealthStatus
    {
        /// <summary>
        /// 健康状态
        /// </summary>
        [JsonPropertyName("state")]
        public HealthState State { get; set; } = HealthState.Unknown;

        /// <summary>
        /// 最后检查时间
        /// </summary>
        [JsonPropertyName("lastCheckTime")]
        public DateTime LastCheckTime { get; set; }

        /// <summary>
        /// 连续失败次数
        /// </summary>
        [JsonPropertyName("consecutiveFailures")]
        public int ConsecutiveFailures { get; set; }

        /// <summary>
        /// 最后响应时间（毫秒）
        /// </summary>
        [JsonPropertyName("lastResponseTime")]
        public long LastResponseTime { get; set; }

        /// <summary>
        /// 最后错误信息
        /// </summary>
        [JsonPropertyName("lastError")]
        public string LastError { get; set; } = string.Empty;

        /// <summary>
        /// 状态文本
        /// </summary>
        public string StateText => State switch
        {
            HealthState.Healthy => "健康",
            HealthState.Unhealthy => "不健康",
            HealthState.Checking => "检查中",
            _ => "未知"
        };

        /// <summary>
        /// 状态颜色
        /// </summary>
        public string StateColor => State switch
        {
            HealthState.Healthy => "#4CAF50",
            HealthState.Unhealthy => "#F44336",
            HealthState.Checking => "#FF9800",
            _ => "#9E9E9E"
        };
    }
}
