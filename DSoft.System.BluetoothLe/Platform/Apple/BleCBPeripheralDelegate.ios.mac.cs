using CoreBluetooth;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe.Platform.Apple
{
    public interface IBleCBPeripheralDelegate
    {
        event EventHandler<CBPeripheralOpenL2CapChannelEventArgs> DidOpenL2CapChannel;
        event EventHandler<CBServiceEventArgs> DiscoveredCharacteristic;
        event EventHandler<CBCharacteristicEventArgs> DiscoveredDescriptor;
        event EventHandler<CBServiceEventArgs> DiscoveredIncludedService;
        event EventHandler<NSError> DiscoveredService;
        event EventHandler InvalidatedService;
        event EventHandler IsReadyToSendWriteWithoutResponse;
        event EventHandler<CBPeripheralServicesEventArgs> ModifiedServices;
        event EventHandler<CBRssiEventArgs> RssiRead;
        event EventHandler<NSError> RssiUpdated;
        event EventHandler<CBCharacteristicEventArgs> UpdatedCharacterteristicValue;
        event EventHandler UpdatedName;
        event EventHandler<CBCharacteristicEventArgs> UpdatedNotificationState;
        event EventHandler<CBDescriptorEventArgs> UpdatedValue;
        event EventHandler<CBCharacteristicEventArgs> WroteCharacteristicValue;
        event EventHandler<CBDescriptorEventArgs> WroteDescriptorValue;
    }

    public class BleCBPeripheralDelegate : CBPeripheralDelegate, IBleCBPeripheralDelegate
    {
        #region Fields
        private event EventHandler<CBPeripheralOpenL2CapChannelEventArgs> _didOpenL2CapChannel;
        private event EventHandler<CBServiceEventArgs> _discoveredCharacteristic;
        private event EventHandler<CBCharacteristicEventArgs> _discoveredDescriptor;
        private event EventHandler<CBServiceEventArgs> _discoveredIncludedService;
        private event EventHandler<NSError> _discoveredService;
        private event EventHandler _invalidatedService;
        private event EventHandler _isReadyToSendWriteWithoutResponse;
        private event EventHandler<CBPeripheralServicesEventArgs> _modifiedServices;
        private event EventHandler<CBRssiEventArgs> _rssiRead;
        private event EventHandler<NSError> _rssiUpdated;
        private event EventHandler<CBCharacteristicEventArgs> _updatedCharacterteristicValue;
        private event EventHandler _updatedName;
        private event EventHandler<CBCharacteristicEventArgs> _updatedNotificationState;
        private event EventHandler<CBDescriptorEventArgs> _updatedValue;
        private event EventHandler<CBCharacteristicEventArgs> _wroteCharacteristicValue;
        private event EventHandler<CBDescriptorEventArgs> _wroteDescriptorValue;
        #endregion

        #region Properties

        event EventHandler<CBPeripheralOpenL2CapChannelEventArgs> IBleCBPeripheralDelegate.DidOpenL2CapChannel
        {
            add => _didOpenL2CapChannel += value;
            remove => _didOpenL2CapChannel -= value;
        }

        event EventHandler<CBServiceEventArgs> IBleCBPeripheralDelegate.DiscoveredCharacteristic
        {
            add => _discoveredCharacteristic += value;
            remove => _discoveredCharacteristic -= value;
        }

        event EventHandler<CBCharacteristicEventArgs> IBleCBPeripheralDelegate.DiscoveredDescriptor
        {
            add => _discoveredDescriptor += value;
            remove => _discoveredDescriptor -= value;
        }

        event EventHandler<CBServiceEventArgs> IBleCBPeripheralDelegate.DiscoveredIncludedService
        {
            add => _discoveredIncludedService += value;
            remove => _discoveredIncludedService -= value;
        }

        event EventHandler<NSError> IBleCBPeripheralDelegate.DiscoveredService
        {
            add => _discoveredService += value;
            remove => _discoveredService -= value;
        }

        event EventHandler IBleCBPeripheralDelegate.InvalidatedService
        {
            add => _invalidatedService += value;
            remove => _invalidatedService -= value;
        }

        event EventHandler IBleCBPeripheralDelegate.IsReadyToSendWriteWithoutResponse
        {
            add => _isReadyToSendWriteWithoutResponse += value;
            remove => _isReadyToSendWriteWithoutResponse -= value;
        }

        event EventHandler<CBPeripheralServicesEventArgs> IBleCBPeripheralDelegate.ModifiedServices
        {
            add => _modifiedServices += value;
            remove => _modifiedServices -= value;
        }

        event EventHandler<CBRssiEventArgs> IBleCBPeripheralDelegate.RssiRead
        {
            add => _rssiRead += value;
            remove => _rssiRead -= value;
        }

        event EventHandler<NSError> IBleCBPeripheralDelegate.RssiUpdated
        {
            add => _rssiUpdated += value;
            remove => _rssiUpdated -= value;
        }

        event EventHandler<CBCharacteristicEventArgs> IBleCBPeripheralDelegate.UpdatedCharacterteristicValue
        {
            add => _updatedCharacterteristicValue += value;
            remove => _updatedCharacterteristicValue -= value;
        }

        event EventHandler IBleCBPeripheralDelegate.UpdatedName
        {
            add => _updatedName += value;
            remove => _updatedName -= value;
        }

        event EventHandler<CBCharacteristicEventArgs> IBleCBPeripheralDelegate.UpdatedNotificationState
        {
            add => _updatedNotificationState += value;
            remove => _updatedNotificationState -= value;
        }

        event EventHandler<CBDescriptorEventArgs> IBleCBPeripheralDelegate.UpdatedValue
        {
            add => _updatedValue += value;
            remove => _updatedValue -= value;
        }

        event EventHandler<CBCharacteristicEventArgs> IBleCBPeripheralDelegate.WroteCharacteristicValue
        {
            add => _wroteCharacteristicValue += value;
            remove => _wroteCharacteristicValue -= value;
        }

        event EventHandler<CBDescriptorEventArgs> IBleCBPeripheralDelegate.WroteDescriptorValue
        {
            add => _wroteDescriptorValue += value;
            remove => _wroteDescriptorValue -= value;
        }
        #endregion

        #region Event Wiring
        public override void DidOpenL2CapChannel(CBPeripheral peripheral, CBL2CapChannel channel, NSError error) => _didOpenL2CapChannel?.Invoke(this, new CBPeripheralOpenL2CapChannelEventArgs(channel, error));

        public override void DiscoveredCharacteristic(CBPeripheral peripheral, CBService service, NSError error) => _discoveredCharacteristic?.Invoke(this, new CBServiceEventArgs(service, error));

        public override void DiscoveredDescriptor(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) => _discoveredDescriptor?.Invoke(this, new CBCharacteristicEventArgs(characteristic, error));

        public override void DiscoveredIncludedService(CBPeripheral peripheral, CBService service, NSError error) => _discoveredIncludedService?.Invoke(this, new CBServiceEventArgs(service, error));

        public override void DiscoveredService(CBPeripheral peripheral, NSError error) => _discoveredService?.Invoke(this, error);

        public override void InvalidatedService(CBPeripheral peripheral) => _invalidatedService?.Invoke(this, null);

        public override void IsReadyToSendWriteWithoutResponse(CBPeripheral peripheral) => _isReadyToSendWriteWithoutResponse?.Invoke(this, null);

        public override void ModifiedServices(CBPeripheral peripheral, CBService[] services) => _modifiedServices?.Invoke(this, new CBPeripheralServicesEventArgs(services));

        public override void RssiRead(CBPeripheral peripheral, NSNumber rssi, NSError error) => _rssiRead?.Invoke(this, new CBRssiEventArgs(rssi, error));

        public override void RssiUpdated(CBPeripheral peripheral, NSError error) => _rssiUpdated?.Invoke(this, error);

        public override void UpdatedCharacterteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) => _updatedCharacterteristicValue?.Invoke(this, new CBCharacteristicEventArgs(characteristic, error));

        public override void UpdatedName(CBPeripheral peripheral) => _updatedName?.Invoke(this, null);

        public override void UpdatedNotificationState(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) => _updatedNotificationState?.Invoke(this, new CBCharacteristicEventArgs(characteristic, error));

        public override void UpdatedValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error) => _updatedValue?.Invoke(this, new CBDescriptorEventArgs(descriptor, error));

        public override void WroteCharacteristicValue(CBPeripheral peripheral, CBCharacteristic characteristic, NSError error) => _wroteCharacteristicValue?.Invoke(this, new CBCharacteristicEventArgs(characteristic, error));

        public override void WroteDescriptorValue(CBPeripheral peripheral, CBDescriptor descriptor, NSError error) => _wroteDescriptorValue?.Invoke(this, new CBDescriptorEventArgs(descriptor, error));


        #endregion
    }
}
