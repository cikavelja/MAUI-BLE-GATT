using System;
using System.Collections.Generic;

namespace MauiBleApp2.Core.Models
{
    /// <summary>
    /// Represents a BLE device discovered during scanning
    /// </summary>
    public class BleDeviceInfo
    {
        /// <summary>
        /// Unique identifier for the device
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Device name (can be null/empty for some devices)
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Received Signal Strength Indicator (RSSI) in dBm
        /// </summary>
        public int Rssi { get; set; }
        
        /// <summary>
        /// Whether the device is connected
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Dictionary of advertised service UUIDs to advertised data
        /// </summary>
        public Dictionary<string, byte[]> AdvertisementData { get; set; } = new Dictionary<string, byte[]>();
    }

    /// <summary>
    /// Represents a GATT service on a BLE device
    /// </summary>
    public class BleGattService
    {
        /// <summary>
        /// Service UUID
        /// </summary>
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// ID of the device this service belongs to
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a primary service
        /// </summary>
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Represents a GATT characteristic on a BLE device
    /// </summary>
    public class BleGattCharacteristic
    {
        /// <summary>
        /// Characteristic UUID
        /// </summary>
        public string Uuid { get; set; } = string.Empty;

        /// <summary>
        /// The UUID of the service this characteristic belongs to
        /// </summary>
        public string ServiceUuid { get; set; } = string.Empty;
        
        /// <summary>
        /// ID of the device this characteristic belongs to
        /// </summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// Whether the characteristic can be read
        /// </summary>
        public bool CanRead { get; set; }

        /// <summary>
        /// Whether the characteristic can be written to
        /// </summary>
        public bool CanWrite { get; set; }

        /// <summary>
        /// Whether the characteristic supports notifications
        /// </summary>
        public bool CanNotify { get; set; }

        /// <summary>
        /// Whether the characteristic supports indications
        /// </summary>
        public bool CanIndicate { get; set; }
    }

    /// <summary>
    /// Event arguments for BLE device discovery events
    /// </summary>
    public class BleDeviceEventArgs : EventArgs
    {
        /// <summary>
        /// The discovered BLE device
        /// </summary>
        public BleDeviceInfo Device { get; set; } = null!;
    }

    /// <summary>
    /// Event arguments for BLE connection state changes
    /// </summary>
    public class BleConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// The BLE device whose connection state changed
        /// </summary>
        public BleDeviceInfo Device { get; set; } = null!;
        
        /// <summary>
        /// Whether the device is now connected
        /// </summary>
        public bool IsConnected { get; set; }
    }
}