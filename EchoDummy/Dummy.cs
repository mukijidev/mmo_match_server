using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using MMO.Protocol;

namespace MMO.EchoDummy
{
    internal static class Dummy
    {
        private const int RecvBufferSize = 256;

        private const int IntervalRandomMs = 200; // Jitter

        public static async Task Loop(IPEndPoint ep, int intervalMs, CancellationToken ct)
        {
            Socket socket = null;
            bool counted = false;

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.NoDelay = true;

                socket.LingerState = new LingerOption(true, 0);

                await socket.ConnectAsync(ep, ct);

                Interlocked.Increment(ref DummyMonitor.Connected);
                counted = true;

                byte[] recvBuffer = new byte[RecvBufferSize];
                int useSize = 0;

                await Task.Delay(Random.Shared.Next(0, intervalMs + 1), ct);
                while(!ct.IsCancellationRequested)
                {
                    long t0 = Stopwatch.GetTimestamp();
                    Packet req = Packet.Alloc();
                    try
                    {
                       
                        PacketMaker.BeginPacket(req);
                        req.WriteUInt16((ushort)PacketType.PACKET_CS_MATCH_REQ_ECHO).WriteInt64(t0);
                        PacketMaker.EndPacket(req);

                        req.Encode(NetConfig.ServerPacketKey);

                        int frameSize = req.GetDataSize();
                        int sent = 0;
                        while (sent < frameSize)
                        {
                            int n = await socket.SendAsync(req.GetBuffer().AsMemory(sent, frameSize - sent)
                                , SocketFlags.None, ct);

                            if (n == 0)
                            {
                                throw new SocketException((int)SocketError.ConnectionReset);
                            }

                            sent += n;
                        }
                    }
                    finally
                    {
                        Packet.Free(req);
                    }
                   

                    long echoedTicks;
                    while(!TryReadFrame(recvBuffer, ref useSize, out echoedTicks))
                    {
                        if (useSize >= recvBuffer.Length)
                        {
                            throw new InvalidOperationException("recv buffer full");
                        }

                        int n = await socket.ReceiveAsync(recvBuffer.AsMemory(useSize, recvBuffer.Length - useSize), SocketFlags.None, ct);
                        if(n == 0)
                        {
                            throw new SocketException((int)SocketError.ConnectionReset);
                        }
                        useSize += n;
                    }

                    DummyMonitor.RecordRtt(Stopwatch.GetTimestamp() - echoedTicks);
                    Interlocked.Increment(ref DummyMonitor.EchoCount);

                    await Task.Delay(intervalMs + Random.Shared.Next(0, IntervalRandomMs), ct);
                }
            }
            catch(OperationCanceledException)
            {
                // cts.cancel
            }
            catch(ObjectDisposedException)
            {
                // already closed
            }
            catch(SocketException e)
            {
                DummyMonitor.ReportError(e);
            }
            catch(Exception e)
            {
                DummyMonitor.ReportError(e);
            }
            finally
            {
                if(counted)
                {
                    Interlocked.Decrement(ref DummyMonitor.Connected);
                }

                if(socket != null)
                {
                    try
                    {
                        socket.Close();
                    }
                    catch
                    {

                    }
                }
            }


        }

        private static bool TryReadFrame(byte[] buffer, ref int useSize, out long echoedTicks)
        {
            echoedTicks = 0;
            if(useSize < NetHeader.Size)
            {
                return false;
            }

            NetHeader header = NetHeader.ReadFrom(buffer);
            
            if(header.Code != NetConfig.ServerPacketCode)
            {
                throw new InvalidOperationException($"Error packet code {header.Code}");
            }

            if(header.Len > NetHeader.MaxPayloadSize)
            {
                throw new InvalidOperationException($"Error packet len max size {header.Len}");
            }

            int frameSize = NetHeader.Size + header.Len;
            if(useSize < frameSize)
            {
                return false;
            }

            Span<byte> payload = buffer.AsSpan(NetHeader.Size, header.Len);
            if(!PacketCodec.Decode(payload, in header, NetConfig.ServerPacketKey))
            {
                throw new InvalidOperationException("Error decode checksum failed");
            }

            ushort type = BinaryPrimitives.ReadUInt16LittleEndian(payload.Slice(0, sizeof(ushort)));
            if(type != (ushort)PacketType.PACKET_SC_MATCH_RES_ECHO)
            {
                throw new InvalidOperationException($"Error echo packet type {type}");
            }

            echoedTicks = BinaryPrimitives.ReadInt64LittleEndian(payload.Slice(sizeof(ushort), sizeof(long)));


            int remainBytes = useSize - frameSize;
            if(remainBytes > 0) 
            {
                    //memmove
                    // src srcoffset, dest, destoffset, count
                    //  [] [] [] [] [] [] [] [] [] [] [] [] // useSize
                    //  [  frmae size      ] [ remain byte]

                Buffer.BlockCopy(buffer, frameSize, buffer, 0, remainBytes);
            }
            useSize = remainBytes;
            return true;
        }



    }



}
