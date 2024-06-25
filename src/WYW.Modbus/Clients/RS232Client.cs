using System;
using System.Collections.Generic;
using System.IO.Ports;

namespace WYW.Modbus.Clients
{
    public class RS232Client : ClientBase
    {
        private readonly SerialPort serialPort;
        public RS232Client(string portName, int baudRate = 9600, int parity = 0, int dataBits = 8, int stopBits = 1, int writeBufferSize = 4096, int receiveBufferSize = 4096)
        {
            serialPort = new SerialPort(portName, baudRate, (Parity)parity, dataBits, (StopBits)stopBits)
            {
                ReadBufferSize = receiveBufferSize,
                WriteBufferSize = writeBufferSize
            };
        }

        public override void Open()
        {
            if (serialPort.IsOpen != true)
            {
                serialPort.Open();
                IsEstablished = IsOpen = true;
            }
        }
        public override void Close()
        {
            serialPort.Close();
            IsEstablished = IsOpen = false;
        }
        public override void ClearReceiveBuffer()
        {
            serialPort?.DiscardInBuffer();
        }
        public override bool Read(ref List<byte> receiveBuffer)
        {
            if (!serialPort.IsOpen)
            {
                ErrorMessage = "串口未打开，请先调用Open方法";
                return false;
            }
            int bytesCount = serialPort.BytesToRead;
            if (bytesCount > 0)
            {
                byte[] buffer = new byte[bytesCount];
                serialPort.Read(buffer, 0, bytesCount);
                receiveBuffer.AddRange(buffer);
            }
            return true;
        }

        public override bool Write(byte[] buffer)
        {
            if (!serialPort.IsOpen)
            {
                ErrorMessage = "串口未打开，请先调用Open方法";
                return false;
            }
            lock(this)
            {
                try
                {
                    serialPort.Write(buffer, 0, buffer.Length);
                }
                catch(Exception ex)
                {
                    ErrorMessage = ex.Message;
                    return false;
                }
                return true;
            }
        }
    }
}
