using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace dsh_deploy.Services
{
    /// <summary>
    /// 服务定位器 - 简单的依赖注入容器
    /// </summary>
    public class ServiceLocator
    {
        private static ServiceLocator? _instance;
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dispatcher _dispatcher;

        private ServiceLocator(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static ServiceLocator Instance => _instance ?? throw new InvalidOperationException("ServiceLocator未初始化");

        /// <summary>
        /// 初始化服务定位器
        /// </summary>
        public static void Initialize(Dispatcher dispatcher)
        {
            _instance = new ServiceLocator(dispatcher);
            _instance.RegisterServices();
        }

        /// <summary>
        /// 注册所有服务
        /// </summary>
        private void RegisterServices()
        {
            // 注册基础服务
            var logService = new LogService(_dispatcher);
            Register(logService);

            var configService = new ConfigService(logService);
            Register(configService);

            var portService = new PortService(logService);
            Register(portService);

            var processService = new ProcessService(logService);
            Register(processService);

            // 注册核心服务
            var dshService = new DshService(_dispatcher);
            Register<IDshService>(dshService);
            Register(dshService);

            // 注册附加服务
            var orphanProcessService = new OrphanProcessService(logService, portService);
            Register(orphanProcessService);

            var crashRecoveryService = new CrashRecoveryService(logService, dshService, _dispatcher);
            Register(crashRecoveryService);

            var updateService = new UpdateService(logService, configService);
            Register(updateService);

            var healthCheckService = new HealthCheckService(logService, dshService);
            Register(healthCheckService);

            var dashboardService = new DashboardService(logService, dshService, _dispatcher);
            Register(dashboardService);
        }

        /// <summary>
        /// 注册服务
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"服务 {typeof(T).Name} 未注册");
        }

        /// <summary>
        /// 尝试获取服务
        /// </summary>
        public T? TryGet<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            return null;
        }

        /// <summary>
        /// 检查服务是否已注册
        /// </summary>
        public bool IsRegistered<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// 释放所有服务
        /// </summary>
        public void DisposeAll()
        {
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _services.Clear();
        }
    }
}
