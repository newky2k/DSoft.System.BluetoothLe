using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    /// <summary>
    /// Convenience overloads over <see cref="Adapter"/>.
    /// </summary>
    /// <remarks>
    /// Renamed from <c>AdapterExtenstion</c> in 4.0. The old name was a misspelling, and the type is
    /// referenced by name in every consumer that writes an explicit call rather than an extension-method call.
    /// </remarks>
    public static class AdapterExtensions
    {
        /// <summary>
        /// Starts scanning for BLE devices.
        /// </summary>
        /// <param name="adapter">Target adapter.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous read operation. The Task will finish after the scan has ended.</returns>
        public static Task StartScanningForDevicesAsync(this Adapter adapter, CancellationToken cancellationToken)
        {
            return adapter.StartScanningForDevicesAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Starts scanning for BLE devices that advertise the services included in <paramref name="serviceUuids"/>.
        /// </summary>
        /// <param name="adapter">Target adapter.</param>
        /// <param name="serviceUuids">Requested service Ids.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous read operation. The Task will finish after the scan has ended.</returns>
        public static Task StartScanningForDevicesAsync(this Adapter adapter, Guid[] serviceUuids, CancellationToken cancellationToken = default)
        {
            return adapter.StartScanningForDevicesAsync(serviceUuids, null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Starts scanning for BLE devices that fulfill the <paramref name="deviceFilter"/>.
        /// DeviceDiscovered will only be called, if <paramref name="deviceFilter"/> returns <c>true</c> for the discovered device.
        /// </summary>
        /// <param name="adapter">Target adapter.</param>
        /// <param name="deviceFilter">Function that filters the devices.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous read operation. The Task will finish after the scan has ended.</returns>
        public static Task StartScanningForDevicesAsync(this Adapter adapter, Func<Device, bool> deviceFilter, CancellationToken cancellationToken = default)
        {
            return adapter.StartScanningForDevicesAsync(deviceFilter: deviceFilter, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Scans until the device with <paramref name="deviceId"/> is discovered, and returns it.
        /// </summary>
        public static Task<Device> DiscoverDeviceAsync(this Adapter adapter, Guid deviceId, CancellationToken cancellationToken = default)
        {
            return DiscoverDeviceAsync(adapter, device => device.Id == deviceId, cancellationToken);
        }

        /// <summary>
        /// Scans until a device the <paramref name="deviceFilter"/> accepts is discovered, and returns it.
        /// </summary>
        /// <exception cref="DeviceDiscoverException">The scan timed out without discovering a matching device.</exception>
        public static async Task<Device> DiscoverDeviceAsync(this Adapter adapter, Func<Device, bool> deviceFilter, CancellationToken cancellationToken = default)
        {
            var device = adapter.DiscoveredDevices.FirstOrDefault(deviceFilter);
            if (device != null)
            {
                return device;
            }

            if (adapter.IsScanning)
            {
                // Now genuinely waits for the running scan to stop, which is what makes the start below safe:
                // from 4.0 onwards, starting a scan while one is running throws.
                await adapter.StopScanningForDevicesAsync().ConfigureAwait(false);
            }

            return await TaskBuilder.FromEvent<Device, EventHandler<DeviceEventArgs>, EventHandler>(
                execute: () => adapter.StartScanningForDevicesAsync(deviceFilter, cancellationToken),

                getCompleteHandler: (complete, reject) => ((sender, args) =>
                {
                    complete(args.Device);

                    // Deliberately not awaited, and the discard records that. This is an event handler running
                    // on the platform's scan callback thread; the task it returns does not complete until the
                    // scan that raised this event has finished tearing down, so awaiting it here would block
                    // the very callback the teardown has to unwind through.
                    _ = adapter.StopScanningForDevicesAsync();
                }),
                subscribeComplete: handler => adapter.DeviceDiscovered += handler,
                unsubscribeComplete: handler => adapter.DeviceDiscovered -= handler,

                getRejectHandler: reject => ((sender, args) => { reject(new DeviceDiscoverException()); }),
                subscribeReject: handler => adapter.ScanTimeoutElapsed += handler,
                unsubscribeReject: handler => adapter.ScanTimeoutElapsed -= handler,

                token: cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Connects to the <paramref name="device"/>.
        /// </summary>
        /// <param name="adapter">Target adapter.</param>
        /// <param name="device">Device to connect to.</param>
        /// <param name="connectParameters">Connection parameters. Contains platform specific parameters needed to achieved connection. The default value is None.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous read operation. The Task will finish after the device has been connected successfuly.</returns>
        /// <exception cref="DeviceConnectionException">Thrown if the device connection fails.</exception>
        public static Task ConnectToDeviceAsync(this Adapter adapter, Device device, ConnectParameters connectParameters, CancellationToken cancellationToken)
        {
            return adapter.ConnectToDeviceAsync(device, connectParameters: connectParameters, cancellationToken: cancellationToken);
        }
    }
}
