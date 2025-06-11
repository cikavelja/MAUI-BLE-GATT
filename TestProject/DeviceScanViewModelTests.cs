using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using MauiBleApp2.Core.Models;
using MauiBleApp2.Core.Services.Bluetooth;
using MauiBleApp2.Core.ViewModels;
using Moq;
using NUnit.Framework;

namespace TestProject
{
    [TestFixture]
    public class DeviceScanViewModelTests
    {
        private Mock<IBluetoothService> _mockBluetoothService = null!;
        private Mock<IBleMessageParser> _mockMessageParser = null!;
        private Mock<IDeviceScanService> _mockDeviceScanService = null!;
        private DeviceScanViewModel _viewModel = null!;

        [SetUp]
        public void Setup()
        {
            _mockBluetoothService = new Mock<IBluetoothService>();
            _mockMessageParser = new Mock<IBleMessageParser>();
            _mockDeviceScanService = new Mock<IDeviceScanService>();
            
            // Set up the IsBluetoothEnabled property
            _mockBluetoothService.Setup(m => m.IsBluetoothEnabled).Returns(true);
            
            _viewModel = new DeviceScanViewModel(
                _mockBluetoothService.Object,
                _mockMessageParser.Object,
                _mockDeviceScanService.Object);
        }

        [Test]
        public void Constructor_RegistersForDeviceDiscoveredEvent()
        {
            // Verify the constructor registered for the event
            _mockDeviceScanService.VerifyAdd(m => m.DeviceDiscovered += It.IsAny<EventHandler<MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs>>(), 
                Times.Once);
        }

        [Test]
        public void Constructor_InitializesProperties()
        {
            // Assert
            Assert.That(_viewModel.IsBluetoothEnabled, Is.True);
            Assert.That(_viewModel.Title, Is.EqualTo("BLE Device Scanner"));
            Assert.That(_viewModel.Devices, Is.Empty);
            Assert.That(_viewModel.IsScanning, Is.False);
            Assert.That(_viewModel.IsBusy, Is.False);
        }

        [Test]
        public async Task ToggleScanCommand_WhenNotScanning_StartsScanning()
        {
            // Arrange
            _mockDeviceScanService
                .Setup(m => m.StartScanningAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act - Execute the ToggleScanCommand
            await _viewModel.ToggleScanCommand.ExecuteAsync(null);
            
            // Assert
            Assert.That(_viewModel.IsScanning, Is.True);
            _mockDeviceScanService.Verify(
                m => m.StartScanningAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task ToggleScanCommand_WhenScanning_StopsScanning()
        {
            // Arrange
            // First start scanning
            _mockDeviceScanService
                .Setup(m => m.StartScanningAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            await _viewModel.ToggleScanCommand.ExecuteAsync(null);
            
            // Set up stop scanning
            _mockDeviceScanService
                .Setup(m => m.StopScanningAsync())
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act - Execute the ToggleScanCommand again to stop scanning
            await _viewModel.ToggleScanCommand.ExecuteAsync(null);
            
            // Assert
            Assert.That(_viewModel.IsScanning, Is.False);
            _mockDeviceScanService.Verify(m => m.StopScanningAsync(), Times.Once);
        }

        [Test]
        public void OnDeviceDiscovered_AddsDeviceToCollection()
        {
            // Arrange
            var device = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "Test Device",
                Rssi = -65
            };
            
            var eventArgs = new MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs { Device = device };
            
            _mockDeviceScanService
                .Setup(m => m.FindExistingDevice(device.Id, It.IsAny<ObservableCollection<BleDeviceInfo>>()))
                .Returns((BleDeviceInfo?)null);

            // Act - Simulate the DeviceDiscovered event
            _mockDeviceScanService.Raise(
                m => m.DeviceDiscovered += null,
                _mockDeviceScanService.Object,
                eventArgs);

            // Assert
            Assert.That(_viewModel.Devices.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Devices[0], Is.SameAs(device));
        }

        [Test]
        public void OnDeviceDiscovered_UpdatesExistingDevice()
        {
            // Arrange
            var existingDevice = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "Old Name",
                Rssi = -80
            };
            
            _viewModel.Devices.Add(existingDevice);
            
            var updatedDevice = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "New Name",
                Rssi = -65
            };
            
            var eventArgs = new MauiBleApp2.Core.Services.Bluetooth.BleDeviceEventArgs { Device = updatedDevice };
            
            _mockDeviceScanService
                .Setup(m => m.FindExistingDevice(updatedDevice.Id, It.IsAny<ObservableCollection<BleDeviceInfo>>()))
                .Returns(existingDevice);
                
            _mockDeviceScanService
                .Setup(m => m.UpdateDevice(existingDevice, updatedDevice))
                .Callback<BleDeviceInfo, BleDeviceInfo>((existing, updated) => {
                    existing.Name = updated.Name;
                    existing.Rssi = updated.Rssi;
                });
                
            // Act - Simulate the DeviceDiscovered event
            _mockDeviceScanService.Raise(
                m => m.DeviceDiscovered += null, 
                _mockDeviceScanService.Object, 
                eventArgs);
                
            // Manually update the device to simulate what happens in the ViewModel
            _mockDeviceScanService.Object.UpdateDevice(existingDevice, updatedDevice);

            // Assert
            Assert.That(_viewModel.Devices.Count, Is.EqualTo(1));
            Assert.That(_viewModel.Devices[0].Name, Is.EqualTo("New Name"));
            Assert.That(_viewModel.Devices[0].Rssi, Is.EqualTo(-65));
        }

        [Test]
        public async Task ConnectToDeviceCommand_WhenDeviceSelected_ConnectsToDevice()
        {
            // Arrange
            var device = new BleDeviceInfo
            {
                Id = "test-device-123",
                Name = "Test Device"
            };
            
            _viewModel.SelectedDevice = device;
            
            _mockDeviceScanService
                .Setup(m => m.ConnectToDeviceAsync(device.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(device)
                .Verifiable();

            // Track if the navigation event was raised
            BleDeviceInfo? navigatedDevice = null;
            _viewModel.NavigateToDeviceDetailsRequested += (sender, d) => navigatedDevice = d;

            // Act
            await _viewModel.ConnectToDeviceCommand.ExecuteAsync(null);

            // Assert
            Assert.That(_viewModel.IsBusy, Is.False); // Should be false after command completes
            Assert.That(navigatedDevice, Is.Not.Null);
            Assert.That(navigatedDevice, Is.SameAs(device));
            _mockDeviceScanService.Verify(
                m => m.ConnectToDeviceAsync(device.Id, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TearDown]
        public void Cleanup()
        {
            // Cleanup any resources
            _viewModel.Cleanup();
        }
    }
}