using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using dsh_deploy.ViewModels;

namespace dsh_deploy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            
            _viewModel = new MainViewModel(Dispatcher);
            DataContext = _viewModel;
            
            // 初始化托盘服务
            _viewModel.InitializeTray(this);
            
            // 窗口关闭时清理
            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 停止状态定时器
            _viewModel.DshService.StopStatusTimer();
        }

        private void MainWindow_Closed(object? sender, EventArgs e)
        {
            // 清理托盘服务
            _viewModel.DshService.StopStatusTimer();
        }
    }
}
