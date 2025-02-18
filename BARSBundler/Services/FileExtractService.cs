using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using BARSBundler.Core.Filetypes;

namespace BARSBundler.Services;

public class FileExtractService
{
    public static async Task ExtractFile<T>(T data, IStorageFile file, Func<T, byte[]> saveFunc)
    {
        using Stream stream = await file.OpenWriteAsync();
        await stream.WriteAsync(saveFunc(data));
        await stream.FlushAsync();
    }
    
    public static async Task ExtractFileWithDialog<T>(T data, Func<T, byte[]> saveFunc, FilePickerSaveOptions save)
    {
        IStorageFile fileData = await FileDialogService.SaveFile(save);

        if (fileData is null) return; // User canceled

        await ExtractFile(data, fileData, saveFunc);
    }

    public static async Task ExtractBwavWithDialog(BARSFile.BarsEntry entry) =>
        await ExtractFileWithDialog(entry.Bwav, BWAVFile.Save, new FilePickerSaveOptions()
        {
            Title = "Save .bwav File",
            DefaultExtension = ".bwav",
            SuggestedFileName = entry.Bamta.Path + ".bwav"
        });
    
    public static async Task ExtractBametaWithDialog(BARSFile.BarsEntry entry) =>
        await ExtractFileWithDialog(entry.Bamta, AMTAFile.Save, new FilePickerSaveOptions()
        {
            Title = "Save .bameta File",
            DefaultExtension = ".bameta",
            SuggestedFileName = entry.Bamta.Path + ".bameta"
        });
    
    private static async void ExtractEntry(BARSFile.BarsEntry entry, IStorageFolder folderData)
    {
        IStorageFile bametaFile = await folderData.CreateFileAsync(entry.Bamta.Path + ".bameta");
        await ExtractFile(entry.Bamta, bametaFile, AMTAFile.Save);
        
        IStorageFile bwavFile = await folderData.CreateFileAsync(entry.Bamta.Path + ".bwav");
        await ExtractFile(entry.Bwav, bwavFile, BWAVFile.Save);
    }

    public static async Task ExtractAllEntries(BARSFile barsFile)
    {
        IStorageFolder extractionFolder = await FileDialogService.OpenFolder(
            new FolderPickerOpenOptions()
            {
                Title = "Select Folder to Extract Entries To",
                AllowMultiple = false
            });
        
        if (extractionFolder is null) return;

        foreach (BARSFile.BarsEntry entry in barsFile.EntryArray)
        {
            ExtractEntry(entry, extractionFolder);
        }
    }
}