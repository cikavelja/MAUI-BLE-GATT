using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using MauiBleApp2.Core.Services.Bluetooth;
using MauiBleApp2.Services.Bluetooth;
using MauiBleApp2.Core.ViewModels;
using MauiBleApp2.Views;
using CoreBluetoothService = MauiBleApp2.Core.Services.Bluetooth.IBluetoothService;

namespace MauiBleApp2
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void OnCounterClicked(object sender, EventArgs e)
        {
            count++;

            CounterBtn.Text = count == 1
                ? "Clicked 1 time"
                : $"Clicked {count} times";

            SemanticScreenReader.Announce(CounterBtn.Text);
        }

        private async void OnNavigateToScanClicked(object sender, EventArgs e)
        {
            if (!await EnsureBluetoothPermissionsAsync())
                return;

            // Use DI to get the services instead of creating new instances
            var bluetoothService = Application.Current.Handler.MauiContext.Services.GetRequiredService<CoreBluetoothService>();
            var viewModel = Application.Current.Handler.MauiContext.Services.GetRequiredService<MauiBleApp2.Core.ViewModels.DeviceScanViewModel>();
            var scanPage = Application.Current.Handler.MauiContext.Services.GetRequiredService<DeviceScanPage>();
            await Navigation.PushAsync(scanPage);
        }

        private async Task<bool> EnsureBluetoothPermissionsAsync()
        {
#if ANDROID && !DEBUG
            // Check if Bluetooth is enabled - Skip this check in DEBUG mode as we're using the simulator
            if (!Plugin.BLE.CrossBluetoothLE.Current.IsOn)
            {
                await DisplayAlert(
                    "Bluetooth Disabled",
                    "Please enable Bluetooth on your device and try again.",
                    "OK");
                return false;
            }
#endif

#if ANDROID
            // Request all necessary permissions
            var scanStatus = await Permissions.RequestAsync<BluetoothScanPermissions>();
            var connectStatus = await Permissions.RequestAsync<BluetoothConnectPermissions>();
            var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (scanStatus != PermissionStatus.Granted ||
                connectStatus != PermissionStatus.Granted ||
                locationStatus != PermissionStatus.Granted)
            {
                bool goToSettings = await DisplayAlert(
                    "Permissions Required",
                    "Bluetooth and Location permissions are required for scanning BLE devices.",
                    "Open Settings",
                    "Cancel");

                if (goToSettings)
                    AppInfo.ShowSettingsUI();

                return false;
            }
#endif
            return true;
        }
    }
}
