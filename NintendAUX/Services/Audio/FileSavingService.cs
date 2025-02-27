using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using NintendAUX.Filetypes.Compression;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using NintendAUX.ViewModels;

namespace NintendAUX.Services.Audio;

public class FileSavingService
{
    // public static async Task SaveBARSFile(BARSFile barsToSave, bool compressFile)
    // {
    //     barsToSave.EntryArray = barsToSave.EntryArray.OrderBy(path => CRC32.Compute(path.Bamta.Path)).ToArray();
    //     
    //     var barsFile = await FileDialogService.SaveFile(new FilePickerSaveOptions()
    //     {
    //         Title = "Save BARS File",
    //         DefaultExtension = compressFile ? "bars.zs" : "bars",
    //         SuggestedFileName = ViewModelLocator.Model.BarsFilePath
    //     });
    //     
    //     if (barsFile != null)
    //     {
    //         using var stream = await barsFile.OpenWriteAsync();
    //         byte[] savedBars = BARSFile.SoftSave(barsToSave);
    //         if (compressFile) 
    //             savedBars = ZSTDUtils.CompressZSTDBytes(savedBars, ViewModelLocator.Model.ZsdicLoaded);
    //         stream.Write(savedBars);
    //         stream.Flush();
    //     }
    // }
    public static string GetExtensionType(bool isCompressed)
    {
        return ViewModelLocator.Model.InputType switch
        {
            InputFileType.Bars => isCompressed ? "bars.zs" : "bars",
            InputFileType.Bwav => "bwav",
            _ => throw new NotImplementedException()
        };
    }

    private static Func<AudioFile, byte[]> GetSaveFunction()
    {
        return ViewModelLocator.Model.InputType switch
        {
            InputFileType.Bars => file => BarsFile.SoftSave(file.AsBarsFile()),
            InputFileType.Bwav => file => BwavFile.Save(file.AsBwavFile()),
            _ => throw new ArgumentException($"Unsupported file type: {ViewModelLocator.Model.InputType}")
        };
    }

    public static async Task SaveFile(IStorageFile file, bool isCompressed)
    {
        var saveFunction = GetSaveFunction();
        var fileBytes = saveFunction(ViewModelLocator.Model.InputFile);

        if (isCompressed)
            fileBytes = fileBytes.CompressZSTDBytes(ViewModelLocator.Model.ZsdicLoaded);

        using var stream = await file.OpenWriteAsync();
        stream.Write(fileBytes);
        stream.Flush();
    }
}