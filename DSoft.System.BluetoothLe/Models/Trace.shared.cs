using System;

namespace Plugin.BLE
{
    public static class Trace
    {
        public static Action<string, object[]> TraceImplementation { get; set; }
        
        public static void Message(string format, params object[] args)
        {
            try
            {
                TraceImplementation?.Invoke(format, args);
            }
            catch { /* ignore */ }
        }
    }
}