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
        public byte MajorVersion;
        public byte MinorVersion;
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

        InfoSize = amtaReader.Position - BaseAddress;

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
            Console.WriteLine("Found Marker offset");
            MarkerTable = new AMTAMarkerTable(ref amtaReader);
        }

        if (Info.MinfOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.MinfOffset;
            Console.WriteLine("Found MINF offset");
            Minf = new MINFFile(ref amtaReader);
        }

        Path = amtaReader.ReadTerminatedStringAt(BaseAddress + 36 + Info.PathOffset);

    }

    public AMTAFile(byte[] data)
    {
        FileReader barsReader = new(new MemoryStream(data));
        this = new AMTAFile(ref barsReader);
    }
}