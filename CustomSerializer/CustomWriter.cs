using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Insight
{
    class CustomWriter
    {
        private List<byte[]> BufferList { get; } = new List<byte[]>();

        public Byte[] GetBuffer => BufferList
            .SelectMany(a => a)
            .ToArray();


        public void WriteData(object data)
        {
            var SerializedStringData = JsonConvert.SerializeObject(data);

            BufferList.Add(Encoding.ASCII.GetBytes(SerializedStringData));
        }
    }
}