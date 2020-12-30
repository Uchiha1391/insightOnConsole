using System.Text;
using Newtonsoft.Json;

namespace Insight
{
    public class CustomReader
    {
        private byte[] DataStorageBuffer;

        public CustomReader(byte[] dataStorageBuffer)
        {
            DataStorageBuffer = dataStorageBuffer;
        }

        /// <summary>
        /// It deserializes the data from json to c# object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ReadDeserializedData<T>()
        {

            var toString = Encoding.UTF8.GetString(DataStorageBuffer);
            T readDeserializedData = JsonConvert.DeserializeObject<T>(toString);
            return readDeserializedData;

        }
    }

}