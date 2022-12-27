using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using WYW.RS232SOCKET.Events;

namespace WYW.RS232SOCKET.Devices
{
   
    class RS232:DeviceBase
    {
        private readonly SerialPort serialPort;

        public RS232(string portName, int baudRate, int parity, int dataBits, int stopBits,int bufferSize)
        {
            serialPort = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits)
            {
                ReadBufferSize = bufferSize,
                WriteBufferSize = bufferSize
            };
        }
        public override void Open()
        {
            if (serialPort != null)
            {
                serialPort.Open();
                serialPort.DataReceived += serialPort_DataReceived;
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("串口已打开。"));
            }
        }
        public override void Close()
        {
            if (serialPort != null)
            {
                serialPort.Close();
                serialPort.DataReceived -= serialPort_DataReceived;
                InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs("串口已关闭。"));
            }
        }
        public override void Write(byte[] content)
        {
            if (serialPort != null)
            {
                InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Send, content));
                try
                {
                    serialPort.Write(content, 0, content.Length);
                }
                catch(Exception ex)
                {
                    InvokeDeviceStatuChangedEvent(new DeviceStautsChangedEventArgs(ex.Message));
                }
                
            }
        }
        private void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (e.EventType == SerialData.Eof)
                return;
            var bytesLength = serialPort.BytesToRead;
            var receivedData = new byte[bytesLength];
            serialPort.Read(receivedData, 0, bytesLength);
            InvokeDeviceDataTransferedMessageEvent(new DeviceDataTransferedEventArgs (MessageType.Receive, receivedData));
        }
    }
}
