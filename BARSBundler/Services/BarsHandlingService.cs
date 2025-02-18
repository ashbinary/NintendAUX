using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using BARSBundler.Compression;
using BARSBundler.Core.Filetypes;

namespace BARSBundler.Services;

public class BarsHandlingService
{
    public static async Task SaveBARSFile(BARSFile barsToSave, bool compressFile, string barsPath, bool zsdicLoaded)
    {
        barsToSave.EntryArray = barsToSave.EntryArray.OrderBy(path => CRC32.Compute(path.Bamta.Path)).ToArray();
        
        
        var barsFile = await FileDialogService.SaveFile(new FilePickerSaveOptions()
        {
            Title = "Save BARS File",
            DefaultExtension = compressFile ? "bars.zs" : "bars",
            SuggestedFileName = barsPath
        });
        
        if (barsFile != null)
        {
            using var stream = await barsFile.OpenWriteAsync();
            byte[] savedBars = BARSFile.SoftSave(barsToSave);
            if (compressFile) 
                savedBars = ZSTDUtils.CompressZSTDBytes(savedBars, zsdicLoaded);
            stream.Write(savedBars);
            stream.Flush();
        }
    }
}