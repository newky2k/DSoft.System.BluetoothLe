using System;
using System.BluetoothLe.Contracts;

namespace System.BluetoothLe.CallbackEventArgs
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