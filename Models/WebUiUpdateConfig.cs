using System.Text.Json.Serialization;

namespace dsh_deploy.Models
{
    /// <summary>
    /// dsh-web-ui 升级配置
    /// </summary>
    public class WebUiUpdateConfig
    {
        /// <summary>
        /// 插件包名
        /// </summary>
        [JsonPropertyName("packageName")]
        public string PackageName { get; set; } = "@linxin666/dsh-web-ui-all";

        /// <summary>
        /// npm 镜像源地址
        /// </summary>
        [JsonPropertyName("registryUrl")]
        public string RegistryUrl { get; set; } = "https://registry.npmmirror.com";
    }
}
