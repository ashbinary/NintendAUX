using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using BARSBundler.Core.Filetypes;

namespace BARSBundler.Services;

public class BarsEntryService
{
    public static async Task<AMTAFile> CreateBameta()
    {
        IStorageFile bametaFile = await FileDialogService.OpenFile(
            new FilePickerOpenOptions()
            {
                Title = "Open .bameta File",
                FileTypeFilter = [new FilePickerFileType(".bameta files") { Patterns = ["*.bameta"] }],
                AllowMultiple = false
            }
        );
        
        if (bametaFile != null)
            return new AMTAFile(await bametaFile.OpenReadAsync()); 
        
        return new AMTAFile();
    }
    
    public static async Task<BWAVFile> CreateBwav()
    {
        IStorageFile bwavFile = await FileDialogService.OpenFile(
            new FilePickerOpenOptions()
            {
                Title = "Open .bwav File",
                FileTypeFilter = [new FilePickerFileType(".bwav files") { Patterns = ["*.bwav"] }],
                AllowMultiple = false
            }
        );
        
        if (bwavFile != null)
            return new BWAVFile(await bwavFile.OpenReadAsync());
        
        return new BWAVFile();
    }
}