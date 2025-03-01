using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using NintendAUX.Filetypes.Compression;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using NintendAUX.ViewModels;

namespace NintendAUX.Services.Audio;

public static class FileLoadingService
{
    private static async Task<byte[]> LoadFileInternal(IStorageFile fileData)
    {
        ViewModelLocator.Model.InputFileName = fileData.Name;

        var fileBytes = await File.ReadAllBytesAsync(fileData.Path.LocalPath);

        if (!MiscUtilities.CheckMagic(ref fileBytes, ViewModelLocator.Model.InputType))
            fileBytes = fileBytes.DecompressZSTDBytes(ViewModelLocator.Model.ZsdicLoaded);

        return fileBytes;
    }

    public static async Task LoadBwavFile(IStorageFile bwavFile)
    {
        ViewModelLocator.Model.InputType = InputFileType.Bwav;
        var bwavData = await LoadFileInternal(bwavFile);
        ViewModelLocator.Model.InputFile = new BwavFile(bwavData);
        NodeService.UpdateNodeArray();
    }

    public static async Task LoadBarsFile(IStorageFile barsFile)
    {
        Console.WriteLine("Load the file?");
        ViewModelLocator.Model.InputType = InputFileType.Bars;
        var barsData = await LoadFileInternal(barsFile);
        ViewModelLocator.Model.InputFile = new BarsFile(barsData);
        NodeService.UpdateNodeArray();
    }

    public static async Task LoadFile(IStorageFile fileData, InputFileType fileType)
    {
        // Why the fuck does this work - 2/24/25
        await (fileType switch
        {
            InputFileType.Bars => LoadBarsFile(fileData),
            InputFileType.Bwav => LoadBwavFile(fileData),
            _ => await new NotImplementedException().CreateExceptionDialog()
        });
    }

    public static async Task LoadTotkDict()
    {
        var zstdPack = await FileDialogService.OpenFile(new FilePickerOpenOptions
        {
            Title = "Open ZSTD Dictionary",
            AllowMultiple = false,
            FileTypeFilter =
                [new FilePickerFileType("ZSTD Dictionaries") { Patterns = ["*.pack.zs", "*.pack", "*.zsdic"] }]
        });

        if (zstdPack is null) return;

        ZSDic.LoadDictionary(await File.ReadAllBytesAsync(zstdPack.Path.LocalPath));
        ViewModelLocator.Model.ZsdicLoaded = true;
    }
}