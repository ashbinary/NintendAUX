using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using NintendAUX.Services;
using NintendAUX.ViewModels;

namespace NintendAUX.Views;

public partial class ErrorHandler : Window
{
    public ErrorHandler()
    {
        InitializeComponent();
        DataContext = ViewModelLocator.ExceptionModel;
        this.Closing += OnExit;
    }
    
    private static void OnExit(object? sender, WindowClosingEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
        {
            Environment.Exit(0);
            desktopApp.MainWindow.Close();
        }
    }

    public async void ExportStackTrace(object? sender, RoutedEventArgs e)
    {
        var stackTraceFile = await FileDialogService.SaveFile(
            new FilePickerSaveOptions()
            {
                Title = "Save Stack Trace",
                DefaultExtension = "log",
                SuggestedFileName = "NintendAUX_StackTrace"
            }
        );

        if (stackTraceFile == null) return;
        
        await File.WriteAllTextAsync(stackTraceFile.Path.LocalPath, ViewModelLocator.ExceptionModel.ErrorDetail);
    }
}