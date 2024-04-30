using WYW.Modbus.Protocols;

namespace WYW.Modbus
{
    public class ExecutionResult
    {
        /// <summary>
        /// 指令是否执行成功，如果接收到了应答，且应答内容复合要求，则为true，否则为false
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 错误信息，仅在IsSuccess=false时有效
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// 接收到的应答值，仅在IsSuccess=true时有效
        /// </summary>
        public ProtocolBase Response { get; internal set; }
        public static ExecutionResult Success(ProtocolBase response)
        {
            return new ExecutionResult() { IsSuccess = true, Response = response };
        }
        public static ExecutionResult Failed(string errorMessage, ProtocolBase response)
        {
            return new ExecutionResult() { IsSuccess = false, ErrorMessage = errorMessage, Response = response };
        }
        public static ExecutionResult Failed(string errorMessage = null)
        {
            return Failed(errorMessage, null);
        }
    }
}
