using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestingOdinSerializerWithoutUnity;

namespace Insight
{
    public interface NetworkMessage
    {
    }

    public class Message : NetworkMessage
    {
        public CallbackStatus Status = CallbackStatus.Default;
    }


    public class ChatMsg : Message
    {
        public short Channel; //0 for global
        public string Origin; //This could be controlled by the server.
        public string Target; //Used for private chat
        public string Data;
    }
}