using System;
using System.Threading.Tasks;
using System.Windows.Input;
using dsh_deploy.Models;
using dsh_deploy.Services;

namespace dsh_deploy.ViewModels
{
    /// <summary>
    /// 服务控制ViewModel - 负责服务启动、停止、重启等操作
    /// </summary>
    public class ServiceControlViewModel : ViewModelBase
    {
        private readonly IDshService _dshService;
        private bool _isStarting;
        private bool _isStopping;

        public ServiceControlViewModel(IDshService dshService)
        {
            _dshService = dshService;

            // 初始化命令
            StartCommand = new AsyncRelayCommand(StartServiceAsync, CanStartService);
            StopCommand = new AsyncRelayCommand(StopServiceAsync, CanStopService);
            RestartCommand = new AsyncRelayCommand(RestartServiceAsync, CanRestartService);
            ClearPortCommand = new AsyncRelayCommand(ClearPortConflictAsync);
        }

        #region 属性

        /// <summary>
        /// 是否正在启动
        /// </summary>
        public bool IsStarting
        {
            get => _isStarting;
            set => SetProperty(ref _isStarting, value);
        }

        /// <summary>
        /// 是否正在停止
        /// </summary>
        public bool IsStopping
        {
            get => _isStopping;
            set => SetProperty(ref _isStopping, value);
        }

        #endregion

        #region 命令

        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }
        public ICommand RestartCommand { get; }
        public ICommand ClearPortCommand { get; }

        #endregion

        #region 命令实现

        private async Task StartServiceAsync()
        {
            IsStarting = true;
            try
            {
                await _dshService.StartServiceAsync();
            }
            finally
            {
                IsStarting = false;
            }
        }

        private bool CanStartService()
        {
            return !IsStarting && !IsStopping && _dshService.CurrentStatus.State != ServiceState.Running;
        }

        private async Task StopServiceAsync()
        {
            IsStopping = true;
            try
            {
                await _dshService.StopServiceAsync();
            }
            finally
            {
                IsStopping = false;
            }
        }

        private bool CanStopService()
        {
            return !IsStarting && !IsStopping && _dshService.CurrentStatus.State == ServiceState.Running;
        }

        private async Task RestartServiceAsync()
        {
            IsStarting = true;
            IsStopping = true;
            try
            {
                await _dshService.RestartServiceAsync();
            }
            finally
            {
                IsStarting = false;
                IsStopping = false;
            }
        }

        private bool CanRestartService()
        {
            return !IsStarting && !IsStopping;
        }

        private async Task ClearPortConflictAsync()
        {
            await _dshService.ClearPortConflictAsync();
        }

        #endregion
    }
}
