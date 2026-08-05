using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Protocol
{
    public class Packet
    {
        public const int BUFFER_DEFAULT_SIZE = 256;

        //byte[] is in gc heap, initialize 0
        private readonly byte[] _buffer = new byte[BUFFER_DEFAULT_SIZE];
        private readonly int _bufferSize = BUFFER_DEFAULT_SIZE;
        private bool _bEncoded = false;
        private int _dataSize = 0;
        private int _writePos = 0;
        private int _readPos = 0;

        //now just  new
        // after performance test


        public static Packet Alloc()
        {
            Packet packet = new Packet();
            packet.Clear();
            return packet;
        }

        public static void Free(Packet packet)
        {

        }

        //required?
        public void Clear()
        {
            _writePos = 0;
            _readPos = 0;
            _dataSize = 0;
            _bEncoded = false;
        }

        public int GetBufferSize() { return _bufferSize; }
        public int GetDataSize() { return _dataSize; }
        public byte[] GetBuffer() {return _buffer;}

        public int MoveWritePos(int size)
        {
            _writePos += size;
            _dataSize += size;
            return size;
        }


        public int MoveReadPos(int size)
        {
            _readPos += size;
            _dataSize -= size;
            return size;
        }

        public int PutData(ReadOnlySpan<byte> src)
        {
            src.CopyTo(_buffer.AsSpan(_writePos, src.Length));
            _writePos += src.Length;
            _dataSize += src.Length;

            return src.Length;
        }

        public int GetData(Span<byte> dest)
        {
            _buffer.AsSpan(_readPos, dest.Length).CopyTo(dest);
            _readPos += dest.Length;
            _dataSize -= dest.Length;
            return dest.Length;
        }

        public Packet WriteByte(byte value)
        {
            _buffer[_writePos] = value;
            _writePos += sizeof(byte);
            _dataSize += sizeof(byte);
            return this;
        }

        public Packet WriteSByte(sbyte value)
        {
            _buffer[_writePos] = (byte)value;
            _writePos += sizeof(sbyte);
            _dataSize += sizeof(sbyte);
            return this;
        }

        public Packet WriteInt16(short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(_buffer.AsSpan(_writePos, sizeof(short)), value);
            _writePos += sizeof(short);
            _dataSize += sizeof(short);
            return this;
        }

        public Packet WriteUInt16(ushort value)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(_buffer.AsSpan(_writePos, sizeof(ushort)), value);
            _writePos += sizeof(ushort);
            _dataSize += sizeof(ushort);
            return this;
        }

        public Packet WriteInt32(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(_buffer.AsSpan(_writePos, sizeof(int)), value);
            _writePos += sizeof(int);
            _dataSize += sizeof(int);
            return this;
        }


        public Packet WriteUInt32(uint value)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(_buffer.AsSpan(_writePos, sizeof(uint)), value);
            _writePos += sizeof(uint);
            _dataSize += sizeof(uint);
            return this;
        }

        public Packet WriteSingle(float value)
        {
            BinaryPrimitives.WriteSingleLittleEndian(_buffer.AsSpan(_writePos, sizeof(float)), value);
            _writePos += sizeof(float);
            _dataSize += sizeof(float);
            return this;
        }

        public Packet WriteDouble(double value)
        {
            BinaryPrimitives.WriteDoubleLittleEndian(_buffer.AsSpan(_writePos, sizeof(double)), value);
            _writePos += sizeof(double);
            _dataSize += sizeof(double);
            return this;
        }

        public Packet WriteInt64(long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(_buffer.AsSpan(_writePos, sizeof(long)), value);
            _writePos += sizeof(long);
            _dataSize += sizeof(long);
            return this;
        }

        public byte ReadByte()
        {
            byte value = _buffer[_readPos];
            _readPos += sizeof(byte);
            _dataSize -= sizeof(byte);
            return value;
        }

        public sbyte ReadSByte()
        {
            sbyte value = (sbyte)_buffer[_readPos];
            _readPos += sizeof(sbyte);
            _dataSize -= sizeof(sbyte);
            return value;
        }

        public short ReadInt16()
        {
            short value = BinaryPrimitives.ReadInt16LittleEndian(_buffer.AsSpan(_readPos, sizeof(short)));
            _readPos += sizeof(short);
            _dataSize -= sizeof(short);
            return value;
        }

        public ushort ReadUInt16()
        {
            ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_buffer.AsSpan(_readPos, sizeof(ushort)));
            _readPos += sizeof(ushort);
            _dataSize -= sizeof(ushort);
            return value;
        }

        public int ReadInt32()
        {
            int value = BinaryPrimitives.ReadInt32LittleEndian(_buffer.AsSpan(_readPos, sizeof(int)));
            _readPos += sizeof(int);
            _dataSize -= sizeof(int);
            return value;
        }

        public uint ReadUInt32()
        {
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(_buffer.AsSpan(_readPos, sizeof(uint)));
            _readPos += sizeof(uint);
            _dataSize -= sizeof(uint);
            return value;
        }

        public float ReadSingle()
        {
            float vlaue = BinaryPrimitives.ReadSingleLittleEndian(_buffer.AsSpan(_readPos, sizeof(float)));
            _readPos += sizeof(float);
            _dataSize -= sizeof(float);
            return vlaue;
        }

        public long ReadInt64()
        {
            long value = BinaryPrimitives.ReadInt64LittleEndian(_buffer.AsSpan(_readPos, sizeof(long)));
            _readPos += sizeof(long);
            _dataSize -= sizeof(long);
            return value;
        }

        public double ReadDouble()
        {
            double value = BinaryPrimitives.ReadDoubleLittleEndian(_buffer.AsSpan(_readPos, sizeof(double)));
            _readPos += sizeof(double);
            _dataSize -= sizeof(double);
            return value;
        }

        public Packet WriteFVector(in FVector v)
        {
            WriteDouble(v.X);
            WriteDouble(v.Y);
            WriteDouble(v.Z);
            return this;
        }

        public FVector ReadFVector()
        {
            FVector v;
            v.X = ReadDouble();
            v.Y = ReadDouble();
            v.Z = ReadDouble();
            return v;
        }


        public Packet WriteFRotator(in FRotator r)
        {
            WriteDouble(r.Pitch);
            WriteDouble(r.Yaw);
            WriteDouble(r.Roll);
            return this;
        }

        public FRotator ReadFRotator()
        {
            FRotator r;
            r.Pitch = ReadDouble();
            r.Yaw = ReadDouble();
            r.Roll = ReadDouble();
            return r;
        }

        public Pos ReadPos()
        {
            Pos p;
            p.Y = ReadInt32();
            p.X = ReadInt32();
            return p;
        }

        public Packet WritePlayerInfo(in PlayerInfo info)
        {
            PutData(MemoryMarshal.AsBytes((ReadOnlySpan<char>)info.NickName));
            WriteInt64(info.PlayerID);
            WriteUInt16(info.Class);
            WriteUInt16(info.Level);
            WriteUInt32(info.Exp);
            WriteInt32(info.Hp);
            return this;
        }

        public PlayerInfo ReadPlayerInfo()
        {
            PlayerInfo info = default;

            GetData(MemoryMarshal.AsBytes((Span<char>)info.NickName));
            info.PlayerID = ReadInt64();
            info.Class = ReadUInt16();
            info.Level = ReadUInt16();
            info.Exp = ReadUInt32();
            info.Hp = ReadInt32();
            return info;
        }

        public Packet WriteMonsterInfo(in MonsterInfo info)
        {
            WriteInt64(info.MonsterID);
            WriteUInt16(info.Type);
            WriteInt32(info.Hp);
            return this;
        }

        public MonsterInfo ReadMonsterInfo()
        {
            MonsterInfo info;
            info.MonsterID = ReadInt64();
            info.Type = ReadUInt16();
            info.Hp = ReadInt32();
            return info;
        }

        public void Encode(ushort packetKey)
        {
            if(_bEncoded)
            {
                return;
            }
            _bEncoded = true;
            PacketCodec.Encode(_buffer.AsSpan(0, _writePos), packetKey);
        }

        public bool Decode(in NetHeader header, ushort packetKey)
        {
            return PacketCodec.Decode(_buffer.AsSpan(0, header.Len), in header, packetKey);
        }

    }

}
