using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib.net
{
    public class BufferLooper
    {
        byte[] buffer;


        public int Length = 0;
        public int Position = 0;
        public int WriteLength = 0;

        public BufferLooper(int size):this(new byte[(int)(size*1.5f)])
        {
        }
        public BufferLooper(byte[] buffer)
        {
            this.buffer = buffer;
            Length = buffer.Length;
        }
        public void Write(Span<byte> data,int offset,int len)
        {
            lock(buffer)
            if(Position + len > Length)
            {
                byte[] newBuffer = new byte[Length * 2];
                buffer.CopyTo(newBuffer.AsSpan());
                buffer = newBuffer;
                Length = buffer.Length;
            }
            data.Slice(offset,len).CopyTo(buffer.AsSpan(Position));
            Position += len;
            WriteLength = Math.Max(Position,WriteLength);
        }
        public void Write(Span<byte> data)
        {
            lock(buffer)
            if(Position + data.Length > Length)
            {
                byte[] newBuffer = new byte[Length * 2];
                buffer.CopyTo(newBuffer.AsSpan());
                buffer = newBuffer;
                Length = buffer.Length;
            }
            data.CopyTo(buffer.AsSpan(Position));
            Position += data.Length;
        }

        public void Read(Span<byte> data, int offset, int len)
        {
            lock(buffer)
            buffer.AsSpan(Position, len).CopyTo(data.Slice(offset, len));
            Position += len;
        }
    }
}
