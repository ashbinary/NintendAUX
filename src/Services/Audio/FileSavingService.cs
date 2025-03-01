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
    public static string GetExtensionType(bool isCompressed)
    {
        return ViewModelLocator.Model.InputType switch
        {
            InputFileType.Bars => isCompressed ? "bars.zs" : "bars",
            InputFileType.Bwav => "bwav",
            _ => new NotImplementedException().CreateExceptionDialog().GetAwaiter().GetResult()
        };
    }

    private static Func<AudioFile, byte[]> GetSaveFunction()
    {
        return ViewModelLocator.Model.InputType switch
        {
            InputFileType.Bars => file => BarsFile.SoftSave(file.AsBarsFile()),
            InputFileType.Bwav => file => BwavFile.Save(file.AsBwavFile()),
            _ => new ArgumentException($"Unsupported file type: {ViewModelLocator.Model.InputType}").CreateExceptionDialog().GetAwaiter().GetResult()
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