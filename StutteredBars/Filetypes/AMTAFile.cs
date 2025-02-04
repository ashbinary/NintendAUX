using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StutteredBars.Helpers;

namespace StutteredBars.Filetypes;

public struct AMTAFile
{
    [StructLayout(LayoutKind.Sequential, Size = 49)]
    public struct AMTAData
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

    public struct AMTAReserve // Obviously not actually the name but idc
    {
        public uint Reserve0;
        public float Reserve1;
        public float Reserve2;
        public float Reserve3;
        public float Reserve4;
        public ushort PointCount;
        public ushort Reserve6;
    }

    private long BaseAddress;

    public AMTAData Data;
    public AmtaSourceInfo[] SourceInfo;
    public AMTAReserve Reserve;
    public string Path;

    public AMTAFile(ref FileReader amtaReader)
    {
        BaseAddress = amtaReader.Position;
        
        Data = MemoryMarshal.AsRef<AMTAData>(
            amtaReader.ReadBytes(Unsafe.SizeOf<AMTAData>())
        );

        SourceInfo = new AmtaSourceInfo[Data.SourceCount];
        
        for (int i = 0; i < Data.SourceCount; i++)
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

        Reserve = MemoryMarshal.AsRef<AMTAReserve>(
            amtaReader.ReadBytes(Unsafe.SizeOf<AMTAReserve>())
        );

        Path = amtaReader.ReadTerminatedStringAt(BaseAddress + 36 + Data.PathOffset);

    }
}