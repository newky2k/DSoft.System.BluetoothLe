using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        internal async Task<IList<Characteristic>> GetCharacteristicsNativeAsync(CancellationToken cancellationToken)
        {
            var accessRequestResponse = await NativeService.RequestAccessAsync().AsTask(cancellationToken);

            // Windows gates service access on a per-application consent prompt, so a refusal here is a normal
            // runtime outcome rather than a programming error, and it is reported as one the consumer can catch.
            if (accessRequestResponse != DeviceAccessStatus.Allowed)
            {
                throw new GattCommunicationException(
                    $"Access to service {NativeService.Uuid} was disallowed with response: {accessRequestResponse}.");
            }

            var result = await NativeService.GetCharacteristicsAsync(BluetoothLE.CacheModeGetCharacteristics).AsTask(cancellationToken);
            result.ThrowIfError();

            // An empty list rather than null: a service with no characteristics is unusual but legal, and
            // returning null made the shared cache rediscover on every call and forced null checks on callers.
            return result.Characteristics?
                .Select(nativeChar => new Characteristic(nativeChar, this))
                .ToList() ?? (IList<Characteristic>)Array.Empty<Characteristic>();
        }

        partial void DisposeNative()
        {
            NativeService?.Dispose();
        }

        #endregion
    }
}
