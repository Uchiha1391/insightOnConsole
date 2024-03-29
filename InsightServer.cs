﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Insight
{
    public class InsightServer : InsightCommon
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(InsightServer));

        protected int serverHostId = -1; //-1 = never connected, 0 = disconnected, 1 = connected

        protected Dictionary<int, InsightNetworkConnection> connections =
            new Dictionary<int, InsightNetworkConnection>();

        protected List<SendToAllFinishedCallbackData> sendToAllFinishedCallbacks =
            new List<SendToAllFinishedCallbackData>();


        public InsightServer()
        {
            transport.OnServerConnected += HandleConnect;
            transport.OnServerDisconnected += HandleDisconnect;
            transport.OnServerDataReceived += HandleData;
            transport.OnServerError += OnError;

            if (AutoStart)
            {
                StartInsight();
            }

            Task.Run(UpdateLoop);
        }

        //public virtual void Start()
        //{
        //    //Application.runInBackground = true;

        //    transport.OnServerConnected += HandleConnect;
        //    transport.OnServerDisconnected += HandleDisconnect;
        //    transport.OnServerDataReceived += HandleData;
        //    transport.OnServerError += OnError;

        //    if (AutoStart)
        //    {
        //        StartInsight();
        //    }
        //}


        public async Task UpdateLoop()
        {
            while (true)
            {
                CheckCallbackTimeouts();


                await Task.Delay(500);
            }
        }

        //public virtual void Update()
        //{
        //    CheckCallbackTimeouts();
        //}

        public override void StartInsight()
        {
            logger.Log("[InsightServer] - Start");
            transport.ServerStart();
            serverHostId = 0;

            connectState = ConnectState.Connected;

            OnStartInsight();
        }

        public override void StopInsight()
        {
            connections.Clear();

            // stop the server when you don't need it anymore
            transport.ServerStop();
            serverHostId = -1;

            connectState = ConnectState.Disconnected;

            OnStopInsight();
        }

        void HandleConnect(int connectionId)
        {
            logger.Log("[InsightServer] - Client connected connectionID: " + connectionId, this);

            // get ip address from connection
            string address = GetConnectionInfo(connectionId);

            // add player info
            InsightNetworkConnection conn = new InsightNetworkConnection();
            conn.Initialize(this, address, serverHostId, connectionId);
            AddConnection(conn);
        }

        void HandleDisconnect(int connectionId)
        {
            logger.Log("[InsightServer] - Client disconnected connectionID: " + connectionId, this);

            InsightNetworkConnection conn;
            if (connections.TryGetValue(connectionId, out conn))
            {
                conn.Disconnect();
                RemoveConnection(connectionId);
            }
        }

        void HandleData(int connectionId, ArraySegment<byte> data, int i)
        {
            NetworkReader reader = new NetworkReader(data);
            short msgType = reader.ReadInt16();
            int callbackId = reader.ReadInt32();
            InsightNetworkConnection insightNetworkConnection;
            if (!connections.TryGetValue(connectionId, out insightNetworkConnection))
            {
                logger.LogError("HandleData: Unknown connectionId: " + connectionId, this);
                return;
            }

            if (callbacks.ContainsKey(callbackId))
            {
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
                callbacks[callbackId].callback.Invoke(msg);
                callbacks.Remove(callbackId);

                CheckForFinishedCallback(callbackId);
            }
            else
            {
                insightNetworkConnection.TransportReceive(data);
            }
        }

        void OnError(int connectionId, Exception exception)
        {
            // TODO Let's discuss how we will handle errors
            logger.LogException(exception);
        }

        public string GetConnectionInfo(int connectionId)
        {
            return transport.ServerGetClientAddress(connectionId);
        }

        /// <summary>
        /// Disconnect client by specified connectionId
        /// </summary>
        /// <param name="connectionId">ConnectionId to be disconnected</param>
        public void Disconnect(int connectionId)
        {
            transport.ServerDisconnect(connectionId);
        }

        bool AddConnection(InsightNetworkConnection conn)
        {
            if (!connections.ContainsKey(conn.connectionId))
            {
                // connection cannot be null here or conn.connectionId
                // would throw NRE
                connections[conn.connectionId] = conn;
                conn.SetHandlers(messageHandlers);
                return true;
            }

            // already a connection with this id
            return false;
        }

        bool RemoveConnection(int connectionId)
        {
            return connections.Remove(connectionId);
        }

        public bool SendToClient<T>(int connectionId, T msg, CallbackHandler callback = null) where T : Message
        {
            if (transport.ServerActive())
            {
                NetworkWriter writer = new NetworkWriter();
                int msgType = GetId(default(Message) != null ? typeof(Message) : msg.GetType());
                writer.WriteUInt16((ushort) msgType);

                int callbackId = 0;
                if (callback != null)
                {
                    callbackId = ++callbackIdIndex; // pre-increment to ensure that id 0 is never used.
                    callbacks.Add(callbackId,
                        new CallbackData() {callback = callback, timeout = MyTime.GetElapsedTImeInSeconds + callbackTimeout});
                }

                writer.WriteInt32(callbackId);

                //Writer<T>.write.Invoke(writer, msg);

                //return connections[connectionId].Send(writer.ToArray());

                #region my custom serializer injection

                var myWriter = new CustomWriter();

                myWriter.WriteData(msg);
                List<byte> Templist = new List<byte>();
                Templist.AddRange(writer.ToArray()); // mirror writer data
                Templist.AddRange(myWriter.GetBuffer); // my writer data
                return connections[connectionId].Send(Templist.ToArray());

                #endregion
            }

            logger.LogError("Server.Send: not connected!", this);
            return false;
        }

        public bool SendToClient<T>(int connectionId, T msg) where T : Message
        {
            return SendToClient(connectionId, msg, null);
        }

        public bool SendToClient(int connectionId, byte[] data)
        {
            if (transport.ServerActive())
            {
                transport.ServerSend(connectionId, 0, new ArraySegment<byte>(data));
                return true;
            }

            logger.LogError("Server.Send: not connected!", this);
            return false;
        }

        public bool SendToAll<T>(T msg, CallbackHandler callback, SendToAllFinishedCallbackHandler finishedCallback)
            where T : Message
        {
            if (transport.ServerActive())
            {
                SendToAllFinishedCallbackData finishedCallbackData = new SendToAllFinishedCallbackData()
                    {requiredCallbackIds = new HashSet<int>()};

                foreach (KeyValuePair<int, InsightNetworkConnection> conn in connections)
                {
                    SendToClient(conn.Key, msg, callback);
                    finishedCallbackData.requiredCallbackIds.Add(callbackIdIndex);
                }

                // you can't have _just_ the finishedCallback, although you _can_ have just
                // "normal" callback. 
                if (finishedCallback != null && callback != null)
                {
                    finishedCallbackData.callback = finishedCallback;
                    finishedCallbackData.timeout = MyTime.GetElapsedTImeInSeconds + callbackTimeout;
                    sendToAllFinishedCallbacks.Add(finishedCallbackData);
                }

                return true;
            }

            logger.LogError("Server.Send: not connected!", this);
            return false;
        }

        public bool SendToAll<T>(T msg, CallbackHandler callback) where T : Message
        {
            return SendToAll(msg, callback, null);
        }

        public bool SendToAll<T>(T msg) where T : Message
        {
            return SendToAll(msg, null, null);
        }

        public bool SendToAll(byte[] bytes)
        {
            if (transport.ServerActive())
            {
                foreach (var conn in connections)
                {
                    conn.Value.Send(bytes);
                }

                return true;
            }

            logger.LogError("Server.Send: not connected!", this);
            return false;
        }

        void OnApplicationQuit()
        {
            logger.Log("[InsightServer] Stopping Server");
            transport.ServerStop();
        }

        void CheckForFinishedCallback(int callbackId)
        {
            foreach (var item in sendToAllFinishedCallbacks)
            {
                if (item.requiredCallbackIds.Contains(callbackId)) item.callbacks++;
                if (item.callbacks >= item.requiredCallbackIds.Count)
                {
                    item.callback.Invoke(CallbackStatus.Success);
                    sendToAllFinishedCallbacks.Remove(item);
                    return;
                }
            }
        }

        protected override void CheckCallbackTimeouts()
        {
            base.CheckCallbackTimeouts();
            foreach (var item in sendToAllFinishedCallbacks)
            {
                if (item.timeout < MyTime.GetElapsedTImeInSeconds)
                {
                    item.callback.Invoke(CallbackStatus.Timeout);
                    sendToAllFinishedCallbacks.Remove(item);
                    return;
                }
            }
        }

        ////----------virtual handlers--------------//
        public virtual void OnStartInsight()
        {
            logger.Log("[InsightServer] - Server started listening");
        }

        public virtual void OnStopInsight()
        {
            logger.Log("[InsightServer] - Server stopping");
        }
    }
}