using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Protocol
{
    public struct NetHeader
    {
        public const int Size = 5; // sizeof(NetHeader)
        public const int CodeOffSet = 0;
        public const int LenOffset = 1;
        public const int RandKeyOffSet = 3;
        public const int CheckSumOffSet = 4;

        public const int MaxPacketNumInSendQueue = 1000;
        public const int MaxPayloadSize = 251;

        public byte Code;
        public ushort Len;
        public byte RandKey;
        public byte CheckSum;

        public static NetHeader ReadFrom(ReadOnlySpan<byte> src)
        {
            NetHeader h;
            h.Code = src[CodeOffSet];
            h.Len = BinaryPrimitives.ReadUInt16LittleEndian(src.Slice(LenOffset, 2));
            h.RandKey = src[RandKeyOffSet];
            h.CheckSum = src[CheckSumOffSet];
            return h;
        }

        public readonly void WriteTo(Span<byte> dst)
        {
            dst[CodeOffSet] = Code;
            BinaryPrimitives.WriteUInt16LittleEndian(dst.Slice(LenOffset, 2), Len);
            dst[RandKeyOffSet] = RandKey;
            dst[CheckSumOffSet] = CheckSum;
        }


    }
}
