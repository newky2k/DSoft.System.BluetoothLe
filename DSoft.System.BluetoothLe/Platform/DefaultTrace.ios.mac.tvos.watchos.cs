using System;

namespace Plugin.BLE
{
    static class DefaultTrace
    {
        static DefaultTrace()
        {
            Trace.TraceImplementation = Console.WriteLine;
        }
    }
}