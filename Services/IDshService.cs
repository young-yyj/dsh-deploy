using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using dsh_deploy.Models;

namespace dsh_deploy.Services
{
    /// <summary>
    /// DSH服务接口
    /// </summary>
    public interface IDshService
    {
        /// <summary>
        /// 日志服务
        /// </summary>
        LogService LogService { get; }

        /// <summary>
        /// 配置服务
        /// </summary>
        ConfigService ConfigService { get; }

        /// <summary>
        /// 端口服务
        /// </summary>
        PortService PortService { get; }

        /// <summary>
        /// 健康检查服务
        /// </summary>
        HealthCheckService? HealthCheckService { get; }

        /// <summary>
        /// 更新服务
        /// </summary>
        UpdateService? UpdateService { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        ServiceStatus CurrentStatus { get; }

        /// <summary>
        /// 状态变化事件
        /// </summary>
        event EventHandler<ServiceStatus>? StatusChanged;

        /// <summary>
        /// 初始化服务
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// 刷新状态
        /// </summary>
        Task<ServiceStatus> RefreshStatusAsync(bool forceRefresh = false);

        /// <summary>
        /// 启动服务
        /// </summary>
        Task<bool> StartServiceAsync();

        /// <summary>
        /// 停止服务
        /// </summary>
        Task<bool> StopServiceAsync();

        /// <summary>
        /// 重启服务
        /// </summary>
        Task<bool> RestartServiceAsync();

        /// <summary>
        /// 清理端口冲突
        /// </summary>
        Task<bool> ClearPortConflictAsync();

        /// <summary>
        /// 获取进程列表
        /// </summary>
        Task<List<ProcessInfo>> GetProcessesAsync();

        /// <summary>
        /// 启动状态定时器
        /// </summary>
        void StartStatusTimer();

        /// <summary>
        /// 停止状态定时器
        /// </summary>
        void StopStatusTimer();
    }
}
