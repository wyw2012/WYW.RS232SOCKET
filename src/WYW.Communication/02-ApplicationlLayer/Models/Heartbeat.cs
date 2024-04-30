using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WYW.Communication.Protocol;

namespace WYW.Communication.ApplicationlLayer
{
    /// <summary>
    /// 心跳配置
    /// </summary>
    public class Heartbeat
    {
        /// <summary>
        /// 是否使能心跳
        /// </summary>
        public bool IsEnabled { get; set; }
        /// <summary>
        /// 超时时间，单位：ms，默认300ms
        /// </summary>
        public int Timeout { get; set; } = 300;
        /// <summary>
        /// 最大重试次数，默认为3
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;
        /// <summary>
        /// 心跳内容
        /// </summary>
        public ProtocolBase Content { get; set; }

        /// <summary>
        /// 心跳间隔时间，单位：s，默认5s
        /// </summary>
        public double IntervalSeconds { get; set; } = 5;
    }
}
