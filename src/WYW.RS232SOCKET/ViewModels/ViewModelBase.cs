using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WYW.UI.Controls;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class ViewModelBase:ObservableObject
    {
        public ViewModelBase()
        {
            BindingCommand();
        }
        private bool isRunning;

        /// <summary>
        /// 是否正在运行
        /// </summary>
        public bool IsRunning
        {
            get => isRunning;
            set => SetProperty(ref isRunning, value);
        }
        /// <summary>
        /// 绑定指令与方法
        /// </summary>
        protected virtual void BindingCommand()
        {

        }
        protected virtual void ProcessWhenTaskCompleted(Task task)
        {
            IsRunning = false;
            if (task.Exception != null && task.Exception.InnerException != null)
            {
                MessageBoxWindow.Error(task.Exception.InnerException.Message);
            }
        }
    }
}
