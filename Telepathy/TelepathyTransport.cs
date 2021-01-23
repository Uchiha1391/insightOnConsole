// wraps Telepathy for use as HLAPI TransportLayer

using System;
using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Threading.Tasks;
using Insight;

// Replaced by Kcp November 2020
namespace Mirror
{
    //[Obsolete("This transport has been replaced by the Kcp Transport and will be removed in a future release.")]
    public class TelepathyTransport : Transport
    {
        // scheme used by this transport
        // "tcp4" means tcp with 4 bytes header, network byte order
        public const string Scheme = "tcp4";

        public ushort port = 7001;

        public bool NoDelay = true;

        public int serverMaxMessageSize = 16 * 1024;

        public int serverMaxReceivesPerTick = 10000;
        public int clientMaxMessageSize = 16 * 1024;

        public int clientMaxReceivesPerTick = 1000;


        protected Telepathy.Client client = new Telepathy.Client();
        protected Telepathy.Server server = new Telepathy.Server();


        public TelepathyTransport()
        {
            // configure

            client.NoDelay = NoDelay;
            client.MaxMessageSize = clientMaxMessageSize;
            server.NoDelay = NoDelay;
            server.MaxMessageSize = serverMaxMessageSize;
            Task.Run(UpdateLoop);
        }

        //void Awake()
        //{
        //    // configure
        //    client.NoDelay = NoDelay;
        //    client.MaxMessageSize = clientMaxMessageSize;
        //    server.NoDelay = NoDelay;
        //    server.MaxMessageSize = serverMaxMessageSize;

        //}

        public override bool Available()
        {
            // C#'s built in TCP sockets run everywhere except on WebGL
            return true;
        }

        // client
        public override bool ClientConnected() => client.Connected;
        public override void ClientConnect(string address) => client.Connect(address, port);

        public override void ClientConnect(Uri uri)
        {
            if (uri.Scheme != Scheme)
                throw new ArgumentException($"Invalid url {uri}, use {Scheme}://host:port instead", nameof(uri));

            int serverPort = uri.IsDefaultPort ? port : uri.Port;
            client.Connect(uri.Host, serverPort);
        }

        public override void ClientSend(int channelId, ArraySegment<byte> segment)
        {
            // telepathy doesn't support allocation-free sends yet.
            // previously we allocated in Mirror. now we do it here.
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);
            client.Send(data);
        }

        bool ProcessClientMessage()
        {
            if (client.GetNextMessage(out Telepathy.Message message))
            {
                switch (message.eventType)
                {
                    case Telepathy.EventType.Connected:
                        OnClientConnected.Invoke();
                        break;
                    case Telepathy.EventType.Data:
                        OnClientDataReceived.Invoke(new ArraySegment<byte>(message.data), Channels.DefaultReliable);
                        break;
                    case Telepathy.EventType.Disconnected:
                        OnClientDisconnected.Invoke();
                        break;
                    default:
                        // TODO:  Telepathy does not report errors at all
                        // it just disconnects,  should be fixed
                        OnClientDisconnected.Invoke();
                        break;
                }

                return true;
            }

            return false;
        }

        public override void ClientDisconnect() => client.Disconnect();

        // IMPORTANT: set script execution order to >1000 to call Transport's
        //            LateUpdate after all others. Fixes race condition where
        //            e.g. in uSurvival Transport would apply Cmds before
        //            ShoulderRotation.LateUpdate, resulting in projectile
        //            spawns at the point before shoulder rotation.
        //public void LateUpdate()
        //{
        //    // note: we need to check enabled in case we set it to false
        //    // when LateUpdate already started.
        //    // (https://github.com/vis2k/Mirror/pull/379)
        //    if (!enabled)
        //        return;

        //    // process a maximum amount of client messages per tick
        //    for (int i = 0; i < clientMaxReceivesPerTick; ++i)
        //    {
        //        // stop when there is no more message
        //        if (!ProcessClientMessage())
        //        {
        //            break;
        //        }

        //        // Some messages can disable transport
        //        // If this is disabled stop processing message in queue
        //        if (!enabled)
        //        {
        //            break;
        //        }
        //    }

        //    // process a maximum amount of server messages per tick
        //    for (int i = 0; i < serverMaxReceivesPerTick; ++i)
        //    {
        //        // stop when there is no more message
        //        if (!ProcessServerMessage())
        //        {
        //            break;
        //        }

        //        // Some messages can disable transport
        //        // If this is disabled stop processing message in queue
        //        if (!enabled)
        //        {
        //            break;
        //        }
        //    }
        //}


        public async Task UpdateLoop()
        {
            while (true)
            {
                // process a maximum amount of client messages per tick
                for (int i = 0; i < clientMaxReceivesPerTick; ++i)
                {
                    // stop when there is no more message
                    if (!ProcessClientMessage())
                    {
                        break;
                    }

                    // Some messages can disable transport
                    // If this is disabled stop processing message in queue
                    //if (!enabled)
                    //{
                    //    break;
                    //}
                }


                await Task.Delay(500);
            }
        }

        public override Uri ServerUri()
        {
            UriBuilder builder = new UriBuilder();
            builder.Scheme = Scheme;
            builder.Host = Dns.GetHostName();
            builder.Port = port;
            return builder.Uri;
        }

        // server
        public override bool ServerActive() => server.Active;
        public override void ServerStart() => server.Start(port);

        public override void ServerSend(int connectionId, int channelId, ArraySegment<byte> segment)
        {
            // telepathy doesn't support allocation-free sends yet.
            // previously we allocated in Mirror. now we do it here.
            byte[] data = new byte[segment.Count];
            Array.Copy(segment.Array, segment.Offset, data, 0, segment.Count);

            // send
            server.Send(connectionId, data);
        }

        public bool ProcessServerMessage()
        {
            if (server.GetNextMessage(out Telepathy.Message message))
            {
                switch (message.eventType)
                {
                    case Telepathy.EventType.Connected:
                        OnServerConnected.Invoke(message.connectionId);
                        break;
                    case Telepathy.EventType.Data:
                        OnServerDataReceived.Invoke(message.connectionId, new ArraySegment<byte>(message.data),
                            Channels.DefaultReliable);
                        break;
                    case Telepathy.EventType.Disconnected:
                        OnServerDisconnected.Invoke(message.connectionId);
                        break;
                    default:
                        // TODO handle errors from Telepathy when telepathy can report errors
                        OnServerDisconnected.Invoke(message.connectionId);
                        break;
                }

                return true;
            }

            return false;
        }

        public override bool ServerDisconnect(int connectionId) => server.Disconnect(connectionId);

        public override string ServerGetClientAddress(int connectionId)
        {
            try
            {
                return server.GetClientAddress(connectionId);
            }
            catch (SocketException)
            {
                // using server.listener.LocalEndpoint causes an Exception
                // in UWP + Unity 2019:
                //   Exception thrown at 0x00007FF9755DA388 in UWF.exe:
                //   Microsoft C++ exception: Il2CppExceptionWrapper at memory
                //   location 0x000000E15A0FCDD0. SocketException: An address
                //   incompatible with the requested protocol was used at
                //   System.Net.Sockets.Socket.get_LocalEndPoint ()
                // so let's at least catch it and recover
                return "unknown";
            }
        }

        public override void ServerStop() => server.Stop();

        // common
        public override void SetPortNumber(ushort portNumber)
        {
            port = portNumber;
        }

        public override void Shutdown()
        {
            Console.WriteLine("TelepathyTransport Shutdown()");
            client.Disconnect();
            server.Stop();
        }

        public override int GetMaxPacketSize(int channelId)
        {
            return serverMaxMessageSize;
        }

        public override string ToString()
        {
            if (server.Active && server.listener != null)
            {
                // printing server.listener.LocalEndpoint causes an Exception
                // in UWP + Unity 2019:
                //   Exception thrown at 0x00007FF9755DA388 in UWF.exe:
                //   Microsoft C++ exception: Il2CppExceptionWrapper at memory
                //   location 0x000000E15A0FCDD0. SocketException: An address
                //   incompatible with the requested protocol was used at
                //   System.Net.Sockets.Socket.get_LocalEndPoint ()
                // so let's use the regular port instead.
                return "Telepathy Server port: " + port;
            }
            else if (client.Connecting || client.Connected)
            {
                return "Telepathy Client ip: " + client.client.Client.RemoteEndPoint;
            }

            return "Telepathy (inactive/disconnected)";
        }
    }
}