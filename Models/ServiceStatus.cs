using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 服务状态枚举
    /// </summary>
    public enum ServiceState
    {
        /// <summary>
        /// 运行中
        /// </summary>
        Running,

        /// <summary>
        /// 已停止
        /// </summary>
        Stopped,

        /// <summary>
        /// 启动中
        /// </summary>
        Starting,

        /// <summary>
        /// 端口冲突
        /// </summary>
        PortConflict,

        /// <summary>
        /// 错误状态
        /// </summary>
        Error
    }

    /// <summary>
    /// 服务状态模型
    /// </summary>
    public class ServiceStatus
    {
        /// <summary>
        /// 服务状态
        /// </summary>
        public ServiceState State { get; set; } = ServiceState.Stopped;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; } = 3080;

        /// <summary>
        /// 进程ID
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// 进程名称
        /// </summary>
        public string? ProcessName { get; set; }

        /// <summary>
        /// 状态消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// 是否在线
        /// </summary>
        public bool IsOnline => State == ServiceState.Running;

        /// <summary>
        /// 状态显示文本
        /// </summary>
        public string StateText => State switch
        {
            ServiceState.Running => "运行中",
            ServiceState.Stopped => "已停止",
            ServiceState.Starting => "正在启动",
            ServiceState.PortConflict => "端口冲突",
            ServiceState.Error => "错误",
            _ => "未知"
        };

        /// <summary>
        /// 状态颜色（用于UI显示）
        /// </summary>
        public string StateColor => State switch
        {
            ServiceState.Running => "#4CAF50",  // 绿色
            ServiceState.Stopped => "#F44336",  // 红色
            ServiceState.Starting => "#FF9800", // 黄色
            ServiceState.PortConflict => "#FF5722", // 橙色
            ServiceState.Error => "#F44336",    // 红色
            _ => "#9E9E9E"                      // 灰色
        };
    }
}
