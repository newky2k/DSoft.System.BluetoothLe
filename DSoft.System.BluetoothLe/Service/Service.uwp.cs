using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public class Service : ServiceBase<GattDeviceService>
    {
        public override Guid Id => NativeService.Uuid;

        //method to get parent devices to check if primary is obsolete
        //return true as a placeholder
        public override bool IsPrimary => true;

        public Service(GattDeviceService nativeService, IDevice device) : base(device, nativeService)
        {
        }

        protected override async Task<IList<ICharacteristic>> GetCharacteristicsNativeAsync()
        {
            var result = await NativeService.GetCharacteristicsAsync(BleImplementation.CacheModeGetCharacteristics);
            result.ThrowIfError();

            return result.Characteristics?
                .Select(nativeChar => new Characteristic(nativeChar, this))
                .Cast<ICharacteristic>()
                .ToList();
        }

        public override void Dispose()
        {
            base.Dispose();
            NativeService?.Dispose();
        }
    }
}
