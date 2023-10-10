using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.Communication.Protocol;
using WYW.Communication.TransferLayer;

namespace WYW.Communication.ApplicationlLayer
{
    public class SCPIDevice : Device
    {
        public SCPIDevice(TransferBase client, ProtocolType protocolType) : base(client)
        {
            ProtocolType=protocolType;
        }
        #region IEEE-488通用指令

        /// <summary>
        /// 获取设备识别码
        /// <para>发送：*IDN?</para>
        /// <para>返回：Itech Electronics,IT7960-350-360,987654321987654321,000.002.128,1.12.R,16.R</para>
        /// </summary>
        /// <returns></returns>
        public ExecutionResult GetDeviceSN()
        {
            if (IsDebugModel)
            {
                return ExecutionResult.Success(null);
            }
            return SendCommand("*IDN", OperationType.Read);
        }
        #endregion

        #region 内部方法
        /// <summary>
        /// 查询指令
        /// </summary>
        /// <param name="cmd">指令字符串</param>
        /// <param name="value">返回值数组</param>
        /// <returns></returns>
        protected ExecutionResult QueryValue(string cmd, out double[] value)
        {
            value = new double[1];
            if (!cmd.EndsWith("?"))
            {
                cmd += "?";
            }
            var result = SendProtocol(GetPackage(cmd));
            if (result.IsSuccess)
            {
                try
                {
                    value = result.Response.FriendlyText.Trim().Split(',').Select(x => double.Parse(x)).ToArray();
                }
                catch (Exception ex)
                {
                    //DeviceStatus = DeviceStatus.Warning;
                    result.IsSuccess = false;
                    result.ErrorMessage = ex.Message;
                }
            }
            return result;
        }
    

        protected ExecutionResult SendCommand(string cmd, OperationType operation)
        {

            if (operation == OperationType.Read)
            {
                if (!cmd.EndsWith("?"))
                {
                    cmd += "?";
                }
                return SendProtocol(GetPackage(cmd), true);
            }
            else
            {
                return SendProtocol(GetPackage(cmd), false);
            }
        }

        #endregion
        private ProtocolBase GetPackage(string cmd)
        {
            switch(ProtocolType)
            {
                case ProtocolType.AsciiCR:
                    return new AsciiCR(cmd);
                case ProtocolType.AsciiLF:
                    return new AsciiLF(cmd);
                case ProtocolType.AsciiCRLF:
                    return new AsciiCRLF(cmd);
            }
            return null;
        }
    }
}
