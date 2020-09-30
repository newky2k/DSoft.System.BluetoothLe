using CoreBluetooth;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;

namespace System.BluetoothLe.Platform.Apple
{
    public interface IBleCBPeripheralDelegate
    {
        event EventHandler<CBPeripheralOpenL2CapChannelEventArgs> OnL2CapChannelOpened;
        event EventHandler<CBServiceEventArgs> OnCharacteristicDiscovered;
        event EventHandler<CBCharacteristicEventArgs> OnDescriptorDiscovered;
        event EventHandler<CBServiceEventArgs> OnIncludedServiceDiscovered;
        event EventHandler<NSError> OnServiceDiscovered;
        event EventHandler OnServiceInvalidated;
        event EventHandler OnIsReadyToSendWriteWithoutResponse;
        event EventHandler<CBPeripheralServicesEventArgs> OnServicesModified;
        event EventHandler<CBRssiEventArgs> OnReadRssi;
        event EventHandler<NSError> OnUpdatedRssi;
        event EventHandler<CBCharacteristicEventArgs> OnCharacterteristicValueUpdated;
        event EventHandler OnNameUpdated;
        event EventHandler<CBCharacteristicEventArgs> OnUpdatedNotificationState;
        event EventHandler<CBDescriptorEventArgs> OnUpdatedValue;
        event EventHandler<CBCharacteristicEventArgs> OnWroteCharacteristicValue;
        event EventHandler<CBDescriptorEventArgs> OnWroteDescriptorValue;
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

        public event EventHandler<CBPeripheralOpenL2CapChannelEventArgs> OnL2CapChannelOpened
        {
            add => _didOpenL2CapChannel += value;
            remove => _didOpenL2CapChannel -= value;
        }

        public event EventHandler<CBServiceEventArgs> OnCharacteristicDiscovered
        {
            add => _discoveredCharacteristic += value;
            remove => _discoveredCharacteristic -= value;
        }

        public event EventHandler<CBCharacteristicEventArgs> OnDescriptorDiscovered
        {
            add => _discoveredDescriptor += value;
            remove => _discoveredDescriptor -= value;
        }

        public event EventHandler<CBServiceEventArgs> OnIncludedServiceDiscovered
        {
            add => _discoveredIncludedService += value;
            remove => _discoveredIncludedService -= value;
        }

        public event EventHandler<NSError> OnServiceDiscovered
        {
            add => _discoveredService += value;
            remove => _discoveredService -= value;
        }

        public event EventHandler OnServiceInvalidated
        {
            add => _invalidatedService += value;
            remove => _invalidatedService -= value;
        }

        public event EventHandler OnIsReadyToSendWriteWithoutResponse
        {
            add => _isReadyToSendWriteWithoutResponse += value;
            remove => _isReadyToSendWriteWithoutResponse -= value;
        }

        public event EventHandler<CBPeripheralServicesEventArgs> OnServicesModified
        {
            add => _modifiedServices += value;
            remove => _modifiedServices -= value;
        }

        public event EventHandler<CBRssiEventArgs> OnReadRssi
        {
            add => _rssiRead += value;
            remove => _rssiRead -= value;
        }

        public event EventHandler<NSError> OnUpdatedRssi
        {
            add => _rssiUpdated += value;
            remove => _rssiUpdated -= value;
        }

        public event EventHandler<CBCharacteristicEventArgs> OnCharacterteristicValueUpdated
        {
            add => _updatedCharacterteristicValue += value;
            remove => _updatedCharacterteristicValue -= value;
        }

        public event EventHandler OnNameUpdated
        {
            add => _updatedName += value;
            remove => _updatedName -= value;
        }

        public event EventHandler<CBCharacteristicEventArgs> OnUpdatedNotificationState
        {
            add => _updatedNotificationState += value;
            remove => _updatedNotificationState -= value;
        }

        public event EventHandler<CBDescriptorEventArgs> OnUpdatedValue
        {
            add => _updatedValue += value;
            remove => _updatedValue -= value;
        }

        public event EventHandler<CBCharacteristicEventArgs> OnWroteCharacteristicValue
        {
            add => _wroteCharacteristicValue += value;
            remove => _wroteCharacteristicValue -= value;
        }

        public event EventHandler<CBDescriptorEventArgs> OnWroteDescriptorValue
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
