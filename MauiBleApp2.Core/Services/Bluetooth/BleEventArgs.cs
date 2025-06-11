using System;
using MauiBleApp2.Core.Models;

namespace MauiBleApp2.Core.Services.Bluetooth
{
    /// <summary>
    /// Event arguments for BLE device events
    /// </summary>
    public class BleDeviceEventArgs : EventArgs
    {
        /// <summary>
        /// The BLE device that was discovered
        /// </summary>
        public BleDeviceInfo Device { get; set; } = null!;
    }
    
    /// <summary>
    /// Event arguments for BLE connection events
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