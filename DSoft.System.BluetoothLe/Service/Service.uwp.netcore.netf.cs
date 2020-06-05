using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe.Extensions;

namespace System.BluetoothLe
{
    public partial class Service
    {
        #region Properties
        internal Guid NativeGuid => NativeService.Uuid;

        //method to get parent devices to check if primary is obsolete
        //return true as a placeholder
        internal bool NativeIsPrimary => true;

        internal GattDeviceService NativeService  {get; private set;}

        #endregion

        #region Constructors

        internal Service(GattDeviceService nativeService, Device device) : this(device)
        {
            NativeService = nativeService;
        }

        #endregion

        #region Methods

        internal async Task<IList<Characteristic>> GetCharacteristicsNativeAsync()
        {
            var result = await NativeService.GetCharacteristicsAsync(BleImplementation.CacheModeGetCharacteristics);
            result.ThrowIfError();

            return result.Characteristics?
                .Select(nativeChar => new Characteristic(nativeChar, this))
                .Cast<Characteristic>()
                .ToList();
        }

        public virtual void Dispose()
        {
            
            NativeService?.Dispose();
        }
        #endregion
    }
}
