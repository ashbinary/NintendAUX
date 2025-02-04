using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using StutteredBars.Filetypes;
using StutteredBars.Helpers;

namespace StutteredBars.Filetypes;

public struct BARSFile
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct BarsHeader
    {
        public uint Magic;
        public uint FileSize;
        public ushort Endianness;
        public byte MinorVersion;
        public byte MajorVersion;
        public uint FileCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BarsEntry
    {
        public uint BamtaOffset;
        public uint BwavOffset;
    }

    public struct BarsReserveData
    {
        public uint FileCount;
        public uint[] FileHashes;
    }

    public BarsHeader Header;
    public uint[] FileHashArray;
    public BarsEntry[] EntryArray;
    public BarsReserveData ReserveData;

    public AMTAFile[] Metadata;
    public BWAVFile[] Tracks;

    public BARSFile(byte[] data)
    {
        FileReader barsReader = new(new MemoryStream(data));

        Header = MemoryMarshal.AsRef<BarsHeader>(barsReader.ReadBytes(Unsafe.SizeOf<BarsHeader>()));
        FileHashArray = new uint[Header.FileCount];

        for (int i = 0; i < Header.FileCount; i++)
            FileHashArray[i] = barsReader.ReadUInt32();

        EntryArray = new BarsEntry[Header.FileCount];

        for (int i = 0; i < Header.FileCount; i++)
            EntryArray[i] = MemoryMarshal.AsRef<BarsEntry>(barsReader.ReadBytes(Unsafe.SizeOf<BarsEntry>()));

        ReserveData.FileCount = barsReader.ReadUInt32();
        ReserveData.FileHashes = new uint[ReserveData.FileCount];

        for (int i = 0; i < ReserveData.FileCount; i++)
            ReserveData.FileHashes[i] = barsReader.ReadUInt32();

        Metadata = new AMTAFile[EntryArray.Length];

        for (int i = 0; i < EntryArray.Length; i++)
        {
            barsReader.Position = EntryArray[i].BamtaOffset;
            Metadata[i] = new AMTAFile(ref barsReader);
        }

        Tracks = new BWAVFile[EntryArray.Length];

        for (int i = 0; i < EntryArray.Length; i++)
        {
            barsReader.Position = EntryArray[i].BwavOffset;
            Tracks[i] = new BWAVFile(ref barsReader);
        }
   }   
}