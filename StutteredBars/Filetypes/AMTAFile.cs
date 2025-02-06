using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;
using StutteredBars.Filetypes.AMTA;
using StutteredBars.Helpers;

namespace StutteredBars.Filetypes;

public struct AMTAFile
{
    [StructLayout(LayoutKind.Sequential, Size = 49)]
    public struct AMTAInfo
    {
        public uint Magic;
        public ushort Endianness;
        public byte MinorVersion;
        public byte MajorVersion;
        public uint Size;
        public uint Reserve0;
        public uint DataOffset;
        public uint MarkerOffset;
        public uint MinfOffset;
        public uint TagOffset;
        public uint Reserve4;
        public uint PathOffset;
        public uint PathHash;
        public uint Flags; // Technically a bitfield, but...
        public byte SourceCount;
    }

    public struct AmtaSourceInfo
    {
        public byte ChannelCount;
        public AmtaChannelInfo[] ChannelInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AmtaChannelInfo
    {
        public byte ChannelIndex;
        public byte Flags;
    }

    private long BaseAddress;
    private long InfoSize;

    public AMTAInfo Info;
    public AmtaSourceInfo[] SourceInfo;

    public AMTAData Data;
    public AMTAMarkerTable MarkerTable;
    public MINFFile Minf;
    public AMTATagTable TagTable;

    public string Path;

    public AMTAFile(ref FileReader amtaReader)
    {
        BaseAddress = amtaReader.Position;
        
        Info = MemoryMarshal.AsRef<AMTAInfo>(
            amtaReader.ReadBytes(Unsafe.SizeOf<AMTAInfo>())
        );

        SourceInfo = new AmtaSourceInfo[Info.SourceCount];
        
        for (int i = 0; i < Info.SourceCount; i++)
        {
            SourceInfo[i].ChannelCount = amtaReader.ReadByte();
            SourceInfo[i].ChannelInfo = new AmtaChannelInfo[SourceInfo[i].ChannelCount];

            for (int j = 0; j < SourceInfo[i].ChannelCount; j++)
            {
                SourceInfo[i].ChannelInfo[j] = MemoryMarshal.AsRef<AmtaChannelInfo>(
                    amtaReader.ReadBytes(Unsafe.SizeOf<AmtaChannelInfo>())
                );
            }
        }

        if (Info.DataOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.DataOffset;
            Data = MemoryMarshal.AsRef<AMTAData>(
                amtaReader.ReadBytes(Unsafe.SizeOf<AMTAData>())
            );
        }

        if (Info.MarkerOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.MarkerOffset;
            MarkerTable = new AMTAMarkerTable(ref amtaReader);
        }

        if (Info.MinfOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.MinfOffset;
            Minf = new MINFFile(ref amtaReader);
        }

        if (Info.TagOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.TagOffset;
            TagTable = new AMTATagTable(ref amtaReader);
        }

        Path = amtaReader.ReadTerminatedStringAt(BaseAddress + 36 + Info.PathOffset);

    }

    public AMTAFile(byte[] data)
    {
        FileReader barsReader = new(new MemoryStream(data));
        this = new AMTAFile(ref barsReader);
    }

    public static byte[] Save(AMTAFile amtaData)
    {
        using MemoryStream saveStream = new();
        FileWriter amtaWriter = new FileWriter(saveStream);

        amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AMTAInfo>(ref amtaData.Info)));
        
        foreach (AmtaSourceInfo sourceData in amtaData.SourceInfo)
        {
            amtaWriter.Write(sourceData.ChannelCount);
            for (int j = 0; j < sourceData.ChannelInfo.Length; j++)
                amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AmtaChannelInfo>(ref sourceData.ChannelInfo[j])));
        }

        amtaWriter.Pad(2);

        if (amtaData.Info.DataOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.DataOffset;
            amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AMTAData>(ref amtaData.Data)));
        }

        if (amtaData.Info.MarkerOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.MarkerOffset;
            amtaWriter.Write(AMTAMarkerTable.Save(amtaData.MarkerTable));
        }

        if (amtaData.Info.MinfOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.MinfOffset;
            amtaWriter.Write(MINFFile.Save(amtaData.Minf));
        }

        if (amtaData.Info.TagOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.TagOffset;
            amtaWriter.Write(AMTATagTable.Save(amtaData.TagTable));
        }

        amtaWriter.WriteTerminatedAt(Marshal.OffsetOf<AMTAInfo>("PathOffset") + amtaData.Info.PathOffset, amtaData.Path);

        return saveStream.ToArray();
    }
}

public struct SimpleAMTA
{
    public byte[] Data;
    
    public SimpleAMTA(byte[] data)
    {
        Data = data;
    }
}