using MauiBleApp2.Core.Models;
using CoreBleDeviceEventArgs = MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs;
using CoreBleConnectionEventArgs = MauiBleApp2.Core.Services.Bluetooth.BleConnectionEventArgs;
using MauiBleApp2.Services.Bluetooth;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MauiBleApp2.Services.Bluetooth
{
    /// <summary>
    /// Adapter class that adapts the platform-specific BluetoothService to implement the Core IBluetoothService interface
    /// </summary>
    public class BluetoothServiceAdapter : Core.Services.Bluetooth.IBluetoothService
    {
        private readonly BluetoothService _bluetoothService;
        
        public event EventHandler<CoreBleDeviceEventArgs>? DeviceDiscovered;
        public event EventHandler<CoreBleConnectionEventArgs>? ConnectionStateChanged;

        public bool IsBluetoothEnabled => _bluetoothService.IsBluetoothEnabled;
        public bool IsScanning => _bluetoothService.IsScanning;

        public BluetoothServiceAdapter(BluetoothService bluetoothService)
        {
            _bluetoothService = bluetoothService ?? throw new ArgumentNullException(nameof(bluetoothService));
            
            // Subscribe to events from the implementation and forward them as Core events
            _bluetoothService.DeviceDiscovered += OnDeviceDiscovered;
            _bluetoothService.ConnectionStateChanged += OnConnectionStateChanged;
        }

        public Task StartScanningAsync(int scanTimeout = 10000, CancellationToken cancellationToken = default)
        {
            return _bluetoothService.StartScanningAsync(scanTimeout, cancellationToken);
        }

        public Task StopScanningAsync()
        {
            return _bluetoothService.StopScanningAsync();
        }

        public async Task<Core.Models.BleDeviceInfo?> ConnectToDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            var result = await _bluetoothService.ConnectToDeviceAsync(deviceId, cancellationToken);
            if (result == null)
                return null;
                
            return ConvertToCore(result);
        }

        public Task DisconnectDeviceAsync(string deviceId)
        {
            return _bluetoothService.DisconnectDeviceAsync(deviceId);
        }

        public async Task<List<Core.Models.BleGattService>> GetServicesAsync(string deviceId)
        {
            var result = await _bluetoothService.GetServicesAsync(deviceId);
            return ConvertToCore(result);
        }

        public async Task<List<Core.Models.BleGattCharacteristic>> GetCharacteristicsAsync(Core.Models.BleGattService service)
        {
            var mapiService = ConvertFromCore(service);
            var result = await _bluetoothService.GetCharacteristicsAsync(mapiService);
            return ConvertToCore(result);
        }

        public async Task<byte[]> ReadCharacteristicAsync(Core.Models.BleGattCharacteristic characteristic)
        {
            var mapiCharacteristic = ConvertFromCore(characteristic);
            return await _bluetoothService.ReadCharacteristicAsync(mapiCharacteristic);
        }

        public async Task<bool> WriteCharacteristicAsync(Core.Models.BleGattCharacteristic characteristic, byte[] data)
        {
            var mapiCharacteristic = ConvertFromCore(characteristic);
            return await _bluetoothService.WriteCharacteristicAsync(mapiCharacteristic, data);
        }

        public async Task<bool> SubscribeToCharacteristicAsync(Core.Models.BleGattCharacteristic characteristic, Action<byte[]> notificationHandler)
        {
            var mapiCharacteristic = ConvertFromCore(characteristic);
            return await _bluetoothService.SubscribeToCharacteristicAsync(mapiCharacteristic, notificationHandler);
        }

        public async Task<bool> UnsubscribeFromCharacteristicAsync(Core.Models.BleGattCharacteristic characteristic)
        {
            var mapiCharacteristic = ConvertFromCore(characteristic);
            return await _bluetoothService.UnsubscribeFromCharacteristicAsync(mapiCharacteristic);
        }
        
        #region Event handlers
        private void OnDeviceDiscovered(object? sender, Core.Models.BleDeviceEventArgs e)
        {
            // Convert and forward the event
            var coreDevice = ConvertToCore(e.Device);
            DeviceDiscovered?.Invoke(this, new CoreBleDeviceEventArgs { Device = coreDevice });
        }

        private void OnConnectionStateChanged(object? sender, Core.Models.BleConnectionEventArgs e)
        {
            // Convert and forward the event
            var coreDevice = ConvertToCore(e.Device);
            ConnectionStateChanged?.Invoke(this, new CoreBleConnectionEventArgs
            {
                Device = coreDevice,
                IsConnected = e.IsConnected
            });
        }
        #endregion
        
        #region Conversion helpers
        // Convert from MAUI model to Core model
        private Core.Models.BleDeviceInfo ConvertToCore(Core.Models.BleDeviceInfo device)
        {
            var coreDevice = new Core.Models.BleDeviceInfo
            {
                Id = device.Id,
                Name = device.Name,
                Rssi = device.Rssi,
                IsConnected = device.IsConnected
            };
            
            // Copy advertisement data
            if (device.AdvertisementData != null)
            {
                foreach (var item in device.AdvertisementData)
                {
                    coreDevice.AdvertisementData[item.Key] = item.Value;
                }
            }
            
            return coreDevice;
        }
        
        private List<Core.Models.BleGattService> ConvertToCore(List<Core.Models.BleGattService> services)
        {
            var result = new List<Core.Models.BleGattService>();
            foreach (var service in services)
            {
                result.Add(new Core.Models.BleGattService
                {
                    Uuid = service.Uuid,
                    DeviceId = service.DeviceId,
                    IsPrimary = service.IsPrimary
                });
            }
            return result;
        }
        
        private List<Core.Models.BleGattCharacteristic> ConvertToCore(List<Core.Models.BleGattCharacteristic> characteristics)
        {
            var result = new List<Core.Models.BleGattCharacteristic>();
            foreach (var characteristic in characteristics)
            {
                result.Add(new Core.Models.BleGattCharacteristic
                {
                    Uuid = characteristic.Uuid,
                    ServiceUuid = characteristic.ServiceUuid,
                    DeviceId = characteristic.DeviceId,
                    CanRead = characteristic.CanRead,
                    CanWrite = characteristic.CanWrite,
                    CanNotify = characteristic.CanNotify,
                    CanIndicate = characteristic.CanIndicate
                });
            }
            return result;
        }
        
        // Convert from Core model to MAUI model
        private Core.Models.BleDeviceInfo ConvertFromCore(Core.Models.BleDeviceInfo device)
        {
            var mapiDevice = new Core.Models.BleDeviceInfo
            {
                Id = device.Id,
                Name = device.Name,
                Rssi = device.Rssi,
                IsConnected = device.IsConnected
            };
            
            // Copy advertisement data
            if (device.AdvertisementData != null)
            {
                foreach (var item in device.AdvertisementData)
                {
                    mapiDevice.AdvertisementData[item.Key] = item.Value;
                }
            }
            
            return mapiDevice;
        }
        
        private Core.Models.BleGattService ConvertFromCore(Core.Models.BleGattService service)
        {
            return new Core.Models.BleGattService
            {
                Uuid = service.Uuid,
                DeviceId = service.DeviceId,
                IsPrimary = service.IsPrimary
            };
        }
        
        private Core.Models.BleGattCharacteristic ConvertFromCore(Core.Models.BleGattCharacteristic characteristic)
        {
            return new Core.Models.BleGattCharacteristic
            {
                Uuid = characteristic.Uuid,
                ServiceUuid = characteristic.ServiceUuid,
                DeviceId = characteristic.DeviceId,
                CanRead = characteristic.CanRead,
                CanWrite = characteristic.CanWrite,
                CanNotify = characteristic.CanNotify,
                CanIndicate = characteristic.CanIndicate
            };
        }
        #endregion
    }
}