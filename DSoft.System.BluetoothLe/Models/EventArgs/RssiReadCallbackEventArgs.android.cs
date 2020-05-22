using System;
using Android.Bluetooth;
using Plugin.BLE.Contracts;

namespace Plugin.BLE.CallbackEventArgs
{
    public class RssiReadCallbackEventArgs : System.EventArgs
    {
        public Exception Error { get; }
        public int Rssi { get; }

        public RssiReadCallbackEventArgs(Exception error, int rssi)
        {
            Error = error;
            Rssi = rssi;
        }
    }
}