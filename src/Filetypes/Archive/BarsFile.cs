using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Utilities;
using NintendAUX.ViewModels;

namespace NintendAUX.Filetypes.Archive;

public class BarsFile
{
    public List<BarsEntry> EntryArray;
    public uint[] FileHashArray;

    public BarsHeader Header;
    public BarsReserveData ReserveData;

    public BarsFile(byte[] data)
    {
        Read(new MemoryStream(data));
    }

    public BarsFile(MemoryStream data)
    {
        Read(data);
    }

    public void Read(MemoryStream data)
    {
        FileReader barsReader = new(data);

        Header = barsReader.ReadStruct<BarsHeader>();
        if (Header.MinorVersion != 2)
            new InvalidDataException("Incorrect BARS file version! Only Version 1.2 is supported.")
                .CreateExceptionDialog();

        FileHashArray = new uint[Header.FileCount];

        for (var i = 0; i < Header.FileCount; i++)
            FileHashArray[i] = barsReader.ReadUInt32();

        EntryArray = new List<BarsEntry>((int)Header.FileCount);

        // Create temporary array to hold entries while reading
        var tempEntries = new BarsEntry[Header.FileCount];

        for (var i = 0; i < Header.FileCount; i++)
        {
            tempEntries[i].BamtaOffset = barsReader.ReadUInt32();
            tempEntries[i].BwavOffset = barsReader.ReadUInt32();
        }

        ReserveData.FileCount = barsReader.ReadUInt32();
        ReserveData.FileHashes = new uint[ReserveData.FileCount];

        for (var i = 0; i < ReserveData.FileCount; i++)
            ReserveData.FileHashes[i] = barsReader.ReadUInt32();

        for (var i = 0; i < tempEntries.Length; i++)
        {
            barsReader.Position = tempEntries[i].BwavOffset;
            tempEntries[i].Bwav = new BwavFile(ref barsReader);
        }

        for (var i = 0; i < tempEntries.Length; i++)
        {
            barsReader.Position = tempEntries[i].BamtaOffset;
            tempEntries[i].Bamta = new AmtaFile(ref barsReader);
        }

        // Add all entries to the List
        EntryArray.AddRange(tempEntries);
    }

    public static byte[] SoftSave(BarsFile barsData)
    {
        using MemoryStream saveStream = new();
        var barsWriter = new FileWriter(saveStream);

        // Write Header data to stream
        barsWriter.WriteStruct(barsData.Header);

        // To support adding new entries, create new file count from metadata
        var newFileCount = barsData.EntryArray.Count;
        barsWriter.WriteAt(Marshal.OffsetOf<BarsHeader>("FileCount"),
            newFileCount); // Use AMTA file amount to calculate data (cannot be dupe)

        foreach (var entry in barsData.EntryArray)
            barsWriter.Write(CRC32.Compute(entry.Bamta.Path));
        //pathList.Add(CRC32.Compute(metadata.Path), metadata.Path);

        var offsetAddress = barsWriter.Position;
        var offsets = new long[newFileCount, 2];

        for (var i = 0; i < newFileCount; i++)
            barsWriter.Write(0xDEADBEEFDEADBEEF); // Create a temporary area for offsets which will be filled in later

        barsWriter.Write(barsData.ReserveData.FileCount);
        foreach (var barsHash in barsData.ReserveData.FileHashes)
            barsWriter.Write(barsHash);

        for (var a = 0; a < barsData.EntryArray.Count; a++)
        {
            offsets[a, 0] = barsWriter.Position;
            barsWriter.Write(AmtaFile.Save(barsData.EntryArray[a].Bamta));
            barsWriter.Align(0x4);
        }

        barsWriter.Align(0x20);

        for (var a = 0; a < barsData.EntryArray.Count; a++)
        {
            offsets[a, 1] = barsWriter.Position;
            barsWriter.Write(BwavFile.Save(barsData.EntryArray[a].Bwav));
            //barsWriter.Align(0x4);
        }

        barsWriter.Position = offsetAddress;
        for (var l = 0; l < offsets.GetLength(0); l++)
        {
            barsWriter.Write((uint)offsets[l, 0]);
            barsWriter.Write((uint)offsets[l, 1]);
        }

        // Finally, save filesize after completing everything
        barsWriter.WriteAt(Marshal.OffsetOf<BarsHeader>("FileSize"), (uint)saveStream.Length);

        return saveStream.ToArray();
    }

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
        public AmtaFile Bamta;
        public BwavFile Bwav;
    }

    public struct BarsReserveData
    {
        public uint FileCount;
        public uint[] FileHashes;
    }
}