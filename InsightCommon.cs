﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mirror;

namespace Insight
{
    public static class Channels
    {
        public const int DefaultReliable = 0;
        public const int DefaultUnreliable = 1;
    }
    public enum CallbackStatus : byte
    {
        Default,
        Success,
        Error,
        Timeout
    }

    public abstract class InsightCommon 
    {
        static readonly ILogger logger = LogFactory.GetLogger(typeof(InsightCommon));

      //  public bool DontDestroy = true; //Sets DontDestroyOnLoad for this object. Default to true. Can be disabled via Inspector or runtime code.
        public bool AutoStart = true;
        public string networkAddress = "localhost";

        protected Dictionary<int, InsightNetworkMessageDelegate> messageHandlers = new Dictionary<int, InsightNetworkMessageDelegate>(); //Default handlers

        protected enum ConnectState
        {
            None,
            Connecting,
            Connected,
            Disconnected,
        }
        protected ConnectState connectState = ConnectState.None;

        protected int callbackIdIndex = 0; // 0 is a _special_ id - it represents _no callback_. 
        protected Dictionary<int, CallbackData> callbacks = new Dictionary<int, CallbackData>();

        public delegate void CallbackHandler(InsightNetworkMessage netMsg);
        public delegate void SendToAllFinishedCallbackHandler(CallbackStatus status);

        public const float callbackTimeout = 30f; // all callbacks have a 30 second time out. 

        public bool isConnected { get { return connectState == ConnectState.Connected; } }

        Transport _transport=new TelepathyTransport();
        public virtual Transport transport
        {
            get
            {
                _transport = _transport ?? new TelepathyTransport();
                if (_transport == null)
                    logger.LogWarning("Insight has no Transport component. Networking won't work without a Transport");
                return _transport;
            }
        }

        public void RegisterHandler<T>(InsightNetworkMessageDelegate handler)
        {
            int msgType = GetId<T>();
            if (messageHandlers.ContainsKey(msgType))
            {
                logger.Log("NetworkConnection.RegisterHandler replacing " + msgType);
            }
            messageHandlers[msgType] = handler;
        }

        public void UnRegisterHandler<T>(InsightNetworkMessageDelegate handler)
        {
            int msgType = GetId<T>();
            if (messageHandlers.ContainsKey(msgType))
            {
                messageHandlers[msgType] -= handler;
            }
        }

        protected virtual void CheckCallbackTimeouts()
        {
            foreach (KeyValuePair<int, CallbackData> callback in callbacks)
            {
                if (callback.Value.timeout < DateTime.UtcNow.Second)
                {
                    callback.Value.callback.Invoke(null);
                    callbacks.Remove(callback.Key);
                    break;
                }
            }
        }

        public void ClearHandlers()
        {
            messageHandlers.Clear();
        }

        public abstract void StartInsight();

        public abstract void StopInsight();

        public struct CallbackData
        {
            public CallbackHandler callback;
            public float timeout;
        }

        [System.Serializable]
        public class SendToAllFinishedCallbackData
        {
            public SendToAllFinishedCallbackHandler callback;
            public HashSet<int> requiredCallbackIds;
            public int callbacks;
            public float timeout;
        }

        public int GetId<T>()
        {
            return typeof(T).FullName.GetStableHashCode() & 0xFFFF;
        }

        public int GetId(Type type)
        {
            return type.FullName.GetStableHashCode() & 0xFFFF;
        }

        public bool UnpackMessage(NetworkReader messageReader, out int msgType)
        {
            // read message type (varint)
            try
            {
                msgType = messageReader.ReadUInt16();
                return true;
            }
            catch (System.IO.EndOfStreamException)
            {
                msgType = 0;
                return false;
            }
        }
    }


}
