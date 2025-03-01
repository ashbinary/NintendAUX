using System;
using System.Globalization;
using Avalonia.Data.Converters;
using NintendAUX.ViewModels;

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
            new NotImplementedException().CreateExceptionDialog();
            return null;
        }
    }
} 