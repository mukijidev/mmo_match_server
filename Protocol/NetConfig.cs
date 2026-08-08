using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Protocol
{
    public static class NetConfig
    {
        public const byte ServerPacketCode = 119;
        public const ushort ServerPacketKey = 119;
        public const byte ClientPacketCode = 119;
        public const ushort ClientPacketKey = 119;

        public const ushort ServerPort = 10304;

        public const int NickNameLen = 20;
        public const int IdLen = 20;
        public const int PassLen = 20;
        public const int IpLen = 16;
        public const int SessionKeyLen = 64;

        public const bool ConsoleSessionLog = true;

    }
}
