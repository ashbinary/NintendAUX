using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BARSBundler.Core.Helpers;
using Microsoft.VisualBasic;

namespace BARSBundler.Core.Filetypes.AMTA;

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

    [StructLayout(LayoutKind.Sequential)]
    public struct MINFPair
    {
        public uint SampleOffset;
        public uint Reserve1;
    }

    public struct MINFInstrument
    {
        public uint NameOffset;
        public ushort Reserve0;
        public ushort Reserve1;
        public uint[] Reserve2;
        public string Name;
    }

    public struct MINFInstrumentInfo
    {
        public uint Reserve0;
        public uint InstrumentOffset;
        public MINFInstrument Instrument;
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
    
    public struct MinfOffsetTable
    {
        public ushort OffsetCount;
        public ushort Reserve0;
        public uint[] Offsets;
    }

    public struct MinfPairTable
    {
        public ushort PairCount;
        public ushort Reserve0;
        public MINFPair[] Pairs;
    }

    public struct MinfInstrumentTable
    {
        public ushort InstrumentCount;
        public ushort Reserve0;
        public MINFInstrumentInfo[] InstrumentInfo;
    }

    private long BaseAddress;

    public MINFInfo Info;
    public string Name;

    public MINFTable0 Table0;
    public MINFTable1 Table1;
    public MINFTable2 Table2;
    public MinfOffsetTable OffsetTable;
    public MinfPairTable PairTable;
    public MinfInstrumentTable InstrumentTable;

    public MINFFile(ref FileReader minfReader)
    {
        BaseAddress = minfReader.Position;

        Info = MemoryMarshal.AsRef<MINFInfo>(
            minfReader.ReadBytes(Unsafe.SizeOf<MINFInfo>())
        );

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("NameOffset") + Info.NameOffset;
        Name = minfReader.ReadTerminatedString();

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("Table0Offset") + Info.Table0Offset;

        Table0.EntryCount = minfReader.ReadUInt16();
        Table0.Reserve0 = minfReader.ReadUInt16();
        Table0.Entries = new MINFTable0Entry[Table0.EntryCount];

        for (int i = 0; i < Table0.EntryCount; i++)
            Table0.Entries[i] = MemoryMarshal.AsRef<MINFTable0Entry>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFTable0Entry>())
            );

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("Table1Offset") + Info.Table1Offset;

        Table1.EntryCount = minfReader.ReadUInt16();
        Table1.Reserve0 = minfReader.ReadUInt16();
        Table1.Entries = new MINFTable1Entry[Table1.EntryCount];

        for (int i = 0; i < Table1.EntryCount; i++)
            Table1.Entries[i] = MemoryMarshal.AsRef<MINFTable1Entry>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFTable1Entry>())
            );

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("Table2Offset") + Info.Table2Offset;

        Table2.EntryCount = minfReader.ReadUInt16();
        Table2.Reserve0 = minfReader.ReadUInt16();
        Table2.Entries = new MINFTable2Entry[Table2.EntryCount];

        for (int i = 0; i < Table2.EntryCount; i++)
            Table2.Entries[i] = MemoryMarshal.AsRef<MINFTable2Entry>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFTable2Entry>())
            );

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("PairTableOffset") + Info.PairTableOffset;

        PairTable.PairCount = minfReader.ReadUInt16();
        PairTable.Reserve0 = minfReader.ReadUInt16();
        PairTable.Pairs = new MINFPair[PairTable.PairCount];

        for (int i = 0; i < PairTable.PairCount; i++)
            PairTable.Pairs[i] = MemoryMarshal.AsRef<MINFPair>(
                minfReader.ReadBytes(Unsafe.SizeOf<MINFPair>())
            );

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("OffsetTableOffset") + Info.OffsetTableOffset;

        OffsetTable.OffsetCount = minfReader.ReadUInt16();
        OffsetTable.Reserve0 = minfReader.ReadUInt16();
        OffsetTable.Offsets = new uint[OffsetTable.OffsetCount];

        for (int i = 0; i < OffsetTable.OffsetCount; i++)
            OffsetTable.Offsets[i] = minfReader.ReadUInt32();

        minfReader.Position = BaseAddress + Marshal.OffsetOf<MINFInfo>("InstrumentInfoTableOffset") + Info.InstrumentInfoTableOffset;

        InstrumentTable.InstrumentCount = minfReader.ReadUInt16();
        InstrumentTable.Reserve0 = minfReader.ReadUInt16();

        if (InstrumentTable.InstrumentCount > 0)
        {
            InstrumentTable.InstrumentInfo = new MINFInstrumentInfo[InstrumentTable.InstrumentCount];

            for (int i = 0; i < InstrumentTable.InstrumentCount; i++)
            {
                long InstrumentTableAddress = minfReader.Position;

                InstrumentTable.InstrumentInfo[i] = new MINFInstrumentInfo()
                {
                    Reserve0 = minfReader.ReadUInt32(),
                    InstrumentOffset = minfReader.ReadUInt32()
                };

                long InstrumentAddress = minfReader.Position;
                minfReader.Position = InstrumentTableAddress + 4 + InstrumentTable.InstrumentInfo[i].InstrumentOffset;
                
                InstrumentTable.InstrumentInfo[i].Instrument = new MINFInstrument() 
                {
                    NameOffset = minfReader.ReadUInt32(),
                    Reserve0 = minfReader.ReadUInt16(),
                    Reserve1 = minfReader.ReadUInt16(),
                };

                InstrumentTable.InstrumentInfo[i].Instrument.Reserve2 = new uint[InstrumentTable.InstrumentInfo[i].Instrument.Reserve0];

                for (int j = 0; j < InstrumentTable.InstrumentInfo[i].Instrument.Reserve0; j++)
                    InstrumentTable.InstrumentInfo[i].Instrument.Reserve2[j] = minfReader.ReadUInt32();

                minfReader.Position = InstrumentTableAddress + 4 + InstrumentTable.InstrumentInfo[i].InstrumentOffset + InstrumentTable.InstrumentInfo[i].Instrument.NameOffset;
                InstrumentTable.InstrumentInfo[i].Instrument.Name = minfReader.ReadTerminatedString();

                Console.WriteLine($"Found {InstrumentTable.InstrumentInfo[i].Instrument.Name}");

                minfReader.Position = InstrumentAddress;
                
            }

            // for (int i = 0; i < InstrumentTable.InstrumentCount; i++)
            // {
            //     minfReader.Position = BaseAddress + InstrumentTable.Instruments[i].InstrumentNameOffset;
            //     InstrumentTable.InstrumentInfo[i].InstrumentName = minfReader.ReadTerminatedString();
            // }
        }
    }

    public static byte[] Save(MINFFile minfData)
    {
        // Table0.EntryCount = minfReader.ReadUInt16();
        // Table0.Reserve0 = minfReader.ReadUInt16();
        // Table0.Entries = new MINFTable0Entry[Table0.EntryCount];

        // for (int i = 0; i < Table0.EntryCount; i++)
        //     Table0.Entries[i] = MemoryMarshal.AsRef<MINFTable0Entry>(
        //         minfReader.ReadBytes(Unsafe.SizeOf<MINFTable0Entry>())
        //     );
        using MemoryStream saveStream = new();
        FileWriter minfWriter = new FileWriter(saveStream);

        minfWriter.Write(MemoryMarshal.AsBytes(new Span<MINFInfo>(ref minfData.Info)));

        minfWriter.Position = Marshal.OffsetOf<MINFInfo>("Table0Offset") + minfData.Info.Table0Offset;
        minfWriter.Write(minfData.Table0.EntryCount);
        minfWriter.Write(minfData.Table0.Reserve0);
        for (int i = 0; i < minfData.Table0.EntryCount; i++)
            minfWriter.Write(MemoryMarshal.AsBytes(new Span<MINFTable0Entry>(ref minfData.Table0.Entries[i])));

        minfWriter.Position = Marshal.OffsetOf<MINFInfo>("Table1Offset") + minfData.Info.Table1Offset;
        minfWriter.Write(minfData.Table1.EntryCount);
        minfWriter.Write(minfData.Table1.Reserve0);
        for (int i = 0; i < minfData.Table1.EntryCount; i++)
            minfWriter.Write(MemoryMarshal.AsBytes(new Span<MINFTable1Entry>(ref minfData.Table1.Entries[i])));

        minfWriter.Position = Marshal.OffsetOf<MINFInfo>("Table2Offset") + minfData.Info.Table2Offset;
        minfWriter.Write(minfData.Table2.EntryCount);
        minfWriter.Write(minfData.Table2.Reserve0);
        for (int i = 0; i < minfData.Table2.EntryCount; i++)
            minfWriter.Write(MemoryMarshal.AsBytes(new Span<MINFTable2Entry>(ref minfData.Table2.Entries[i])));

        minfWriter.Position = Marshal.OffsetOf<MINFInfo>("PairTableOffset") + minfData.Info.PairTableOffset;
        minfWriter.Write(minfData.PairTable.PairCount);
        minfWriter.Write(minfData.PairTable.Reserve0);
        for (int i = 0; i < minfData.PairTable.PairCount; i++)
            minfWriter.Write(MemoryMarshal.AsBytes(new Span<MINFPair>(ref minfData.PairTable.Pairs[i])));

        minfWriter.Position = Marshal.OffsetOf<MINFInfo>("OffsetTableOffset") + minfData.Info.OffsetTableOffset;
        minfWriter.Write(minfData.OffsetTable.OffsetCount);
        minfWriter.Write(minfData.OffsetTable.Reserve0);
        for (int i = 0; i < minfData.OffsetTable.OffsetCount; i++)
            minfWriter.Write(minfData.OffsetTable.Offsets[i]);
        
        minfWriter.Position = Marshal.OffsetOf<MINFInfo>("InstrumentInfoTableOffset") + minfData.Info.InstrumentInfoTableOffset;
        minfWriter.Write(minfData.InstrumentTable.InstrumentCount);
        minfWriter.Write(minfData.InstrumentTable.Reserve0);

        for (int i = 0; i < minfData.InstrumentTable.InstrumentCount; i++)
        {
            minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Reserve0);
            minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].InstrumentOffset);

            // long instrumentPosition = minfWriter.Position;
            // minfWriter.Position = Marshal.OffsetOf<MINFInfo>("InstrumentInfoTableOffset") + minfData.Info.InstrumentInfoTableOffset + minfData.InstrumentTable.InstrumentInfo[i].InstrumentOffset;

            // minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.NameOffset);
            // minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve0);
            // minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve1);

            // for (int j = 0; j < minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve0; j++)
            // {
            //     minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve2[j]);
            // }

            // long instrumentTableAddress = minfWriter.Position;
            
            // minfWriter.Position = instrumentPosition + 4 + minfData.InstrumentTable.InstrumentInfo[i].InstrumentOffset + minfData.InstrumentTable.InstrumentInfo[i].Instrument.NameOffset;
            // minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Name);

            // minfWriter.Position = instrumentTableAddress;
        }

        long[] nameOffsetList = new long[minfData.InstrumentTable.InstrumentCount];

        for (int i = 0; i < minfData.InstrumentTable.InstrumentCount; i++)
        {
            nameOffsetList[i] = minfWriter.Position;
            minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.NameOffset);
            minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve0);
            minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve1);
            for (int j = 0; j < minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve0; j++)
            {
                minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Reserve2[j]);
            }
        }

        for (int i = 0; i < minfData.InstrumentTable.InstrumentCount; i++)
        {
            minfWriter.Position = nameOffsetList[i] + minfData.InstrumentTable.InstrumentInfo[i].Instrument.NameOffset;
            minfWriter.Write(minfData.InstrumentTable.InstrumentInfo[i].Instrument.Name);
        }

        minfWriter.WriteAt(Marshal.OffsetOf<MINFInfo>("NameOffset") + minfData.Info.NameOffset, minfData.Name);

        return saveStream.ToArray();
    }

    
}