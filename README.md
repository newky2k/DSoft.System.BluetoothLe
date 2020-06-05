# DSoft.System.BluetoothLe

Cross-platform Bluetooth Low Energy library, based on [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) , for Xamarin.iOS, Xamarin.Android, Xamarin.Mac, UWP(Preview), .Net Core 3.x(Preview - Windows 10 only - WPF & Windows.Forms), TVOS(Preview) and WatchOS(Preview), Tizen(coming soon)

## .Net Core 3.x support

We have had added .Net Core support for Windows using the Windows RT API using `Microsoft.Windows.SDK.Contracts`.  The UWP and .Net Core implementations are essentially the same as they use the same APIs.  We have duplicated `ObservableBluetoothLeDevice` from Windows Community Toolkit as it doesn't work with `Microsoft.Windows.SDK.Contracts` and .Net Core

### Plugin.BLE

As of 1st of June 2020 this is essentially [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) repackaged in a single Multi-target project with additional preview support for TVOS, WatchOS and .Net Core 3.x.  Mac support is fixed(there is a [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le/pull/431) issue with Assembly names when using Xamarin.Forms and Xamarin.Mac), based on the [UWP](https://github.com/xabre/xamarin-bluetooth-le/tree/uwp_creators_update) branch.

## Divergence

I have migrated the source code to single Multi-target library and added support for TVOS, .Net Core and WatchOS(untested). Additionally, I have moved away from Base classes (`DeviceBase`, `AdapterBase` etc) and are using multi-targeted partial classes instead.

I have changed the namespaces from `Plugin.BLE` to `System.BluetoothLe` and the main class from `CrossBluetoothLe` to `BluetoothLe`

This allows for seperation of the projects but also a fair amount of drop-in-ability for anyone using [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le)

The docs on Plugin.BLE should still be acurrate, with the changes noted above taken into consideration.

## Preview

Xamarin.iOS, Xamarin.Android and Xamarin.Mac should be considered stable, as they are in Plugin.BLE.

All other platforms are in preview and essentially untested and compile without issue only.

This is a work in progress and although its based on a stable library, this will change and no guarenteee is made about the API at this stage or the stability of the library.

## RoadMap

The first step is to stabilize the API and supported platforms and then extend to other platforms (Tizen, UWP, WPF/Net Core).

New docs and samples
