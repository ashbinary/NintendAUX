using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using NintendAUX.Views;

namespace NintendAUX.ViewModels;

public static class ViewModelLocator
{
    public static MainWindowViewModel Model { get; set; } = new();
}