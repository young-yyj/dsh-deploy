using System.Configuration;
using System.Data;
using System.Windows;
using dsh_deploy.Services;

namespace dsh_deploy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 初始化服务定位器
            ServiceLocator.Initialize(Dispatcher);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 释放所有服务
            ServiceLocator.Instance.DisposeAll();
            
            base.OnExit(e);
        }
    }

}
