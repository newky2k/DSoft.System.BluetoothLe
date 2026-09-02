using System;
using System.Threading;
using System.Threading.Tasks;
using CoreBluetooth;
using System.BluetoothLe;
using Foundation;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    public partial class Descriptor
    {

        protected Guid NativeGuid => NativeDescriptor.UUID.GuidFromUuid();

        protected CBDescriptor NativeDescriptor { get; private set; }

        protected byte[] NativeValue
        {
            get
            {
                switch (NativeDescriptor.Value)
                {
                    case NSData data:
                        return data.ToArray();
                    case NSNumber number:
                        return BitConverter.GetBytes(number.UInt64Value);
                    case NSString nsString:
                        return System.Text.Encoding.UTF8.GetBytes(nsString.ToString());
                    default:
                        //TODO https://developer.apple.com/reference/corebluetooth/cbuuid/1667288-characteristic_descriptors
                        Trace.Message($"Descriptor: can't convert {NativeDescriptor.Value?.GetType().Name} with value {NativeDescriptor.Value?.ToString()} to byte[]");
                        return null;
                }
            }
        }

        private readonly CBPeripheral _parentDevice;
        private readonly IBleCentralManagerDelegate _bleCentralManagerDelegate;

        internal Descriptor(CBDescriptor nativeDescriptor, CBPeripheral parentDevice, Characteristic characteristic, IBleCentralManagerDelegate bleCentralManagerDelegate) : this(characteristic)
        {
            NativeDescriptor = nativeDescriptor;

            _parentDevice = parentDevice;
            _bleCentralManagerDelegate = bleCentralManagerDelegate;
        }

        protected Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken)
        {
            var exception = new DescriptorReadException($"Device '{Characteristic.Service.Device.Id}' disconnected while reading descriptor with {Id}.", Id);

            return TaskBuilder.FromEvent<byte[], EventHandler<CBDescriptorEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                   execute: () =>
                   {
                       if (_parentDevice.State != CBPeripheralState.Connected)
                           throw exception;

                       _parentDevice.ReadValue(NativeDescriptor);

                       return Task.CompletedTask;
                   },
                   getCompleteHandler: (complete, reject) => (sender, args) =>
                   {
                       if (!IsSameDescriptor(args.Descriptor))
                           return;

                       if (args.Error != null)
                           reject(new DescriptorReadException($"Read descriptor async error: {args.Error.Description}", Id, (int)args.Error.Code));
                       else
                           complete(Value);
                   },
                   subscribeComplete: handler => _parentDevice.UpdatedValue += handler,
                   unsubscribeComplete: handler => _parentDevice.UpdatedValue -= handler,
                   getRejectHandler: reject => ((sender, args) =>
                   {
                       if (args.Peripheral.Identifier == _parentDevice.Identifier)
                           reject(exception);
                   }),
                   subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                   unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                   token: cancellationToken);
        }

        protected Task WriteNativeAsync(byte[] data, CancellationToken cancellationToken)
        {
            var exception = new DescriptorWriteException($"Device '{Characteristic.Service.Device.Id}' disconnected while writing descriptor with {Id}.", Id);

            return TaskBuilder.FromEvent<bool, EventHandler<CBDescriptorEventArgs>, EventHandler<CBPeripheralErrorEventArgs>>(
                    execute: () =>
                    {
                        if (_parentDevice.State != CBPeripheralState.Connected)
                            throw exception;

                        _parentDevice.WriteValue(NSData.FromArray(data), NativeDescriptor);

                        return Task.CompletedTask;
                    },
                    getCompleteHandler: (complete, reject) => (sender, args) =>
                    {
                        if (!IsSameDescriptor(args.Descriptor))
                            return;

                        if (args.Error != null)
                            reject(new DescriptorWriteException(args.Error.Description, Id, (int)args.Error.Code));
                        else
                            complete(true);
                    },
                    subscribeComplete: handler => _parentDevice.WroteDescriptorValue += handler,
                    unsubscribeComplete: handler => _parentDevice.WroteDescriptorValue -= handler,
                    getRejectHandler: reject => ((sender, args) =>
                    {
                        if (args.Peripheral.Identifier == _parentDevice.Identifier)
                            reject(exception);
                    }),
                    subscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral += handler,
                    unsubscribeReject: handler => _bleCentralManagerDelegate.DisconnectedPeripheral -= handler,
                    token: cancellationToken);
        }

        /// <summary>
        /// Decides whether a delegate callback is about this descriptor and not merely one sharing its UUID.
        /// </summary>
        /// <remarks>
        /// Every notifiable characteristic carries a 0x2902 descriptor, so matching on UUID alone let one
        /// characteristic's subscription be completed by another's callback. The native handle identifies the
        /// attribute exactly.
        /// </remarks>
        private bool IsSameDescriptor(CBDescriptor other)
            => other != null && NativeDescriptor != null && other.Handle == NativeDescriptor.Handle;
    }
}
