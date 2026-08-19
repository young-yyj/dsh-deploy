using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using dsh_deploy.Models;
using dsh_deploy.Services;

namespace dsh_deploy.ViewModels
{
    /// <summary>
    /// 日志ViewModel - 负责日志显示和过滤
    /// </summary>
    public class LogViewModel : ViewModelBase
    {
        private readonly LogService _logService;
        private string _filterKeyword = string.Empty;
        private LogLevel _filterLevel = LogLevel.DEBUG;

        public LogViewModel(LogService logService)
        {
            _logService = logService;

            // 初始化命令
            ClearLogCommand = new RelayCommand(ClearLogs);
            ApplyFilterCommand = new RelayCommand(ApplyFilter);
            ResetFilterCommand = new RelayCommand(ResetFilter);
        }

        #region 属性

        /// <summary>
        /// 日志集合
        /// </summary>
        public ObservableCollection<LogEntry> Logs => _logService.Logs;

        /// <summary>
        /// 过滤后的日志集合
        /// </summary>
        public ObservableCollection<LogEntry> FilteredLogs => _logService.FilteredLogs;

        /// <summary>
        /// 过滤关键词
        /// </summary>
        public string FilterKeyword
        {
            get => _filterKeyword;
            set
            {
                SetProperty(ref _filterKeyword, value);
                ApplyFilter();
            }
        }

        /// <summary>
        /// 过滤级别
        /// </summary>
        public LogLevel FilterLevel
        {
            get => _filterLevel;
            set
            {
                SetProperty(ref _filterLevel, value);
                ApplyFilter();
            }
        }

        #endregion

        #region 命令

        public ICommand ClearLogCommand { get; }
        public ICommand ApplyFilterCommand { get; }
        public ICommand ResetFilterCommand { get; }

        #endregion

        #region 命令实现

        private void ClearLogs()
        {
            _logService.Clear();
        }

        private void ApplyFilter()
        {
            _logService.SetFilter(_filterLevel, _filterKeyword);
        }

        private void ResetFilter()
        {
            _filterKeyword = string.Empty;
            _filterLevel = LogLevel.DEBUG;
            OnPropertyChanged(nameof(FilterKeyword));
            OnPropertyChanged(nameof(FilterLevel));
            _logService.ResetFilter();
        }

        #endregion
    }
}
