using System;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    internal partial class BleImplementation : IBluetoothLE
    {
        #region Fields
        private readonly Lazy<Adapter> _adapter;
        private BluetoothState _state;
        #endregion

        #region Events
        public event EventHandler<BluetoothStateChangedArgs> StateChanged;

        #endregion

        #region Properties
        public bool IsAvailable => _state != BluetoothState.Unavailable;

        public bool IsOn => _state == BluetoothState.On;

        public Adapter Adapter => _adapter.Value;

        public BluetoothState State
        {
            get => _state;
            protected set
            {
                if (_state == value)
                    return;

                var oldState = _state;
                _state = value;
                StateChanged?.Invoke(this, new BluetoothStateChangedArgs(oldState, _state));
            }
        }

        #endregion

        #region Constructors

        internal BleImplementation()
        {
            _adapter = new Lazy<Adapter>(CreateAdapter, System.Threading.LazyThreadSafetyMode.PublicationOnly);
        }

        #endregion

        #region Methods

        public void Initialize()
        {
            InitializeNative();
            State = GetInitialStateNative();
        }

        private Adapter CreateAdapter()
        {
            return CreateNativeAdapter();
        }

        #endregion
    }
}
