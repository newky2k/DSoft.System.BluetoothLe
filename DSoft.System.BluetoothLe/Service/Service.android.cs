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
        internal Guid NativeGuid => Guid.ParseExact(NativeService.Uuid.ToString(), "d");

        internal bool NativeIsPrimary => NativeService.Type == GattServiceType.Primary;

        internal BluetoothGattService NativeService { get; private set; }

        #endregion

        #region Constructors

        internal Service(BluetoothGattService nativeService, BluetoothGatt gatt, IGattCallback gattCallback, Device device) : this(device)
        {
            NativeService = nativeService;

            _gatt = gatt;
            _gattCallback = gattCallback;
        }

        #endregion

        #region Methods

        internal Task<IList<Characteristic>> GetCharacteristicsNativeAsync()
        {
            return Task.FromResult<IList<Characteristic>>(
                NativeService.Characteristics.Select(characteristic => new Characteristic(characteristic, _gatt, _gattCallback, this))
                .Cast<Characteristic>().ToList());
        }

        #endregion

        public virtual void Dispose()
        {

        }
    }
}