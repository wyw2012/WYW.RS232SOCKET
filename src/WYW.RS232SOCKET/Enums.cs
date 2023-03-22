namespace WYW.RS232SOCKET
{

    enum CommunicationType
    {
        RS232,
        TCPClient,
        TCPServer,
        UDPClient,
        UDPServer,
    }
    enum ModbusType
    {
        主站,
        从站,
    }

    enum ProtocolType
    {
        Hex = 0,
        UTF8 = 1,
        /// <summary>
        /// 文本末尾加回车
        /// </summary>
        UTF8_CR = 2,
        /// <summary>
        /// 文本末尾加回车换行
        /// </summary>
        UTF8_CRLF = 3,
        /// <summary>
        /// 文本末尾加一个字节的累加和
        /// </summary>
        UTF8_CheckSum = 4,
    }
    public enum RegisterValueType
    {
        UInt16,
        UInt32,
        Float,
        Double,
    }
    public enum RegisterWriteType
    {
        读写,
        只读,
    }
}
