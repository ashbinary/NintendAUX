using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NintendAUX.ViewModels;

namespace NintendAUX.Filetypes.Audio;

public struct BwavFile
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
        Center = 2
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
        public BwavChannelPan ChannelPan;
        public uint SampleRate;
        public int NonPrefetchSampleCount;
        public int SampleCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public short[] DspAdpcmCoefficients;

        public uint NonPrefetchSamplesOffset;
        public uint SamplesOffset;
        public ushort LoopPointCount;
        public ushort BaseLoopPoint;
        public uint LoopEnd;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public ResBwavLoopPoint[] LoopPointArray;

        public readonly int AlignedSampleSize => SampleCount / 14 * 8;

        // TODO: actually figure out the math for unaligned samples
        public readonly int UnalignedSampleSize =>
            SampleCount % 14 == 0 ? 0 : (SampleCount % 14 + 1) / 2 + 2;

        public byte[] OSamples;
        public ResOpusHeader OOpus;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BwavHeader
    {
        public uint Magic;
        public ushort Endianess;
        public ushort Version;
        public int SamplesCrc32;
        public ushort IsPrefetch;
        public ushort ChannelCount;
    }

    public BwavHeader Header;
    public ResBwavChannelInfo[] ChannelInfoArray;

    private readonly long FileBase;

    public BwavFile(ref FileReader bwavReader)
    {
        Header = MemoryMarshal.AsRef<BwavHeader>(
            bwavReader.ReadBytes(Unsafe.SizeOf<BwavHeader>())
        );

        FileBase = bwavReader.Position;

        ChannelInfoArray = new ResBwavChannelInfo[Header.ChannelCount];

        for (var i = 0; i < Header.ChannelCount; i++)
        {
            ChannelInfoArray[i] = new ResBwavChannelInfo
            {
                Encoding = (BwavEncoding)bwavReader.ReadUInt16(),
                ChannelPan = (BwavChannelPan)bwavReader.ReadUInt16(),
                SampleRate = bwavReader.ReadUInt32(),
                NonPrefetchSampleCount = bwavReader.ReadInt32(),
                SampleCount = bwavReader.ReadInt32()
            };

            if (ChannelInfoArray[i].Encoding == BwavEncoding.Opus)
                new NotImplementedException("OPUS encoding is not supported!").CreateExceptionDialog();

            ChannelInfoArray[i].DspAdpcmCoefficients = new short[16];

            for (var j = 0; j < 16; j++)
                ChannelInfoArray[i].DspAdpcmCoefficients[j] = bwavReader.ReadInt16();

            ChannelInfoArray[i].NonPrefetchSamplesOffset = bwavReader.ReadUInt32();
            ChannelInfoArray[i].SamplesOffset = bwavReader.ReadUInt32();
            ChannelInfoArray[i].LoopPointCount = bwavReader.ReadUInt16();
            ChannelInfoArray[i].BaseLoopPoint = bwavReader.ReadUInt16();
            ChannelInfoArray[i].LoopEnd = bwavReader.ReadUInt32();

            ChannelInfoArray[i].LoopPointArray = new ResBwavLoopPoint[ChannelInfoArray[i].LoopPointCount];
            for (var j = 0; j < ChannelInfoArray[i].LoopPointCount; j++)
                ChannelInfoArray[i].LoopPointArray[j] = new ResBwavLoopPoint
                {
                    LoopStart = bwavReader.ReadUInt32(),
                    AdpcmPredictorScale = bwavReader.ReadUInt16(),
                    AdpcmHistoryArray = [bwavReader.ReadInt16(), bwavReader.ReadInt16()],
                    Reserve0 = bwavReader.ReadUInt16()
                };


            ChannelInfoArray[i].OSamples =
                new byte[ChannelInfoArray[i].AlignedSampleSize + ChannelInfoArray[i].UnalignedSampleSize];
            bwavReader.Position = FileBase + ChannelInfoArray[i].SamplesOffset - Marshal.SizeOf<BwavHeader>();
            ChannelInfoArray[i].OSamples = bwavReader.ReadBytes(ChannelInfoArray[i].OSamples.Length);

            bwavReader.Position = FileBase + (i + 1) * 76;
        }
    }

    public BwavFile(byte[] data)
    {
        FileReader fileReader = new(new MemoryStream(data));
        this = new BwavFile(ref fileReader);
    }

    public BwavFile(Stream data)
    {
        FileReader fileReader = new(data);
        this = new BwavFile(ref fileReader);
    }

    public static byte[] Save(BwavFile bwavData)
    {
        using MemoryStream saveStream = new();
        var bwavWriter = new FileWriter(saveStream);

        bwavWriter.Write(MemoryMarshal.AsBytes(new Span<BwavHeader>(ref bwavData.Header)));

        //long bwavPosition = bwavWriter.Position;
        List<long> sampleOffsets = [];

        foreach (var channelInfo in bwavData.ChannelInfoArray)
        {
            bwavWriter.Write((ushort)channelInfo.Encoding);
            bwavWriter.Write((ushort)channelInfo.ChannelPan);
            bwavWriter.Write(channelInfo.SampleRate);
            bwavWriter.Write(channelInfo.NonPrefetchSampleCount);
            bwavWriter.Write(channelInfo.SampleCount);
            for (var i = 0; i < channelInfo.DspAdpcmCoefficients.Length; ++i)
                bwavWriter.Write(channelInfo.DspAdpcmCoefficients[i]);
            bwavWriter.Write(channelInfo.NonPrefetchSamplesOffset);

            sampleOffsets.Add(bwavWriter.Position);
            bwavWriter.Write(channelInfo.SamplesOffset);

            bwavWriter.Write(channelInfo.LoopPointCount);
            bwavWriter.Write(channelInfo.BaseLoopPoint);
            bwavWriter.Write(channelInfo.LoopEnd);

            foreach (var loopPoint in channelInfo.LoopPointArray)
            {
                bwavWriter.Write(loopPoint.LoopStart);
                bwavWriter.Write(loopPoint.AdpcmPredictorScale);
                foreach (var historyArray in loopPoint.AdpcmHistoryArray)
                    bwavWriter.Write(historyArray);
                bwavWriter.Write(loopPoint.Reserve0);
            }

            // bwavPosition = bwavWriter.Position;
            //
            if (channelInfo.Encoding == BwavEncoding.Opus)
                throw new NotImplementedException("OPUS encoding is not supported!");
            //
            // // To-do: Support OPUS
            // bwavWriter.Position = channelInfo.SamplesOffset;
            // bwavWriter.Write(channelInfo.OSamples);
            //
            // bwavWriter.Position = bwavPosition;
        }

        var offsetIndex = 0;
        long basePosition = bwavWriter.Position;

        bwavWriter.Align(0x40);

        for (int i = 0; i < bwavData.ChannelInfoArray.Length; i++)
        {
            var curPosition = bwavWriter.Position;
            bwavWriter.WriteAt(sampleOffsets[offsetIndex], Convert.ToInt32(curPosition));
            bwavWriter.Position = curPosition;

            bwavWriter.Write(bwavData.ChannelInfoArray[i].OSamples);

            bwavWriter.Align(i != bwavData.Header.ChannelCount - 1 ? 0x40 : 0x4); // don't align last one

            offsetIndex++;
        }

        //byte[] hashedData = saveStream.ToArray().Skip(Convert.ToInt32(basePosition)).ToArray();
        
        //bwavWriter.WriteAt(Marshal.OffsetOf<BwavHeader>("SamplesCrc32"), CRC32.Compute(hashedData));


        return saveStream.ToArray();
    }
}