using System;
using System.Diagnostics;

namespace System.BluetoothLe
{
    static class DefaultTrace
    {
        static DefaultTrace()
        {
            //uses WriteLine for trace
            Trace.TraceImplementation = (s, objects) => Debug.WriteLine(s, objects);
        }
    }
}