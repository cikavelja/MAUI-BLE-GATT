#if ANDROID
using Microsoft.Maui.ApplicationModel;

public class BluetoothScanPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[]
    {
        (Android.Manifest.Permission.BluetoothScan, true)
    };
}

public class BluetoothConnectPermissions : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions => new[]
    {
        (Android.Manifest.Permission.BluetoothConnect, true)
    };
}
#endif
