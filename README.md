# .NET MAUI BLE Sample App

A cross-platform Bluetooth Low Energy (BLE) application built with .NET MAUI. This app demonstrates how to scan for, connect to, and interact with BLE devices across iOS and Android platforms.

## Features

- BLE device scanning
- Connect to BLE devices
- Discover GATT services and characteristics
- Read/write data to BLE characteristics
- Subscribe to notifications/indications
- Cross-platform implementation (Android/iOS)
- BLE simulation for testing without real hardware

## Architecture

This application uses:

- **MVVM pattern** with CommunityToolkit.MVVM
- **Dependency Injection** for services
- **Platform abstraction** for BLE functionality
- **Unit tests** for BLE parsing logic

## Project Structure

```
MauiBleApp2/
??? Models/                   # Data models
??? Services/                 # Service interfaces and implementations
?   ??? Bluetooth/           # BLE service implementation
?       ??? Simulation/      # BLE simulation components
??? ViewModels/              # MVVM view models
??? Views/                   # XAML pages
??? App.xaml                 # Application entry point
??? MauiProgram.cs          # Service registration
```

## Setup Instructions

### Prerequisites

- .NET 8 SDK with MAUI workload installed
- For Android: Android SDK with API 31+
- For iOS: macOS with Xcode 14+

### Installation

1. Clone the repository:
   ```
   git clone https://github.com/yourusername/MauiBleApp2.git
   cd MauiBleApp2
   ```

2. Restore NuGet packages:
   ```
   dotnet restore
   ```

3. Build the application:
   ```
   dotnet build
   ```

### Running the App

#### Android

```
dotnet build -t:Run -f net8.0-android
```

#### iOS (requires macOS)

```
dotnet build -t:Run -f net8.0-ios
```

## Testing with BLE Simulator

The app includes a BLE simulator for testing without physical devices:

1. Change `MauiProgram.cs` to register the simulated service:

```csharp
// Replace this line:
builder.Services.AddSingleton<IBluetoothService, BluetoothService>();

// With:
builder.Services.AddSingleton<IBluetoothService, SimulatedBluetoothService>();
```

2. Run the application and use the simulated devices that appear in the scan.

## Testing with Real Hardware

To test with actual BLE devices:

1. Ensure Bluetooth is enabled on your device
2. Start the app and press "Scan" to discover nearby BLE devices
3. Select a device from the list and tap "Connect"
4. Explore services and characteristics of the connected device
5. Read, write, and subscribe to notifications as supported by the device

## Permission Requirements

### Android

The following permissions are required and defined in the `AndroidManifest.xml`:

- `android.permission.BLUETOOTH`
- `android.permission.BLUETOOTH_ADMIN`
- `android.permission.BLUETOOTH_SCAN`
- `android.permission.BLUETOOTH_ADVERTISE`
- `android.permission.BLUETOOTH_CONNECT`
- `android.permission.ACCESS_FINE_LOCATION`

### iOS

The following permissions are required and defined in `Info.plist`:

- `NSBluetoothAlwaysUsageDescription`
- `NSBluetoothPeripheralUsageDescription`

## CI/CD

This project includes GitHub Actions workflows for:

- Building and testing the application
- Building Android and iOS packages
- Deploying to TestFlight and Google Play internal testing

### Adding Test Users

#### TestFlight
1. Log in to App Store Connect
2. Navigate to Users and Access > TestFlight Internal Testing
3. Add email addresses for internal testers

#### Google Play Internal Testing
1. Log in to Google Play Console
2. Navigate to Testing > Internal testing
3. Add tester email addresses to the list

## Development

### Adding a New BLE Service

To add support for a specific BLE service:

1. Define the service UUID and characteristic UUIDs in a constants file
2. Extend the BLE message parser if needed for specific data formats
3. Create a dedicated service class that uses `IBluetoothService` for communication

## License

[MIT License](LICENSE)

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.