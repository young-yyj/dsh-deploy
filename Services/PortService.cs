using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 端口服务 - 负责端口检测和管理
    /// </summary>
    public class PortService
    {
        private readonly LogService _logService;

        public PortService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 检查端口是否被占用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="quickCheck">是否快速检查（不获取进程信息）</param>
        /// <returns>端口信息</returns>
        public async Task<PortInfo> CheckPortAsync(int port, bool quickCheck = false)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var properties = IPGlobalProperties.GetIPGlobalProperties();
                    var connections = properties.GetActiveTcpConnections();
                    
                    var portConnection = connections.FirstOrDefault(c => c.LocalEndPoint.Port == port);
                    
                    if (portConnection == null)
                    {
                        return new PortInfo
                        {
                            Port = port,
                            IsInUse = false
                        };
                    }

                    var portInfo = new PortInfo
                    {
                        Port = port,
                        IsInUse = true,
                        ConnectionState = portConnection.State.ToString(),
                        LocalAddress = portConnection.LocalEndPoint.ToString(),
                        RemoteAddress = portConnection.RemoteEndPoint?.ToString() ?? "N/A"
                    };

                    if (!quickCheck)
                    {
                        // 获取进程信息
                        try
                        {
                            // 注意：在.NET中获取进程ID需要使用其他方法
                            // 这里简化处理，实际项目中可能需要P/Invoke
                            portInfo.ProcessId = null;
                            portInfo.ProcessName = "Unknown";
                        }
                        catch (Exception ex)
                        {
                            _logService.Log(LogLevel.WARN, $"获取进程信息失败: {ex.Message}");
                        }
                    }

                    _logService.Log(LogLevel.DEBUG, $"端口 {port} 状态: {(portInfo.IsInUse ? "占用" : "可用")}");
                    return portInfo;
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.ERROR, $"检查端口 {port} 失败: {ex.Message}");
                    return new PortInfo
                    {
                        Port = port,
                        IsInUse = false
                    };
                }
            });
        }

        /// <summary>
        /// 检查端口是否可用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否可用</returns>
        public async Task<bool> IsPortAvailableAsync(int port)
        {
            var portInfo = await CheckPortAsync(port, quickCheck: true);
            return !portInfo.IsInUse;
        }

        /// <summary>
        /// 查找可用端口
        /// </summary>
        /// <param name="startPort">起始端口</param>
        /// <param name="range">范围</param>
        /// <returns>可用端口，如果没有找到返回-1</returns>
        public async Task<int> FindAvailablePortAsync(int startPort = 3080, int range = 10)
        {
            for (int port = startPort; port < startPort + range; port++)
            {
                if (await IsPortAvailableAsync(port))
                {
                    return port;
                }
            }
            return -1;
        }

        /// <summary>
        /// 获取所有监听端口
        /// </summary>
        /// <returns>监听端口列表</returns>
        public async Task<List<PortInfo>> GetListeningPortsAsync()
        {
            return await Task.Run(() =>
            {
                var ports = new List<PortInfo>();
                try
                {
                    var properties = IPGlobalProperties.GetIPGlobalProperties();
                    var listeners = properties.GetActiveTcpListeners();

                    foreach (var listener in listeners)
                    {
                        ports.Add(new PortInfo
                        {
                            Port = listener.Port,
                            IsInUse = true,
                            LocalAddress = listener.ToString(),
                            ConnectionState = "LISTENING"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.ERROR, $"获取监听端口失败: {ex.Message}");
                }
                return ports;
            });
        }
    }
}
