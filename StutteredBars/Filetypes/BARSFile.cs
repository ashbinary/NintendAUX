using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
        Tracks = new BWAVFile[EntryArray.Length];

        for (int i = 0; i < EntryArray.Length; i++)
        {
            barsReader.Position = EntryArray[i].BwavOffset;
            Tracks[i] = new BWAVFile(ref barsReader);
        }

        for (int i = 0; i < EntryArray.Length; i++)
        {
            barsReader.Position = EntryArray[i].BamtaOffset;
            Metadata[i] = new AMTAFile(ref barsReader);
        }
    }

    public static byte[] SoftSave(BARSFile barsData)
    {
        using MemoryStream saveStream = new();
        FileWriter barsWriter = new FileWriter(saveStream);

        barsWriter.Write(MemoryMarshal.AsBytes(new Span<BarsHeader>(ref barsData.Header)));

        int newFileCount = barsData.Metadata.Length;
        barsWriter.WriteAt(Marshal.OffsetOf<BarsHeader>("FileCount"), newFileCount); // Use AMTA file amount to calculate data (cannot be dupe)

        SortedDictionary<uint, string> pathList = new();
        foreach (AMTAFile metadata in barsData.Metadata)
            pathList.Add(CRC32.Compute(metadata.Path), metadata.Path);

        foreach (uint fileHash in pathList.Keys)
            barsWriter.Write(fileHash);

        long offsetAddress = barsWriter.Position;
        long[,] offsets = new long[newFileCount, 2];

        foreach (var temp in offsets)
            barsWriter.Write(0xDEADBEEF); // Create a temporary area for offsets which will be filled in later

        barsWriter.Write(barsData.ReserveData.FileCount);
        foreach (uint barsHash in barsData.ReserveData.FileHashes)
            barsWriter.Write(barsHash);

        for (int a = 0; a < barsData.Metadata.Length; a++)
        {
            offsets[a, 0] = barsWriter.Position;
            barsWriter.Write(AMTAFile.Save(barsData.Metadata[a]));
            barsWriter.Align(0x4);
        }

        barsWriter.Align(0x20);

        for (int a = 0; a < barsData.Tracks.Length; a++)
        {
            offsets[a, 1] = barsWriter.Position;
            barsWriter.Write(BWAVFile.Save(barsData.Tracks[a]));
            //barsWriter.Align(0x4);
        }
        
        barsWriter.Position = offsetAddress;
        for (int l = 0; l < offsets.GetLength(0); l++)
        {
            barsWriter.Write((uint)offsets[l, 0]);
            barsWriter.Write((uint)offsets[l, 1]);
        }

        barsWriter.WriteAt(Marshal.OffsetOf<BarsHeader>("FileSize"), (uint)saveStream.Length);

        return saveStream.ToArray();    
    }   
}