using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiBleApp2.Core.Models;
using MauiBleApp2.Core.Services.Bluetooth;

namespace MauiBleApp2.Core.ViewModels
{
    /// <summary>
    /// View model for scanning and displaying BLE devices
    /// </summary>
    public partial class DeviceScanViewModel : BaseViewModel
    {
        private readonly IBluetoothService _bluetoothService;
        private readonly IBleMessageParser _messageParser;
        private readonly EnhancedBleMessageParser? _enhancedParser;
        private readonly BleMessageFormatter? _messageFormatter;
        private readonly IDeviceScanService _deviceScanService;
        private CancellationTokenSource? _scanCancellationTokenSource;

        /// <summary>
        /// Collection of discovered BLE devices
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BleDeviceInfo> _devices = new();

        /// <summary>
        /// Whether Bluetooth is currently enabled
        /// </summary>
        [ObservableProperty]
        private bool _isBluetoothEnabled;

        /// <summary>
        /// Whether scanning is currently in progress
        /// </summary>
        [ObservableProperty]
        private bool _isScanning;

        /// <summary>
        /// Selected device from the list
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsDeviceSelected))]
        private BleDeviceInfo? _selectedDevice;

        /// <summary>
        /// Whether a device is currently selected
        /// </summary>
        public bool IsDeviceSelected => SelectedDevice != null;

        /// <summary>
        /// Event raised when navigation to device details is requested
        /// </summary>
        public event EventHandler<BleDeviceInfo>? NavigateToDeviceDetailsRequested;

        /// <summary>
        /// Event raised when an alert should be shown
        /// </summary>
        public event EventHandler<AlertEventArgs>? ShowAlertRequested;

        public DeviceScanViewModel(
            IBluetoothService bluetoothService, 
            IBleMessageParser messageParser, 
            IDeviceScanService deviceScanService,
            EnhancedBleMessageParser? enhancedParser = null,
            BleMessageFormatter? messageFormatter = null)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            _messageParser = messageParser ?? throw new ArgumentNullException(nameof(messageParser));
            _deviceScanService = deviceScanService ?? throw new ArgumentNullException(nameof(deviceScanService));
            _enhancedParser = enhancedParser;
            _messageFormatter = messageFormatter;
            
            Title = "BLE Device Scanner";

            // Register event handlers
            _deviceScanService.DeviceDiscovered += OnDeviceDiscovered;
            
            // Initialize properties
            IsBluetoothEnabled = _bluetoothService.IsBluetoothEnabled;
        }

        /// <summary>
        /// Command to handle when a device is tapped in the list
        /// </summary>
        [RelayCommand]
        public void DeviceTapped(BleDeviceInfo device)
        {
            if (device == null)
                return;
                
            SelectedDevice = device;
        }

        /// <summary>
        /// Command to toggle scanning for BLE devices
        /// </summary>
        [RelayCommand]
        public async Task ToggleScanAsync()
        {
            if (!IsBluetoothEnabled)
            {
                ShowAlert("Bluetooth Disabled", "Please enable Bluetooth on your device and try again.", "OK");
                return;
            }

            if (IsScanning)
            {
                await StopScanAsync();
            }
            else
            {
                await StartScanAsync();
            }
        }

        /// <summary>
        /// Command to connect to the selected device
        /// </summary>
        [RelayCommand]
        public async Task ConnectToDeviceAsync()
        {
            if (SelectedDevice == null)
                return;

            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var deviceId = SelectedDevice.Id;
                var deviceInfo = await _deviceScanService.ConnectToDeviceAsync(deviceId);

                if (deviceInfo != null)
                {
                    // Raise event to request navigation
                    NavigateToDeviceDetailsRequested?.Invoke(this, deviceInfo);
                }
                else
                {
                    ShowAlert("Connection Failed", "Failed to connect to the selected device.", "OK");
                }
            }
            catch (Exception ex)
            {
                ShowAlert("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
                SelectedDevice = null;
            }
        }

        /// <summary>
        /// Start scanning for BLE devices
        /// </summary>
        private async Task StartScanAsync()
        {
            if (IsScanning)
                return;

            Devices.Clear();
            IsScanning = true;
            
            try
            {
                _scanCancellationTokenSource = new CancellationTokenSource();
                await _deviceScanService.StartScanningAsync(15000, _scanCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                ShowAlert("Error", $"Failed to start scanning: {ex.Message}", "OK");
                IsScanning = false;
            }
        }

        /// <summary>
        /// Stop scanning for BLE devices
        /// </summary>
        private async Task StopScanAsync()
        {
            if (!IsScanning)
                return;

            try
            {
                _scanCancellationTokenSource?.Cancel();
                await _deviceScanService.StopScanningAsync();
            }
            catch (Exception ex)
            {
                ShowAlert("Error", $"Failed to stop scanning: {ex.Message}", "OK");
            }
            finally
            {
                _scanCancellationTokenSource?.Dispose();
                _scanCancellationTokenSource = null;
                IsScanning = false;
            }
        }

        /// <summary>
        /// Event handler for when a device is discovered
        /// </summary>
        private void OnDeviceDiscovered(object? sender, MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs args)
        {
            if (args.Device == null)
                return;
                
            // In the core library, we don't have access to MainThread
            // The UI layer will need to handle thread synchronization
            OnDeviceDiscoveredCore(args.Device);
        }
        
        // This can be called directly for testing
        internal void OnDeviceDiscoveredCore(BleDeviceInfo device)
        {
            // Use the device management service to add or update the device
            var existingDevice = _deviceScanService.FindExistingDevice(device.Id, Devices);
            
            if (existingDevice != null)
            {
                // Update existing device
                _deviceScanService.UpdateDevice(existingDevice, device);
            }
            else
            {
                // Add new device to the list
                Devices.Add(device);
            }
        }
        
        // Helper method to show alerts
        private void ShowAlert(string title, string message, string buttonText)
        {
            ShowAlertRequested?.Invoke(this, new AlertEventArgs(title, message, buttonText));
        }
        
        // Clean up resources when the view model is no longer needed
        public void Cleanup()
        {
            _deviceScanService.DeviceDiscovered -= OnDeviceDiscovered;
            _scanCancellationTokenSource?.Dispose();
        }
    }

    /// <summary>
    /// Event arguments for alert requests
    /// </summary>
    public class AlertEventArgs : EventArgs
    {
        public string Title { get; }
        public string Message { get; }
        public string ButtonText { get; }

        public AlertEventArgs(string title, string message, string buttonText)
        {
            Title = title;
            Message = message;
            ButtonText = buttonText;
        }
    }
}