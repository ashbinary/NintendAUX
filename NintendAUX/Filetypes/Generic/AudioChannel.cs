namespace NintendAUX.Filetypes.Generic;

public struct AudioChannel(ChannelPan channelPan, short[] data)
{
    public ChannelPan ChannelPan = channelPan;
    public short[] Data = data;
}

public enum ChannelPan
{
    Left,
    Right,
    Center
}