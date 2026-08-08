using System;
using System.Buffers.Binary;


namespace MMO.Protocol
{
    public static class PacketMaker
    {
        public static void MP_SC_GAME_RES_LOGIN(Packet packet, long accountNo, byte status)
        {
            NetHeader header = default;                       
            header.Code = NetConfig.ServerPacketCode;
            header.RandKey = (byte)Random.Shared.Next(0, 256); // Random.Shared thread safe
            Span<byte> headerBytes = stackalloc byte[NetHeader.Size];  // stackalloc stack array
            header.WriteTo(headerBytes);
            packet.PutData(headerBytes);

            ushort type = (ushort)PacketType.PACKET_SC_GAME_RES_LOGIN;
            packet.WriteUInt16(type)
                  .WriteInt64(accountNo)
                  .WriteByte(status);

            ushort len = (ushort)(packet.GetDataSize() - NetHeader.Size);
            BinaryPrimitives.WriteUInt16LittleEndian(
                packet.GetBuffer().AsSpan(NetHeader.LenOffset, sizeof(ushort)), len);   // C++ memcpy (ptr + NET_HEADER_SIZE_INDEX, &len, 2)
        }

        public static void MP_SC_GAME_RES_FIELD_MOVE(Packet packet, byte status, ushort fieldID)
        {
            NetHeader header = default;
            header.Code = NetConfig.ServerPacketCode;
            header.RandKey = (byte)Random.Shared.Next(0, 256);
            Span<byte> headerBytes = stackalloc byte[NetHeader.Size];
            header.WriteTo(headerBytes);
            packet.PutData(headerBytes);

            ushort type = (ushort)PacketType.PACKET_SC_GAME_RES_FIELD_MOVE;
            packet.WriteUInt16(type)
                  .WriteByte(status)
                  .WriteUInt16(fieldID);

            ushort len = (ushort)(packet.GetDataSize() - NetHeader.Size);
            BinaryPrimitives.WriteUInt16LittleEndian(
                packet.GetBuffer().AsSpan(NetHeader.LenOffset, sizeof(ushort)), len);
        }


        public static void MP_SC_MATCH_RES_ECHO(Packet packet, long clientTimestamp)
        {
            BeginPacket(packet);
            packet.WriteUInt16((ushort)PacketType.PACKET_SC_MATCH_RES_ECHO)
                  .WriteInt64(clientTimestamp);
            EndPacket(packet);
        }

        public static void BeginPacket(Packet packet)
        {
            NetHeader header = default;
            header.Code = NetConfig.ServerPacketCode;
            header.RandKey = (byte)Random.Shared.Next(0, 256);
            Span<byte> headerBytes = stackalloc byte[NetHeader.Size];
            header.WriteTo(headerBytes);
            packet.PutData(headerBytes);
        }

        public static void EndPacket(Packet packet)
        {
            ushort len = (ushort)(packet.GetDataSize() - NetHeader.Size);
            BinaryPrimitives.WriteUInt16LittleEndian(
                packet.GetBuffer().AsSpan(NetHeader.LenOffset, sizeof(ushort)), len);
        }
    }
}
