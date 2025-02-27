using System;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;
using NintendAUX.Models;

namespace NintendAUX.Converters
{
    public class NodePropertyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return string.Empty;

            // Check if we should return hex value
            bool returnInHex = false;
            string propertyPath = parameter.ToString();
            if (propertyPath.EndsWith(".AsHex"))
            {
                returnInHex = true;
                propertyPath = propertyPath.Replace(".AsHex", "");
            }

            if (string.IsNullOrEmpty(propertyPath))
                return string.Empty;

            // Split the property path into segments
            string[] segments = propertyPath.Split('.');
            object currentObject = value;

            // Navigate through the property path
            foreach (string segment in segments)
            {
                if (currentObject == null)
                    return string.Empty;

                // Get the property info for the current segment
                PropertyInfo property = currentObject.GetType().GetProperty(segment);
                if (property != null)
                {
                    currentObject = property.GetValue(currentObject);
                }
                else
                {
                    // Try to get field info if property not found
                    FieldInfo field = currentObject.GetType().GetField(segment);
                    if (field != null)
                    {
                        currentObject = field.GetValue(currentObject);
                    }
                    else
                    {
                        return string.Empty; // Property or field not found
                    }
                }
            }

            if (returnInHex && currentObject != null)
            {
                #pragma warning disable CS8603 // don't care about no bum nulls i checked already
                return currentObject switch // I <3 PATTERN MATCHING
                {
                    uint uintValue => $"{uintValue:X8}",
                    int intValue => $"{intValue:X8}",
                    long longValue => $"{longValue:X16}",
                    _ => currentObject.ToString()
                };
                #pragma warning restore CS8603
            }

            return currentObject?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 