using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Core.Models;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Interface for device scanning and management services
    /// </summary>
    public interface IDeviceScanService
    {
        /// <summary>
        /// Event triggered when a BLE device is discovered
        /// </summary>
        event EventHandler<BleDeviceEventArgs>? DeviceDiscovered;
        
        /// <summary>
        /// Start scanning for BLE devices
        /// </summary>
        /// <param name="scanTimeout">Timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task that completes when scanning is started</returns>
        Task StartScanningAsync(int scanTimeout = 10000, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop scanning for BLE devices
        /// </summary>
        /// <returns>Task that completes when scanning is stopped</returns>
        Task StopScanningAsync();
        
        /// <summary>
        /// Connect to a BLE device by its ID
        /// </summary>
        /// <param name="deviceId">ID of the device to connect to</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Connected device information or null if connection failed</returns>
        Task<BleDeviceInfo?> ConnectToDeviceAsync(string deviceId, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Find an existing device in a collection by its ID
        /// </summary>
        /// <param name="deviceId">Device ID to find</param>
        /// <param name="devices">Collection of devices to search in</param>
        /// <returns>Found device or null if not found</returns>
        BleDeviceInfo? FindExistingDevice(string deviceId, ObservableCollection<BleDeviceInfo> devices);
        
        /// <summary>
        /// Update an existing device with information from a newly discovered device
        /// </summary>
        /// <param name="existingDevice">Device to update</param>
        /// <param name="newDevice">Device with updated information</param>
        void UpdateDevice(BleDeviceInfo existingDevice, BleDeviceInfo newDevice);
    }
}