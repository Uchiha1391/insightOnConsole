using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Insight;

namespace Insight
{
    public class ChatClient
    {
        public ChatClient(InsightClient clientSocket)
        {
            _clientSocket = clientSocket;
        }

        static readonly ILogger Logger = LogFactory.GetLogger(typeof(ChatClient));
        public string ChatLog;
        InsightClient _clientSocket;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Origin">its just a placeholder for player name, id etc</param>
        /// <param name="Data"></param>
        public void SendChatMsg(string Origin, string Data)
        {
            _clientSocket.Send(new ChatMsg()
            {
                Origin = Origin,
                Data = Data
            });
        }

        void RegisterHandlers()
        {
            _clientSocket.RegisterHandler<ChatMsg>(HandleChatMsg);
        }

        public void HandleChatMsg(InsightNetworkMessage netMsg)
        {
            Logger.Log("[InsightClient] - HandleChatMsg()");

            ChatMsg message = netMsg.ReadMessage<ChatMsg>();

            ChatLog += message.Origin + ": " + message.Data + "\n";

            Console.WriteLine(ChatLog);
        }

        //Has server control the username (MasterServer Example)
        public void SendChatMsg(string data)
        {
            _clientSocket.Send(new ChatMsg() {Data = data});
        }
    }
}