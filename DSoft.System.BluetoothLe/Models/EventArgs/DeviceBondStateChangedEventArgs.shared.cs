using Plugin.BLE.Contracts;

namespace Plugin.BLE.EventArgs
{
    public class DeviceBondStateChangedEventArgs : System.EventArgs
    {
        public IDevice Device { get; set; }
        public DeviceBondState State { get; set; }
    }
}