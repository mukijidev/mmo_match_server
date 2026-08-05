using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MMO.Protocol;

//IOCP -> , await socket.ReceiveAsync
//런타임의 iocp 스레드풀이 콜백을 호출

namespace MMO.Server
{
 
    public abstract class NetServer
    {
        private Socket _listenSocket;

        private Task _acceptTask;

        //TODO:
        private bool _bStopNetwork;
        private ushort _serverPort;

        //TODO:
        private readonly ConcurrentDictionary<long, Session> _sessionMap = new ConcurrentDictionary<long, Session>();
        private long _sessionIdGenerator;

        private readonly ushort _packetKey = NetConfig.ServerPacketKey;

        private long _totalAccept;
        private long _acceptErrorTotal;
        private long _totalDisconnect;
        private long _processRecvPacket;
        private long _processSendPacket;

        protected virtual Session CreateSession()
        {
            return new Session();
        }

        protected abstract bool OnConnectionRequest();
        protected abstract void OnError(int errorCode, string errorMessage);

        protected abstract void OnClientJoin(long sessionId, string ip, ushort port);
        protected abstract void OnClientLeave(long sessionId);
        protected abstract void HandleRecvPacket(long sessionId, Packet packet);

        public bool Start(ushort port, int nagle = 1, int backlog = 10000)
        {
            _serverPort = port;
            try
            {
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenSocket.LingerState = new LingerOption(true, 0); // 키고 0초
                _listenSocket.NoDelay = (nagle == 0); // NoDelay fale , Nagle 켜기
                _listenSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _listenSocket.Listen(backlog); // the maximum length of the pending connect queues
            }
            catch(SocketException e)
            {
                OnError((int)e.SocketErrorCode, "listen socket start failed : " + e.Message);
                return false;
            }

            _acceptTask = AcceptThread();
            return true;
        }

        //TODO: Close 확인
        public void Stop()
        {
            _bStopNetwork = true;

            try
            {
                _listenSocket.Close();
            }
            catch
            {
                Console.WriteLine("Stop() listen socket close error");
            }

            foreach(KeyValuePair<long, Session> kv in _sessionMap)
            {
                DisconnectSession(kv.Value);
            }

            try
            {
                _acceptTask.Wait(5000); // ms
            }
            catch { }
        }

        public bool IsNetworkStop()
        {
            return _bStopNetwork;
        }

        private async Task AcceptThread()
        {
            while(!_bStopNetwork)
            {
                Socket clientSocket;

                // when close socket
                try
                {
                    clientSocket = await _listenSocket.AcceptAsync();

                }
                catch (ObjectDisposedException)
                {
                    //already closed
                    break;
                }
                catch (SocketException e)
                {
                    //TODO
                    if (_bStopNetwork)
                        break;

                    Logger.Log("Accept", LogLevel.Error,
                $"accept failed : {e.SocketErrorCode}");



                    Interlocked.Increment(ref _acceptErrorTotal);
                    continue;
                }

                Interlocked.Increment(ref _totalAccept);

                if (!OnConnectionRequest())
                {
                    clientSocket.Close();
                    continue;
                }

                try
                {
                    AcceptProcess(clientSocket);
                } 
                catch(Exception e)
                {
                    Logger.Log("Accept", LogLevel.Error, "accept process failed :" + e.Message);
                    try { clientSocket.Close(); } 
                    catch { }
                }
            }
        }

        private void AcceptProcess(Socket clientSocket)
        {
            IPEndPoint remote = clientSocket.RemoteEndPoint as IPEndPoint;
            if(remote == null)
            {
                clientSocket.Close();
                return;
            }

            string ip = remote.Address.ToString();
            ushort port = (ushort)remote.Port;

            clientSocket.NoDelay = _listenSocket.NoDelay;

            long sessionId = Interlocked.Increment(ref _sessionIdGenerator);

            Session session = CreateSession();
            session.Init(sessionId, clientSocket, ip, port);
            _sessionMap.TryAdd(sessionId, session);
            OnClientJoin(sessionId, ip, port);




            _ = RecvThread(session);
        }

        private async Task RecvThread(Session s)
        {
            try
            {
                while (!_bStopNetwork && !s._disconnectRequested)
                {
                    int directEnqueueSize = s._recvQueue.GetDirectEnqueueSize();
                    if(directEnqueueSize == 0)
                    {
                        OnError(0, $"ring buffer full s id={s._sessionId}");
                        break;
                    }

                    //memory로 넘겨야함
                    int recvBytes = await s._socket.ReceiveAsync(
                        new Memory<byte>(s._recvQueue.GetBuffer(),
                        s._recvQueue.Rear, directEnqueueSize), SocketFlags.None);

                    //fin
                    if(recvBytes == 0)
                    {
                        break;
                    }

                    s._recvQueue.MoveRear(recvBytes);
                    if(!ProcessRecvPacket(s))
                    {
                        break;
                    }
                }
            }
            catch (SocketException e)
            {
                //정상끊김
                if (e.SocketErrorCode != SocketError.ConnectionReset && // 10054 RST
                    e.SocketErrorCode != SocketError.ConnectionAborted && // 10053
                    e.SocketErrorCode != SocketError.OperationAborted) // cancelioEx
                {
                    OnError((int)e.SocketErrorCode, $"recv error, sessionId={s._sessionId} : {e.Message}");
                }
            }
            catch (ObjectDisposedException)
            {
                //정상
            }
            catch (Exception e)
            {
                //에러
                Logger.Log("RecvThread", LogLevel.Error, $"recv error, sessionId = {s._sessionId} : {e.Message}");
            }
            finally
            {
                ReleaseSession(s);
            }
        }

        private bool ProcessRecvPacket(Session s)
        {
            Span<byte> headerBytes = stackalloc byte[NetHeader.Size];

            while(true)
            {
                if(s._disconnectRequested)
                {
                    return false;
                }

                //header 5 byte
                if(s._recvQueue.GetUseSize() < NetHeader.Size)
                {
                    break;
                }

                int peekVal = s._recvQueue.Peek(headerBytes);
                if(peekVal != NetHeader.Size)
                {
                    Logger.Log("ProcessRecvPacket", LogLevel.Error, $"peek failed");
                    return false;
                }

                NetHeader header = NetHeader.ReadFrom(headerBytes);

                if(header.Code!= NetConfig.ClientPacketCode)
                {
                    Logger.Log("ProcessRecvPacket", LogLevel.Error, $"clientpacketcode");
                    return false;
                }

                int dataSize = header.Len;

                if(dataSize > NetHeader.MaxPayloadSize)
                {
                    Logger.Log("ProcessRecvPacket", LogLevel.Error, $"max payload");
                    return false;
                }
                // cannot make packet
                if(s._recvQueue.GetUseSize() < NetHeader.Size + dataSize)
                {
                    break;
                }

                //헤더 버림
                s._recvQueue.MoveFront(NetHeader.Size);

                Packet packet = Packet.Alloc();
                s._recvQueue.Dequeue(packet.GetBuffer().AsSpan(0, dataSize));
                packet.MoveWritePos(dataSize);

                bool bDecodeSucceed = packet.Decode(in header, _packetKey);
                if(!bDecodeSucceed)
                {
                    Logger.Log("ProcessRecvPacket", LogLevel.Error, $"decode faield");
                    return false;
                }

                Interlocked.Increment(ref _processRecvPacket);
                try
                {
                    HandleRecvPacket(s._sessionId, packet);
                }
                finally
                {
                    Packet.Free(packet);
                }

            }


            return true;
        }

        public void SendPacket(long sessionId, Packet packet)
        {
            if(sessionId < 0)
            {
                return;
            }

            Session s = GetSession(sessionId);
            if(s == null)
            {
                Packet.Free(packet);
                return;
            }

            if(s._disconnectRequested)
            {
                Packet.Free(packet);
                return;
            }

            packet.Encode(_packetKey);

            //1000개
            long queueSize = Interlocked.Increment(ref s._sendQueueSize);
            if (queueSize > NetHeader.MaxPacketNumInSendQueue)
            {
                //TODO: LOG
                Interlocked.Decrement(ref s._sendQueueSize);
                Disconnect(sessionId);
                Packet.Free(packet);
                return;
            }

            s._sendQueue.Enqueue(packet);
            Interlocked.Increment(ref _processSendPacket);
            SendPost(s);
        }

        public void SendPackets(long sessionId, List<Packet> packets)
        {

        }

        private void SendPost(Session s)
        {
            //CAS (ref x, value, comparand)
            //isSending을 0이었으면 1로바꾼다. 원래1이었으면 그냥 return
            if(Interlocked.CompareExchange(ref s._isSending, 1, 0) != 0)
            {
                return;
            }

            _ = SendThread(s);
        }

        private async Task SendThread(Session s)
        {

            try
            {

                while (true)
                {
                    while (s._sendQueue.TryDequeue(out Packet packet))
                    {
                        Interlocked.Decrement(ref s._sendQueueSize);

                        ReadOnlyMemory<byte> frame = packet.GetBuffer().AsMemory(0, packet.GetDataSize());

                        int n = await s._socket.SendAsync(frame, SocketFlags.None);
                        if (n != frame.Length)
                        {
                            Logger.Log("SendThread", LogLevel.Error, $"partial send," +
                                $"sessiondId = {s._sessionId} sent = {n} len = {frame.Length}");

                            Interlocked.Exchange(ref s._isSending, 0);
                            DisconnectSession(s);
                            return;
                        }

                        Packet.Free(packet);
                    }

                    //일단 0으로 바꾸고
                    Interlocked.Exchange(ref s._isSending, 0);

                    if (s._sendQueue.IsEmpty)
                    {
                        return;
                    }

                    if (Interlocked.CompareExchange(ref s._isSending, 1, 0) != 0)
                    {
                        return;
                    }
                }
            }
            catch (SocketException)
            {
                DisconnectSession(s);
            }
            catch (ObjectDisposedException)
            {
                //closed socket
                DisconnectSession(s);
            }
            catch (Exception e)
            {
                Logger.Log("SendThread", LogLevel.Error, $"send excetption, sessionId = {s._sessionId} : {e.Message}");
                DisconnectSession(s);
            }


        }

        public void Disconnect(long sessionId)
        {
            if (sessionId < 0)
                return;

            Session s = GetSession(sessionId);
            if( s == null)
            {
                return;
            }

            DisconnectSession(s);
        }


        private void DisconnectSession(Session s)
        {
            s._disconnectRequested = true;
            try
            {
                s._socket.Shutdown(SocketShutdown.Both);
            }
            catch
            {

            }

            try
            {
                s._socket.Close();
            }
            catch
            {

            }

        }

        
        private void ReleaseSession(Session s)
        {
            if(!_sessionMap.TryRemove(s._sessionId, out Session removed))
            {
                return;
            }

            s._disconnectRequested = true;
            try
            {
                s._socket.Close();
            }
            catch
            {

            }
            s.ClearSendQueue();
            Interlocked.Increment(ref _totalDisconnect);
            OnClientLeave(s._sessionId);
        }
        
        protected Session GetSession(long sessionId)
        {
            _sessionMap.TryGetValue(sessionId, out Session s);
            return s;
        }

        public int GetSessionNum()

        {
            return _sessionMap.Count;
        }


        public long GetTotalAccept() { return Interlocked.Read(ref _totalAccept); }
        public long GetAcceptErrorTotal() { return Interlocked.Read(ref _acceptErrorTotal); }
        public long GetTotalDisconnect() { return Interlocked.Read(ref _totalDisconnect); }

        public long GetRecvMessageTps()
        {
            return Interlocked.Exchange(ref _processRecvPacket, 0);
        }

        public long GetSendMessageTps()
        {
            return Interlocked.Exchange(ref _processSendPacket, 0);
        }

        public ushort GetServerPort()
        {
            return _serverPort;
        }
    }
}
