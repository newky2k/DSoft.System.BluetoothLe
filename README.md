# DSoft.System.BluetoothLe

`DSoft.System.BluetoothLe` is a cross-platform Bluetooth Low Energy library for modern .NET. It provides one API for scanning, connecting, discovering GATT services/characteristics, reading, writing, and receiving characteristic updates across mobile and desktop targets.

The library started as a fork/repackage of [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) and has been migrated from Xamarin targets to .NET platform targets.

## Supported Targets

| Target | Minimum OS |
| --- | --- |
| `net10.0-android` | Android API 21 |
| `net10.0-ios` | iOS 12.2 |
| `net10.0-maccatalyst` | Mac Catalyst 15.0 |
| `net10.0-macos` | macOS 12.0 |
| `net10.0-tvos` | tvOS 12.2 |
| `net10.0-windows10.0.19041.0` | Windows 10 1809 |
| `net481` | Windows 10 1809 |
| `net10.0` / `netstandard2.0` | API surface only; platform Bluetooth calls throw on unsupported platforms |

## Install

Reference the package from your app project:

```xml
<PackageReference Include="DSoft.System.BluetoothLe" Version="1.0.0" />
```

When working from source, reference the project:

```xml
<ProjectReference Include="..\DSoft.System.BluetoothLe\DSoft.System.BluetoothLe.csproj" />
```

Use the library from the `System.BluetoothLe` namespace:

```csharp
using System.BluetoothLe;
using System.BluetoothLe.EventArgs;
```

## Platform Setup

Your app must request the operating-system permissions needed for Bluetooth. The library does not replace runtime permission prompts or app manifest entries.

### Android

Add Bluetooth permissions to your Android manifest. For Android 12/API 31 and later, apps normally need `BLUETOOTH_SCAN` and `BLUETOOTH_CONNECT`. Older Android versions commonly require `BLUETOOTH`, `BLUETOOTH_ADMIN`, and location permission for scanning.

Example manifest entries:

```xml
<uses-permission android:name="android.permission.BLUETOOTH" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" android:maxSdkVersion="30" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-feature android:name="android.hardware.bluetooth_le" android:required="false" />
```

Request dangerous permissions at runtime before scanning or connecting.

### iOS, macOS, Mac Catalyst, tvOS

Add Bluetooth usage descriptions to your app's `Info.plist` where required by the platform:

```xml
<key>NSBluetoothAlwaysUsageDescription</key>
<string>This app uses Bluetooth to connect to nearby BLE devices.</string>
```

For iOS background BLE scenarios, also configure the appropriate background modes in your app.

### Windows

Windows support uses the Windows Runtime Bluetooth APIs. The .NET for Windows and .NET Framework implementations share the same implementation and require Windows 10 1809 or later with a Bluetooth LE-capable adapter.

Packaged Windows apps should declare the Bluetooth capability in the app manifest. Desktop WPF apps should still handle unavailable Bluetooth hardware/radio states at runtime.

## Quick Start

Get the current platform implementation:

```csharp
var bluetooth = BluetoothLE.Current;

if (!bluetooth.IsAvailable)
{
    throw new InvalidOperationException("Bluetooth LE is not available on this device.");
}

if (!bluetooth.IsOn)
{
    throw new InvalidOperationException("Bluetooth is not turned on.");
}

var adapter = bluetooth.Adapter;
```

Scan for devices:

```csharp
var adapter = BluetoothLE.Current.Adapter;

adapter.DeviceDiscovered += (sender, args) =>
{
    Console.WriteLine($"Found {args.Device.NameOrId} ({args.Device.Id}) RSSI {args.Device.Rssi}");
};

adapter.ScanTimeout = 10000;
adapter.ScanMode = ScanMode.LowLatency;

await adapter.StartScanningForDevicesAsync();
```

Scan for devices that advertise a service:

```csharp
var heartRateService = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");

await adapter.StartScanningForDevicesAsync(
    serviceUuids: new[] { heartRateService },
    deviceFilter: device => !string.IsNullOrWhiteSpace(device.Name),
    allowDuplicatesKey: false);
```

Connect to a discovered device:

```csharp
var device = adapter.DiscoveredDevices.FirstOrDefault();

if (device == null)
{
    throw new InvalidOperationException("No BLE device was discovered.");
}

await adapter.ConnectToDeviceAsync(device);
```

Connect to a known device by id:

```csharp
var knownDeviceId = Guid.Parse("00000000-0000-0000-0000-000000000000");
var device = await adapter.ConnectToKnownDeviceAsync(knownDeviceId);
```

Discover services and characteristics:

```csharp
var services = await device.GetServicesAsync();

foreach (var service in services)
{
    Console.WriteLine($"{service.Name}: {service.Id}");

    var characteristics = await service.GetCharacteristicsAsync();

    foreach (var characteristic in characteristics)
    {
        Console.WriteLine($"  {characteristic.Name}: {characteristic.Id} ({characteristic.Properties})");
    }
}
```

Read and write a characteristic:

```csharp
var serviceId = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");
var characteristicId = Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb");

var service = await device.GetServiceAsync(serviceId);
var characteristic = await service.GetCharacteristicAsync(characteristicId);

if (characteristic.CanRead)
{
    byte[] value = await characteristic.ReadAsync();
}

if (characteristic.CanWrite)
{
    await characteristic.WriteAsync(new byte[] { 0x01, 0x02, 0x03 });
}
```

Subscribe to characteristic updates:

```csharp
characteristic.ValueUpdated += (sender, args) =>
{
    var value = args.Characteristic.Value;
    Console.WriteLine(BitConverter.ToString(value));
};

await characteristic.StartUpdatesAsync();

// Later:
await characteristic.StopUpdatesAsync();
```

Disconnect:

```csharp
await adapter.DisconnectDeviceAsync(device);
```

## Useful API Surface

- `BluetoothLE.Current`: singleton entry point for the current platform.
- `BluetoothLE.State`, `IsAvailable`, `IsOn`: current Bluetooth state.
- `BluetoothLE.StateChanged`: Bluetooth state notifications.
- `Adapter.StartScanningForDevicesAsync`: scan for BLE devices.
- `Adapter.DeviceDiscovered`: raised the first time a device is discovered during a scan.
- `Adapter.DeviceAdvertised`: raised for matching advertisements.
- `Adapter.ConnectToDeviceAsync`: connect to a discovered device.
- `Adapter.ConnectToKnownDeviceAsync`: connect directly by known platform device id.
- `Device.GetServicesAsync`: discover GATT services.
- `Service.GetCharacteristicsAsync`: discover GATT characteristics.
- `Characteristic.ReadAsync`, `WriteAsync`, `StartUpdatesAsync`, `StopUpdatesAsync`: interact with characteristic values.
- `Descriptor.ReadAsync`, `WriteAsync`: interact with descriptors.

## Notes

- BLE device identifiers are platform-specific. Persist known device ids only for the same platform/device context.
- Scanning and connecting require OS permissions and Bluetooth hardware. Always handle `BluetoothState.Unavailable` and `BluetoothState.Off`.
- `net10.0` and `netstandard2.0` builds keep the shared API available, but platform Bluetooth operations require a supported platform target.
- Some Android and CoreBluetooth APIs used by the migrated implementation are marked obsolete by newer SDK analyzers. The library currently preserves the existing behavior while the platform-specific implementations continue to be modernized.

## Building From Source

Restore and build the active solution with the .NET 10 SDK:

```powershell
dotnet restore DSoft.System.BluetoothLe.sln
dotnet build DSoft.System.BluetoothLe.sln --no-restore
```

The old `DSoft.System.BluetoothLeOld` project remains in the repository as migration reference material, but the active solution builds the migrated `DSoft.System.BluetoothLe` project.

## Relationship To Plugin.BLE

This project keeps the broad shape of Plugin.BLE but uses:

- Namespace: `System.BluetoothLe`
- Entry point: `BluetoothLE.Current`
- Multi-targeted partial classes instead of the original base-class layout

Existing Plugin.BLE concepts map closely to this library, but code should be updated to the names above.
