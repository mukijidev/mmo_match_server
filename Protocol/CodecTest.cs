using System;
using System.Buffers.Binary;
using System.IO;
using System.Globalization;
using System.Linq;

namespace MMO.Protocol
{
    public static class CodecTest
    {
        private const string VectorFileName = "codec_vectors.txt";

        private static int _passed;
        private static int _failed;

        public static bool Run()
        {
            _passed = 0;
            _failed = 0;

            Console.WriteLine("=== codec test ===");

            CompareVectors();
            Console.WriteLine($"result passed : {_passed}, failed : {_failed}");
            return _failed == 0;
        }


        private static bool CompareVectors() {
            string path = Path.Combine(AppContext.BaseDirectory, VectorFileName);

            if (!File.Exists(path))
            {
                Console.WriteLine("file not existss");
                Fail($"{VectorFileName} not found at {path}");
                return false;
            }

            int lineNo = 0;
            int caseCount = 0;
            
            foreach(string raw in File.ReadLines(path))
            {
                lineNo++;
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;

                string[] f = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if(f.Length != 5)
                {
                    Fail($"line {lineNo} : no 5 fields");
                    break;
                }

                string name = f[0];
                ushort key = byte.Parse(f[1], NumberStyles.HexNumber);
                byte randKey = byte.Parse(f[2], NumberStyles.HexNumber);

                byte[] payload;
                if (f[3] == "-")
                {
                    payload = Array.Empty<byte>();
                }
                else
                {
                    payload = Convert.FromHexString(f[3]);
                }

                if(!CompareCase(name, key, randKey, payload, f[4]))
                {
                    return false;
                }
                caseCount++;

            }

            Pass($"{VectorFileName} x {caseCount}");
            return true;
        }

        private static bool CompareCase(string name, ushort key, byte randKey, byte[] payload, string expectedHex)
        {
            byte[] frame = new byte[NetHeader.Size + payload.Length];

            frame[NetHeader.CodeOffSet] = NetConfig.ServerPacketCode;
            BinaryPrimitives.WriteUInt16LittleEndian(
                 frame.AsSpan(NetHeader.LenOffset, sizeof(ushort)), (ushort)payload.Length);
            frame[NetHeader.RandKeyOffSet] = randKey;
            frame[NetHeader.CheckSumOffSet] = 0;
            payload.CopyTo(frame, NetHeader.Size);

            PacketCodec.Encode(frame, key);

            string hexVal = Convert.ToHexString(frame);
            if(hexVal != expectedHex)
            {
                Fail($"{name}");
                return false;
            }

            NetHeader header = NetHeader.ReadFrom(frame);
            Span<byte> decoded = frame.AsSpan(NetHeader.Size, header.Len);

            if(!PacketCodec.Decode(decoded, in header, key))
            {
                Fail($"{name}");
                return false;
            }

            if(!decoded.SequenceEqual(payload))
            {
                Fail($"{name}");
                return false;
            }

            return true;
        }


        private static void Pass(string name)
        {
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }

        private static void Fail(string name)
        {
            
            _failed++;
            Console.WriteLine($"[FAIL] {name}");
        }

    }
}
