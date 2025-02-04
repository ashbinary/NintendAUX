namespace StutteredBars;

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class BWAVFile
{
    public long FileBase = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct ResOpusHeader
    {
        public ushort Reserve0;
        public ushort Reserve1;
        public uint Reserve2;
        public uint Reserve3;
        public uint SampleRate;
        public uint Reserve4;
        public uint Reserve5;
        public uint Reserve6;
        public uint Reserve7;
        public ushort Reserve8;
        public ushort Reserve9;
        public uint SectionSize;

        // Dynamic opus data storage (C# does not support flexible arrays)
        public byte[]? OpusData;
    }

    public enum BwavEncoding : ushort
    {
        Pcm16 = 0,
        Adpcm = 1,
        Opus = 2
    }

    public enum BwavChannelPan : ushort
    {
        Left = 0,
        Right = 1,
        Middle = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AdpcmContext
    {
        public ushort PredictorScale;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public short[] HistoryArray;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ResBwavLoopPoint
    {
        public uint LoopStart;
        public AdpcmContext AdpcmContext;
        public ushort Reserve0;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ResBwavChannelInfo
    {
        public BwavEncoding Encoding;
        public ushort ChannelPan;
        public uint SampleRate;
        public uint NonPrefetchSampleCount;
        public uint SampleCount;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ushort[] DspAdpcmCoefficients;
        
        public uint NonPrefetchSamplesOffset;
        public uint SamplesOffset;
        public ushort LoopPointCount;
        public ushort BaseLoopPoint;
        public uint LoopEnd;

        public List<ResBwavLoopPoint> LoopPointArray;

        public uint AlignedSampleSize => (SampleCount / 14) * 8;
        public uint UnalignedSampleSize => (SampleCount % 14 == 0) ? 0 : ((SampleCount % 14) / 2) + (SampleCount % 2) + 1;

        public byte[]? OSamples;
        public ResOpusHeader? OOpus;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BwavHeader
    {
        public uint Magic;
        public ushort Endianess;
        public ushort Version;
        public uint SamplesCrc32;
        public ushort IsPrefetch;
        public ushort ChannelCount;
    }

    public BwavHeader Header;
    public List<ResBwavChannelInfo> ChannelInfoArray;

    

}