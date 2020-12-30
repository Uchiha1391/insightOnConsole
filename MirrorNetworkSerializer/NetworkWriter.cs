using System;

namespace Insight
{
    public class NetworkWriter
    {
        byte[] buffer = new byte[1500];
        int position;
        int length;




        public void WriteInt32(int value) => WriteUInt32((uint)value);
        public void WriteUInt32(uint value)
        {
            EnsureLength(position + 4);
            buffer[position++] = (byte)value;
            buffer[position++] = (byte)(value >> 8);
            buffer[position++] = (byte)(value >> 16);
            buffer[position++] = (byte)(value >> 24);
        }


        public void WriteByte(byte value)
        {
            EnsureLength(position + 1);
            buffer[position++] = value;
        }

        void EnsureLength(int value)
        {
            if (length < value)
            {
                length = value;
                EnsureCapacity(value);
            }
        }

        void EnsureCapacity(int value)
        {
            if (buffer.Length < value)
            {
                int capacity = Math.Max(value, buffer.Length * 2);
                Array.Resize(ref buffer, capacity);
            }
        }

        public byte[] ToArray()
        {
            byte[] data = new byte[length];
            Array.ConstrainedCopy(buffer, 0, data, 0, length);
            return data;
        }
    }

    public static class NetworkWriterExtensions
    {
        public static void WriteUInt16(this NetworkWriter writer, ushort value)
        {
            writer.WriteByte((byte) value);
            writer.WriteByte((byte) (value >> 8));
        }
    }
}