using System.Threading.Tasks;
using NintendAUX.Filetypes.Generic;
using NintendAUX.ViewModels;

namespace NintendAUX.Services.Entry;

public class EntryReplaceService
{
    public static async Task ReplaceBwav(int nodeIndex)
    {
        var newBwav = await EntryCreateService.CreateBwav();
        if (MiscUtilities.CheckMagic(newBwav.Header.Magic, InputFileType.Bwav))
            ViewModelLocator.Model.InputFile.UpdateBwavAt(nodeIndex, newBwav);
    }

    public static async Task ReplaceBameta(int nodeIndex)
    {
        var newBameta = await EntryCreateService.CreateBameta();
        if (MiscUtilities.CheckMagic(newBameta.Info.Magic, InputFileType.Amta))
            ViewModelLocator.Model.InputFile.UpdateBametaAt(nodeIndex, newBameta);
    }
}