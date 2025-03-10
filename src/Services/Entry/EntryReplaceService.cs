using System;
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
    
    public static async Task ReplacePrefetch(int nodeIndex)
    {
        var newBwav = await EntryCreateService.CreateBwav();

        newBwav.Header.IsPrefetch = 1; // true
        
        // Slice it down to 0.3 seconds for accurate prefetching
        for (int channel = 0; channel < newBwav.ChannelInfoArray.Length; channel++)
        {
            var sampleCount = (int)(Convert.ToInt32(newBwav.ChannelInfoArray[channel].SampleRate) * 0.3);

            newBwav.ChannelInfoArray[channel].SampleCount = sampleCount;    
            newBwav.ChannelInfoArray[channel].NonPrefetchSampleCount = sampleCount;
            
            newBwav.ChannelInfoArray[channel].OSamples = newBwav.ChannelInfoArray[channel].OSamples[..sampleCount];
        }
        
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