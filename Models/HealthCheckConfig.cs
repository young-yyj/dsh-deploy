using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 健康检查配置
    /// </summary>
    public class HealthCheckConfig
    {
        /// <summary>
        /// 是否启用健康检查
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 检查间隔（毫秒）
        /// </summary>
        [JsonPropertyName("interval")]
        public int Interval { get; set; } = 30000; // 30秒

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 5000; // 5秒

        /// <summary>
        /// 不健康阈值（连续失败次数）
        /// </summary>
        [JsonPropertyName("unhealthyThreshold")]
        public int UnhealthyThreshold { get; set; } = 3;

        /// <summary>
        /// 是否自动重启
        /// </summary>
        [JsonPropertyName("autoRestart")]
        public bool AutoRestart { get; set; } = true;

        /// <summary>
        /// 是否通知用户
        /// </summary>
        [JsonPropertyName("notifyUser")]
        public bool NotifyUser { get; set; } = true;

        /// <summary>
        /// 健康检查URL
        /// </summary>
        [JsonPropertyName("healthUrl")]
        public string HealthUrl { get; set; } = "http://127.0.0.1:3080";
    }
}
