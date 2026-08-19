using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        private Process? _dshProcess;

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
                    // DSH 使用 3080-3090 端口：先取这些端口的占用进程 PID
                    var dshPids = PortService.GetPortProcessIdMap()
                        .Where(kv => kv.Key >= 3080 && kv.Key <= 3090)
                        .Select(kv => kv.Value)
                        .ToHashSet();

                    if (dshPids.Count == 0)
                    {
                        return processes;
                    }

                    foreach (var process in Process.GetProcessesByName("node"))
                    {
                        try
                        {
                            if (!dshPids.Contains(process.Id))
                            {
                                continue;
                            }

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
        /// 在 PATH 中解析命令的完整路径（Windows 下优先 .exe/.cmd/.bat），
        /// 找不到时原样返回。用于解决 .cmd 垫片在 UseShellExecute=false 下裸名启动失败的问题。
        /// </summary>
        public static string ResolveCommandPath(string command)
        {
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            string[] extensions = OperatingSystem.IsWindows()
                ? new[] { ".exe", ".cmd", ".bat", "" }
                : new[] { "" };

            foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                foreach (var ext in extensions)
                {
                    var candidate = Path.Combine(dir, command + ext);
                    if (File.Exists(candidate))
                    {
                        return candidate;
                    }
                }
            }

            return command;
        }

        /// <summary>
        /// 启动DSH服务
        /// </summary>
        /// <param name="command">命令</param>
        /// <param name="args">参数</param>
        /// <returns>是否成功</returns>
        public async Task<bool> StartDshServiceAsync(string command = "dsh", string args = "web")
        {
            // 解析命令完整路径（解决 Windows 下 .cmd 垫片裸名启动失败）
            var commandPath = ResolveCommandPath(command);

            // 安全验证
            if (!SecurityService.IsCommandSafe(commandPath))
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
                        FileName = commandPath,
                        Arguments = args,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    var process = new Process
                    {
                        StartInfo = startInfo,
                        EnableRaisingEvents = true
                    };

                    // 异步排空输出流，防止常驻进程缓冲区填满导致阻塞
                    process.OutputDataReceived += (_, e) =>
                    {
                        if (e.Data != null) _logService.Log(LogLevel.DEBUG, $"[dsh] {e.Data}");
                    };
                    process.ErrorDataReceived += (_, e) =>
                    {
                        if (e.Data != null) _logService.Log(LogLevel.WARN, $"[dsh] {e.Data}");
                    };

                    if (!process.Start())
                    {
                        process.Dispose();
                        return false;
                    }

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // 持有引用，防止 GC 回收导致事件订阅中断、输出流停止排空
                    _dshProcess = process;

                    _logService.Info($"DSH服务已启动 (PID: {process.Id})");
                    return true;
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
            Process? process = null;
            try
            {
                process = Process.GetProcessById(processId);
                // 注意：在.NET 8中，Process类不直接提供CommandLine属性
                // 这里返回进程名称作为替代
                return process.ProcessName;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                process?.Dispose();
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
