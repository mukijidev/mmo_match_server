using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Protocol
{
    public enum PacketType : ushort
    {
        PACKET_GAME_SERVER = 1000,
        
        PACKET_CS_GAME_REQ_LOGIN = 1001,
        PACKET_SC_GAME_RES_LOGIN = 1002,

        PACKET_CS_GAME_REQ_FIELD_MOVE = 1003,
        PACKET_SC_GAME_RES_FIELD_MOVE = 1004,

        PACKET_SC_GAME_SPAWN_MY_CHARACTER = 1005,

        PACKET_CS_GAME_REQ_PLAYER_LIST = 1018,
        PACKET_SC_GAME_RES_PLAYER_LIST = 1019,

        PACKET_CS_GAME_REQ_SELECT_PLAYER = 1020,
        PACKET_SC_GAME_RES_SELECT_PLAYER = 1021,
    }

}
