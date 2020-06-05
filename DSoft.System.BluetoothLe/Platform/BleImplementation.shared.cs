using System;
using System.BluetoothLe.Contracts;
using System.BluetoothLe.EventArgs;
using System.BluetoothLe.Utils;

namespace System.BluetoothLe
{
    public partial class BleImplementation : IBluetoothLE
    {
        private readonly Lazy<IAdapter> _adapter;
        private BluetoothState _state;

        public event EventHandler<BluetoothStateChangedArgs> StateChanged;

        public bool IsAvailable => _state != BluetoothState.Unavailable;
        public bool IsOn => _state == BluetoothState.On;
        public IAdapter Adapter => _adapter.Value;

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

        internal BleImplementation()
        {
            _adapter = new Lazy<IAdapter>(CreateAdapter, System.Threading.LazyThreadSafetyMode.PublicationOnly);
        }

        public void Initialize()
        {
            InitializeNative();
            State = GetInitialStateNative();
        }

        private IAdapter CreateAdapter()
        {
            if (!IsAvailable)
                return new FakeAdapter();

            return CreateNativeAdapter();
        }
    }
}
