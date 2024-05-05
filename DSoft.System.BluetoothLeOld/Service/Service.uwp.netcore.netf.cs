using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using System.BluetoothLe.Extensions;
using Windows.Devices.Enumeration;
using Windows.Devices.Bluetooth;

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
            var accessRequestResponse = await NativeService.RequestAccessAsync();

            // Returns Allowed
            if (accessRequestResponse != DeviceAccessStatus.Allowed)
            {
                throw new Exception("Access to service " + NativeService.Uuid.ToString() + " was disallowed w/ response: " + accessRequestResponse);
            }

            var result = await NativeService.GetCharacteristicsAsync(BluetoothLE.CacheModeGetCharacteristics);
            result.ThrowIfError();

            return result.Characteristics?
                .Select(nativeChar => new Characteristic(nativeChar, this))
                .Cast<Characteristic>()
                .ToList();
        }

        internal async Task<Characteristic> GetCharacteristicNativeAsync(Guid characteristicId)
        {
            var accessRequestResponse = await NativeService.RequestAccessAsync();

            // Returns Allowed
            if (accessRequestResponse != DeviceAccessStatus.Allowed)
            {
                throw new Exception("Access to service " + NativeService.Uuid.ToString() + " was disallowed w/ response: " + accessRequestResponse);
            }

            var result = await NativeService.GetCharacteristicsForUuidAsync(characteristicId, BluetoothLE.CacheModeGetCharacteristics);
            result.ThrowIfError();

            if (!result.Characteristics.Any())
                return null;

            var first = result.Characteristics.First();

            return new Characteristic(first, this);
        }

        public virtual void Dispose()
        {
                  
            NativeService?.Dispose();
        }
        #endregion
    }
}
