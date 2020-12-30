using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestingOdinSerializerWithoutUnity;

namespace Insight
{
    public class InsightClient : InsightCommon
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(InsightClient));

        public bool AutoReconnect = true;
        private int clientID = -1; //-1 = never connected, 0 = disconnected, 1 = connected
        private int connectionID = 0;

        InsightNetworkConnection insightNetworkConnection;

        public float ReconnectDelayInSeconds = 5f;
        float _reconnectTimer;

        public InsightClient()
        {
            if (AutoStart)
            {
                StartInsight();
            }

            clientID = 0;
            insightNetworkConnection = new InsightNetworkConnection();
            insightNetworkConnection.Initialize(this, networkAddress, clientID, connectionID);
            insightNetworkConnection.SetHandlers(messageHandlers);

            transport.OnClientConnected += OnConnected;
            transport.OnClientDataReceived += HandleBytes;
            transport.OnClientDisconnected += OnDisconnected;
            transport.OnClientError += OnError;

            Task.Run(UpdateLoop);
        }

        //public virtual void Start()
        //{
        //    // Application.runInBackground = true;

        //    if (AutoStart)
        //    {
        //        StartInsight();
        //    }

        //    clientID = 0;
        //    insightNetworkConnection = new InsightNetworkConnection();
        //    insightNetworkConnection.Initialize(this, networkAddress, clientID, connectionID);
        //    insightNetworkConnection.SetHandlers(messageHandlers);

        //    transport.OnClientConnected+=OnConnected;
        //    transport.OnClientDataReceived+=HandleBytes;
        //    transport.OnClientDisconnected+=OnDisconnected;
        //    transport.OnClientError+=OnError;
        //}


        public async Task UpdateLoop()
        {
            while (true)
            {

                CheckConnection();

                CheckCallbackTimeouts();

                await Task.Delay(500);
            }
        }

        //public void Update()
        //{
        //    CheckConnection();

        //    CheckCallbackTimeouts();
        //}

        public void StartInsight(string Address)
        {
            if (string.IsNullOrEmpty(Address))
            {
                logger.LogError("[InsightClient] - Address provided in StartInsight is Null or Empty. Not Starting.");
                return;
            }

            networkAddress = Address;

            StartInsight();
        }

        public override void StartInsight()
        {
            transport.ClientConnect(networkAddress);

            OnStartInsight();

            _reconnectTimer = MyTime.GetElapsedTImeInSeconds + ReconnectDelayInSeconds;
        }

        public void StartInsight(Uri uri)
        {
            transport.ClientConnect(uri);

            OnStartInsight();

            _reconnectTimer = MyTime.GetElapsedTImeInSeconds + ReconnectDelayInSeconds;
        }

        public override void StopInsight()
        {
            transport.ClientDisconnect();
            OnStopInsight();
        }

        private void CheckConnection()
        {
            if (AutoReconnect)
            {
                if (!isConnected && (_reconnectTimer < MyTime.GetElapsedTImeInSeconds))
                {
                    logger.Log("[InsightClient] - Trying to reconnect...");
                    _reconnectTimer = MyTime.GetElapsedTImeInSeconds + ReconnectDelayInSeconds;
                    StartInsight();
                }
            }
        }

        public void Send(byte[] data)
        {
            transport.ClientSend(0, new ArraySegment<byte>(data));
        }

        public void Send<T>(T msg) where T : Message
        {
            Send(msg, null);
        }

        public void Send<T>(T msg, CallbackHandler callback) where T : Message
        {
            if (!transport.ClientConnected())
            {
                logger.LogError("[InsightClient] - Client not connected!");
                return;
            }

            NetworkWriter writer = new NetworkWriter();
            int msgType = GetId(default(T) != null ? typeof(T) : msg.GetType());
            writer.WriteUInt16((ushort) msgType);

            int callbackId = 0;
            if (callback != null)
            {
                callbackId = ++callbackIdIndex; // pre-increment to ensure that id 0 is never used.
                callbacks.Add(callbackId, new CallbackData()
                {
                    callback = callback,
                    timeout = MyTime.GetElapsedTImeInSeconds + callbackTimeout
                });
            }

            writer.WriteInt32(callbackId);

            //Writer<T>.write.Invoke(writer, msg);
            //transport.ClientSend(0, new ArraySegment<byte>(writer.ToArray()));


            #region my custom serializer injection

            var myWriter = new CustomWriter();

            myWriter.WriteData(msg);

            List<byte> Templist = new List<byte>();
            Templist.AddRange(writer.ToArray()); // mirror writer data
            Templist.AddRange(myWriter.GetBuffer); // my writer data
            transport.ClientSend(0, new ArraySegment<byte>(Templist.ToArray()));

            #endregion
        }

        void HandleCallbackHandler(CallbackStatus status, NetworkReader reader)
        {
        }

        void OnConnected()
        {
            if (insightNetworkConnection != null)
            {
                logger.Log("[InsightClient] - Connected to Insight Server");
                connectState = ConnectState.Connected;
            }
            else logger.LogError("Skipped Connect message handling because m_Connection is null.");
        }

        void OnDisconnected()
        {
            connectState = ConnectState.Disconnected;

            StopInsight();
        }

        private void HandleBytes(ArraySegment<byte> data, int i)
        {
            InsightNetworkMessageDelegate msgDelegate;
            NetworkReader
                reader = new NetworkReader(
                    data); // the data is directly passed to the reader and the reader is passed to InsightNetwork message


            if (UnpackMessage(reader, out int msgType))
            {
                int callbackId = reader.ReadInt32();

                #region My custom reader injecting

                byte[] Tempbuffer = new byte[data.Count - reader.Position];

                Array.Copy(data.ToArray(), reader.Position, Tempbuffer, 0, data.Count - reader.Position);


                CustomReader myCustomReader = new CustomReader(Tempbuffer);

                #endregion

                InsightNetworkMessage msg = new InsightNetworkMessage(insightNetworkConnection, callbackId)
                {
                    msgType = msgType,
                    reader = reader,
                    Creader = myCustomReader
                };

                if (callbacks.ContainsKey(callbackId))
                {
                    callbacks[callbackId].callback.Invoke(msg);
                    callbacks.Remove(callbackId);
                }
                else if (messageHandlers.TryGetValue(msgType, out msgDelegate))
                {
                    msgDelegate(msg);
                }
            }
            else
            {
                //NOTE: this throws away the rest of the buffer. Need moar error codes
                logger.LogError("Unknown message ID " + msgType); // + " connId:" + connectionId);
            }
        }

        void OnError(Exception exception)
        {
            // TODO Let's discuss how we will handle errors
            logger.LogException(exception);
        }

        void OnApplicationQuit()
        {
            logger.Log("[InsightClient] Stopping Client");
            StopInsight();
        }

        ////------------Virtual Handlers-------------
        public void OnStartInsight()
        {
            logger.Log("[InsightClient] - Connecting to Insight Server: " + networkAddress);
        }

        public void OnStopInsight()
        {
            logger.Log("[InsightClient] - Disconnecting from Insight Server");
        }
    }
}