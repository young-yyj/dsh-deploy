using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 恢复策略枚举
    /// </summary>
    public enum RecoveryStrategy
    {
        /// <summary>
        /// 立即重启
        /// </summary>
        Immediate,

        /// <summary>
        /// 固定延迟重启
        /// </summary>
        Delayed,

        /// <summary>
        /// 指数退避重启
        /// </summary>
        Exponential
    }

    /// <summary>
    /// 崩溃恢复配置
    /// </summary>
    public class CrashRecoveryConfig
    {
        /// <summary>
        /// 是否启用崩溃恢复
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 恢复策略
        /// </summary>
        [JsonPropertyName("strategy")]
        public RecoveryStrategy Strategy { get; set; } = RecoveryStrategy.Exponential;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        [JsonPropertyName("maxRetries")]
        public int MaxRetries { get; set; } = 5;

        /// <summary>
        /// 初始延迟（毫秒）
        /// </summary>
        [JsonPropertyName("initialDelay")]
        public int InitialDelay { get; set; } = 1000;

        /// <summary>
        /// 最大延迟（毫秒）
        /// </summary>
        [JsonPropertyName("maxDelay")]
        public int MaxDelay { get; set; } = 60000;

        /// <summary>
        /// 成功运行多久后重置重试计数（毫秒）
        /// </summary>
        [JsonPropertyName("resetAfter")]
        public int ResetAfter { get; set; } = 300000;

        /// <summary>
        /// 计算下次重试延迟
        /// </summary>
        /// <param name="retryCount">当前重试次数</param>
        /// <returns>延迟毫秒数</returns>
        public int CalculateDelay(int retryCount)
        {
            return Strategy switch
            {
                RecoveryStrategy.Immediate => 0,
                RecoveryStrategy.Delayed => InitialDelay,
                RecoveryStrategy.Exponential => Math.Min(
                    InitialDelay * (int)Math.Pow(2, retryCount),
                    MaxDelay),
                _ => InitialDelay
            };
        }
    }
}
