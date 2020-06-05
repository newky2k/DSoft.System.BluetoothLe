using System.BluetoothLe.Contracts;

namespace System.BluetoothLe.EventArgs
{
    public class DeviceBondStateChangedEventArgs : System.EventArgs
    {
        public IDevice Device { get; set; }
        public DeviceBondState State { get; set; }
    }
}