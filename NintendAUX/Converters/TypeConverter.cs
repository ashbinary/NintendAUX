using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace NintendAUX.Converters
{
    public class TypeConverter : IValueConverter
    {
        public static readonly TypeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            var typeToCheck = parameter as Type;
            return typeToCheck?.IsInstanceOfType(value) ?? false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 