using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using MMO.Protocol;


// C# -> GC 가 참조가 남아있는 객체를 회수하지 않음
namespace MMO.Server
{
    public class Session
    {
        public long _sessionId;
        public Socket _socket;

        public readonly RecvRingBuffer _recvQueue = new RecvRingBuffer();

        public readonly ConcurrentQueue<Packet> _sendQueue = new ConcurrentQueue<Packet>();
        public long _sendQueueSize;

        public string _ip = "";
        public ushort _port;
        public int _isSending;


        //TODO:주의
        public bool _disconnectRequested;

        public void Init(long sessionId, Socket socket, string ip, ushort port)
        {
            _sessionId = sessionId;
            _socket = socket;
            _ip = ip;
            _port = port;
            _isSending = 0;
            _sendQueueSize = 0;
            _disconnectRequested = false;
            _recvQueue.ClearBuffer();
        }

        public void ClearSendQueue()
        {
            while (_sendQueue.TryDequeue(out Packet packet))
            {
                Packet.Free(packet);
            }

            _sendQueueSize = 0;
        }


    }
}
