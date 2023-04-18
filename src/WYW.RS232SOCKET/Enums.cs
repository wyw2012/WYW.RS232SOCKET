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
        Int16,
        UInt32,
        Int32,
        UInt64,
        Int64,
        Float,
        Double,
    }
    public enum RegisterWriteType
    {
        读写,
        只读,
    }
    /// <summary>
    /// 对齐方式
    /// </summary>
    public enum RegisterEndianType
    {
        /// <summary>
        /// 高位在前，例如：4 3 2 1
        /// </summary>
        大端模式,
        /// <summary>
        /// 高位在后，例如：1 2 3 4
        /// </summary>
        小端模式,
        /// <summary>
        /// 2 1 4 3
        /// </summary>
        大端反转,
        /// <summary>
        ///  3 4 1 2
        /// </summary>
        小端反转
    }

}
