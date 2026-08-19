using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace dsh_deploy.ViewModels
{
    /// <summary>
    /// ViewModel基类
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 触发属性变化
        /// </summary>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设置属性值
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 设置属性值并执行命令
        /// </summary>
        protected bool SetProperty<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
        {
            if (SetProperty(ref field, value, propertyName))
            {
                onChanged?.Invoke();
                return true;
            }
            return false;
        }
    }
}
