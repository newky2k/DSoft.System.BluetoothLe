using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.Bluetooth;
using System.BluetoothLe;


namespace System.BluetoothLe
{
    public partial class Service
    {
        #region Fields
        private readonly BluetoothGatt _gatt;
        private readonly IGattCallback _gattCallback;
        #endregion

        #region Properties
        protected Guid NativeGuid => Guid.ParseExact(NativeService.Uuid.ToString(), "d");

        protected bool NativeIsPrimary => NativeService.Type == GattServiceType.Primary;

        protected BluetoothGattService NativeService { get; private set; }

        #endregion

        #region Constructors

        public Service(BluetoothGattService nativeService, BluetoothGatt gatt, IGattCallback gattCallback, IDevice device) : this(device)
        {
            NativeService = nativeService;

            _gatt = gatt;
            _gattCallback = gattCallback;
        }

        #endregion

        #region Methods

        protected Task<IList<ICharacteristic>> GetCharacteristicsNativeAsync()
        {
            return Task.FromResult<IList<ICharacteristic>>(
                NativeService.Characteristics.Select(characteristic => new Characteristic(characteristic, _gatt, _gattCallback, this))
                .Cast<ICharacteristic>().ToList());
        }

        #endregion

        public virtual void Dispose()
        {

        }
    }
}