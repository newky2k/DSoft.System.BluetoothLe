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
    public partial class Service
    {
        #region Properties
        protected Guid NativeGuid => NativeService.Uuid;

        //method to get parent devices to check if primary is obsolete
        //return true as a placeholder
        protected bool NativeIsPrimary => true;

        protected GattDeviceService NativeService  {get; private set;}

        #endregion

        #region Constructors

        internal Service(GattDeviceService nativeService, IDevice device) : this(device)
        {
            NativeService = nativeService;
        }

        #endregion

        #region Methods

        protected async Task<IList<ICharacteristic>> GetCharacteristicsNativeAsync()
        {
            var result = await NativeService.GetCharacteristicsAsync(BleImplementation.CacheModeGetCharacteristics);
            result.ThrowIfError();

            return result.Characteristics?
                .Select(nativeChar => new Characteristic(nativeChar, this))
                .Cast<ICharacteristic>()
                .ToList();
        }

        public virtual void Dispose()
        {
            
            NativeService?.Dispose();
        }
        #endregion
    }
}
