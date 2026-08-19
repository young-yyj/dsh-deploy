using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly Dictionary<int, PortInfo> _portCache = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheTimeout = TimeSpan.FromSeconds(5);
        private readonly object _cacheLock = new();

        // 端口→PID 映射缓存（netstat 解析，带短 TTL）
        private static readonly Dictionary<int, int> _portPidCache = new();
        private static DateTime _portPidCacheTime = DateTime.MinValue;
        private static readonly object _portPidLock = new();
        private static readonly TimeSpan PortPidCacheTtl = TimeSpan.FromSeconds(3);

        public PortService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 检查端口是否被占用
        /// </summary>
        /// <param name="port">端口号</param>
        /// <param name="quickCheck">是否快速检查（不获取进程信息）</param>
        /// <param name="useCache">是否使用缓存</param>
        /// <returns>端口信息</returns>
        public async Task<PortInfo> CheckPortAsync(int port, bool quickCheck = false, bool useCache = true)
        {
            // 检查缓存
            if (useCache && quickCheck)
            {
                lock (_cacheLock)
                {
                    if (_portCache.TryGetValue(port, out var cached) &&
                        DateTime.Now - _lastCacheUpdate < _cacheTimeout)
                    {
                        return cached;
                    }
                }
            }

            return await Task.Run(() =>
            {
                try
                {
                    var properties = IPGlobalProperties.GetIPGlobalProperties();
                    var connections = properties.GetActiveTcpConnections();
                    
                    var portConnection = connections.FirstOrDefault(c => c.LocalEndPoint.Port == port);
                    
                    PortInfo portInfo;
                    if (portConnection == null)
                    {
                        portInfo = new PortInfo
                        {
                            Port = port,
                            IsInUse = false
                        };
                    }
                    else
                    {
                        portInfo = new PortInfo
                        {
                            Port = port,
                            IsInUse = true,
                            ConnectionState = portConnection.State.ToString(),
                            LocalAddress = portConnection.LocalEndPoint.ToString(),
                            RemoteAddress = portConnection.RemoteEndPoint?.ToString() ?? "N/A"
                        };

                        if (!quickCheck)
                        {
                            // 通过 netstat 获取占用端口的进程信息
                            try
                            {
                                var pidMap = GetPortProcessIdMap();
                                if (pidMap.TryGetValue(port, out var pid))
                                {
                                    portInfo.ProcessId = pid;
                                    try
                                    {
                                        using var process = Process.GetProcessById(pid);
                                        portInfo.ProcessName = process.ProcessName;
                                    }
                                    catch
                                    {
                                        portInfo.ProcessName = "Unknown";
                                    }
                                }
                                else
                                {
                                    portInfo.ProcessId = null;
                                    portInfo.ProcessName = "Unknown";
                                }
                            }
                            catch (Exception ex)
                            {
                                _logService.Log(LogLevel.WARN, $"获取进程信息失败: {ex.Message}");
                            }
                        }
                    }

                    // 更新缓存
                    if (useCache)
                    {
                        lock (_cacheLock)
                        {
                            _portCache[port] = portInfo;
                            _lastCacheUpdate = DateTime.Now;
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

        /// <summary>
        /// 清除端口缓存
        /// </summary>
        public void ClearCache()
        {
            lock (_cacheLock)
            {
                _portCache.Clear();
                _lastCacheUpdate = DateTime.MinValue;
            }
        }

        /// <summary>
        /// 获取端口→进程ID映射（netstat 解析，带短 TTL 缓存）
        /// </summary>
        public static Dictionary<int, int> GetPortProcessIdMap()
        {
            lock (_portPidLock)
            {
                if (DateTime.Now - _portPidCacheTime < PortPidCacheTtl && _portPidCache.Count > 0)
                {
                    return new Dictionary<int, int>(_portPidCache);
                }
            }

            var map = BuildPortProcessIdMap();

            lock (_portPidLock)
            {
                _portPidCache.Clear();
                foreach (var kv in map)
                {
                    _portPidCache[kv.Key] = kv.Value;
                }
                _portPidCacheTime = DateTime.Now;
            }

            return new Dictionary<int, int>(map);
        }

        /// <summary>
        /// 通过 netstat -ano 构建端口→PID 映射
        /// </summary>
        private static Dictionary<int, int> BuildPortProcessIdMap()
        {
            var map = new Dictionary<int, int>();
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = "-ano -p tcp",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true,
                        StandardOutputEncoding = Encoding.UTF8
                    }
                };

                if (!process.Start())
                {
                    return map;
                }

                var output = process.StandardOutput.ReadToEnd();
                if (!process.WaitForExit(10000))
                {
                    try { process.Kill(); } catch { }
                    return map;
                }

                // 只解析地址/端口/PID 列（均为 ASCII，不受系统语言影响）
                var regex = new Regex(
                    @"^\s*TCP\s+(?<local>[0-9\.\[\]]+):(?<port>\d+)\s+[0-9\.\[\]]+:\d+\s+\S+\s+(?<pid>\d+)\s*$",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

                foreach (var line in output.Split('\n'))
                {
                    var match = regex.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    if (int.TryParse(match.Groups["port"].Value, out var port) &&
                        int.TryParse(match.Groups["pid"].Value, out var pid) &&
                        !map.ContainsKey(port))
                    {
                        map[port] = pid;
                    }
                }
            }
            catch
            {
                // netstat 失败时返回空表，调用方自行降级
            }

            return map;
        }
    }
}
