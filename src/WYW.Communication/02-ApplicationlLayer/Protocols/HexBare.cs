using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WYW.Communication.Protocol
{
    /// <summary>
    /// 无协议，Byte数组
    /// </summary>
    public class HexBare : ProtocolBase
    {
        #region 子类必须实现的
 
        internal HexBare(byte[] fullBytes)
        {
            FullBytes = fullBytes;
            Content = fullBytes;
            FriendlyText = fullBytes.ToHexString();
        }
        internal static List<ProtocolBase> Analyse(List<byte> buffer)
        {
            List<ProtocolBase> result = new List<ProtocolBase>();
            result.Add(new HexBare(buffer.ToArray()));
            buffer.Clear();
            return result;
        }

        #endregion
    }
}
