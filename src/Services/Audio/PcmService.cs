using System.Threading.Tasks;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using NintendAUX.Utilities;

namespace NintendAUX.Services.Audio;

public class PcmService
{
    private static readonly ChannelPan[] ChannelPanStereo = [ChannelPan.Left, ChannelPan.Right];

    public static async Task<short[]> DecodeChannel(BwavFile.ResBwavChannelInfo channelInfo)
    {
        return ConversionUtilities.DecodeChannel(ref channelInfo);
    }

    public static async Task<AudioChannel[]> DecodeMonoChannel(BwavFile.ResBwavChannelInfo channelInfo)
    {
        return [new AudioChannel(ChannelPan.Center, await DecodeChannel(channelInfo))];
    }

    // TO-DO: Use channel pan properly
    public static async Task<AudioChannel[]> DecodeChannels(BwavFile.ResBwavChannelInfo[] channelInfos)
    {
        var channels = new AudioChannel[channelInfos.Length];
        for (var i = 0; i < channelInfos.Length; i++)
            channels[i] = new AudioChannel(ConvertFromBwav(channelInfos[i].ChannelPan),
                await DecodeChannel(channelInfos[i]));
        return channels;
    }

    public static ChannelPan ConvertFromBwav(BwavFile.BwavChannelPan channelPan)
    {
        return channelPan switch
        {
            BwavFile.BwavChannelPan.Center => ChannelPan.Center,
            BwavFile.BwavChannelPan.Left => ChannelPan.Left,
            BwavFile.BwavChannelPan.Right => ChannelPan.Right
        };
    }
}