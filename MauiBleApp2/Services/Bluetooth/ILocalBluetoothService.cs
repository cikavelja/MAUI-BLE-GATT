using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Core.Models;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Interface for platform-agnostic BLE functionality
    /// </summary>
    public interface ILocalBluetoothService
    {
        /// <summary>
        /// Event triggered when a BLE device is discovered during scanning
        /// </summary>
        event EventHandler<BleDeviceEventArgs> DeviceDiscovered;
        
        /// <summary>
        /// Event triggered when the connection state to a device changes
        /// </summary>
        event EventHandler<BleConnectionEventArgs> ConnectionStateChanged;

        /// <summary>
        /// True if the device's Bluetooth adapter is on and ready
        /// </summary>
        bool IsBluetoothEnabled { get; }
        
        /// <summary>
        /// True if the service is currently scanning for devices
        /// </summary>
        bool IsScanning { get; }

        /// <summary>
        /// Start scanning for BLE devices
        /// </summary>
        /// <param name="scanTimeout">Optional scan timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token to stop scanning</param>
        /// <returns>Task that completes when scanning stops</returns>
        Task StartScanningAsync(int scanTimeout = 10000, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Stop scanning for BLE devices
        /// </summary>
        Task StopScanningAsync();

        /// <summary>
        /// Connect to a specific BLE device
        /// </summary>
        /// <param name="deviceId">The device ID to connect to</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection attempt</param>
        /// <returns>Connected device information or null if connection failed</returns>
        Task<BleDeviceInfo?> ConnectToDeviceAsync(string deviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnect from a connected BLE device
        /// </summary>
        /// <param name="deviceId">The device ID to disconnect from</param>
        Task DisconnectDeviceAsync(string deviceId);

        /// <summary>
        /// Get GATT services for a connected device
        /// </summary>
        /// <param name="deviceId">The connected device ID</param>
        /// <returns>List of GATT services</returns>
        Task<List<BleGattService>> GetServicesAsync(string deviceId);

        /// <summary>
        /// Get characteristics for a specific GATT service
        /// </summary>
        /// <param name="service">The GATT service</param>
        /// <returns>List of characteristics for this service</returns>
        Task<List<BleGattCharacteristic>> GetCharacteristicsAsync(BleGattService service);

        /// <summary>
        /// Read data from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to read from</param>
        /// <returns>Byte array of data read from characteristic</returns>
        Task<byte[]> ReadCharacteristicAsync(BleGattCharacteristic characteristic);

        /// <summary>
        /// Write data to a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to write to</param>
        /// <param name="data">Byte array of data to write</param>
        /// <returns>True if write was successful</returns>
        Task<bool> WriteCharacteristicAsync(BleGattCharacteristic characteristic, byte[] data);

        /// <summary>
        /// Subscribe to notifications from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to subscribe to</param>
        /// <param name="notificationHandler">Callback when notification data is received</param>
        /// <returns>True if subscription was successful</returns>
        Task<bool> SubscribeToCharacteristicAsync(
            BleGattCharacteristic characteristic,
            Action<byte[]> notificationHandler);

        /// <summary>
        /// Unsubscribe from notifications from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to unsubscribe from</param>
        /// <returns>True if unsubscription was successful</returns>
        Task<bool> UnsubscribeFromCharacteristicAsync(BleGattCharacteristic characteristic);
    }
}