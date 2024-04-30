using System.ComponentModel;

namespace WYW.Modbus
{
    public enum RegisterWriteType
    {
        [Description("读写")]
        RW,
        [Description("只读")]
        R,
        [Description("只写")]
        W
    }
}
