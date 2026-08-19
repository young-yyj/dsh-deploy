using System.ComponentModel;
using System.Windows;
using dsh_deploy.ViewModels;
using MessageBox = System.Windows.MessageBox;

namespace dsh_deploy
{
    /// <summary>
    /// dsh-web-ui 升级窗口
    /// </summary>
    public partial class UpgradeWindow : Window
    {
        private readonly WebUiUpdateViewModel _viewModel;

        public UpgradeWindow()
        {
            InitializeComponent();

            _viewModel = new WebUiUpdateViewModel { Window = this };
            DataContext = _viewModel;

            // 输出自动滚动到底部
            _viewModel.Output.CollectionChanged += (_, _) =>
            {
                if (OutputList.Items.Count > 0)
                {
                    OutputList.ScrollIntoView(OutputList.Items[OutputList.Items.Count - 1]);
                }
            };
        }

        private void UpgradeWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!_viewModel.IsUpgrading)
            {
                return;
            }

            // 升级中关闭窗口需二次确认，确认后取消升级
            var result = MessageBox.Show(
                "升级正在进行中，关闭窗口将取消升级。\n\n确定取消并关闭吗？",
                "升级进行中", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _viewModel.CancelUpgrade();
        }
    }
}
