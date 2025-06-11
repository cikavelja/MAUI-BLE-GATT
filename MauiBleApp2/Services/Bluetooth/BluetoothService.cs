using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Core.Models;
using MauiBleApp2.Core.Services.Bluetooth;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Implementation of platform's Bluetooth service using Plugin.BLE
    /// </summary>
    public class BluetoothService : ILocalBluetoothService
    {
        private readonly IBluetoothLE _bleAdapter;
        private readonly IAdapter _adapter;
        private readonly Dictionary<string, IDevice> _deviceCache = new();
        
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
        public bool IsBluetoothEnabled => _bleAdapter.IsOn;
        
        /// <summary>
        /// True if the service is currently scanning for devices
        /// </summary>
        public bool IsScanning => _adapter.IsScanning;

        public BluetoothService()
        {
            _bleAdapter = CrossBluetoothLE.Current;
            _adapter = CrossBluetoothLE.Current.Adapter;
            
            // Subscribe to device discovery events
            _adapter.DeviceDiscovered += OnDeviceDiscovered;
            _adapter.DeviceConnected += OnDeviceConnectionStateChanged;
            _adapter.DeviceDisconnected += OnDeviceConnectionStateChanged;
            
            // Subscribe to adapter state changes
            _bleAdapter.StateChanged += (s, e) => {
                Console.WriteLine($"Bluetooth state changed from {e.OldState} to {e.NewState}");
            };
        }

        /// <summary>
        /// Start scanning for BLE devices
        /// </summary>
        /// <param name="scanTimeout">Optional scan timeout in milliseconds</param>
        /// <param name="cancellationToken">Cancellation token to stop scanning</param>
        /// <returns>Task that completes when scanning stops</returns>
        public async Task StartScanningAsync(int scanTimeout = 10000, CancellationToken cancellationToken = default)
        {
            if (!_bleAdapter.IsOn)
            {
                throw new InvalidOperationException("Bluetooth is not enabled");
            }

            _deviceCache.Clear();
            
            try
            {
                Console.WriteLine("Starting BLE device scan...");
                
                // Create a scan filter that accepts all devices
                Func<IDevice, bool> scanFilter = (device) => true;
                
                // Start scanning with a timeout
                // Using explicit Guid[] overload to avoid ambiguity
                await _adapter.StartScanningForDevicesAsync(
                    serviceUuids: (Guid[])null, // No service UUID filter to discover all devices
                    deviceFilter: scanFilter,
                    allowDuplicatesKey: true, // Allow duplicate readings for improved discovery
                    cancellationToken: cancellationToken);
                
                Console.WriteLine("BLE scan started successfully");
                
                // We'll handle timeout on our end to avoid potential Plugin.BLE issues
                if (scanTimeout > 0)
                {
                    _ = Task.Delay(scanTimeout, cancellationToken)
                        .ContinueWith(async _ => 
                        {
                            try
                            {
                                if (!cancellationToken.IsCancellationRequested && _adapter.IsScanning)
                                {
                                    Console.WriteLine("Scan timeout reached, stopping scan");
                                    await StopScanningAsync();
                                }
                            }
                            catch (Exception ex) 
                            { 
                                Console.WriteLine($"Error stopping scan on timeout: {ex.Message}");
                            }
                        }, TaskScheduler.Default);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start BLE scan: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stop scanning for BLE devices
        /// </summary>
        public async Task StopScanningAsync()
        {
            if (_adapter.IsScanning)
            {
                await _adapter.StopScanningForDevicesAsync();
                Console.WriteLine("BLE scan stopped");
            }
        }

        /// <summary>
        /// Connect to a specific BLE device
        /// </summary>
        /// <param name="deviceId">The device ID to connect to</param>
        /// <param name="cancellationToken">Cancellation token to cancel connection attempt</param>
        /// <returns>Connected device information or null if connection failed</returns>
        public async Task<BleDeviceInfo?> ConnectToDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[DEBUG] ConnectToDeviceAsync called with deviceId: {deviceId}");
            if (!_bleAdapter.IsOn)
            {
                Console.WriteLine("[DEBUG] Bluetooth is not enabled");
                throw new InvalidOperationException("Bluetooth is not enabled");
            }

            IDevice device = null!;
            
            // Find device in cache or from adapter
            if (_deviceCache.ContainsKey(deviceId))
            {
                device = _deviceCache[deviceId];
                Console.WriteLine($"[DEBUG] Device found in cache: {device.Name} ({device.Id})");
            }
            else
            {
                var knownDevices = _adapter.GetSystemConnectedOrPairedDevices();
                device = knownDevices.FirstOrDefault(d => d.Id.ToString() == deviceId);
                
                if (device == null)
                {
                    Console.WriteLine($"[DEBUG] Device {deviceId} not found in cache or system devices");
                    return null;
                }
                Console.WriteLine($"[DEBUG] Device found in system devices: {device.Name} ({device.Id})");
                _deviceCache[deviceId] = device;
            }

            try
            {
                Console.WriteLine($"[DEBUG] Attempting to connect to device: {device.Name ?? "Unknown"} ({device.Id})");
                
                // Try to connect to the device with auto connect
                var connectParameters = new ConnectParameters(autoConnect: false, forceBleTransport: true);
                await _adapter.ConnectToDeviceAsync(device, connectParameters, cancellationToken);
                
                Console.WriteLine($"[DEBUG] Successfully connected to device: {device.Name ?? "Unknown"} ({device.Id})");
                
                return new BleDeviceInfo
                {
                    Id = device.Id.ToString(),
                    Name = device.Name ?? "Unknown Device",
                    Rssi = device.Rssi,
                    IsConnected = device.State == Plugin.BLE.Abstractions.DeviceState.Connected
                };
            }
            catch (DeviceConnectionException ex)
            {
                Console.WriteLine($"[DEBUG] Failed to connect to device: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Disconnect from a connected BLE device
        /// </summary>
        /// <param name="deviceId">The device ID to disconnect from</param>
        public async Task DisconnectDeviceAsync(string deviceId)
        {
            if (_deviceCache.ContainsKey(deviceId))
            {
                var device = _deviceCache[deviceId];
                if (device.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                {
                    await _adapter.DisconnectDeviceAsync(device);
                }
            }
        }

        /// <summary>
        /// Get GATT services for a connected device
        /// </summary>
        /// <param name="deviceId">The connected device ID</param>
        /// <returns>List of GATT services</returns>
        public async Task<List<BleGattService>> GetServicesAsync(string deviceId)
        {
            if (!_deviceCache.ContainsKey(deviceId))
            {
                throw new InvalidOperationException($"Device {deviceId} not found in cache");
            }
            
            var device = _deviceCache[deviceId];
            if (device.State != Plugin.BLE.Abstractions.DeviceState.Connected)
            {
                throw new InvalidOperationException($"Device {deviceId} is not connected");
            }

            var services = await device.GetServicesAsync();
            
            return services.Select(s => new BleGattService
            {
                Uuid = s.Id.ToString(),
                DeviceId = deviceId,
                IsPrimary = true // Plugin.BLE doesn't provide this information directly
            }).ToList();
        }

        /// <summary>
        /// Get characteristics for a specific GATT service
        /// </summary>
        /// <param name="service">The GATT service</param>
        /// <returns>List of characteristics for this service</returns>
        public async Task<List<BleGattCharacteristic>> GetCharacteristicsAsync(BleGattService service)
        {
            if (!_deviceCache.ContainsKey(service.DeviceId))
            {
                throw new InvalidOperationException($"Device {service.DeviceId} not found in cache");
            }

            var device = _deviceCache[service.DeviceId];
            var nativeService = await device.GetServiceAsync(Guid.Parse(service.Uuid));
            if (nativeService == null)
            {
                throw new InvalidOperationException($"Service {service.Uuid} not found on device {service.DeviceId}");
            }

            var characteristics = await nativeService.GetCharacteristicsAsync();
            
            return characteristics.Select(c => new BleGattCharacteristic
            {
                Uuid = c.Id.ToString(),
                ServiceUuid = service.Uuid,
                DeviceId = service.DeviceId,
                CanRead = c.CanRead,
                CanWrite = c.CanWrite,
                CanNotify = c.CanUpdate,
                CanIndicate = c.CanUpdate // Plugin.BLE combines notifications and indications
            }).ToList();
        }

        /// <summary>
        /// Read data from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to read from</param>
        /// <returns>Byte array of data read from characteristic</returns>
        public async Task<byte[]> ReadCharacteristicAsync(BleGattCharacteristic characteristic)
        {
            if (!_deviceCache.ContainsKey(characteristic.DeviceId))
            {
                throw new InvalidOperationException($"Device {characteristic.DeviceId} not found in cache");
            }

            var device = _deviceCache[characteristic.DeviceId];
            var service = await device.GetServiceAsync(Guid.Parse(characteristic.ServiceUuid));
            if (service == null)
            {
                throw new InvalidOperationException($"Service {characteristic.ServiceUuid} not found on device");
            }

            var nativeChar = await service.GetCharacteristicAsync(Guid.Parse(characteristic.Uuid));
            if (nativeChar == null)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} not found in service");
            }

            if (!nativeChar.CanRead)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} does not support reading");
            }

            var result = await nativeChar.ReadAsync();
            return result.data; // Return only the data part of the tuple
        }

        /// <summary>
        /// Write data to a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to write to</param>
        /// <param name="data">Byte array of data to write</param>
        /// <returns>True if write was successful</returns>
        public async Task<bool> WriteCharacteristicAsync(BleGattCharacteristic characteristic, byte[] data)
        {
            if (!_deviceCache.ContainsKey(characteristic.DeviceId))
            {
                throw new InvalidOperationException($"Device {characteristic.DeviceId} not found in cache");
            }

            var device = _deviceCache[characteristic.DeviceId];
            var service = await device.GetServiceAsync(Guid.Parse(characteristic.ServiceUuid));
            if (service == null)
            {
                throw new InvalidOperationException($"Service {characteristic.ServiceUuid} not found on device");
            }

            var nativeChar = await service.GetCharacteristicAsync(Guid.Parse(characteristic.Uuid));
            if (nativeChar == null)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} not found in service");
            }

            if (!nativeChar.CanWrite)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} does not support writing");
            }

            try
            {
                await nativeChar.WriteAsync(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Subscribe to notifications from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to subscribe to</param>
        /// <param name="notificationHandler">Callback when notification data is received</param>
        /// <returns>True if subscription was successful</returns>
        public async Task<bool> SubscribeToCharacteristicAsync(BleGattCharacteristic characteristic, Action<byte[]> notificationHandler)
        {
            if (!_deviceCache.ContainsKey(characteristic.DeviceId))
            {
                throw new InvalidOperationException($"Device {characteristic.DeviceId} not found in cache");
            }

            var device = _deviceCache[characteristic.DeviceId];
            var service = await device.GetServiceAsync(Guid.Parse(characteristic.ServiceUuid));
            if (service == null)
            {
                throw new InvalidOperationException($"Service {characteristic.ServiceUuid} not found on device");
            }

            var nativeChar = await service.GetCharacteristicAsync(Guid.Parse(characteristic.Uuid));
            if (nativeChar == null)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} not found in service");
            }

            if (!nativeChar.CanUpdate)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} does not support notifications");
            }

            try
            {
                // Handle notifications
                nativeChar.ValueUpdated += (sender, args) =>
                {
                    notificationHandler(args.Characteristic.Value);
                };
                
                await nativeChar.StartUpdatesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Unsubscribe from notifications from a characteristic
        /// </summary>
        /// <param name="characteristic">The characteristic to unsubscribe from</param>
        /// <returns>True if unsubscription was successful</returns>
        public async Task<bool> UnsubscribeFromCharacteristicAsync(BleGattCharacteristic characteristic)
        {
            if (!_deviceCache.ContainsKey(characteristic.DeviceId))
            {
                throw new InvalidOperationException($"Device {characteristic.DeviceId} not found in cache");
            }

            var device = _deviceCache[characteristic.DeviceId];
            var service = await device.GetServiceAsync(Guid.Parse(characteristic.ServiceUuid));
            if (service == null)
            {
                throw new InvalidOperationException($"Service {characteristic.ServiceUuid} not found on device");
            }

            var nativeChar = await service.GetCharacteristicAsync(Guid.Parse(characteristic.Uuid));
            if (nativeChar == null)
            {
                throw new InvalidOperationException($"Characteristic {characteristic.Uuid} not found in service");
            }

            try
            {
                await nativeChar.StopUpdatesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #region Event Handlers

        private void OnDeviceDiscovered(object sender, DeviceEventArgs e)
        {
            // Log all discovered devices, even those with no name
            Console.WriteLine($"Device discovered: {e.Device.Name ?? "Unnamed"} ({e.Device.Id}), RSSI: {e.Device.Rssi}");
            
            // Cache the device - even nameless ones can sometimes be useful
            _deviceCache[e.Device.Id.ToString()] = e.Device;
            
            // Create BleDeviceInfo from IDevice
            var deviceInfo = new BleDeviceInfo
            {
                Id = e.Device.Id.ToString(),
                Name = string.IsNullOrEmpty(e.Device.Name) ? $"Unknown Device ({e.Device.Id.ToString().Substring(0, 8)})" : e.Device.Name,
                Rssi = e.Device.Rssi,
                IsConnected = e.Device.State == Plugin.BLE.Abstractions.DeviceState.Connected
            };
            
            // Trigger the device discovered event
            DeviceDiscovered?.Invoke(this, new Core.Models.BleDeviceEventArgs { Device = deviceInfo });
        }

        private void OnDeviceConnectionStateChanged(object sender, DeviceEventArgs e)
        {
            Console.WriteLine($"Device connection state changed: {e.Device.Name ?? "Unknown"} - {e.Device.State}");
            
            // Create BleDeviceInfo from IDevice
            var deviceInfo = new BleDeviceInfo
            {
                Id = e.Device.Id.ToString(),
                Name = e.Device.Name ?? "Unknown Device",
                Rssi = e.Device.Rssi,
                IsConnected = e.Device.State == Plugin.BLE.Abstractions.DeviceState.Connected
            };
            
            // Trigger the connection state changed event
            ConnectionStateChanged?.Invoke(this, new Core.Models.BleConnectionEventArgs 
            { 
                Device = deviceInfo,
                IsConnected = e.Device.State == Plugin.BLE.Abstractions.DeviceState.Connected
            });
        }

        #endregion
    }
}