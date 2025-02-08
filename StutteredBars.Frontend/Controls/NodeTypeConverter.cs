using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using StutteredBars.Frontend.Models;

public class NodeTypeConverter : IMultiValueConverter
{
    public object? Convert(IList<object>? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value[0] is Node node && value[1] is TreeView treeView)
        {
            return treeView.Resources[$"{node.Type}ContextMenu"] as ContextMenu;
        }
        return null;
    }

    public object? Convert(object value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}