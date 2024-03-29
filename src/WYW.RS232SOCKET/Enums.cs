﻿namespace WYW.RS232SOCKET
{
    enum CommunicationType
    {
        RS232,
        TCPClient,
        TCPServer,
        UDPClient,
        UDPServer,
        VISA,
    }
    enum StationType
    {
        主站,
        从站,
    }

    enum TextProtocolType
    {
        Hex = 0,
        UTF8 = 1,
        /// <summary>
        /// 文本末尾加回车
        /// </summary>
        UTF8_CR = 2,
        /// <summary>
        /// 文本末尾加回车
        /// </summary>
        UTF8_LF = 3,
        /// <summary>
        /// 文本末尾加回车换行
        /// </summary>
        UTF8_CRLF = 4,
        /// <summary>
        /// 文本末尾加一个字节的累加和
        /// </summary>
        UTF8_CheckSum = 5,
    }
    /// <summary>
    /// 字符串编码类型
    /// </summary>
    enum EncodeType
    {
        ASCII=1,
        UTF8 = 2,
        Unicode=3

    }
}
