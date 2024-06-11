using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using WYW.Modbus.Clients;
using WYW.Modbus.Protocols;

namespace WYW.Modbus
{
    public class Device : DeviceBase
    {
        public Device(ClientBase client, ProtocolType protocol) : base(client, protocol)
        {
        }


        #region 公共方法
        public ExecutionResult SendCommand(string cmd, bool isNeedResponse = true, int maxSendCount = 1, int responseTimeout = 300)
        {
            var obj = GetObject(cmd);
            return SendProtocol(obj, isNeedResponse, maxSendCount, responseTimeout);
        }
        #endregion

        #region 心跳
        public ProtocolBase CreateHeartBeatContent(string cmd)
        {
            return GetObject(cmd);
        }
        #endregion

        internal override ExecutionResult SendProtocol(ProtocolBase obj, bool isNeedResponse, int maxSendCount, int responseTimeout)
        {
            if (!Client.IsEstablished)
            {
                return ExecutionResult.Failed(Properties.Message.CommunicationUnconnected);
            }
            var receiveBuffer = new List<byte>();
            List<ProtocolBase> response = null;
            bool responseReceived = false;
            bool result=false;
            for (int i = 0; i < maxSendCount; i++)
            {
                DateTime startTime = DateTime.Now;
                if (LogEnabled)
                {
                    Logger.WriteLine(GetLogFolder(), $"[{startTime:yyyy-MM-dd HH:mm:ss.fff}] [Tx] {obj.FriendlyText}");
                }
                if (IsPrintDebugInfo)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Tx] {obj.FriendlyText}");
                }
                result = Client.Write(obj.FullBytes);
                if (!result)
                {
                    return ExecutionResult.Failed(Client.ErrorMessage);
                }
                if (!isNeedResponse)
                {
                    return ExecutionResult.Success(null);
                }
                while ((DateTime.Now - startTime).TotalMilliseconds < responseTimeout)
                {
                    result = Client.Read(ref receiveBuffer);
                    if (result)
                    {
                        response = obj.GetResponse(receiveBuffer);
                        if (response.Count >= 1)
                        {
                            responseReceived = true;
                            break;
                        }
                    }
                    Thread.Sleep(1);
                }
                if (responseReceived)
                {
                    break;
                }
            }
            if (responseReceived)
            {
                var responseObject = response.Last();
                if (LogEnabled)
                {
                    Logger.WriteLine(GetLogFolder(), $"[{responseObject.CreateTime:yyyy-MM-dd HH:mm:ss.fff}] [Rx] {responseObject.FriendlyText}");
                }
                if (IsPrintDebugInfo)
                {
                    Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [Rx] {responseObject.FriendlyText} ");
                }
                lastReceiveTime = DateTime.Now;
                IsConnected = true;
                return ExecutionResult.Success(responseObject);
            }
            else
            {
                return ExecutionResult.Failed(Properties.Message.CommunicationTimeout);
            }
        }
        private ProtocolBase GetObject(string cmd)
        {
            switch (Protocol)
            {
                case ProtocolType.AsciiCR:
                    return new AsciiCR(cmd);
                case ProtocolType.AsciiLF:
                    return new AsciiLF(cmd);
                case ProtocolType.AsciiCRLF:
                    return new AsciiCRLF(cmd);
                default:
                    return null;
            }
        }

    }
}
