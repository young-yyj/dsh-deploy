using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 应用程序配置
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// Web界面URL
        /// </summary>
        [JsonPropertyName("webUrl")]
        public string WebUrl { get; set; } = "http://127.0.0.1:3080";

        /// <summary>
        /// 端口号
        /// </summary>
        [JsonPropertyName("port")]
        public int Port { get; set; } = 3080;

        /// <summary>
        /// 是否开机自启
        /// </summary>
        [JsonPropertyName("autoStart")]
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// 是否显示通知
        /// </summary>
        [JsonPropertyName("notifications")]
        public bool Notifications { get; set; } = true;

        /// <summary>
        /// 是否播放声音
        /// </summary>
        [JsonPropertyName("soundEnabled")]
        public bool SoundEnabled { get; set; } = true;

        /// <summary>
        /// 日志级别
        /// </summary>
        [JsonPropertyName("logLevel")]
        public string LogLevel { get; set; } = "INFO";

        /// <summary>
        /// 状态检查间隔（秒）
        /// </summary>
        [JsonPropertyName("statusCheckInterval")]
        public int StatusCheckInterval { get; set; } = 30;

        /// <summary>
        /// DSH命令路径
        /// </summary>
        [JsonPropertyName("dshCommand")]
        public string DshCommand { get; set; } = "dsh";

        /// <summary>
        /// DSH参数
        /// </summary>
        [JsonPropertyName("dshArgs")]
        public string DshArgs { get; set; } = "web";

        /// <summary>
        /// 窗口位置X
        /// </summary>
        [JsonPropertyName("windowX")]
        public double WindowX { get; set; } = -1;

        /// <summary>
        /// 窗口位置Y
        /// </summary>
        [JsonPropertyName("windowY")]
        public double WindowY { get; set; } = -1;

        /// <summary>
        /// 窗口宽度
        /// </summary>
        [JsonPropertyName("windowWidth")]
        public double WindowWidth { get; set; } = 400;

        /// <summary>
        /// 窗口高度
        /// </summary>
        [JsonPropertyName("windowHeight")]
        public double WindowHeight { get; set; } = 500;

        /// <summary>
        /// 是否最小化到托盘
        /// </summary>
        [JsonPropertyName("minimizeToTray")]
        public bool MinimizeToTray { get; set; } = true;

        /// <summary>
        /// 是否显示托盘图标
        /// </summary>
        [JsonPropertyName("showTrayIcon")]
        public bool ShowTrayIcon { get; set; } = true;

        /// <summary>
        /// 崩溃恢复配置
        /// </summary>
        [JsonPropertyName("crashRecovery")]
        public CrashRecoveryConfig CrashRecovery { get; set; } = new CrashRecoveryConfig();

        /// <summary>
        /// 自动更新配置
        /// </summary>
        [JsonPropertyName("autoUpdate")]
        public UpdateConfig AutoUpdate { get; set; } = new UpdateConfig();

        /// <summary>
        /// 健康检查配置
        /// </summary>
        [JsonPropertyName("healthCheck")]
        public HealthCheckConfig HealthCheck { get; set; } = new HealthCheckConfig();
    }
}
