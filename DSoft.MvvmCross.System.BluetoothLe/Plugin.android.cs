using MvvmCross;
using MvvmCross.IoC;
using MvvmCross.Logging;
using MvvmCross.Plugin;
using System.BluetoothLe;

namespace MvvmCross.System.BluetoothLe
{
    [MvxPlugin]
    public class Plugin
     : IMvxPlugin
    {
        public Plugin()
        {
            var log = Mvx.IoCProvider.Resolve<IMvxLog>();
            Trace.TraceImplementation = log.Trace;
        }
        public void Load()
        {
            Trace.Message("Loading bluetooth low energy plugin");
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton<IBluetoothLE>(() => BluetoothLE.Current);
            Mvx.IoCProvider.LazyConstructAndRegisterSingleton(() => Mvx.IoCProvider.Resolve<IBluetoothLE>().Adapter);
        }
    }
}