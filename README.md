# DSoft.System.BluetoothLe
Cross-platform Bluetooth Low Energy library, based on [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) ,  for iOS, Android, UWP, Mac, TVOS(Preview) and WatchOS(Preview), Tizen(coming soon)

As of 1st of June 2020 this is essentially [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le) repackaged in a single Multi-target project with additional support for TVOS and Watchos support and fixed Mac support, based on the [UWP](https://github.com/xabre/xamarin-bluetooth-le/tree/uwp_creators_update) branch

## Divergence

I have migrated the source code to single Multi-target library and added support for TVOS and WatchOS(untested).

I have changed the namespaces from `Plugin.BLE` to `System.BluetoothLe` and the main class from `CrossBluetoothLe` to `BluetoothLe`

This allows for seperation of the projects but also a fair amount of drop-in-ability for anyone using [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le)

The docs on Plugin.BLE should still be acurrate, with the changes noted above taken into consideration.

# Alpha

This is a work in progress and although its based on a stable library, this will change and no guarenteee is made about the API at this stage or the stability of the library.

## RoadMap

The first step is to stabilize the API and supported platforms and then extend to other platforms (Tizen, WPF/Net Core).

New docs and samples

