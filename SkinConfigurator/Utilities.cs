using SkinConfigurator.ViewModels;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace SkinConfigurator
{
    public sealed class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strVal)
            {
                return string.IsNullOrWhiteSpace(strVal) ? Visibility.Hidden : Visibility.Visible;
            }
            return value == null ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ValueToVisibilityConverter<TValue> : IValueConverter
    {
        public abstract TValue VisibleValue { get; set; }
        public Visibility FalseState { get; set; } = Visibility.Collapsed;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Equals(value, VisibleValue) ? Visibility.Visible : FalseState;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class BoolToVisibilityConverter : ValueToVisibilityConverter<bool>
    {
        public override bool VisibleValue { get; set; }
    }

    public sealed class PackComponentTypeVisibilityConverter : ValueToVisibilityConverter<PackComponentType>
    {
        public override PackComponentType VisibleValue { get; set; }
    }

    public sealed class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            throw new ArgumentException("value is not a color");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class ContrastingTextConverter : IValueConverter
    {
        private const float _whiteBlackTextThreshold = 150;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Color background) throw new ArgumentException("must be applied to color");

            float intensity = (background.R * 0.299f) + (background.G * 0.587f) + (background.B * 0.114f);
            if (intensity > _whiteBlackTextThreshold)
            {
                return new SolidColorBrush(Colors.Black);
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public interface IValidated
    {
        bool IsValid { get; }
    }

    internal static class ColorWrangler
    {
        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color color) =>
            System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);

        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color color) =>
            System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
