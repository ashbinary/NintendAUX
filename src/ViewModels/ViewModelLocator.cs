using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using NintendAUX.Views;

namespace NintendAUX.ViewModels;

public static class ViewModelLocator
{
    public static MainWindowViewModel Model { get; set; } = new MainWindowViewModel();
    public static ErrorHandlerViewModel ExceptionModel { get; set; } = new();

    private static ErrorHandler _errorHandler { get; set; } = new();
    
    public static async Task<dynamic> CreateExceptionDialog(this Exception ex)
    {
        await ShowExceptionDialogAsync(ex);
        return null;
    }

    private static async Task ShowExceptionDialogAsync(Exception ex)
    {
        var mainWindow = GetMainWindow();

        // Update the error model
        ExceptionModel.ErrorText = $"{ex.GetType().Name}: {ex.Message}";
        ExceptionModel.ErrorDetail = $"{Environment.StackTrace}";
        
        Console.WriteLine(ExceptionModel.ErrorDetail);

        // Position the error dialog in the center of the main window
        int errorPosX = (int)(mainWindow.Position.X + (mainWindow.Width / 2) - (_errorHandler.Width / 2));
        int errorPosY = (int)(mainWindow.Position.Y + (mainWindow.Height / 2) - (_errorHandler.Height / 2));
        _errorHandler.Position = new PixelPoint(errorPosX, errorPosY);
        
        // Show the dialog
        await _errorHandler.ShowDialog(mainWindow);
    }
    
    private static Window? GetMainWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

}