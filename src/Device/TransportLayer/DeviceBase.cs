
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using WYW.RS232SOCKET.Events;

namespace WYW.RS232SOCKET.Devices
{
    abstract class DeviceBase
    {

        public abstract void Open();
        public abstract void Close();
        public abstract void Write(byte[] content); 
        public delegate void DeviceStatusChangedEventHandler(object sender, DeviceStautsChangedEventArgs e);
        public event DeviceStatusChangedEventHandler DeviceStatusChangedEvent;

        public delegate void CommunicationMessageEventHandler(object sender, DeviceDataTransferedEventArgs  e);
        public event CommunicationMessageEventHandler CommunicationMessageEvent;

        protected void InvokeDeviceDataTransferedMessageEvent(DeviceDataTransferedEventArgs  e)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                CommunicationMessageEvent?.Invoke(this, e);
            });
        }
        protected void InvokeDeviceStatuChangedEvent(DeviceStautsChangedEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                DeviceStatusChangedEvent?.Invoke(this, e);
            });
        }

    }
}
