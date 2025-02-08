using System.Text;
using StutteredBars.Filetypes;

namespace StutteredBars.Frontend.Parsers;

public static class InfoParser
{
    public static StringBuilder builder = new StringBuilder();
    
    public static string ParseData(AMTAFile info, BWAVFile track)
    {
        builder.Clear();
        builder.AppendLine($"Asset Name\n{info.Path}\n");
        builder.AppendLine($"Endianness\n{GetEndianness(info.Info.Endianness)}\n");
        
        builder.AppendLine($"Prefetch File?\n{(track.Header.IsPrefetch == 0 ? "No" : "Yes")}");
        
        if (info.Info.TagOffset != 0) builder.AppendLine($"\nTag Offset\n{info.Info.TagOffset:x8}");
        if (info.Info.MarkerOffset != 0) builder.AppendLine($"\nMarker Offset\n{info.Info.MarkerOffset:x8}");
        if (info.Info.MinfOffset != 0) builder.AppendLine($"\nMINF Offset\n{info.Info.MinfOffset:x8}");
        return builder.ToString();
    }

    public static string GetEndianness(int input)
    {
        switch (input)
        {
            case 0xFFFE: return "Big Endian";
            case 0xFEFF: return "Little Endian";
        }
        return "Unknown";
    }
}

// public struct AMTAInfo
// {
//     public uint Magic;
//     public ushort Endianness;
//     public byte MinorVersion;
//     public byte MajorVersion;
//     public uint Size;
//     public uint Reserve0;
//     public uint DataOffset;
//     public uint MarkerOffset;
//     public uint MinfOffset;
//     public uint TagOffset;
//     public uint Reserve4;
//     public uint PathOffset;
//     public uint PathHash;
//     public uint Flags; // Technically a bitfield, but...
//     public byte SourceCount;
// }