using System;

namespace System.BluetoothLe.EventArgs
{
    public class MtuRequestCallbackEventArgs : System.EventArgs
    {
        public Exception Error { get; }
        public int Mtu { get; }

        public MtuRequestCallbackEventArgs(Exception error, int mtu)
        {
            Error = error;
            Mtu = mtu;
        }
    }
}