using MMO.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Server
{
    public class MatchServer : NetServer
    {
      

        protected override void OnClientLeave(long sessionId)
        {
            Console.WriteLine($"Leave sessionId = {sessionId}");
        }

        protected override void OnClientJoin(long sessionId, string ip, ushort port)
        {
            Console.WriteLine($"Join sessionId = {sessionId}");
        }

        protected override bool OnConnectionRequest()
        {
            return true;
        }

        protected override void OnError(int errorCode, string errorMessage)
        {
            Console.WriteLine($"[Error] code = {errorCode}_{errorMessage}");
        }

        protected override void HandleRecvPacket(long sessionId, Packet packet)
        {
            // 패킷타입 처음 uint16
            ushort type = packet.ReadUInt16();

            switch((PacketType)type)
            {
                case PacketType.PACKET_CS_GAME_REQ_LOGIN:
                    {
                        //HandleLogin(sessionId, packet);
                        break;
                    }

                default:
                    break;
            }
        }

        private void HandleLogin(long sessionId, Packet packet)
        {
            long accountNo = packet.ReadInt64();

            byte[] sessionToken = new byte[NetConfig.SessionKeyLen];
            packet.GetData(sessionToken);

            Console.WriteLine($"[login] ssid = {sessionId} accountNo = {accountNo}");

            byte status = 1;
            Packet resPacket = Packet.Alloc();
            PacketMaker.MP_SC_GAME_RES_LOGIN(resPacket, accountNo, status);
            SendPacket(sessionId, resPacket);
        }

        public void LogServerInfo()
        {
            Console.WriteLine(
                $"session = {GetSessionNum()} acccept = {GetTotalAccept()} disconnect = {GetTotalDisconnect()}"
                + $"acceptErr = {GetAcceptErrorTotal()}"
                + $"recvTps = {GetRecvMessageTps()} sendTps = {GetSendMessageTps()}");
        }

    }
}
