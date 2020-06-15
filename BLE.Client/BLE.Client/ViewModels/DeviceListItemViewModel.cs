using System;
using MvvmCross.ViewModels;
using System.BluetoothLe;

namespace BLE.Client.ViewModels
{
    public class DeviceListItemViewModel : MvxNotifyPropertyChanged
    {
        public Device Device { get; private set; }

        public Guid Id => Device.Id;
        public bool IsConnected => Device.State == DeviceState.Connected;
        public int Rssi => Device.Rssi;
        public string Name => Device.Name;

        public DeviceListItemViewModel(Device device)
        {
            Device = device;
        }

        public void Update(Device newDevice = null)
        {
            if (newDevice != null)
            {
                Device = newDevice;
            }
            RaisePropertyChanged(nameof(IsConnected));
            RaisePropertyChanged(nameof(Rssi));
        }
    }
}
