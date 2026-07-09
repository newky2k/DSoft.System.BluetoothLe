# DSoft.System.BluetoothLe

Cross-platform Bluetooth Low Energy library, based on [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le), for .NET Android, .NET iOS, .NET macOS, .NET Mac Catalyst, .NET tvOS, .NET for Windows, .NET, .NET Standard, and .NET Framework 4.8.1.

## .NET Windows Support

Windows support uses the Windows Runtime Bluetooth APIs. The .NET for Windows and .NET Framework implementations share the same platform implementation, with `ObservableBluetoothLeDevice` adapted from Windows Community Toolkit to use the WPF application dispatcher.

### Note: Windows support requires Windows 10 version 1803 and above

### Plugin.BLE

As of 1st of June 2020 this started as [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) repackaged in a single multi-target project. The library has since been migrated from Xamarin targets to modern .NET platform targets.

## Divergence

I have migrated the source code to a single multi-target library. Additionally, I have moved away from base classes (`DeviceBase`, `AdapterBase` etc) and use multi-targeted partial classes instead.

I have changed the namespaces from `Plugin.BLE` to `System.BluetoothLe` and the main class from `CrossBluetoothLe` to `BluetoothLe`

This allows for seperation of the projects but also a fair amount of drop-in-ability for anyone using [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le)

The docs on Plugin.BLE should still be acurrate, with the changes noted above taken into consideration.

## Preview

.NET iOS, .NET Android, and .NET macOS carry forward the stable Plugin.BLE implementations.

All other platforms are in preview and essentially untested and compile without issue only.

This is a work in progress and although its based on a stable library, this will change and no guarenteee is made about the API at this stage or the stability of the library.

## RoadMap

The first step is to stabilize the API and supported platforms and then extend to other platforms.

New docs and samples
