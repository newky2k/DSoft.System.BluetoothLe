﻿using System;
using Plugin.BLE.Contracts;
using Plugin.BLE.EventArgs;
using Plugin.BLE.Utils;

namespace Plugin.BLE
{
    public abstract class BleImplementationBase : IBluetoothLE
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

        protected BleImplementationBase()
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

        protected abstract void InitializeNative();
        protected abstract BluetoothState GetInitialStateNative();
        protected abstract IAdapter CreateNativeAdapter();
    }
}