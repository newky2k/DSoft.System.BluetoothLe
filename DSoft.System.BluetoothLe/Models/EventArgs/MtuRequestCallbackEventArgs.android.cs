using System;
using Plugin.BLE.Contracts;

namespace Plugin.BLE.CallbackEventArgs
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