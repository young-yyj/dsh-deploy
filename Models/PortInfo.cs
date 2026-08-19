using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.Models
{
    /// <summary>
    /// 端口信息
    /// </summary>
    public class PortInfo
    {
        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 是否被占用
        /// </summary>
        public bool IsInUse { get; set; }

        /// <summary>
        /// 占用进程ID
        /// </summary>
        public int? ProcessId { get; set; }

        /// <summary>
        /// 占用进程名称
        /// </summary>
        public string? ProcessName { get; set; }

        /// <summary>
        /// 进程路径
        /// </summary>
        public string? ProcessPath { get; set; }

        /// <summary>
        /// 是否是DSH进程
        /// </summary>
        public bool IsDshProcess { get; set; }

        /// <summary>
        /// 连接状态
        /// </summary>
        public string ConnectionState { get; set; } = "Unknown";

        /// <summary>
        /// 本地地址
        /// </summary>
        public string LocalAddress { get; set; } = string.Empty;

        /// <summary>
        /// 远程地址
        /// </summary>
        public string RemoteAddress { get; set; } = string.Empty;
    }
}
