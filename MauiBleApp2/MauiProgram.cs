using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using MauiBleApp2.Views;
using MauiBleApp2.ViewModels;

namespace MauiBleApp2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register platform-specific services
#if DEBUG
            // Use simulated Bluetooth service for debugging
            builder.Services.AddSingleton<Services.Bluetooth.Simulation.SimulatedBluetoothService>();
            builder.Services.AddSingleton<MauiBleApp2.Core.Services.Bluetooth.IBluetoothService, Services.Bluetooth.Simulation.SimulatedBluetoothServiceAdapter>(sp =>
                new Services.Bluetooth.Simulation.SimulatedBluetoothServiceAdapter(
                    sp.GetRequiredService<Services.Bluetooth.Simulation.SimulatedBluetoothService>()));
            // Register the platform IBluetoothService as a factory that returns null (or throw) to avoid CS0311 error
            // Removed obsolete IBluetoothService registration from MauiBleApp2.Services.Bluetooth (use Core interface instead)
#else
            builder.Services.AddSingleton<Services.Bluetooth.BluetoothService>();
            builder.Services.AddSingleton<MauiBleApp2.Core.Services.Bluetooth.IBluetoothService, Services.Bluetooth.BluetoothServiceAdapter>(sp =>
                new Services.Bluetooth.BluetoothServiceAdapter(
                    sp.GetRequiredService<Services.Bluetooth.BluetoothService>()));
            builder.Services.AddSingleton<Services.Bluetooth.IBluetoothService>(sp =>
                throw new NotSupportedException("Use MauiBleApp2.Core.Services.Bluetooth.IBluetoothService instead of MauiBleApp2.Services.Bluetooth.IBluetoothService in DI."));
#endif

            // Register Core project BLE services and formatters
            builder.Services.AddSingleton<MauiBleApp2.Core.Services.Bluetooth.IBleMessageParser, MauiBleApp2.Core.Services.Bluetooth.BleMessageParser>();
            builder.Services.AddSingleton<MauiBleApp2.Core.Services.Bluetooth.EnhancedBleMessageParser>();
            builder.Services.AddSingleton<MauiBleApp2.Core.Services.Bluetooth.BleMessageFormatter>(sp => 
                new MauiBleApp2.Core.Services.Bluetooth.BleMessageFormatter(
                    sp.GetRequiredService<MauiBleApp2.Core.Services.Bluetooth.EnhancedBleMessageParser>()));
            builder.Services.AddSingleton<MauiBleApp2.Core.Services.Bluetooth.IDeviceScanService, MauiBleApp2.Core.Services.Bluetooth.DeviceScanService>();
            
            // Register views
            builder.Services.AddSingleton<DeviceScanPage>(sp =>
                new DeviceScanPage(
                    sp.GetRequiredService<Core.ViewModels.DeviceScanViewModel>(),
                    sp));
            builder.Services.AddTransient<DeviceDetailsPage>();
            
            // Register viewmodels
            builder.Services.AddSingleton<Core.ViewModels.DeviceScanViewModel>();
            builder.Services.AddTransient<DeviceDetailsViewModel>(sp =>
                new DeviceDetailsViewModel(
                    sp.GetRequiredService<MauiBleApp2.Core.Services.Bluetooth.IBluetoothService>(),
                    sp.GetRequiredService<MauiBleApp2.Core.Services.Bluetooth.IBleMessageParser>(),
                    sp.GetService<MauiBleApp2.Core.Services.Bluetooth.EnhancedBleMessageParser>(),
                    sp.GetService<MauiBleApp2.Core.Services.Bluetooth.BleMessageFormatter>()));

#if DEBUG
            builder.Services.AddLogging(logging =>
            {
                logging.AddDebug();
            });
#endif

            return builder.Build();
        }
    }
}
