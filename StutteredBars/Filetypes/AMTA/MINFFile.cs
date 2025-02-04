using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StutteredBars.Helpers;

namespace StutteredBars.Filetypes.AMTA;

public struct MINFFile
{
    [StructLayout(LayoutKind.Sequential)]
    public struct MINFInfo
    {
        public uint Magic;
        public ushort Endianness;
        public byte MajorVersion;
        public byte MinorVersion;
        public uint FileSize;
        public uint NameOffset;
        public uint Reserve0;
        public uint SampleRate;
        public uint Reserve1;
        public uint Reserve2;
        public uint Reserve3;
        public ushort Reserve4;
        public byte Reserve5;
        public byte Reserve6;
        public ushort Reserve7;
        public ushort Reserve8;
        public uint Table0Offset;
        public uint Table1Offset;
        public uint Table2Offset;
        public uint PairTableOffset;
        public uint OffsetTableOffset;
        public uint InstrumentInfoTableOffset;
        public uint Reserve15;
        public uint Reserve16;
    }   

    [StructLayout(LayoutKind.Sequential)]
    public struct MINFTable0Entry
    {
        public uint SamplePos;
        public ushort Reserve1;
        public byte Reserve2;
        public byte Reserve3;
        public ushort Reserve4;
        public ushort Reserve5;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINFTable1Entry
    {
        public uint Reserve0;
        public uint Reserve1;
        public uint Reserve2;
        public uint Reserve3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINFTable2Entry
    {
        public uint Reserve0;
        public uint Reserve1;
        public uint Reserve2;
        public float Reserve3;
        public uint Reserve4;
    }

    public struct MINFTable0
    {
        public ushort EntryCount;
        public ushort Reserve0;
        public MINFTable0Entry[] Entries; 
    }

    public struct MINFTable1
    {
        public ushort EntryCount;
        public ushort Reserve0;
        public MINFTable1Entry[] Entries; 
    }

    public struct MINFTable2
    {
        public ushort EntryCount;
        public ushort Reserve0;
        public MINFTable2Entry[] Entries; 
    }

    private long BaseAddress;

    public MINFInfo Info;
    public MINFTable0 Table0;
    public MINFTable1 Table1;
    public MINFTable2 Table2;

    public MINFFile(ref FileReader minfReader)
    {
        BaseAddress = minfReader.Position;

        Info = MemoryMarshal.AsRef<MINFInfo>(
            minfReader.ReadBytes(Unsafe.SizeOf<MINFInfo>())
        );

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("Table0Offset") + Info.Table0Offset;

        Table0.EntryCount = minfReader.ReadUInt16();
        Table0.Reserve0 = minfReader.ReadUInt16();
        Table0.Entries = new MINFTable0Entry[Table0.EntryCount];

        for (int i = 0; i < Table0.EntryCount; i++)
            Table0.Entries[i] = MemoryMarshal.AsRef<MINFTable0Entry>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFTable0Entry>())
            );

        Table1.EntryCount = minfReader.ReadUInt16();
        Table1.Reserve0 = minfReader.ReadUInt16();
        Table1.Entries = new MINFTable1Entry[Table1.EntryCount];

        for (int i = 0; i < Table1.EntryCount; i++)
            Table1.Entries[i] = MemoryMarshal.AsRef<MINFTable1Entry>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFTable1Entry>())
            );

        Table2.EntryCount = minfReader.ReadUInt16();
        Table2.Reserve0 = minfReader.ReadUInt16();
        Table2.Entries = new MINFTable2Entry[Table2.EntryCount];

        for (int i = 0; i < Table2.EntryCount; i++)
            Table2.Entries[i] = MemoryMarshal.AsRef<MINFTable2Entry>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFTable2Entry>())
            );
    }
}