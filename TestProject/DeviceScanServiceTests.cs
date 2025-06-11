using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Core.Models;
using MauiBleApp2.Core.Services.Bluetooth;
using Moq;
using NUnit.Framework;

namespace TestProject
{
    [TestFixture]
    public class DeviceScanServiceTests
    {
        private Mock<IBluetoothService> _mockBluetoothService = null!;
        private DeviceScanService _deviceScanService = null!;
        private ObservableCollection<BleDeviceInfo> _devices = null!;

        [SetUp]
        public void Setup()
        {
            _mockBluetoothService = new Mock<IBluetoothService>();
            _deviceScanService = new DeviceScanService(_mockBluetoothService.Object);
            _devices = new ObservableCollection<BleDeviceInfo>();
        }

        [Test]
        public async Task StartScanningAsync_CallsBluetoothService()
        {
            // Arrange
            int scanTimeout = 5000;
            var cancellationToken = CancellationToken.None;
            
            _mockBluetoothService
                .Setup(m => m.StartScanningAsync(scanTimeout, cancellationToken))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _deviceScanService.StartScanningAsync(scanTimeout, cancellationToken);

            // Assert
            _mockBluetoothService.Verify(
                m => m.StartScanningAsync(scanTimeout, cancellationToken),
                Times.Once);
        }

        [Test]
        public async Task StopScanningAsync_CallsBluetoothService()
        {
            // Arrange
            _mockBluetoothService
                .Setup(m => m.StopScanningAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _deviceScanService.StopScanningAsync();

            // Assert
            _mockBluetoothService.Verify(m => m.StopScanningAsync(), Times.Once);
        }

        [Test]
        public async Task ConnectToDeviceAsync_CallsBluetoothService()
        {
            // Arrange
            var deviceId = "test-device-123";
            var expectedDevice = new BleDeviceInfo
            {
                Id = deviceId,
                Name = "Test Device",
                Rssi = -65
            };
            
            _mockBluetoothService
                .Setup(m => m.ConnectToDeviceAsync(deviceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedDevice)
                .Verifiable();

            // Act
            var result = await _deviceScanService.ConnectToDeviceAsync(deviceId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(deviceId));
            _mockBluetoothService.Verify(
                m => m.ConnectToDeviceAsync(deviceId, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void FindExistingDevice_WithMatchingId_ReturnsDevice()
        {
            // Arrange
            var deviceId = "test-device-123";
            var device = new BleDeviceInfo
            {
                Id = deviceId,
                Name = "Test Device"
            };
            _devices.Add(device);

            // Act
            var result = _deviceScanService.FindExistingDevice(deviceId, _devices);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(device));
        }

        [Test]
        public void FindExistingDevice_WithNonMatchingId_ReturnsNull()
        {
            // Arrange
            var deviceId = "test-device-123";
            var device = new BleDeviceInfo
            {
                Id = "different-id",
                Name = "Test Device"
            };
            _devices.Add(device);

            // Act
            var result = _deviceScanService.FindExistingDevice(deviceId, _devices);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void UpdateDevice_UpdatesProperties()
        {
            // Arrange
            var existingDevice = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "Old Name",
                Rssi = -80,
                IsConnected = false
            };

            var newDevice = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "New Name",
                Rssi = -65,
                IsConnected = true
            };

            // Act
            _deviceScanService.UpdateDevice(existingDevice, newDevice);

            // Assert
            Assert.That(existingDevice.Name, Is.EqualTo("New Name"));
            Assert.That(existingDevice.Rssi, Is.EqualTo(-65));
            Assert.That(existingDevice.IsConnected, Is.True);
        }

        [Test]
        public void DeviceDiscovered_RaisesEvent()
        {
            // Arrange
            var device = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "Test Device"
            };
            
            var eventArgs = new MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs { Device = device };
            MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs? capturedArgs = null;
            
            _deviceScanService.DeviceDiscovered += (sender, args) => capturedArgs = args;

            // Act
            // Simulate the event being raised from the Bluetooth service
            _mockBluetoothService.Raise(
                m => m.DeviceDiscovered += null,
                _mockBluetoothService.Object,
                eventArgs);

            // Assert
            Assert.That(capturedArgs, Is.Not.Null);
            Assert.That(capturedArgs.Device, Is.SameAs(device));
        }
    }
}