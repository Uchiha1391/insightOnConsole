﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Insight
{
    // Handles network messages on client and server
    public delegate void InsightNetworkMessageDelegate(InsightNetworkMessage netMsg);

    public class InsightNetworkMessage
    {

        public CustomReader Creader;
        public int msgType;
        InsightNetworkConnection conn;
        public NetworkReader reader;
        public int callbackId { get; protected set; }
        public int connectionId { get { return conn.connectionId; } }

        public InsightNetworkMessage()
        {

        }

        public InsightNetworkMessage(InsightNetworkConnection conn)
        {
            this.conn = conn;
        }

        public InsightNetworkMessage(InsightNetworkConnection conn, int callbackId)
        {
            this.callbackId = callbackId;
            this.conn = conn;
        }

        public T ReadMessage<T>() where T : Message, new()
        {


            //return reader.Read<T>();
            return Creader.ReadDeserializedData<T>();
        }

        public void Reply()
        {
            Reply(new Message());
        }

        public void Reply<T>(T msg) where T : Message, new()
        {
            NetworkWriter writer = new NetworkWriter();
            int msgType = conn.GetActiveInsight().GetId(default(Message) != null ? typeof(Message) : msg.GetType());
            writer.WriteUInt16((ushort)msgType);

            writer.WriteInt32(callbackId);


            //   Writer<T>.write.Invoke(writer, msg);

            // conn.Send(writer.ToArray());

            #region my custom serializer injection

            var myWriter = new CustomWriter();

            myWriter.WriteData(msg);

            List<byte> Templist = new List<byte>();
            Templist.AddRange(writer.ToArray()); // mirror writer data
            Templist.AddRange(myWriter.GetBuffer);// my writer data
            conn.Send(Templist.ToArray());


            #endregion
        }
    }

    public class InsightNetworkConnection : IDisposable
        {

            static readonly ILogger logger = LogFactory.GetLogger(typeof(InsightNetworkConnection));

            Dictionary<int, InsightNetworkMessageDelegate> m_MessageHandlers;

            public int hostId = -1;
            public int connectionId = -1;
            public bool isReady;
            public string address;
            public float lastMessageTime;
            public bool logNetworkMessages;
            public bool isConnected { get { return hostId != -1; } }

            InsightClient client;
            InsightServer server;

            public virtual void Initialize(InsightClient clientTransport, string networkAddress, int networkHostId, int networkConnectionId)
            {
                address = networkAddress;
                hostId = networkHostId;
                connectionId = networkConnectionId;
                client = clientTransport;
            }

            public virtual void Initialize(InsightServer serverTransport, string networkAddress, int networkHostId, int networkConnectionId)
            {
                address = networkAddress;
                hostId = networkHostId;
                connectionId = networkConnectionId;
                server = serverTransport;
            }

            ~InsightNetworkConnection()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                // Take yourself off the Finalization queue
                // to prevent finalization code for this object
                // from executing a second time.
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {

            }

            public void Disconnect()
            {
                isReady = false;
            }

            internal void SetHandlers(Dictionary<int, InsightNetworkMessageDelegate> handlers)
            {
                m_MessageHandlers = handlers;
            }

            public bool InvokeHandlerNoData(short msgType)
            {
                return InvokeHandler(msgType, null);
            }

            public bool InvokeHandler(short msgType, NetworkReader reader)
            {
                InsightNetworkMessageDelegate msgDelegate;
                if (m_MessageHandlers.TryGetValue(msgType, out msgDelegate))
                {
                    InsightNetworkMessage message = new InsightNetworkMessage();
                    message.msgType = msgType;
                    message.reader = reader;

                    msgDelegate(message);
                    return true;
                }
                logger.LogError("NetworkConnection InvokeHandler no handler for " + msgType);
                return false;
            }

            public bool InvokeHandler(InsightNetworkMessage netMsg)
            {
                InsightNetworkMessageDelegate msgDelegate;
                if (m_MessageHandlers.TryGetValue(netMsg.msgType, out msgDelegate))
                {
                    msgDelegate(netMsg);
                    return true;
                }
                return false;
            }

            public void RegisterHandler(short msgType, InsightNetworkMessageDelegate handler)
            {
                if (m_MessageHandlers.ContainsKey(msgType))
                {
                    logger.Log("NetworkConnection.RegisterHandler replacing " + msgType);
                }
                m_MessageHandlers[msgType] = handler;
            }

            public void UnregisterHandler(short msgType)
            {
                m_MessageHandlers.Remove(msgType);
            }

            public virtual bool Send(byte[] bytes)
            {
                return SendBytes(bytes);
            }

            // protected because no one except NetworkConnection should ever send bytes directly to the client, as they
            // would be detected as some kind of message. send messages instead.
            protected virtual bool SendBytes(byte[] bytes)
            {
                //Currently no support for transport channels in Insight.
                if (bytes.Length > GetActiveInsight().transport.GetMaxPacketSize(0))
                {
                    logger.LogError("NetworkConnection:SendBytes cannot send packet larger than " + int.MaxValue + " bytes");
                    return false;
                }

                if (bytes.Length == 0)
                {
                    // zero length packets getting into the packet queues are bad.
                    logger.LogError("NetworkConnection:SendBytes cannot send zero bytes");
                    return false;
                }

                return TransportSend(bytes);
            }

            public virtual void TransportReceive(ArraySegment<byte> data)
            {
                // unpack message
                NetworkReader reader = new NetworkReader(data);

                if (GetActiveInsight().UnpackMessage(reader, out int msgType))
                {
                    logger.Log("ConnectionRecv " + this + " msgType:" + msgType + " content:" + BitConverter.ToString(data.Array, data.Offset, data.Count));

                    int callbackId = reader.ReadInt32();

                    // try to invoke the handler for that message
                    InsightNetworkMessageDelegate msgDelegate;

                    #region My custom reader injecting


                    byte[] Tempbuffer = new byte[data.Count - reader.Position];

                    Array.Copy(data.ToArray(), reader.Position, Tempbuffer, 0, data.Count - reader.Position);


                    CustomReader myCustomReader = new CustomReader(Tempbuffer);




                    #endregion

                    if (m_MessageHandlers.TryGetValue(msgType, out msgDelegate))
                    {
                        // create message here instead of caching it. so we can add it to queue more easily.
                        InsightNetworkMessage msg = new InsightNetworkMessage(this, callbackId);
                        msg.msgType = msgType;
                        msg.reader = reader;
                        msg.Creader = myCustomReader;

                        msgDelegate(msg);
                        lastMessageTime = DateTime.UtcNow.Second;
                    }
                }

                else
                {
                    //NOTE: this throws away the rest of the buffer. Need moar error codes
                    logger.LogError("Unknown message ID " + msgType + " connId:" + connectionId);
                }
            }

            protected virtual bool TransportSend(byte[] bytes)
            {
                if (client != null)
                {
                    client.Send(bytes);
                    return true;
                }
                else if (server != null)
                {
                    return server.SendToClient(connectionId, bytes);
                }
                return false;
            }

            public InsightCommon GetActiveInsight()
            {
                if (client != null)
                {
                    return client;
                }
                else
                {
                    return server;
                }
            }
        }

    }
