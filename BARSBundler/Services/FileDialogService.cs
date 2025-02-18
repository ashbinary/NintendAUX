﻿using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace BARSBundler.Services;

public class FileDialogService
{
    public static async Task<IStorageFile> SaveFile(FilePickerSaveOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.SaveFilePickerAsync(options);
            if (files != null)
                return files;
        }

        return null;
    }
    
    public static async Task<IStorageFolder> OpenFolder(FolderPickerOpenOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.OpenFolderPickerAsync(options);
            if (files.Count > 0)
                return files[0];
        }

        return null;
    }
    
    public static async Task<IStorageFile> OpenFile(FilePickerOpenOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
                return files[0];
        }

        return null;
    }
}