using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MauiBleApp2.Core.Models;

namespace MauiBleApp2.Services.Bluetooth.Simulation
{
    /// <summary>
    /// A simulated BLE GATT server for testing BLE communication without real hardware
    /// </summary>
    public class BleGattServerSimulator
    {
        private readonly Dictionary<string, SimulatedGattService> _services = new();
        private readonly ConcurrentDictionary<string, List<Action<byte[]>>> _notificationSubscribers = new();
        private bool _isRunning;
        private readonly string _deviceId = Guid.NewGuid().ToString();
        private readonly string _deviceName = "Simulated BLE Device";
        
        private readonly Timer? _notificationTimer;
        
        /// <summary>
        /// Create a new GATT server simulator with predefined services
        /// </summary>
        public BleGattServerSimulator()
        {
            // Create some simulated services and characteristics
            InitializeSimulatedServices();
            
            // Create a timer for sending periodic notifications
            _notificationTimer = new Timer(SendSimulatedNotifications, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Start the simulated GATT server
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;
            
            _isRunning = true;
            _notificationTimer?.Change(0, 1000); // Send notifications every second
        }

        /// <summary>
        /// Stop the simulated GATT server
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;
            
            _isRunning = false;
            _notificationTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Get information about the simulated BLE device
        /// </summary>
        public BleDeviceInfo GetDeviceInfo()
        {
            return new BleDeviceInfo
            {
                Id = _deviceId,
                Name = _deviceName,
                IsConnected = _isRunning,
                Rssi = -65 // Simulated signal strength
            };
        }

        /// <summary>
        /// Get the list of available GATT services
        /// </summary>
        public List<BleGattService> GetServices()
        {
            return _services.Values.Select(s => new BleGattService
            {
                Uuid = s.Uuid,
                DeviceId = _deviceId,
                IsPrimary = true
            }).ToList();
        }

        /// <summary>
        /// Get characteristics for a specific service
        /// </summary>
        public List<BleGattCharacteristic> GetCharacteristics(string serviceUuid)
        {
            if (!_services.TryGetValue(serviceUuid, out var service))
                return new List<BleGattCharacteristic>();

            return service.Characteristics.Values.Select(c => new BleGattCharacteristic
            {
                Uuid = c.Uuid,
                ServiceUuid = serviceUuid,
                DeviceId = _deviceId,
                CanRead = c.CanRead,
                CanWrite = c.CanWrite,
                CanNotify = c.CanNotify,
                CanIndicate = c.CanIndicate
            }).ToList();
        }

        /// <summary>
        /// Read a characteristic value
        /// </summary>
        public byte[] ReadCharacteristic(string serviceUuid, string characteristicUuid)
        {
            if (!_services.TryGetValue(serviceUuid, out var service))
                throw new ArgumentException($"Service {serviceUuid} not found");

            if (!service.Characteristics.TryGetValue(characteristicUuid, out var characteristic))
                throw new ArgumentException($"Characteristic {characteristicUuid} not found");

            if (!characteristic.CanRead)
                throw new InvalidOperationException($"Characteristic {characteristicUuid} is not readable");

            return characteristic.GetValue();
        }

        /// <summary>
        /// Write a value to a characteristic
        /// </summary>
        public void WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data)
        {
            if (!_services.TryGetValue(serviceUuid, out var service))
                throw new ArgumentException($"Service {serviceUuid} not found");

            if (!service.Characteristics.TryGetValue(characteristicUuid, out var characteristic))
                throw new ArgumentException($"Characteristic {characteristicUuid} not found");

            if (!characteristic.CanWrite)
                throw new InvalidOperationException($"Characteristic {characteristicUuid} is not writable");

            characteristic.SetValue(data);
        }

        /// <summary>
        /// Subscribe to notifications from a characteristic
        /// </summary>
        public void SubscribeToNotifications(string serviceUuid, string characteristicUuid, Action<byte[]> notificationHandler)
        {
            if (!_services.TryGetValue(serviceUuid, out var service))
                throw new ArgumentException($"Service {serviceUuid} not found");

            if (!service.Characteristics.TryGetValue(characteristicUuid, out var characteristic))
                throw new ArgumentException($"Characteristic {characteristicUuid} not found");

            if (!characteristic.CanNotify)
                throw new InvalidOperationException($"Characteristic {characteristicUuid} does not support notifications");

            string key = $"{serviceUuid}:{characteristicUuid}";
            _notificationSubscribers.AddOrUpdate(
                key,
                new List<Action<byte[]>> { notificationHandler },
                (_, currentHandlers) => 
                {
                    currentHandlers.Add(notificationHandler);
                    return currentHandlers;
                });
        }

        /// <summary>
        /// Unsubscribe from notifications from a characteristic
        /// </summary>
        public void UnsubscribeFromNotifications(string serviceUuid, string characteristicUuid, Action<byte[]> notificationHandler)
        {
            string key = $"{serviceUuid}:{characteristicUuid}";
            
            if (_notificationSubscribers.TryGetValue(key, out var handlers))
            {
                handlers.Remove(notificationHandler);
                
                if (handlers.Count == 0)
                {
                    _notificationSubscribers.TryRemove(key, out _);
                }
            }
        }

        /// <summary>
        /// Set up simulated services and characteristics
        /// </summary>
        private void InitializeSimulatedServices()
        {
            // Device Information Service (0x180A)
            var disUuid = "0000180A-0000-1000-8000-00805F9B34FB";
            var disService = new SimulatedGattService(disUuid);
            
            // Model Number Characteristic (0x2A24)
            var modelNumberUuid = "00002A24-0000-1000-8000-00805F9B34FB";
            disService.AddCharacteristic(new SimulatedGattCharacteristic(
                modelNumberUuid,
                true,   // CanRead
                false,  // CanWrite
                false,  // CanNotify
                false,  // CanIndicate
                System.Text.Encoding.UTF8.GetBytes("BLE-SIM-2000")
            ));
            
            // Manufacturer Name Characteristic (0x2A29)
            var manufacturerUuid = "00002A29-0000-1000-8000-00805F9B34FB";
            disService.AddCharacteristic(new SimulatedGattCharacteristic(
                manufacturerUuid,
                true,   // CanRead
                false,  // CanWrite
                false,  // CanNotify
                false,  // CanIndicate
                System.Text.Encoding.UTF8.GetBytes("MAUI BLE Simulator")
            ));
            
            _services.Add(disUuid, disService);
            
            // Battery Service (0x180F)
            var batteryServiceUuid = "0000180F-0000-1000-8000-00805F9B34FB";
            var batteryService = new SimulatedGattService(batteryServiceUuid);
            
            // Battery Level Characteristic (0x2A19)
            var batteryLevelUuid = "00002A19-0000-1000-8000-00805F9B34FB";
            batteryService.AddCharacteristic(new SimulatedGattCharacteristic(
                batteryLevelUuid,
                true,   // CanRead
                false,  // CanWrite
                true,   // CanNotify
                false,  // CanIndicate
                new byte[] { 100 } // 100% battery
            ));
            
            _services.Add(batteryServiceUuid, batteryService);
            
            // Custom Service
            var customServiceUuid = "12345678-1234-5678-1234-56789ABCDEF0";
            var customService = new SimulatedGattService(customServiceUuid);
            
            // Custom Read/Write Characteristic
            var readWriteUuid = "12345678-1234-5678-1234-56789ABCDEF1";
            customService.AddCharacteristic(new SimulatedGattCharacteristic(
                readWriteUuid,
                true,   // CanRead
                true,   // CanWrite
                false,  // CanNotify
                false,  // CanIndicate
                System.Text.Encoding.UTF8.GetBytes("Hello BLE World!")
            ));
            
            // Custom Notification Characteristic
            var notifyUuid = "12345678-1234-5678-1234-56789ABCDEF2";
            customService.AddCharacteristic(new SimulatedGattCharacteristic(
                notifyUuid,
                true,   // CanRead
                false,  // CanWrite
                true,   // CanNotify
                false,  // CanIndicate
                System.Text.Encoding.UTF8.GetBytes("0")
            ));
            
            // Temperature Characteristic (simulated sensor)
            var temperatureUuid = "12345678-1234-5678-1234-56789ABCDEF3";
            customService.AddCharacteristic(new SimulatedGattCharacteristic(
                temperatureUuid,
                true,   // CanRead
                false,  // CanWrite
                true,   // CanNotify
                false,  // CanIndicate
                BitConverter.GetBytes(22.5f) // 22.5�C
            ));
            
            _services.Add(customServiceUuid, customService);
        }

        /// <summary>
        /// Send simulated notifications to subscribers
        /// </summary>
        private void SendSimulatedNotifications(object? state)
        {
            if (!_isRunning)
                return;
            
            try
            {
                // Update battery level (decrease by 1% each time, loop back to 100% when it reaches 0%)
                var batteryServiceUuid = "0000180F-0000-1000-8000-00805F9B34FB";
                var batteryLevelUuid = "00002A19-0000-1000-8000-00805F9B34FB";
                
                if (_services.TryGetValue(batteryServiceUuid, out var batteryService) &&
                    batteryService.Characteristics.TryGetValue(batteryLevelUuid, out var batteryChar))
                {
                    var batteryLevel = batteryChar.GetValue()[0];
                    batteryLevel = (byte)(batteryLevel > 0 ? batteryLevel - 1 : 100);
                    batteryChar.SetValue(new byte[] { batteryLevel });
                    
                    // Send notification if someone is subscribed
                    string batteryKey = $"{batteryServiceUuid}:{batteryLevelUuid}";
                    if (_notificationSubscribers.TryGetValue(batteryKey, out var batteryHandlers))
                    {
                        foreach (var handler in batteryHandlers)
                        {
                            handler(new byte[] { batteryLevel });
                        }
                    }
                }
                
                // Update counter notification
                var customServiceUuid = "12345678-1234-5678-1234-56789ABCDEF0";
                var notifyUuid = "12345678-1234-5678-1234-56789ABCDEF2";
                
                if (_services.TryGetValue(customServiceUuid, out var customService) &&
                    customService.Characteristics.TryGetValue(notifyUuid, out var notifyChar))
                {
                    var counterStr = System.Text.Encoding.UTF8.GetString(notifyChar.GetValue());
                    if (int.TryParse(counterStr, out var counter))
                    {
                        counter++;
                        notifyChar.SetValue(System.Text.Encoding.UTF8.GetBytes(counter.ToString()));
                        
                        // Send notification if someone is subscribed
                        string counterKey = $"{customServiceUuid}:{notifyUuid}";
                        if (_notificationSubscribers.TryGetValue(counterKey, out var counterHandlers))
                        {
                            foreach (var handler in counterHandlers)
                            {
                                handler(System.Text.Encoding.UTF8.GetBytes(counter.ToString()));
                            }
                        }
                    }
                }
                
                // Update simulated temperature (oscillate between 20 and 30 degrees)
                var temperatureUuid = "12345678-1234-5678-1234-56789ABCDEF3";
                
                if (_services.TryGetValue(customServiceUuid, out var tempService) &&
                    tempService.Characteristics.TryGetValue(temperatureUuid, out var tempChar))
                {
                    var currentTemp = BitConverter.ToSingle(tempChar.GetValue(), 0);
                    var time = DateTime.Now.TimeOfDay.TotalSeconds;
                    var newTemp = 25 + 5 * Math.Sin(time / 10); // Oscillate between 20 and 30
                    tempChar.SetValue(BitConverter.GetBytes((float)newTemp));
                    
                    // Send notification if someone is subscribed
                    string tempKey = $"{customServiceUuid}:{temperatureUuid}";
                    if (_notificationSubscribers.TryGetValue(tempKey, out var tempHandlers))
                    {
                        foreach (var handler in tempHandlers)
                        {
                            handler(BitConverter.GetBytes((float)newTemp));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in simulated notification: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// A simulated GATT service
    /// </summary>
    public class SimulatedGattService
    {
        /// <summary>
        /// Service UUID
        /// </summary>
        public string Uuid { get; }
        
        /// <summary>
        /// Characteristics in this service
        /// </summary>
        public Dictionary<string, SimulatedGattCharacteristic> Characteristics { get; } = new();

        public SimulatedGattService(string uuid)
        {
            Uuid = uuid;
        }

        public void AddCharacteristic(SimulatedGattCharacteristic characteristic)
        {
            Characteristics[characteristic.Uuid] = characteristic;
        }
    }

    /// <summary>
    /// A simulated GATT characteristic
    /// </summary>
    public class SimulatedGattCharacteristic
    {
        /// <summary>
        /// Characteristic UUID
        /// </summary>
        public string Uuid { get; }
        
        /// <summary>
        /// Whether this characteristic can be read
        /// </summary>
        public bool CanRead { get; }
        
        /// <summary>
        /// Whether this characteristic can be written to
        /// </summary>
        public bool CanWrite { get; }
        
        /// <summary>
        /// Whether this characteristic supports notifications
        /// </summary>
        public bool CanNotify { get; }
        
        /// <summary>
        /// Whether this characteristic supports indications
        /// </summary>
        public bool CanIndicate { get; }
        
        private byte[] _value;
        private readonly object _valueLock = new();

        public SimulatedGattCharacteristic(
            string uuid, 
            bool canRead, 
            bool canWrite, 
            bool canNotify, 
            bool canIndicate, 
            byte[] initialValue)
        {
            Uuid = uuid;
            CanRead = canRead;
            CanWrite = canWrite;
            CanNotify = canNotify;
            CanIndicate = canIndicate;
            _value = initialValue;
        }

        public byte[] GetValue()
        {
            lock (_valueLock)
            {
                return _value.ToArray(); // Return a copy
            }
        }

        public void SetValue(byte[] value)
        {
            lock (_valueLock)
            {
                _value = value.ToArray(); // Store a copy
            }
        }
    }
}