using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WYW.RS232SOCKET.Events
{
    /// <summary>
    /// 接收或发送数据事件通知
    /// </summary>
    class DeviceDataTransferedEventArgs :EventArgs
    {
        public DeviceDataTransferedEventArgs (MessageType type,byte[] content)
        {
            CreateTime = DateTime.Now;
            Content = content;
            Type = type;
        }
        public DateTime CreateTime { get;  }
        public byte[] Content { get;  }
        public MessageType Type { get;  }
        public string ToHex()
        {
            if(Content==null || Content.Length==0)
            {
                return "";
            }
            var sb = new StringBuilder();
            for (var i = 0; i < Content.Length; i++)
            {
                sb.Append(Content[i].ToString("X2") + " ");
            }
            string type = Type == MessageType.Receive ? "Rx" : "Tx";
            return $"[{CreateTime:HH:mm:ss.fff}] [{type}] {sb.ToString().Trim()}";
        }
        public string ToASCII()
        {
            if (Content == null || Content.Length == 0)
            {
                return "";
            }
            string type = Type == MessageType.Receive ? "Rx" : "Tx";
            return $"[{CreateTime:HH:mm:ss.fff}] [{type}] {Content.ToUTF8()}";
        }

    }
    public enum MessageType
    {
        Send,
        Receive,
    }
}
