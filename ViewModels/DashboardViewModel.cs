using System;
using System.Threading.Tasks;
using System.Windows.Input;
using dsh_deploy.Models;
using dsh_deploy.Services;

namespace dsh_deploy.ViewModels
{
    /// <summary>
    /// 仪表盘ViewModel - 负责仪表盘数据显示
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly DashboardService _dashboardService;
        private DashboardData _dashboardData;

        public DashboardViewModel(DashboardService dashboardService)
        {
            _dashboardService = dashboardService;
            _dashboardData = new DashboardData();

            // 订阅数据更新事件
            _dashboardService.DataUpdated += OnDataUpdated;

            // 初始化命令
            RefreshCommand = new AsyncRelayCommand(RefreshAsync);
            RunDiagnosticsCommand = new AsyncRelayCommand(RunDiagnosticsAsync);
        }

        #region 属性

        /// <summary>
        /// 仪表盘数据
        /// </summary>
        public DashboardData DashboardData
        {
            get => _dashboardData;
            set => SetProperty(ref _dashboardData, value);
        }

        #endregion

        #region 命令

        public ICommand RefreshCommand { get; }
        public ICommand RunDiagnosticsCommand { get; }

        #endregion

        #region 命令实现

        private async Task RefreshAsync()
        {
            await _dashboardService.RefreshAsync();
        }

        private async Task RunDiagnosticsAsync()
        {
            // 诊断逻辑在MainViewModel中实现
            await Task.CompletedTask;
        }

        #endregion

        #region 事件处理

        private void OnDataUpdated(object? sender, DashboardData data)
        {
            DashboardData = data;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 启动自动刷新
        /// </summary>
        public void StartAutoRefresh(int intervalSeconds = 5)
        {
            _dashboardService.StartAutoRefresh(intervalSeconds);
        }

        /// <summary>
        /// 停止自动刷新
        /// </summary>
        public void StopAutoRefresh()
        {
            _dashboardService.StopAutoRefresh();
        }

        #endregion
    }
}
