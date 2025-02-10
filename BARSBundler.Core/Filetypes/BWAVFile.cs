using BARSBundler.Core.Helpers;

namespace BARSBundler.Core.Filetypes;

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Core.Helpers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Core.Filetypes.AMTA;

public struct FullBWAVFile
{

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
    public struct ResBwavLoopPoint
    {
        public uint LoopStart;
        public ushort AdpcmPredictorScale;
        public short[] AdpcmHistoryArray;
        public ushort Reserve0;
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    public struct ResBwavChannelInfo
    {
        public BwavEncoding Encoding;
        public ushort ChannelPan;
        public uint SampleRate;
        public uint NonPrefetchSampleCount;
        public uint SampleCount;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] DspAdpcmCoefficients;
        
        public uint NonPrefetchSamplesOffset;
        public uint SamplesOffset;
        public ushort LoopPointCount;
        public ushort BaseLoopPoint;
        public uint LoopEnd;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public ResBwavLoopPoint[] LoopPointArray;

        public readonly uint AlignedSampleSize => SampleCount / 14 * 8;
        public readonly uint UnalignedSampleSize => (SampleCount % 14 == 0) ? 0 : (SampleCount % 14 / 2) + (SampleCount % 2) + 1;

        public byte[] OSamples;
        public ResOpusHeader OOpus;
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
    public ResBwavChannelInfo[] ChannelInfoArray;

    private long FileBase;

    public FullBWAVFile(ref FileReader bwavReader)
    {
        Header = MemoryMarshal.AsRef<BwavHeader>(
            bwavReader.ReadBytes(Unsafe.SizeOf<BwavHeader>())
        );

        FileBase = bwavReader.Position;

        ChannelInfoArray = new ResBwavChannelInfo[Header.ChannelCount];

        for (int i = 0; i < Header.ChannelCount; i++)
        {
            ChannelInfoArray[i] = new ResBwavChannelInfo
            {
                Encoding = (BwavEncoding)bwavReader.ReadUInt16(),
                ChannelPan = bwavReader.ReadUInt16(),
                SampleRate = bwavReader.ReadUInt32(),
                NonPrefetchSampleCount = bwavReader.ReadUInt32(),
                SampleCount = bwavReader.ReadUInt32(),
                DspAdpcmCoefficients = bwavReader.ReadBytes(32),
                NonPrefetchSamplesOffset = bwavReader.ReadUInt32(),
                SamplesOffset = bwavReader.ReadUInt32(),
                LoopPointCount = bwavReader.ReadUInt16(),
                BaseLoopPoint = bwavReader.ReadUInt16(),
                LoopEnd = bwavReader.ReadUInt32()
            };

            // for (int j = 0; j < 16; j++)
            //     ChannelInfoArray[i].DspAdpcmCoefficients[j] = bwavReader.ReadUInt16();
            
            ChannelInfoArray[i].LoopPointArray = new ResBwavLoopPoint[ChannelInfoArray[i].LoopPointCount];
            for (int j = 0; j < ChannelInfoArray[i].LoopPointCount; j++)
                ChannelInfoArray[i].LoopPointArray[j] = new ResBwavLoopPoint
                {
                    LoopStart = bwavReader.ReadUInt32(),
                    AdpcmPredictorScale = bwavReader.ReadUInt16(),
                    AdpcmHistoryArray = [bwavReader.ReadInt16(), bwavReader.ReadInt16()],
                    Reserve0 = bwavReader.ReadUInt16()
                };

            if (ChannelInfoArray[i].Encoding == BwavEncoding.Adpcm)
            {
                ChannelInfoArray[i].OSamples = new byte[ChannelInfoArray[i].AlignedSampleSize + ChannelInfoArray[i].UnalignedSampleSize];
                bwavReader.Position = ChannelInfoArray[i].SamplesOffset;
                for (int x = 0; x < ChannelInfoArray[i].OSamples.Length; x++)
                    ChannelInfoArray[i].OSamples[x] = bwavReader.ReadByte();
            } 
            else if (ChannelInfoArray[i].Encoding == BwavEncoding.Opus)
            {
                bwavReader.Position = FileBase + ChannelInfoArray[i].SamplesOffset;
                ChannelInfoArray[i].OOpus = new ResOpusHeader
                {
                    Reserve0 = bwavReader.ReadUInt16(),
                    Reserve1 = bwavReader.ReadUInt16(),
                    Reserve2 = bwavReader.ReadUInt32(),
                    Reserve3 = bwavReader.ReadUInt32(),
                    SampleRate = bwavReader.ReadUInt32(),
                    Reserve4 = bwavReader.ReadUInt32(),
                    Reserve5 = bwavReader.ReadUInt32(),
                    Reserve6 = bwavReader.ReadUInt32(),
                    Reserve7 = bwavReader.ReadUInt32(),
                    Reserve8 = bwavReader.ReadUInt16(),
                    Reserve9 = bwavReader.ReadUInt16(),
                    SectionSize = bwavReader.ReadUInt32()
                };

                ChannelInfoArray[i].OOpus.OpusData = new byte[ChannelInfoArray[i].OOpus.SectionSize];

                for (int y = 0; y < ChannelInfoArray[i].OOpus.OpusData.Length; y++)
                    ChannelInfoArray[i].OOpus.OpusData[y] = bwavReader.ReadByte();
        
            }

            bwavReader.Position = FileBase + ((i + 1) * 76);

        }
    }

    public FullBWAVFile(byte[] data)
    {
        FileReader fileReader = new(new MemoryStream(data));
        this = new FullBWAVFile(ref fileReader);
    }

    public static byte[] Save(FullBWAVFile bwavData)
    {
        using MemoryStream saveStream = new();
        FileWriter bwavWriter = new FileWriter(saveStream);

        bwavWriter.Write(MemoryMarshal.AsBytes(new Span<BwavHeader>(ref bwavData.Header)));

        long bwavPosition = bwavWriter.Position;

        foreach (ResBwavChannelInfo channelInfo in bwavData.ChannelInfoArray)
        {
            bwavWriter.Write((ushort)channelInfo.Encoding);
            bwavWriter.Write(channelInfo.ChannelPan);
            bwavWriter.Write(channelInfo.SampleRate);
            bwavWriter.Write(channelInfo.NonPrefetchSampleCount);
            bwavWriter.Write(channelInfo.SampleCount);
            bwavWriter.Write(channelInfo.DspAdpcmCoefficients);
            bwavWriter.Write(channelInfo.NonPrefetchSamplesOffset);
            bwavWriter.Write(channelInfo.SamplesOffset);
            bwavWriter.Write(channelInfo.LoopPointCount);
            bwavWriter.Write(channelInfo.BaseLoopPoint);
            bwavWriter.Write(channelInfo.LoopEnd);

            foreach (ResBwavLoopPoint loopPoint in channelInfo.LoopPointArray)
            {
                bwavWriter.Write(loopPoint.LoopStart);
                bwavWriter.Write(loopPoint.AdpcmPredictorScale);
                foreach (short historyArray in loopPoint.AdpcmHistoryArray)
                    bwavWriter.Write(historyArray);
                bwavWriter.Write(loopPoint.Reserve0);
            }

            bwavPosition = bwavWriter.Position;

            if (channelInfo.Encoding == BwavEncoding.Opus) 
                throw new NotImplementedException();

            // To-do: Support OPUS
            bwavWriter.Position = channelInfo.SamplesOffset;
            bwavWriter.Write(channelInfo.OSamples);

            bwavWriter.Position = bwavPosition;
        }

        return saveStream.ToArray();
    }

}

public struct BWAVFile // whatever LOLLLLL
{
    public struct BwavHeader
    {
        public uint Magic;
        public ushort Endianess;
        public ushort Version;
        public uint SamplesCrc32;
        public ushort IsPrefetch;
        public ushort ChannelCount;
    }

    private long BaseAddress;
    public BwavHeader Header;
    public byte[] Data;

    public BWAVFile(ref FileReader bwavReader)
    {
        BaseAddress = bwavReader.Position;

        Header = MemoryMarshal.AsRef<BwavHeader>(
            bwavReader.ReadBytes(Unsafe.SizeOf<BwavHeader>())
        );

        bool foundBWAV = false;
        while (!foundBWAV)
        {
            if (bwavReader.Position >= bwavReader.BaseStream.Length - 4)
            {
                bwavReader.Position += 4;
                break; // Found end of stream, just go with it
            }
            byte[] bwavData = bwavReader.ReadBytes(4);
            bwavReader.Position -= 3;

            if (bwavData[0] != 0x42) continue; // B
            if (bwavData[1] != 0x57) continue; // W
            if (bwavData[2] != 0x41) continue; // A
            if (bwavData[3] != 0x56) continue; // V

            foundBWAV = true;
            bwavReader.Position -= 1; // To reset the stream if found
        }

        int bwavLength = Convert.ToInt32(bwavReader.Position - BaseAddress);
        bwavReader.Position = BaseAddress;

        Data = bwavReader.ReadBytes(bwavLength);
    }

    public BWAVFile(byte[] data)
    {
        FileReader fileReader = new(new MemoryStream(data));
        this = new BWAVFile(ref fileReader);
    }
    
    public BWAVFile(Stream data)
    {
        FileReader fileReader = new(data);
        this = new BWAVFile(ref fileReader);
    }

    public static byte[] Save(BWAVFile bwavData)
    {
        using MemoryStream saveStream = new(bwavData.Data);
        FileWriter bwavWriter = new FileWriter(saveStream);

        bwavWriter.Write(MemoryMarshal.AsBytes(new Span<BwavHeader>(ref bwavData.Header))); 

        return saveStream.ToArray();
    }
}