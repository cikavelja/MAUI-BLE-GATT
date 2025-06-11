using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiBleApp2.Core.Models;
using MauiBleApp2.Core.Services.Bluetooth;
using System.Collections.Generic;
using MauiBleApp2.Core.ViewModels;

namespace MauiBleApp2.ViewModels
{
    /// <summary>
    /// View model for interacting with a connected BLE device
    /// </summary>
    public partial class DeviceDetailsViewModel : BaseViewModel
    {
        private readonly IBluetoothService _bluetoothService;
        private readonly IBleMessageParser _messageParser;
        private readonly EnhancedBleMessageParser? _enhancedParser;
        private readonly BleMessageFormatter? _messageFormatter;
        
        /// <summary>
        /// Connected BLE device
        /// </summary>
        [ObservableProperty]
        private BleDeviceInfo _device = null!;

        /// <summary>
        /// Collection of discovered GATT services
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BleGattService> _services = new();

        /// <summary>
        /// Currently selected service
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsServiceSelected))]
        private BleGattService? _selectedService;
        
        /// <summary>
        /// Collection of characteristics for the selected service
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<BleGattCharacteristic> _characteristics = new();
        
        /// <summary>
        /// Currently selected characteristic
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsCharacteristicSelected))]
        [NotifyPropertyChangedFor(nameof(CanRead))]
        [NotifyPropertyChangedFor(nameof(CanWrite))]
        [NotifyPropertyChangedFor(nameof(CanNotify))]
        private BleGattCharacteristic? _selectedCharacteristic;
        
        /// <summary>
        /// Text to write to the selected characteristic
        /// </summary>
        [ObservableProperty]
        private string _writeText = string.Empty;
        
        /// <summary>
        /// Text read from the selected characteristic
        /// </summary>
        [ObservableProperty]
        private string _readText = string.Empty;
        
        /// <summary>
        /// Whether notifications are enabled for the selected characteristic
        /// </summary>
        [ObservableProperty]
        private bool _notificationsEnabled;
        
        /// <summary>
        /// Text received from notifications
        /// </summary>
        [ObservableProperty]
        private string _notificationText = string.Empty;
        
        /// <summary>
        /// Available data formats
        /// </summary>
        [ObservableProperty]
        private ObservableCollection<string> _dataFormats = new();
        
        /// <summary>
        /// Selected data format
        /// </summary>
        [ObservableProperty]
        private string _selectedDataFormat = "Text";
        
        /// <summary>
        /// Whether a service is selected
        /// </summary>
        public bool IsServiceSelected => SelectedService != null;
        
        /// <summary>
        /// Whether a characteristic is selected
        /// </summary>
        public bool IsCharacteristicSelected => SelectedCharacteristic != null;
        
        /// <summary>
        /// Whether the selected characteristic supports reading
        /// </summary>
        public bool CanRead => SelectedCharacteristic?.CanRead ?? false;
        
        /// <summary>
        /// Whether the selected characteristic supports writing
        /// </summary>
        public bool CanWrite => SelectedCharacteristic?.CanWrite ?? false;
        
        /// <summary>
        /// Whether the selected characteristic supports notifications
        /// </summary>
        public bool CanNotify => SelectedCharacteristic?.CanNotify ?? false;

        public DeviceDetailsViewModel(
            IBluetoothService bluetoothService, 
            IBleMessageParser messageParser,
            EnhancedBleMessageParser? enhancedParser = null,
            BleMessageFormatter? messageFormatter = null)
        {
            _bluetoothService = bluetoothService;
            _messageParser = messageParser;
            _enhancedParser = enhancedParser;
            _messageFormatter = messageFormatter;
            
            Title = "Device Details";
            
            // Initialize data formats
            DataFormats.Add("Text");
            DataFormats.Add("Integer");
            DataFormats.Add("Float");
            
            if (_messageFormatter != null)
            {
                DataFormats.Add("Health Data");
                DataFormats.Add("Environment Data");
                DataFormats.Add("Custom Structure");
            }
        }

        /// <summary>
        /// Command to handle when a service is tapped in the list
        /// </summary>
        [RelayCommand]
        public void ServiceTapped(BleGattService service)
        {
            if (service == null)
                return;
                
            SelectedService = service;
        }

        /// <summary>
        /// Command to handle when a characteristic is tapped in the list
        /// </summary>
        [RelayCommand]
        public void CharacteristicTapped(BleGattCharacteristic characteristic)
        {
            if (characteristic == null)
                return;
                
            SelectedCharacteristic = characteristic;
        }

        /// <summary>
        /// Initialize the view model when the page appears
        /// </summary>
        [RelayCommand]
        public async Task InitializeAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                // Update title with device name
                Title = $"{Device.Name} ({Device.Id})";
                
                // Get services for the device
                var services = await _bluetoothService.GetServicesAsync(Device.Id);
                
                // Update services collection
                Services.Clear();
                foreach (var service in services)
                {
                    Services.Add(service);
                }
                // Auto-select the first service if available
                if (Services.Count > 0)
                {
                    SelectedService = Services[0];
                }
                
                // Register connection state change handler
                _bluetoothService.ConnectionStateChanged += OnConnectionStateChanged;
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert(
                    "Error", 
                    $"Failed to initialize device: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Command to disconnect from the device
        /// </summary>
        [RelayCommand]
        private async Task DisconnectAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                // Unsubscribe from notifications if enabled
                if (NotificationsEnabled && SelectedCharacteristic != null)
                {
                    await _bluetoothService.UnsubscribeFromCharacteristicAsync(SelectedCharacteristic);
                    NotificationsEnabled = false;
                }
                
                await _bluetoothService.DisconnectDeviceAsync(Device.Id);
                
                // Go back to the device scan page
                await App.Current!.MainPage!.Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert(
                    "Error", 
                    $"Failed to disconnect: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Command to load characteristics for the selected service
        /// </summary>
        [RelayCommand]
        private async Task LoadCharacteristicsAsync()
        {
            if (SelectedService == null || IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                // Get characteristics for the selected service
                var characteristics = await _bluetoothService.GetCharacteristicsAsync(SelectedService);
                
                // Update characteristics collection
                Characteristics.Clear();
                foreach (var characteristic in characteristics)
                {
                    Characteristics.Add(characteristic);
                }
                
                // Reset selected characteristic
                SelectedCharacteristic = null;
                ReadText = string.Empty;
                WriteText = string.Empty;
                NotificationText = string.Empty;
                NotificationsEnabled = false;
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert(
                    "Error", 
                    $"Failed to load characteristics: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Command to read from the selected characteristic
        /// </summary>
        [RelayCommand]
        private async Task ReadCharacteristicAsync()
        {
            if (SelectedCharacteristic == null || !CanRead || IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                // Read data from the characteristic
                var data = await _bluetoothService.ReadCharacteristicAsync(SelectedCharacteristic);
                
                // Parse data based on the selected format
                ReadText = FormatReceivedData(data);
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert(
                    "Error", 
                    $"Failed to read characteristic: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Command to write to the selected characteristic
        /// </summary>
        [RelayCommand]
        private async Task WriteCharacteristicAsync()
        {
            if (SelectedCharacteristic == null || !CanWrite || IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                // Convert text to bytes based on the selected format
                byte[] data = FormatDataToSend();
                
                // Write data to the characteristic
                var success = await _bluetoothService.WriteCharacteristicAsync(SelectedCharacteristic, data);
                
                if (success)
                {
                    await App.Current!.MainPage!.DisplayAlert(
                        "Success", 
                        "Data written successfully", 
                        "OK");
                }
                else
                {
                    await App.Current!.MainPage!.DisplayAlert(
                        "Failed", 
                        "Failed to write data", 
                        "OK");
                }
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert(
                    "Error", 
                    $"Failed to write characteristic: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Command to toggle notifications for the selected characteristic
        /// </summary>
        [RelayCommand]
        private async Task ToggleNotificationsAsync()
        {
            if (SelectedCharacteristic == null || !CanNotify || IsBusy)
                return;

            try
            {
                IsBusy = true;
                
                if (NotificationsEnabled)
                {
                    // Unsubscribe from notifications
                    var success = await _bluetoothService.UnsubscribeFromCharacteristicAsync(SelectedCharacteristic);
                    if (success)
                    {
                        NotificationsEnabled = false;
                    }
                    else
                    {
                        await App.Current!.MainPage!.DisplayAlert(
                            "Failed", 
                            "Failed to unsubscribe from notifications", 
                            "OK");
                    }
                }
                else
                {
                    // Subscribe to notifications
                    var success = await _bluetoothService.SubscribeToCharacteristicAsync(
                        SelectedCharacteristic,
                        data =>
                        {
                            // Parse data based on the selected format
                            string parsedData = FormatReceivedData(data);
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                NotificationText = parsedData;
                            });
                        });
                    
                    if (success)
                    {
                        NotificationsEnabled = true;
                    }
                    else
                    {
                        await App.Current!.MainPage!.DisplayAlert(
                            "Failed", 
                            "Failed to subscribe to notifications", 
                            "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await App.Current!.MainPage!.DisplayAlert(
                    "Error", 
                    $"Failed to toggle notifications: {ex.Message}", 
                    "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Format the received data based on the selected format
        /// </summary>
        private string FormatReceivedData(byte[] data)
        {
            try
            {
                if (data == null || data.Length == 0)
                    return "Empty data";
                
                // Use enhanced parser if available
                if (_enhancedParser != null)
                {
                    switch (SelectedDataFormat)
                    {
                        case "Text":
                            return _enhancedParser.Parse<string>(data);
                        case "Integer":
                            return _enhancedParser.Parse<int>(data).ToString();
                        case "Float":
                            return _enhancedParser.Parse<float>(data).ToString("F2");
                        case "Health Data" when _messageFormatter != null:
                            var healthData = _messageFormatter.ParseMessage("Health", data);
                            return FormatDictionaryAsString(healthData);
                        case "Environment Data" when _messageFormatter != null:
                            var envData = _messageFormatter.ParseMessage("Environment", data);
                            return FormatDictionaryAsString(envData);
                        case "Custom Structure" when _messageFormatter != null:
                            var customData = _messageFormatter.ParseMessage("CustomStructure", data);
                            return FormatDictionaryAsString(customData);
                        default:
                            return _enhancedParser.Parse<string>(data);
                    }
                }
                else
                {
                    // Use standard parser
                    switch (SelectedDataFormat)
                    {
                        case "Text":
                            return _messageParser.ParseString(data);
                        case "Integer":
                            return _messageParser.ParseInt(data).ToString();
                        case "Float":
                            return _messageParser.ParseFloat(data).ToString("F2");
                        default:
                            return _messageParser.ParseString(data);
                    }
                }
            }
            catch (Exception ex)
            {
                return $"Error parsing data: {ex.Message}";
            }
        }

        /// <summary>
        /// Format the data to send based on the selected format
        /// </summary>
        private byte[] FormatDataToSend()
        {
            try
            {
                // Use enhanced parser if available
                if (_enhancedParser != null)
                {
                    switch (SelectedDataFormat)
                    {
                        case "Text":
                            return _enhancedParser.ToByteArray<string>(WriteText);
                        case "Integer":
                            if (int.TryParse(WriteText, out int intValue))
                                return _enhancedParser.ToByteArray<int>(intValue);
                            throw new FormatException("Invalid integer format");
                        case "Float":
                            if (float.TryParse(WriteText, out float floatValue))
                                return _enhancedParser.ToByteArray<float>(floatValue);
                            throw new FormatException("Invalid float format");
                        default:
                            return _enhancedParser.ToByteArray<string>(WriteText);
                    }
                }
                else
                {
                    // Use standard parser
                    switch (SelectedDataFormat)
                    {
                        case "Text":
                            return _messageParser.ToByteArray(WriteText);
                        case "Integer":
                            if (int.TryParse(WriteText, out int intValue))
                                return _messageParser.ToByteArray(intValue);
                            throw new FormatException("Invalid integer format");
                        case "Float":
                            if (float.TryParse(WriteText, out float floatValue))
                                return _messageParser.ToByteArray(floatValue);
                            throw new FormatException("Invalid float format");
                        default:
                            return _messageParser.ToByteArray(WriteText);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new FormatException($"Error formatting data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Format a dictionary as a string
        /// </summary>
        private string FormatDictionaryAsString(Dictionary<string, object> dict)
        {
            if (dict == null || dict.Count == 0)
                return "Empty data";
            
            var sb = new System.Text.StringBuilder();
            foreach (var kvp in dict)
            {
                sb.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            return sb.ToString();
        }
        
        /// <summary>
        /// Event handler for device connection state changes
        /// </summary>
        private void OnConnectionStateChanged(object? sender, MauiBleApp2.Core.Services.Bluetooth.BleConnectionEventArgs args)
        {
            if (args.Device.Id == Device.Id && !args.IsConnected)
            {
                // Device got disconnected, navigate back to scan page
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current!.MainPage!.DisplayAlert(
                        "Device Disconnected", 
                        "The device has disconnected", 
                        "OK");
                    
                    await App.Current!.MainPage!.Navigation.PopAsync();
                });
            }
        }
    }
}