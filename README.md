# DSoft.System.BluetoothLe

`DSoft.System.BluetoothLe` is a cross-platform Bluetooth Low Energy library for modern .NET. It provides one API for scanning, connecting, discovering GATT services/characteristics, reading, writing, and receiving characteristic updates across mobile and desktop targets.

The library started as a fork/repackage of [Plugin.BLE](https://github.com/dotnet-bluetooth-le/dotnet-bluetooth-le) and has been migrated from Xamarin targets to .NET platform targets.

## Supported Targets

| Target | Minimum OS |
| --- | --- |
| `net10.0-android` | Android API 21 |
| `net10.0-ios` | iOS 15.0 |
| `net10.0-maccatalyst` | Mac Catalyst 15.0 |
| `net10.0-macos` | macOS 15.0 |
| `net10.0-tvos` | tvOS 15.0 |
| `net10.0-windows10.0.19041.0` | Windows 10 1809 |
| `net481` | Windows 10 1809 |
| `net10.0` / `netstandard2.0` | API surface only; platform Bluetooth calls throw on unsupported platforms |

## Install

Reference the package from your app project:

```xml
<PackageReference Include="DSoft.System.BluetoothLe" Version="4.0.*" />
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

// Await readiness rather than testing IsOn. A radio that has only just been brought up has not settled and
// reports BluetoothState.Unknown, so an immediate IsOn check fails against a perfectly good adapter.
var state = await bluetooth.WaitForStateAsync(BluetoothState.On, cancellationToken);

var adapter = bluetooth.Adapter;
```

If you would rather report the real state to the user than wait for a particular one, await
`WaitForAvailabilityAsync()`, which completes as soon as the radio settles into any determinate state -
including `Off` and `Unauthorized`.

Scan for devices:

```csharp
var adapter = BluetoothLE.Current.Adapter;

adapter.DeviceDiscovered += (sender, args) =>
{
    Console.WriteLine($"Found {args.Device.NameOrId} ({args.Device.Id}) RSSI {args.Device.Rssi}");
};

adapter.ScanTimeout = 10000; // milliseconds
adapter.ScanMode = ScanMode.LowLatency;

await adapter.StartScanningForDevicesAsync(cancellationToken: cancellationToken);
```

Starting a scan while one is already running throws `InvalidOperationException`, and a scan the platform
refuses to start (radio off, permission denied, unsupported) throws `AdapterScanException` carrying a
`ScanFailureReason` rather than completing empty after the full timeout.

Scan for devices that advertise a service:

```csharp
var heartRateService = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");

await adapter.StartScanningForDevicesAsync(
    serviceUuids: new[] { heartRateService },
    deviceFilter: device => !string.IsNullOrWhiteSpace(device.Name),
    allowDuplicatesKey: false,
    cancellationToken: cancellationToken);
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
- `Device.BondState`: the device's bond (pairing) state, as the platform reports it.
- `Adapter.DeviceBondStateChanged`: raised on Android when a device's bond state changes.
- `IGattStatusException.GattStatus`, `GattFailure`: the GATT status on a failed read or write.
- `Device.CreateBondAsync`, `RemoveBondAsync`: request or remove a bond on Android.

## Bonding

A peripheral that requires an encrypted link only answers a central it is bonded (paired) with. What an app can see of that bond depends on the platform.

| Platform | `Device.BondState` | `Adapter.DeviceBondStateChanged` |
| --- | --- | --- |
| Android | `NotBonded`, `Bonding` or `Bonded`, read from the system on every call | Raised on the main thread for every bond change the system broadcasts, including changes made in the system Bluetooth settings |
| iOS, macOS, Mac Catalyst, tvOS | Always `Unknown` | Never raised |
| Windows | `Bonded` or `NotBonded`, from the device's pairing information | Never raised |
| `net10.0` / `netstandard2.0` | Always `Unknown` | Never raised |

**Apple platforms.** CoreBluetooth gives an app no way to read or request a bond. The operating system pairs on demand: the first time the app reads, writes or subscribes to a characteristic the peripheral protects, iOS shows the pairing prompt itself, and once the user accepts the link is encrypted for that and later operations. If the user declines, or the peripheral has lost its keys (for example after a factory reset), the operation fails, and the user has to remove the device in Settings > Bluetooth before pairing again. Design the app around attempting the protected operation and handling its failure, not around checking the bond first.

**Android.** On Android 12 and later, reading the bond state and receiving bond changes both need the `BLUETOOTH_CONNECT` permission. Without it `BondState` reports `Unknown` and the event is not raised. Android, like Apple platforms, normally starts bonding by itself the first time a protected characteristic is accessed; `DeviceBondStateChanged` reports the outcome.

**Requesting and removing a bond (Android).** `Device.CreateBondAsync` starts bonding, showing the system pairing prompt where the peripheral asks for one, and completes with `true` once the device is bonded or `false` if bonding fails or is refused. Use it for an explicit "Pair" action instead of waiting for the first protected operation. `Device.RemoveBondAsync` forgets a stale bond, for example after the peripheral has been factory reset. Android has no public API for removal, so it is **best effort**: it calls a hidden platform method that newer Android versions may refuse, and returns `false` when it cannot remove the bond. In that case, send the user to the system Bluetooth settings. Both methods return `false` on Apple platforms and Windows. A cancellation token stops the wait, but not bonding the system has already started.

```csharp
if (device.BondState != DeviceBondState.Bonded && !await device.CreateBondAsync(cancellationToken))
{
    // Not bonded: tell the user to pair, or to forget the device in Settings and try again.
}
```

**Recognising a security failure.** A peripheral that requires a bonded, encrypted link refuses an unbonded central with ATT error Insufficient Authentication (0x05) or Insufficient Encryption (0x0F). The read and write exceptions (`CharacteristicReadException`, `CharacteristicWriteException`, `DescriptorReadException` and `DescriptorWriteException`) carry that status in `GattStatus`, classified in `GattFailure`, on Android, Apple platforms and Windows. All four implement `IGattStatusException`, so one catch covers them:

```csharp
try
{
    await characteristic.WriteAsync(data, cancellationToken);
}
catch (BleException ex) when (ex is IGattStatusException { GattFailure: GattFailure.InsufficientAuthentication or GattFailure.InsufficientEncryption })
{
    // Ask the user to pair, or to forget the device in Settings and pair again.
}
```

Subscribing with `StartUpdatesAsync` fails as a `DescriptorWriteException` on Android, where it writes the configuration descriptor itself, and as a `CharacteristicReadException` on Apple platforms. `GattStatus` is `null`, and `GattFailure` is `None`, when no GATT status was reported: the operation could not start, the device disconnected, or on Apple platforms the error was not an ATT error. `NativeStatus` and the exception messages are unchanged.

`BondState` never throws: it reports `Unknown` wherever the state cannot be read, including for a stand-in device and after the device has been disposed.

## Notes

- BLE device identifiers are platform-specific. Persist known device ids only for the same platform/device context.
- Scanning and connecting require OS permissions and Bluetooth hardware. Always handle `BluetoothState.Unavailable` and `BluetoothState.Off`.
- `net10.0` and `netstandard2.0` builds keep the shared API available, but platform Bluetooth operations require a supported platform target.
- Some Android APIs used by the implementation are marked obsolete by newer SDK analyzers (CA1422). Migrating to the API 33 GATT overloads rewrites the notification delivery path, so it is deliberately deferred until it can be tested against real hardware; the behaviour is correct today.
- Every event this library raises is raised on a native callback thread - the Android GATT callback thread or the CoreBluetooth delegate queue - and is not marshalled to your UI thread. Marshal in your handler.
- `Trace.TraceImplementation` receives every diagnostic message the library emits. It writes to `System.Diagnostics.Trace` by default; assign your own delegate to bridge it to your logging framework, or set it to `null` to silence the library.
- `Device` and `Service` can be derived outside the library to build a stand-in peripheral where no radio is present, for a device picker or for unit tests of code that takes a `Device`. Derive from `Device` and call `protected Device(Guid id, string name, int rssi = 0)`; derive from `Service` and call `protected Service(Device device)`. A stand-in has no adapter or native peripheral, so members that reach the platform fail as they do on a target without Bluetooth support. In 4.0 this constructor replaces the parameterless `protected Device()` that 3.x exposed, and `Device.Id` is no longer assignable from outside the library - pass the identity to the constructor instead.
- Upgrading from 3.x: `AdvertisementRecordType.UuidsIncomple16Bit` was renamed to `UuidsIncomplete16Bit` in 4.0, a spelling correction that breaks compilation of any code that read that record type. Rename the reference; the value (`0x02`) is unchanged.

## Building From Source

Restore and build the active solution with the .NET 10 SDK:

```powershell
dotnet restore DSoft.System.BluetoothLe.slnx
dotnet build DSoft.System.BluetoothLe.slnx --no-restore
```

`net10.0-windows10.0.19041.0` and `net481` are only built when the host is Windows; on macOS and Linux the
remaining seven target frameworks build and the two Windows ones are skipped.

### Continuous integration

GitHub Actions (`.github/workflows/`):

- `ci.yml`: runs on pull requests to `main` and `development`. Builds Release for every target framework. Publishes nothing.
- `release.yml`: runs on every push to `main` (changes only to Markdown or workflow files are skipped; run it by hand to test a workflow change). Builds Release as `4.0.yyMM.<run number + 100>` plus `RELEASE_SUFFIX` (empty for a stable version; set it to `-prerelease` in the workflow to publish a prerelease), uploads the package as the `drop` artifact and pushes it to nuget.org with Trusted Publishing (OIDC, `NUGET_USER` secret, `nuget` environment), then tags the commit `v<version>` and creates a GitHub release with the package attached. `AssemblyVersion` is not stamped by CI: it stays pinned in the project file (see "Strong naming").

### Strong naming

The assembly is strong-named with `DSoft.snk`, which is committed to this repository on purpose. A strong
name is an identity, not a signature: it lets the runtime tell this assembly apart from another of the same
name, and it is what `net481` binding needs. It is not a guarantee of origin, and publishing the key does not
weaken anything that was ever guaranteed. Verify the package through NuGet, not through the strong name.

## Relationship To Plugin.BLE

This project keeps the broad shape of Plugin.BLE but uses:

- Namespace: `System.BluetoothLe`
- Entry point: `BluetoothLE.Current`
- Multi-targeted partial classes instead of the original base-class layout

Existing Plugin.BLE concepts map closely to this library, but code should be updated to the names above.
