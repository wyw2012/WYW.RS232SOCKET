using System.ComponentModel;

namespace WYW.Modbus
{
    public enum RegisterType
    {
        [Description("保持寄存器")]
        HoldingRegister,
        [Description("输入寄存器")]
        InputRegister,
        [Description("离散量输入")]
        DiscreteInputRegister,
        [Description("线圈")]
        Coil,
    }
}
