using System;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;

namespace NintendAUX.Models;

public class BarsEntryNodeData
{
    public uint AmtaOffset;
    public uint BwavOffset;

    public BarsEntryNodeData(BarsFile.BarsEntry entry)
    {
        AmtaOffset = entry.BamtaOffset;
        BwavOffset = entry.BwavOffset;
    }
}

public class BwavNodeData
{
    public ushort ChannelCount;
    public bool IsPrefetch;

    public BwavNodeData(BwavFile.BwavHeader header)
    {
        ChannelCount = header.ChannelCount;
        IsPrefetch = header.IsPrefetch == 0 ? true : false;
    }
}

public class BwavChannelNodeData
{
    public BwavFile.BwavChannelPan ChannelPan;
    public BwavFile.BwavEncoding Encoding;
    public uint LoopEnd;

    public ushort LoopStart;
    public int SampleCount;

    public uint SampleRate;

    public BwavChannelNodeData(BwavFile.ResBwavChannelInfo channelInfo, bool isPrefetch)
    {
        Encoding = channelInfo.Encoding;
        ChannelPan = channelInfo.ChannelPan;

        SampleRate = channelInfo.SampleRate;
        SampleCount = isPrefetch ? channelInfo.SampleCount : Convert.ToInt32(channelInfo.NonPrefetchSampleCount);

        LoopStart = channelInfo.BaseLoopPoint;
        LoopEnd = channelInfo.LoopEnd;
    }
}

public class AmtaNodeData
{
    // Offsets are set as string so N/A can be placed instead of 0
    public string DataOffset;
    public string MarkerOffset;
    public string MinfOffset;

    public int SourceCount;
    public string TagOffset;

    public AmtaNodeData(AmtaFile.AMTAInfo info)
    {
        DataOffset = info.DataOffset == 0 ? "N/A" : info.DataOffset.ToString();
        MarkerOffset = info.MarkerOffset == 0 ? "N/A" : info.MarkerOffset.ToString();
        MinfOffset = info.MinfOffset == 0 ? "N/A" : info.MinfOffset.ToString();
        TagOffset = info.TagOffset == 0 ? "N/A" : info.TagOffset.ToString();
        SourceCount = info.SourceCount;
    }
}