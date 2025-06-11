using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Models;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Service responsible for scanning for BLE devices and managing discovered devices
    /// </summary>
    public class DeviceScanService : IDeviceScanService
    {
        private readonly IBluetoothService _bluetoothService;
        
        /// <summary>
        /// Event triggered when a BLE device is discovered
        /// </summary>
        public event EventHandler<BleDeviceEventArgs>? DeviceDiscovered;
        
        public DeviceScanService(IBluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            
            // Register for device discovery events from the bluetooth service
            _bluetoothService.DeviceDiscovered += OnBluetoothServiceDeviceDiscovered;
        }
        
        /// <summary>
        /// Start scanning for BLE devices
        /// </summary>
        /// <param name="scanTimeout">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when scanning is started</returns>
        public async Task StartScanningAsync(int scanTimeout = 10000, CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.WriteLine("DeviceScanService: Starting BLE scan");
                await _bluetoothService.StartScanningAsync(scanTimeout, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeviceScanService: Failed to start scanning: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Stop scanning for BLE devices
        /// </summary>
        /// <returns>Task that completes when scanning is stopped</returns>
        public async Task StopScanningAsync()
        {
            try
            {
                Debug.WriteLine("DeviceScanService: Stopping BLE scan");
                await _bluetoothService.StopScanningAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeviceScanService: Failed to stop scanning: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Connect to a BLE device by its ID
        /// </summary>
        /// <param name="deviceId">ID of the device to connect to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connected device information or null if connection failed</returns>
        public async Task<BleDeviceInfo?> ConnectToDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.WriteLine($"DeviceScanService: Connecting to device {deviceId}");
                return await _bluetoothService.ConnectToDeviceAsync(deviceId, cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DeviceScanService: Failed to connect to device: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Find an existing device in a collection by its ID
        /// </summary>
        /// <param name="deviceId">Device ID to find</param>
        /// <param name="devices">Collection of devices to search in</param>
        /// <returns>Found device or null if not found</returns>
        public BleDeviceInfo? FindExistingDevice(string deviceId, ObservableCollection<BleDeviceInfo> devices)
        {
            if (string.IsNullOrEmpty(deviceId) || devices == null)
                return null;
                
            return devices.FirstOrDefault(d => d.Id == deviceId);
        }
        
        /// <summary>
        /// Update an existing device with information from a newly discovered device
        /// </summary>
        /// <param name="existingDevice">Device to update</param>
        /// <param name="newDevice">Device with updated information</param>
        public void UpdateDevice(BleDeviceInfo existingDevice, BleDeviceInfo newDevice)
        {
            if (existingDevice == null || newDevice == null)
                return;
                
            // Update device properties
            existingDevice.Name = newDevice.Name;
            existingDevice.Rssi = newDevice.Rssi;
            existingDevice.IsConnected = newDevice.IsConnected;
            
            // Update advertisement data
            if (newDevice.AdvertisementData != null && newDevice.AdvertisementData.Count > 0)
            {
                foreach (var item in newDevice.AdvertisementData)
                {
                    existingDevice.AdvertisementData[item.Key] = item.Value;
                }
            }
        }
        
        /// <summary>
        /// Event handler for device discovered events from the bluetooth service
        /// </summary>
        private void OnBluetoothServiceDeviceDiscovered(object? sender, BleDeviceEventArgs args)
        {
            Debug.WriteLine($"DeviceScanService: Device discovered: {args.Device?.Name} ({args.Device?.Id})");
            DeviceDiscovered?.Invoke(this, args);
        }
        
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            _bluetoothService.DeviceDiscovered -= OnBluetoothServiceDeviceDiscovered;
        }
    }
}