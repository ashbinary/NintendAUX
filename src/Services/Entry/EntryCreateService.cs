using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using NintendAUX.Filetypes.Audio;

namespace NintendAUX.Services.Entry;

public static class EntryCreateService
{
    public static async Task<AmtaFile> CreateBameta()
    {
        var bametaFile = await FileDialogService.OpenFile(
            new FilePickerOpenOptions
            {
                Title = "Open .bameta File",
                FileTypeFilter = [new FilePickerFileType(".bameta files") { Patterns = ["*.bameta"] }],
                AllowMultiple = false
            }
        );

        if (bametaFile != null)
            return new AmtaFile(await bametaFile.OpenReadAsync());

        return new AmtaFile();
    }    

    public static async Task<BwavFile> CreateBwav()
    {
        var bwavFile = await FileDialogService.OpenFile(
            new FilePickerOpenOptions
            {
                Title = "Open .bwav File",
                FileTypeFilter = [new FilePickerFileType(".bwav files") { Patterns = ["*.bwav"] }],
                AllowMultiple = false
            }
        );

        if (bwavFile != null)
            return new BwavFile(await bwavFile.OpenReadAsync());

        return new BwavFile();
    }
}