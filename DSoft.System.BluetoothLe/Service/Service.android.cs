using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using System.BluetoothLe;
using System.BluetoothLe.Contracts;

namespace System.BluetoothLe
{
    public class Service : ServiceBase<BluetoothGattService>
    {
        private readonly BluetoothGatt _gatt;
        private readonly IGattCallback _gattCallback;

        public override Guid Id => Guid.ParseExact(NativeService.Uuid.ToString(), "d");
        public override bool IsPrimary => NativeService.Type == GattServiceType.Primary;

        public Service(BluetoothGattService nativeService, BluetoothGatt gatt, IGattCallback gattCallback, IDevice device) 
            : base(device, nativeService)
        {
            _gatt = gatt;
            _gattCallback = gattCallback;
        }

        protected override Task<IList<ICharacteristic>> GetCharacteristicsNativeAsync()
        {
            return Task.FromResult<IList<ICharacteristic>>(
                NativeService.Characteristics.Select(characteristic => new Characteristic(characteristic, _gatt, _gattCallback, this))
                .Cast<ICharacteristic>().ToList());
        }
    }
}