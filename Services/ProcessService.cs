using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 进程服务 - 负责进程管理
    /// </summary>
    public class ProcessService
    {
        private readonly LogService _logService;

        public ProcessService(LogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// 获取DSH进程列表
        /// </summary>
        /// <returns>DSH进程列表</returns>
        public async Task<List<ProcessInfo>> GetDshProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var processes = new List<ProcessInfo>();
                try
                {
                    // 获取所有node.exe进程
                    var nodeProcesses = Process.GetProcessesByName("node");

                    foreach (var process in nodeProcesses)
                    {
                        try
                        {
                            // 检查是否是DSH进程（通过检查端口占用）
                            bool isDshProcess = IsDshProcessByPort(process.Id);
                            
                            if (isDshProcess)
                            {
                                processes.Add(new ProcessInfo
                                {
                                    ProcessId = process.Id,
                                    ProcessName = process.ProcessName,
                                    CommandLine = "dsh web",
                                    StartTime = process.StartTime,
                                    MemoryUsage = process.WorkingSet64,
                                    IsDshProcess = true
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logService.Log(LogLevel.WARN, $"获取进程信息失败 (PID: {process.Id}): {ex.Message}");
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logService.Log(LogLevel.ERROR, $"获取DSH进程失败: {ex.Message}");
                }
                return processes;
            });
        }

        /// <summary>
        /// 通过端口占用判断是否是DSH进程
        /// </summary>
        private bool IsDshProcessByPort(int processId)
        {
            try
            {
                // 检查进程是否占用了DSH常用端口（3080）
                var portService = new PortService(_logService);
                var portInfo = portService.CheckPortAsync(3080, quickCheck: true).Result;
                
                if (portInfo.IsInUse && portInfo.ProcessId == processId)
                {
                    return true;
                }
                
                // 检查进程是否占用了其他DSH端口（3081-3090）
                for (int port = 3081; port <= 3090; port++)
                {
                    var info = portService.CheckPortAsync(port, quickCheck: true).Result;
                    if (info.IsInUse && info.ProcessId == processId)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 启动DSH服务
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="args">参数</param>
        /// <returns>是否成功</returns>
        public async Task<bool> StartDshServiceAsync(string command = "dsh", string args = "web")
        {
            // 安全验证
            if (!SecurityService.IsCommandSafe(command))
            {
                _logService.Error($"启动DSH服务失败: 不安全的命令 '{command}'");
                return false;
            }

            if (!SecurityService.AreArgumentsSafe(args))
            {
                _logService.Error($"启动DSH服务失败: 不安全的参数 '{args}'");
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        _logService.Info($"DSH服务已启动 (PID: {process.Id})");
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    _logService.Error($"启动DSH服务失败: {SecurityService.SanitizeLogMessage(ex.Message)}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 停止DSH服务
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="timeoutSeconds">超时秒数</param>
        /// <returns>是否成功</returns>
        public async Task<bool> StopProcessAsync(int processId, int timeoutSeconds = 5)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    
                    // 先尝试优雅关闭
                    if (process.CloseMainWindow())
                    {
                        if (process.WaitForExit(timeoutSeconds * 1000))
                        {
                            _logService.Info($"进程已优雅退出 (PID: {processId})");
                            return true;
                        }
                    }

                    // 强制终止
                    process.Kill();
                    if (process.WaitForExit(2000))
                    {
                        _logService.Info($"进程已强制终止 (PID: {processId})");
                        return true;
                    }

                    _logService.Warn($"无法终止进程 (PID: {processId})");
                    return false;
                }
                catch (ArgumentException)
                {
                    // 进程已退出
                    return true;
                }
                catch (Exception ex)
                {
                    _logService.Error($"停止进程失败 (PID: {processId}): {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 停止所有DSH进程
        /// </summary>
        /// <returns>停止的进程数</returns>
        public async Task<int> StopAllDshProcessesAsync()
        {
            var processes = await GetDshProcessesAsync();
            int stoppedCount = 0;

            foreach (var process in processes)
            {
                if (await StopProcessAsync(process.ProcessId))
                {
                    stoppedCount++;
                }
            }

            return stoppedCount;
        }

        /// <summary>
        /// 检查进程是否存在
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <returns>是否存在</returns>
        public async Task<bool> ProcessExistsAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    process.Dispose();
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// 获取进程命令行（简化版本，不依赖WMI）
        /// </summary>
        private string GetProcessCommandLine(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                // 注意：在.NET 8中，Process类不直接提供CommandLine属性
                // 这里返回进程名称作为替代
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 判断是否是DSH进程
        /// </summary>
        private bool IsDshProcess(string commandLine)
        {
            if (string.IsNullOrEmpty(commandLine)) return false;
            
            return commandLine.Contains("deepseek-ai/dsh") ||
                   commandLine.Contains("dsh web") ||
                   commandLine.Contains("dsh-web") ||
                   commandLine.Contains("@deepseek-ai/dsh");
        }
    }
}
