using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;

namespace NintendAUX.Services;

public class FileExtractService
{
    public static async Task ExtractFile<T>(T data, IStorageFile file, Func<T, byte[]> saveFunc = null)
    {
        using var stream = await file.OpenWriteAsync();
        if (saveFunc != null) await stream.WriteAsync(saveFunc(data));
        else await stream.WriteAsync(data as byte[]);
        await stream.FlushAsync();
    }

    public static async Task ExtractFileWithDialog<T>(T data, Func<T, byte[]> saveFunc, FilePickerSaveOptions save)
    {
        var fileData = await FileDialogService.SaveFile(save);

        if (fileData is null) return; // User canceled

        await ExtractFile(data, fileData, saveFunc);
    }

    public static async Task ExtractBwavWithDialog(BarsFile.BarsEntry entry)
    {
        await ExtractFileWithDialog(entry.Bwav, BwavFile.Save, new FilePickerSaveOptions
        {
            Title = "Save .bwav File",
            DefaultExtension = ".bwav",
            SuggestedFileName = entry.Bamta.Path + ".bwav"
        });
    }


    public static async Task ExtractBametaWithDialog(BarsFile.BarsEntry entry)
    {
        await ExtractFileWithDialog(entry.Bamta, AmtaFile.Save, new FilePickerSaveOptions
        {
            Title = "Save .bameta File",
            DefaultExtension = ".bameta",
            SuggestedFileName = entry.Bamta.Path + ".bameta"
        });
    }

    private static async Task ExtractEntry(BarsFile.BarsEntry entry, IStorageFolder folderData)
    {
        var bametaFile = await folderData.CreateFileAsync(entry.Bamta.Path + ".bameta");
        await ExtractFile(entry.Bamta, bametaFile, AmtaFile.Save);

        var bwavFile = await folderData.CreateFileAsync(entry.Bamta.Path + ".bwav");
        await ExtractFile(entry.Bwav, bwavFile, BwavFile.Save);
    }

    public static async Task ExtractAllEntries(BarsFile barsFile)
    {
        var extractionFolder = await FileDialogService.OpenFolder(
            new FolderPickerOpenOptions
            {
                Title = "Select Folder to Extract Entries To",
                AllowMultiple = false
            });

        if (extractionFolder is null) return;

        foreach (var entry in barsFile.EntryArray) await ExtractEntry(entry, extractionFolder);
    }
}