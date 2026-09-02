using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Bluetooth;
using System.BluetoothLe.Utils;
using System.BluetoothLe.EventArgs;

namespace System.BluetoothLe
{
    public partial class Descriptor
    {
        private readonly BluetoothGatt _gatt;
        private readonly IGattCallback _gattCallback;

        protected Guid NativeGuid => Guid.ParseExact(NativeDescriptor.Uuid.ToString(), "d");

        protected byte[] NativeValue => NativeDescriptor.GetValue() ?? Array.Empty<byte>();

        protected BluetoothGattDescriptor NativeDescriptor { get; private set; }

        internal Descriptor(BluetoothGattDescriptor nativeDescriptor, BluetoothGatt gatt, IGattCallback gattCallback, Characteristic characteristic) : this(characteristic)
        {
            NativeDescriptor = nativeDescriptor;

            _gattCallback = gattCallback;
            _gatt = gatt;
        }

        protected Task WriteNativeAsync(byte[] data, CancellationToken cancellationToken)
        {
            return TaskBuilder.FromEvent<bool, EventHandler<DescriptorCallbackEventArgs>, EventHandler>(
               execute: () => { InternalWrite(data); return Task.CompletedTask; },
               getCompleteHandler: (complete, reject) => ((sender, args) =>
               {
                   if (!IsSameDescriptor(args.Descriptor))
                       return;

                   if (args.Exception != null)
                       reject(new DescriptorWriteException($"Write descriptor {Id} failed with GATT status {args.Status}.", Id, (int)args.Status));
                   else
                       complete(true);
               }),
               subscribeComplete: handler => _gattCallback.DescriptorValueWritten += handler,
               unsubscribeComplete: handler => _gattCallback.DescriptorValueWritten -= handler,
               getRejectHandler: reject => ((sender, args) =>
               {
                   reject(new DescriptorWriteException($"Device '{Characteristic.Service.Device.Id}' disconnected while writing descriptor with {Id}.", Id));
               }),
               subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
               unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
               token: cancellationToken);
        }

        private void InternalWrite(byte[] data)
        {
            if (!NativeDescriptor.SetValue(data))
                throw new DescriptorWriteException("GATT: SET descriptor value failed", Id);

            if (!_gatt.WriteDescriptor(NativeDescriptor))
                throw new DescriptorWriteException("GATT: WRITE descriptor value failed", Id);
        }

        protected async Task<byte[]> ReadNativeAsync(CancellationToken cancellationToken)
        {
            return await TaskBuilder.FromEvent<byte[], EventHandler<DescriptorCallbackEventArgs>, EventHandler>(
               execute: () => { ReadInternal(); return Task.CompletedTask; },
               getCompleteHandler: (complete, reject) => ((sender, args) =>
                  {
                      if (!IsSameDescriptor(args.Descriptor))
                          return;

                      if (args.Exception != null)
                      {
                          reject(new DescriptorReadException($"Read descriptor {Id} failed with GATT status {args.Status}.", Id, (int)args.Status));
                          return;
                      }

                      complete(args.Descriptor.GetValue() ?? Array.Empty<byte>());
                  }),
               subscribeComplete: handler => _gattCallback.DescriptorValueRead += handler,
               unsubscribeComplete: handler => _gattCallback.DescriptorValueRead -= handler,
               getRejectHandler: reject => ((sender, args) =>
               {
                   reject(new DescriptorReadException($"Device '{Characteristic.Service.Device.Id}' disconnected while reading descriptor with {Id}.", Id));
               }),
               subscribeReject: handler => _gattCallback.ConnectionInterrupted += handler,
               unsubscribeReject: handler => _gattCallback.ConnectionInterrupted -= handler,
               token: cancellationToken);
        }

        private void ReadInternal()
        {
            if (!_gatt.ReadDescriptor(NativeDescriptor))
                throw new DescriptorReadException("GATT: read descriptor returned FALSE", Id);
        }

        /// <summary>
        /// Decides whether a callback is about this descriptor and not merely one sharing its UUID.
        /// </summary>
        /// <remarks>
        /// Every notifiable characteristic on a peripheral carries a 0x2902 descriptor, so a match on UUID alone
        /// meant that enabling notifications on one characteristic could be completed - or failed - by the
        /// callback for a different one. The owning characteristic's instance id is what separates them.
        /// </remarks>
        private bool IsSameDescriptor(BluetoothGattDescriptor other)
        {
            if (other == null || NativeDescriptor == null)
                return false;

            if (!SameUuid(other.Uuid, NativeDescriptor.Uuid))
                return false;

            var otherCharacteristic = other.Characteristic;
            var ownCharacteristic = NativeDescriptor.Characteristic;

            if (otherCharacteristic == null || ownCharacteristic == null)
                return true;

            return otherCharacteristic.InstanceId == ownCharacteristic.InstanceId
                && SameUuid(otherCharacteristic.Uuid, ownCharacteristic.Uuid);
        }

        // Java.Util.UUID does not overload ==, so the operator compares managed wrapper references and only
        // happens to work while the runtime hands back the same peer for the same Java object. Equals crosses
        // to Java's own equals and compares the value, which is what is actually meant here.
        private static bool SameUuid(Java.Util.UUID left, Java.Util.UUID right)
            => left != null && right != null && left.Equals(right);
    }
}
