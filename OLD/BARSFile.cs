using System.Runtime.InteropServices;

namespace StutteredBars;

public class BARSFile
{
    [StructLayout(LayoutKind.Sequential)]
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

    [StructLayout(LayoutKind.Sequential)]
    public struct AmtaDetail
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
        public uint PathOffset; // Important for finding filename 
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Path;
    }

    public struct Amta
    {
        public byte[] Data;
    }

    public struct BarsReserveData
    {
        public uint FileCount;
        public uint[] FileHashes;
    }

    public struct Bwav
    {
        public byte[] Data;
    }

    public uint[] FileDataHash;
    public BarsEntry[] EntryArray;

    public Bwav[] BwavList;
    public AmtaDetail[] AmtaList;
    public Amta[] AmtaData;
    
    public BarsHeader Header;
    public BarsReserveData ReserveData;
}