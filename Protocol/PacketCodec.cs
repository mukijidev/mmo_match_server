using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Protocol
{
    public static class PacketCodec
    {
        
        //frame
        //[code][len][randKey][checkSsum][payLoad]
        public static void Encode(Span<byte> frame, ushort packetKey)
        {

            ushort len = BinaryPrimitives.ReadUInt16LittleEndian(frame.Slice(NetHeader.LenOffset, 2));
            byte randomKey = frame[NetHeader.RandKeyOffSet];

            Span<byte> payload = frame.Slice(NetHeader.Size, len);

            byte checkSum = 0;
            for(int i = 0; i < len; i++)
            {
                checkSum += payload[i];
            }

            byte p = (byte)(checkSum ^ (byte)(randomKey + 1));
            frame[NetHeader.CheckSumOffSet] = (byte)(p ^ (byte)(packetKey + +1));
            byte prevData = frame[NetHeader.CheckSumOffSet];

            for(int i = 0; i < len; i++)
            {
                int add = i + 2;
                p = (byte)(payload[i] ^ (byte)(p + randomKey + add));
                byte c = (byte)(p ^ (byte)(prevData + packetKey + add));
                payload[i] = c;
                prevData = c; 
            }
        }

        //payload
        //in == const NetHeader&
        public static bool Decode(Span<byte> payload, in NetHeader header, ushort packetKey)
        {
            byte randKey = header.RandKey;
            byte encodedCheckSum = header.CheckSum;
            byte pPrev = (byte)(encodedCheckSum ^ (byte)(packetKey + 1));

            byte prevData = encodedCheckSum;

            byte decodedCheckSum = (byte)(pPrev ^ (byte)(randKey + 1));

            byte checkSum = 0;
            int len = header.Len;
            for(int i =0; i <len; i++)
            {
                int add = i + 2;
                byte c = payload[i];

                byte p = (byte)(c ^ (byte)(prevData + packetKey + add));

                prevData = c;
                c = (byte)(p ^ (byte)(pPrev + randKey + add));
                payload[i] = c;
                checkSum += c;
                pPrev = p;
            }

            return decodedCheckSum == checkSum;
        }




    }

}
