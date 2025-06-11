using System.Globalization;

namespace MauiBleApp2.Converters
{
    public class ScanButtonTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isScanning)
            {
                return isScanning ? "Stop Scan" : "Start Scan";
            }
            return "Scan";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}