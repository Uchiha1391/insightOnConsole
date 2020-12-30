using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestingOdinSerializerWithoutUnity
{
    public static class MyTime
    {
        public static int GetElapsedTImeInSeconds { get; set; } = 0;


        public static async Task RunTimeLoop()
        {
            while (true)
            {
                await Task.Delay(3000);
                TimeSpan Span = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
                GetElapsedTImeInSeconds = Span.Seconds;
            }

        }
    }
}
