using WYW.Communication.Protocol;
using WYW.Communication;
namespace WYW.RS232SOCKET.Models
{
    internal class NormalScriptModel:ObservableObject
    {

        private int id;

        /// <summary>
        /// 
        /// </summary>
        public int ID { get => id; set => SetProperty(ref id, value); }

        private string command;

        /// <summary>
        /// 
        /// </summary>
        public string Command { get => command; set => SetProperty(ref command, value); }

        private int sleepTime;

        /// <summary>
        /// 
        /// </summary>
        public int SleepTime { get => sleepTime; set => SetProperty(ref sleepTime, value); }

        private bool isNeedResponse;

        /// <summary>
        /// 
        /// </summary>
        public bool IsNeedResponse { get => isNeedResponse; set => SetProperty(ref isNeedResponse, value); }


        private string responseContent;

        /// <summary>
        /// 
        /// </summary>
        public string ResponseContent { get => responseContent; set => SetProperty(ref responseContent, value); }

    }
}
