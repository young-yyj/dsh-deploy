using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 版本信息
    /// </summary>
    public class VersionInfo
    {
        /// <summary>
        /// 当前版本
        /// </summary>
        [JsonPropertyName("currentVersion")]
        public string CurrentVersion { get; set; } = string.Empty;

        /// <summary>
        /// 最新版本
        /// </summary>
        [JsonPropertyName("latestVersion")]
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// 是否有更新
        /// </summary>
        [JsonPropertyName("hasUpdate")]
        public bool HasUpdate { get; set; }

        /// <summary>
        /// 最后检查时间
        /// </summary>
        [JsonPropertyName("lastCheckTime")]
        public DateTime LastCheckTime { get; set; }

        /// <summary>
        /// 更新说明
        /// </summary>
        [JsonPropertyName("releaseNotes")]
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>
        /// 下载URL
        /// </summary>
        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; } = string.Empty;

        /// <summary>
        /// 版本比较结果
        /// </summary>
        public int CompareVersions()
        {
            if (string.IsNullOrEmpty(CurrentVersion) || string.IsNullOrEmpty(LatestVersion))
                return 0;

            try
            {
                var current = new Version(CurrentVersion);
                var latest = new Version(LatestVersion);
                return current.CompareTo(latest);
            }
            catch
            {
                return string.Compare(CurrentVersion, LatestVersion, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}
