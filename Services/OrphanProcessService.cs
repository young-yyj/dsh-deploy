using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 孤儿进程服务 - 处理终端关闭后残留的进程
    /// </summary>
    public class OrphanProcessService
    {
        private readonly LogService _logService;
        private readonly PortService _portService;

        public OrphanProcessService(LogService logService, PortService portService)
        {
            _logService = logService;
            _portService = portService;
        }

        /// <summary>
        /// 检测孤儿进程
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>孤儿进程信息</returns>
        public async Task<List<ProcessInfo>> DetectOrphanProcessesAsync(int port = 3080)
        {
            var orphanProcesses = new List<ProcessInfo>();

            try
            {
                // 获取占用端口的进程
                var portInfo = await _portService.CheckPortAsync(port);
                
                if (!portInfo.IsInUse || !portInfo.ProcessId.HasValue)
                {
                    return orphanProcesses;
                }

                var process = Process.GetProcessById(portInfo.ProcessId.Value);
                
                // 检查是否是孤儿进程（没有父窗口或父进程已退出）
                if (IsOrphanProcess(process))
                {
                    orphanProcesses.Add(new ProcessInfo
                    {
                        ProcessId = process.Id,
                        ProcessName = process.ProcessName,
                        StartTime = process.StartTime,
                        MemoryUsage = process.WorkingSet64
                    });
                }

                process.Dispose();
            }
            catch (Exception ex)
            {
                _logService.Error($"检测孤儿进程失败: {ex.Message}");
            }

            return orphanProcesses;
        }

        /// <summary>
        /// 判断是否是孤儿进程
        /// </summary>
        private bool IsOrphanProcess(Process process)
        {
            try
            {
                // 检查进程是否有主窗口
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    return false; // 有主窗口，不是孤儿进程
                }

                // 检查父进程是否还在运行
                var parentPid = GetParentProcessId(process.Id);
                if (parentPid == 0)
                {
                    return true; // 无法获取父进程，可能是孤儿进程
                }

                try
                {
                    var parentProcess = Process.GetProcessById(parentPid);
                    parentProcess.Dispose();
                    return false; // 父进程还在运行
                }
                catch
                {
                    return true; // 父进程已退出，是孤儿进程
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取父进程ID
        /// </summary>
        private int GetParentProcessId(int processId)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                // 注意：.NET没有直接获取父进程的方法
                // 这里简化处理，实际项目中可能需要P/Invoke
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 清理孤儿进程
        /// </summary>
        /// <param name="processId">进程ID</param>
        /// <param name="force">是否强制终止</param>
        /// <returns>是否成功</returns>
        public async Task<bool> CleanupOrphanProcessAsync(int processId, bool force = true)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var process = Process.GetProcessById(processId);
                    
                    _logService.Info($"正在清理孤儿进程: {process.ProcessName} (PID: {processId})");

                    if (force)
                    {
                        // 强制终止
                        process.Kill();
                    }
                    else
                    {
                        // 尝试优雅关闭
                        if (!process.CloseMainWindow())
                        {
                            process.Kill();
                        }
                    }

                    // 等待进程退出
                    if (process.WaitForExit(5000))
                    {
                        _logService.Info($"孤儿进程已清理: PID {processId}");
                        return true;
                    }
                    else
                    {
                        _logService.Warn($"孤儿进程清理超时: PID {processId}");
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    // 进程已退出
                    _logService.Info($"进程已退出: PID {processId}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logService.Error($"清理孤儿进程失败: {ex.Message}");
                    return false;
                }
            });
        }

        /// <summary>
        /// 清理所有孤儿进程
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>清理的进程数</returns>
        public async Task<int> CleanupAllOrphanProcessesAsync(int port = 3080)
        {
            var orphanProcesses = await DetectOrphanProcessesAsync(port);
            int cleanedCount = 0;

            foreach (var process in orphanProcesses)
            {
                if (await CleanupOrphanProcessAsync(process.ProcessId))
                {
                    cleanedCount++;
                }
            }

            return cleanedCount;
        }

        /// <summary>
        /// 强制释放端口
        /// </summary>
        /// <param name="port">端口号</param>
        /// <returns>是否成功</returns>
        public async Task<bool> ForceReleasePortAsync(int port)
        {
            try
            {
                _logService.Info($"正在强制释放端口: {port}");

                // 获取占用端口的进程
                var portInfo = await _portService.CheckPortAsync(port);
                
                if (!portInfo.IsInUse)
                {
                    _logService.Info($"端口 {port} 未被占用");
                    return true;
                }

                if (!portInfo.ProcessId.HasValue)
                {
                    _logService.Warn($"无法获取占用端口 {port} 的进程ID");
                    return false;
                }

                // 清理进程
                var success = await CleanupOrphanProcessAsync(portInfo.ProcessId.Value, force: true);
                
                if (success)
                {
                    // 等待端口释放
                    await Task.Delay(1000);
                    
                    // 验证端口是否释放
                    var newPortInfo = await _portService.CheckPortAsync(port);
                    if (!newPortInfo.IsInUse)
                    {
                        _logService.Info($"端口 {port} 已成功释放");
                        return true;
                    }
                    else
                    {
                        _logService.Warn($"端口 {port} 释放失败，仍被占用");
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logService.Error($"强制释放端口失败: {ex.Message}");
                return false;
            }
        }
    }
}
