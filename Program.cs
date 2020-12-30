using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Insight;
using Newtonsoft.Json;


namespace TestingOdinSerializerWithoutUnity
{
    class Program
    {
        static async Task Main(string[] args)
        {

            Console.WriteLine(" click to satart");
            Console.ReadLine();

            Task.Run(MyTime.RunTimeLoop);

            var InsightClientSocket = new InsightClient();

            Task.Run((() => ChatMessageSend(InsightClientSocket)));


            Console.ReadLine();


        }

        private async static Task ChatMessageSend(InsightClient InsightClientSocket)
        {
            await Task.Delay(3000);

            var ChatClient = new ChatClient(InsightClientSocket);
            ChatClient.SendChatMsg("karan","talking from console application");
            Console.WriteLine("message sent");

        }
    }
}
