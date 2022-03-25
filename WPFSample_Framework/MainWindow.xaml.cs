using System;
using System.BluetoothLe;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFSample_Framework
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string _log;

		public MainWindow()
		{
			InitializeComponent();
		}

		private async void Button_Click(object sender, RoutedEventArgs e)
		{

			try
			{
				if (BluetoothLE.Current.State != BluetoothState.On)
				{
					//wait for it to initialise
					await Task.Delay(TimeSpan.FromSeconds(5));

					if (BluetoothLE.Current.State != BluetoothState.On)
						throw new Exception("Bluetooth not available");
				}

				var bleAdapter = BluetoothLE.Current.Adapter;

				await bleAdapter.StartScanningForDevicesAsync();

				var devices = bleAdapter.DiscoveredDevices;

				if (!devices.Any())
					throw new Exception("No devices found");

				foreach (var device in devices)
				{
					Debug.WriteLine($"Connecting to device: {device.Id}");

					try
					{
						await bleAdapter.ConnectToDeviceAsync(device);

						try
						{
							var services = await device.GetServicesAsync();

							Debug.WriteLine($"Found {services.Count} service(s)");

							foreach (var service in services)
							{
								try
								{
									var characteristics = await service.GetCharacteristicsAsync();

									Debug.WriteLine($"Found {characteristics.Count} characteritics(s) for service: {service.Id}");

								}
								catch (Exception ex)
								{
									Debug.WriteLine($"Excpetion loading characteristics: {ex.Message}");
								}
							}
						}
						catch (Exception ex)
						{
							Debug.WriteLine($"Excpetion loading services: {ex.Message}");
						}

						//try
						//{
						//	Debug.WriteLine($"Disconnecting device: {device.Id}");

						//	Task.Run(async () => await bleAdapter.DisconnectDeviceAsync(device));

						//}
						//catch (Exception ex)
						//{

						//	Debug.WriteLine($"Excpetion disconnecting device: {ex.Message}");
						//}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Excpetion connecting to device: {ex.Message}");
					}
					
					
				}

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
			
				
		}

		private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{

		}
	}
}
