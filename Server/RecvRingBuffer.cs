using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMO.Server
{
    public class RecvRingBuffer
    {
        public const int DEFAULT_RINGUBFFER_SIZE = 10000;

        private readonly byte[] _buffer;
        private readonly int _bufferSize;
        private readonly int _realBufferSize; // full/ empty +1
        private int _rear;
        private int _front;

        public RecvRingBuffer() : this(DEFAULT_RINGUBFFER_SIZE)
        {

        }

        public RecvRingBuffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            _realBufferSize = bufferSize + 1;
            _buffer = new byte[_realBufferSize];

            _rear = 0;
            _front = 0;
        }

        public byte[] GetBuffer() {  return _buffer; }
        public int GetBufferSize() { return _bufferSize; }
        public int Rear { get { return _rear; } } 
        public int Front { get { return _front; } }

        public int GetUseSize()
        {
            int useSize = _rear - _front;
            if (useSize >= 0)
            {
                return useSize;
            }
            return _realBufferSize + useSize;
        }

        public int GetFreeSize()
        {
            return _bufferSize - GetUseSize();
        }

        public void ClearBuffer()
        {
            _rear = 0;
            _front = 0;
        }

        public int Enqueue(ReadOnlySpan<byte> src)
        {
            int dataSize  = src.Length;

            if(GetFreeSize() < dataSize)
            {
                return 0;
            }

            int f = _front;

            if(_rear >=f )
            {
                int bytesToEnd = _realBufferSize - _rear;

                if (bytesToEnd >= dataSize)
                {
                    src.CopyTo(_buffer.AsSpan(_rear, dataSize));
                }
                else
                {
                    src.Slice(0, bytesToEnd).CopyTo(_buffer.AsSpan(_rear, bytesToEnd));
                    src.Slice(bytesToEnd).CopyTo(_buffer.AsSpan(0, dataSize-bytesToEnd));
                }
            }
            else
            {
                src.CopyTo(_buffer.AsSpan(_rear, dataSize));
            }

            _rear = (_rear+ dataSize) % _realBufferSize;
            return dataSize;
        }

        // front > rear 데이터 감김 나눠 읽어야함
        // front < rear 데이터 연속 한번에읽음
        public int Dequeue(Span<byte> dest)
        {
            int dataSize = dest.Length;
            if(GetUseSize() < dataSize)
            {
                return 0;
            }

            int r = _rear;

            if ( _front >= r )
            {
                int bytesToEnd = _realBufferSize - _front;
                if(bytesToEnd >= dataSize)
                {
                    _buffer.AsSpan(_front, dataSize).CopyTo(dest);
                }
                else
                {
                    _buffer.AsSpan(_front, bytesToEnd).CopyTo(dest.Slice(0, bytesToEnd));
                    _buffer.AsSpan(0, dataSize - bytesToEnd).CopyTo(dest.Slice(bytesToEnd));
                }
            }
            else
            {
                _buffer.AsSpan(_front, dataSize).CopyTo(dest);
            }

            _front = (_front + dataSize) % _realBufferSize;
            return dataSize;
        }

        public int Peek(Span<byte> dest)
        {
            int dataSize = dest.Length;
            if(GetUseSize() < dataSize) 
            {
                    return 0;
            }

            int bytesToEnd = _realBufferSize - _front;
            if(bytesToEnd >= dataSize)
            {
                _buffer.AsSpan(_front, dataSize).CopyTo(dest);
            }
            else
            {
                _buffer.AsSpan(_front, bytesToEnd).CopyTo(dest.Slice(0, bytesToEnd));
                _buffer.AsSpan(0, dataSize - bytesToEnd).CopyTo(dest.Slice(bytesToEnd));
            }

                return dataSize;
        }


        public int GetDirectEnqueueSize()
        {
            int f = _front;

            if ((_rear + 1) % _realBufferSize == f)
            {
                return 0;
            }

            if (_rear >= f)
            {
                if (f == 0)
                {
                    return _realBufferSize - _rear - 1;
                }
                return _realBufferSize - _rear;
            }

            return _bufferSize - GetUseSize();
        }

        public int GetDirectDequeueSize()
        {
            int r = _rear;
            if(_front > r )
            {
                return _realBufferSize - _front;
            }
            return GetUseSize();
        }

        public int MoveRear(int size)
        {
            _rear = (_rear + size) % _realBufferSize; ;
            return size;
        }

        public int MoveFront(int size)
        {
            _front = (_front + size) % _realBufferSize; ;
            return size;
        }

    }
}
