using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Protocol
{
    public struct FVector
    {
        public double X;
        public double Y;
        public double Z;

        public FVector(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public struct FRotator
    {
        public double Pitch;
        public double Yaw;
        public double Roll;

        public FRotator(double pitch, double yaw, double roll)
        {
            Pitch = pitch;
            Yaw = yaw;
            Roll = roll;
        }
    }

    public struct Pos
    {
        public int Y;
        public int X;

        public Pos(int y, int x)
        {
            Y = y; X = x;
        }
    }

    [InlineArray(NetConfig.NickNameLen)]
    public struct NickNameBuffer { private char e; }




    public struct PlayerInfo
    {
        public long PlayerID;
        public NickNameBuffer NickName;
        public ushort Class;
        public ushort Level;
        public uint Exp;
        public int Hp;
    }

    public struct MonsterInfo
    {
        public long MonsterID;
        public ushort Type;
        public int Hp;
    }
}
