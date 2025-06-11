using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Core.Models;
using MauiBleApp2.Core.Services.Bluetooth;

namespace MauiBleApp2.Services.Bluetooth.Simulation
{
    /// <summary>
    /// A simulated implementation of Bluetooth service for testing without real hardware
    /// </summary>
    public class SimulatedBluetoothService
    {
        private readonly BleGattServerSimulator _simulator;
        private bool _isScanning;
        private readonly Random _random = new();
        
        /// <summary>
        /// Dictionary to track notification handlers for each characteristic
        /// Key: DeviceId:ServiceUuid:CharacteristicUuid, Value: Map of handler reference to handler function
        /// </summary>
        private readonly Dictionary<string, Dictionary<object, Action<byte[]>>> _notificationHandlers = new();
        
        /// <summary>
        /// Event triggered when a BLE device is discovered during scanning
        /// </summary>
        public event EventHandler<Core.Models.BleDeviceEventArgs>? DeviceDiscovered;
        
        /// <summary>
        /// Event triggered when the connection state to a device changes
        /// </summary>
        public event EventHandler<Core.Models.BleConnectionEventArgs>? ConnectionStateChanged;

        /// <summary>
        /// True if the device's Bluetooth adapter is on and ready
        /// </summary>
        public bool IsBluetoothEnabled => true; // Always enabled in simulation
        
        /// <summary>
        /// True if the service is currently scanning for devices
        /// </summary>
        public bool IsScanning => _isScanning;

        public SimulatedBluetoothService()
        {
            _simulator = new BleGattServerSimulator();
        }

        /// <summary>
        /// Start scanning for BLE devices
        /// </summary>
        /// <param name="scanTimeout">Optional scan timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token to stop scanning</param>
        /// <returns>Task that completes when scanning stops</returns>
        public async Task StartScanningAsync(int scanTimeout = 10000, CancellationToken cancellationToken = default)
        {
            if (_isScanning)
                return;

            _isScanning = true;
            
            // Run the scanner for the specified timeout or until cancelled
            try
            {
                System.Diagnostics.Debug.WriteLine("SimulatedBluetoothService: Starting scan");
                
                // Discover the simulated device after a short delay to simulate scanning
                await Task.Delay(500, cancellationToken);
                
                // Trigger device discovery event for simulated device - our main GATT server
                var deviceInfo = _simulator.GetDeviceInfo();
                System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Discovered main device: {deviceInfo.Name} ({deviceInfo.Id})");
                DeviceDiscovered?.Invoke(this, new Core.Models.BleDeviceEventArgs { Device = deviceInfo });
                
                // Also discover some "random" devices to make the app more realistic
                await DiscoverRandomDevicesAsync(scanTimeout, cancellationToken);
                
                // Wait for the timeout
                if (scanTimeout > 0)
                {
                    await Task.Delay(scanTimeout, cancellationToken);
                    await StopScanningAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Scan was cancelled
                await StopScanningAsync();
            }
        }

        /// <summary>
        /// Stop scanning for BLE devices
        /// </summary>
        public Task StopScanningAsync()
        {
            System.Diagnostics.Debug.WriteLine("SimulatedBluetoothService: Stopping scan");
            _isScanning = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Connect to a specific BLE device
        /// </summary>
        /// <param name="deviceId">The device ID to connect to</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection attempt</param>
        /// <returns>Connected device information or null if connection failed</returns>
        public async Task<BleDeviceInfo?> ConnectToDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            // Check if this is our simulated device
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (deviceId == simulatedDeviceInfo.Id)
            {
                System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Connecting to {simulatedDeviceInfo.Name} ({deviceId})");
                // Simulate connection delay
                await Task.Delay(1000, cancellationToken);
                
                // Start the simulator
                _simulator.Start();
                
                // Get updated device info with connected state
                simulatedDeviceInfo = _simulator.GetDeviceInfo();
                
                // Trigger connection state changed event
                ConnectionStateChanged?.Invoke(this, new Core.Models.BleConnectionEventArgs 
                { 
                    Device = simulatedDeviceInfo, 
                    IsConnected = true 
                });
                
                return simulatedDeviceInfo;
            }
            
            // For any other device ID, simulate failure
            System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Failed to connect to device {deviceId} (not our simulated device)");
            await Task.Delay(2000, cancellationToken);
            return null;
        }

        /// <summary>
        /// Disconnect from a connected BLE device
        /// </summary>
        /// <param name="deviceId">The device ID to disconnect from</param>
        public Task DisconnectDeviceAsync(string deviceId)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (deviceId == simulatedDeviceInfo.Id)
            {
                System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Disconnecting from {simulatedDeviceInfo.Name} ({deviceId})");
                _simulator.Stop();
                
                // Get updated device info with disconnected state
                simulatedDeviceInfo = _simulator.GetDeviceInfo();
                
                // Trigger connection state changed event
                ConnectionStateChanged?.Invoke(this, new Core.Models.BleConnectionEventArgs 
                { 
                    Device = simulatedDeviceInfo, 
                    IsConnected = false 
                });
                
                // Clear all notification handlers on disconnect
                CleanupHandlersForDevice(deviceId);
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Get GATT services for a connected device
        /// </summary>
        /// <param name="deviceId">The connected device ID</param>
        /// <returns>List of GATT services</returns>
        public Task<List<BleGattService>> GetServicesAsync(string deviceId)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (deviceId != simulatedDeviceInfo.Id || !simulatedDeviceInfo.IsConnected)
                throw new InvalidOperationException("Device is not connected");
            
            // Get services from simulator
            var services = _simulator.GetServices();
            System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Got {services.Count} services from device {simulatedDeviceInfo.Name}");
            return Task.FromResult(services);
        }

        /// <summary>
        /// Get characteristics for a specific GATT service
        /// </summary>
        /// <param name="service">The GATT service</param>
        /// <returns>List of characteristics for this service</returns>
        public Task<List<BleGattCharacteristic>> GetCharacteristicsAsync(BleGattService service)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (service.DeviceId != simulatedDeviceInfo.Id || !simulatedDeviceInfo.IsConnected)
                throw new InvalidOperationException("Device is not connected");
            
            // Get characteristics from simulator
            var characteristics = _simulator.GetCharacteristics(service.Uuid);
            System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Got {characteristics.Count} characteristics from service {service.Uuid}");
            return Task.FromResult(characteristics);
        }

        /// <summary>
        /// Read data from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to read from</param>
        /// <returns>Byte array of data read from characteristic</returns>
        public Task<byte[]> ReadCharacteristicAsync(BleGattCharacteristic characteristic)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (characteristic.DeviceId != simulatedDeviceInfo.Id || !simulatedDeviceInfo.IsConnected)
                throw new InvalidOperationException("Device is not connected");
            
            // Read from simulator
            var data = _simulator.ReadCharacteristic(characteristic.ServiceUuid, characteristic.Uuid);
            return Task.FromResult(data);
        }

        /// <summary>
        /// Write data to a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to write to</param>
        /// <param name="data">Byte array of data to write</param>
        /// <returns>True if write was successful</returns>
        public Task<bool> WriteCharacteristicAsync(BleGattCharacteristic characteristic, byte[] data)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (characteristic.DeviceId != simulatedDeviceInfo.Id || !simulatedDeviceInfo.IsConnected)
                throw new InvalidOperationException("Device is not connected");
            
            try
            {
                // Write to simulator
                _simulator.WriteCharacteristic(characteristic.ServiceUuid, characteristic.Uuid, data);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Subscribe to notifications from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to subscribe to</param>
        /// <param name="notificationHandler">Callback when notification data is received</param>
        /// <returns>True if subscription was successful</returns>
        public Task<bool> SubscribeToCharacteristicAsync(BleGattCharacteristic characteristic, Action<byte[]> notificationHandler)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (characteristic.DeviceId != simulatedDeviceInfo.Id || !simulatedDeviceInfo.IsConnected)
                throw new InvalidOperationException("Device is not connected");
            
            try
            {
                // Create a key to identify this characteristic
                string key = GetCharacteristicKey(characteristic);
                
                // Store the handler reference
                if (!_notificationHandlers.TryGetValue(key, out var handlers))
                {
                    handlers = new Dictionary<object, Action<byte[]>>();
                    _notificationHandlers[key] = handlers;
                }
                
                // Store the handler with its instance as key
                handlers[notificationHandler] = notificationHandler;
                
                // Subscribe to simulator
                _simulator.SubscribeToNotifications(
                    characteristic.ServiceUuid,
                    characteristic.Uuid,
                    notificationHandler);
                
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Unsubscribe from notifications from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to unsubscribe from</param>
        /// <returns>True if unsubscription was successful</returns>
        public Task<bool> UnsubscribeFromCharacteristicAsync(BleGattCharacteristic characteristic)
        {
            var simulatedDeviceInfo = _simulator.GetDeviceInfo();
            
            if (characteristic.DeviceId != simulatedDeviceInfo.Id || !simulatedDeviceInfo.IsConnected)
                throw new InvalidOperationException("Device is not connected");
            
            try
            {
                string key = GetCharacteristicKey(characteristic);
                
                if (_notificationHandlers.TryGetValue(key, out var handlers))
                {
                    // Unsubscribe each handler
                    foreach (var handler in handlers.Values)
                    {
                        _simulator.UnsubscribeFromNotifications(
                            characteristic.ServiceUuid,
                            characteristic.Uuid,
                            handler);
                    }
                    
                    // Clear the handlers for this characteristic
                    _notificationHandlers.Remove(key);
                }
                
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
        
        /// <summary>
        /// Discover random devices as part of the simulation
        /// </summary>
        private async Task DiscoverRandomDevicesAsync(int duration, CancellationToken cancellationToken)
        {
            // Generate between 3 and 8 random devices
            int numDevices = _random.Next(3, 9);
            int delayBetweenDiscoveries = Math.Min(1000, duration / (numDevices + 1)); // Ensure devices appear quicker
            
            for (int i = 0; i < numDevices; i++)
            {
                // Stop if scanning has been cancelled
                if (cancellationToken.IsCancellationRequested || !_isScanning)
                    break;
                    
                await Task.Delay(_random.Next(delayBetweenDiscoveries / 2, delayBetweenDiscoveries), cancellationToken);
                
                // Create a random device
                var deviceType = _random.Next(5);
                string name;
                string id = Guid.NewGuid().ToString();
                
                switch (deviceType)
                {
                    case 0:
                        name = "Simulated Headphones";
                        break;
                    case 1:
                        name = "Simulated Fitness Tracker";
                        break;
                    case 2:
                        name = "Simulated Heart Rate Monitor";
                        break;
                    case 3:
                        name = "Simulated Smart Watch";
                        break;
                    default:
                        name = $"Unknown Device {_random.Next(100)}";
                        break;
                }
                
                // Some devices might have no name
                if (_random.Next(10) < 2)
                {
                    name = string.Empty;
                }
                
                var device = new BleDeviceInfo
                {
                    Id = id,
                    Name = name,
                    Rssi = -1 * _random.Next(40, 95), // Random RSSI between -40 and -95
                    IsConnected = false
                };
                
                System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Discovered random device: {device.Name} ({device.Id})");
                
                // Trigger device discovery event
                DeviceDiscovered?.Invoke(this, new Core.Models.BleDeviceEventArgs { Device = device });
            }
        }
        
        /// <summary>
        /// Generate a unique key for a characteristic
        /// </summary>
        private string GetCharacteristicKey(BleGattCharacteristic characteristic)
        {
            return $"{characteristic.DeviceId}:{characteristic.ServiceUuid}:{characteristic.Uuid}";
        }
        
        /// <summary>
        /// Clean up all notification handlers for a device when disconnecting
        /// </summary>
        private void CleanupHandlersForDevice(string deviceId)
        {
            // Find and remove all handlers for this device
            var keysToRemove = new List<string>();
            
            foreach (var entry in _notificationHandlers)
            {
                if (entry.Key.StartsWith($"{deviceId}:"))
                {
                    keysToRemove.Add(entry.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _notificationHandlers.Remove(key);
            }
            
            System.Diagnostics.Debug.WriteLine($"SimulatedBluetoothService: Cleaned up {keysToRemove.Count} notification handlers for device {deviceId}");
        }
    }
}