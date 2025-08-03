using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SeroDesk.Core
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
    
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }
            return true;
        }
    }
    
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SignalToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int signalStrength && parameter is string barIndexStr && int.TryParse(barIndexStr, out int barIndex))
            {
                return signalStrength >= barIndex ? 
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White) :
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BatteryToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int batteryLevel)
            {
                if (batteryLevel <= 20)
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 59, 48)); // Red
                else if (batteryLevel <= 50)
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 149, 0)); // Orange
                else
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 199, 89)); // Green
            }
            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BatteryToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int batteryLevel)
            {
                // Battery width is max 18px (20px - 2px for borders)
                return Math.Max(1, (batteryLevel / 100.0) * 18);
            }
            return 18.0;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class IconToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Drawing.Icon icon)
            {
                try
                {
                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                        icon.Handle,
                        System.Windows.Int32Rect.Empty,
                        System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                }
                catch
                {
                    return System.Windows.DependencyProperty.UnsetValue;
                }
            }
            return System.Windows.DependencyProperty.UnsetValue;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class SliderValueToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double sliderValue)
            {
                // Convert slider value (0-100) to width percentage
                // Assuming parent width is available through parameter or default to reasonable value
                var maxWidth = 300.0; // Default max width for sliders
                if (parameter is double paramWidth)
                {
                    maxWidth = paramWidth;
                }
                
                return (sliderValue / 100.0) * maxWidth;
            }
            return 0.0;
        }
        
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}