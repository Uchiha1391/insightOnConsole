using System;
using System.IO;

namespace Insight
{
    public class NetworkReader
    {
        // internal buffer
        // byte[] pointer would work, but we use ArraySegment to also support
        // the ArraySegment constructor
        internal ArraySegment<byte> buffer;

        // 'int' is the best type for .Position. 'short' is too small if we send >32kb which would result in negative .Position
        // -> converting long to int is fine until 2GB of data (MAX_INT), so we don't have to worry about overflows here
        public int Position;

        public NetworkReader(ArraySegment<byte> segment)
        {
            buffer = segment;
        }

        public NetworkReader(byte[] bytes)
        {
            buffer = new ArraySegment<byte>(bytes);
        }

        public byte ReadByte()
        {
            if (Position + 1 > buffer.Count)
            {
                throw new EndOfStreamException("ReadByte out of range:" + ToString());
            }

            return buffer.Array[buffer.Offset + Position++];
        }

        public int ReadInt32() => (int) ReadUInt32();

        public uint ReadUInt32()
        {
            uint value = 0;
            value |= ReadByte();
            value |= (uint) (ReadByte() << 8);
            value |= (uint) (ReadByte() << 16);
            value |= (uint) (ReadByte() << 24);
            return value;
        }
    }

    public static class NetworkReaderExtensions
    {
        public static short ReadInt16(this NetworkReader reader) => (short) reader.ReadUInt16();

        public static ushort ReadUInt16(this NetworkReader reader)
        {
            ushort value = 0;
            value |= reader.ReadByte();
            value |= (ushort) (reader.ReadByte() << 8);
            return value;

        }
    }
}