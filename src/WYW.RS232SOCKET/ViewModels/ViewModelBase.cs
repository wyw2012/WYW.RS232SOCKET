using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.RS232SOCKET.ViewModels
{
    internal class ViewModelBase:ObservableObject
    {
        private bool isDisableUI;

        /// <summary>
        /// 是否禁用UI
        /// </summary>
        public bool IsDisableUI
        {
            get => isDisableUI;
            set => SetProperty(ref isDisableUI, value);
        }
    }
}
