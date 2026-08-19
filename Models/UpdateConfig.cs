using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 自动更新配置
    /// </summary>
    public class UpdateConfig
    {
        /// <summary>
        /// 是否启用自动更新检查
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 检查间隔（毫秒）
        /// </summary>
        [JsonPropertyName("checkInterval")]
        public int CheckInterval { get; set; } = 3600000; // 1小时

        /// <summary>
        /// 是否自动下载更新
        /// </summary>
        [JsonPropertyName("autoDownload")]
        public bool AutoDownload { get; set; } = false;

        /// <summary>
        /// 是否通知用户
        /// </summary>
        [JsonPropertyName("notifyUser")]
        public bool NotifyUser { get; set; } = true;

        /// <summary>
        /// 更新源URL
        /// </summary>
        [JsonPropertyName("updateUrl")]
        public string UpdateUrl { get; set; } = "https://registry.npmjs.org/@deepseek-ai/dsh";
    }
}
