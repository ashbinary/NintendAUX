using System.ComponentModel;
using System.Text;

namespace StutteredBars;

public class BARSSaver
{
    public byte[] SaveBARSFile(BARSFile fileData)
    {
        CRC32 fileHasher = new CRC32();
        List<int> amtaOffsets = new();
        List<int> bwavOffsets = new();

        MemoryStream exportStream = new MemoryStream();
        using var writer = new FileWriter(exportStream, true);
        writer.IsBigEndian = false;

        // Header
        writer.Write("BARS", Encoding.ASCII);
        writer.Write(fileData.Header.FileSize);
        writer.Write((ushort) 0xFEFF);
        writer.Write((byte) 0x2);
        writer.Write((byte) 0x1);
        writer.Write(fileData.Header.FileCount);

        // File Path Hash Array
        foreach (BARSFile.AmtaDetail amtaFile in fileData.AmtaList)
        {
            writer.Write(ReverseBytes(fileHasher.ComputeHash(Encoding.ASCII.GetBytes(amtaFile.Path))));
        }

        // Fill Entry Array with zeros, then come back to populate
        var originPoint = writer.BaseStream.Position;

        foreach (BARSFile.Amta amtaFile in fileData.AmtaData)
        {
            writer.Write(0xDEADBEEFDEADBEEF); // lol
        }

        writer.Write(fileData.ReserveData.FileCount);

        foreach (uint hash in fileData.ReserveData.FileHashes)
        {
            writer.Write(hash);
        }

    //     public struct Amta
    // {
    //     public uint Magic;
    //     public ushort Endianness;
    //     public byte MajorVersion;
    //     public byte MinorVersion;
    //     public uint Size;
    //     public uint Reserve0;
    //     public uint DataOffset;
    //     public uint MarkerOffset;
    //     public uint MinfOffset;
    //     public uint TagOffset;
    //     public uint Reserve4;
    //     public uint PathOffset; // Important for finding filename 
        
    //     [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
    //     public string Path;
    // }

        foreach (BARSFile.Amta amtaFile in fileData.AmtaData)
        {
            amtaOffsets.Add(Convert.ToInt32(writer.BaseStream.Position));
            writer.Write(amtaFile.Data);
        }

        foreach (BARSFile.Bwav bwavFile in fileData.BwavList)
        {
            bwavOffsets.Add(Convert.ToInt32(writer.BaseStream.Position));
            writer.Write(bwavFile.Data);
        }

        writer.BaseStream.Position = originPoint;

        for (int i = 0; i < fileData.AmtaData.Length; i++)
        {
            writer.Write(amtaOffsets[i]);
            writer.Write(bwavOffsets[i]);
        }

        using FileStream fileStream = File.Create("brappyfart.bars");
        {
            exportStream.Seek(0, SeekOrigin.Begin);
            exportStream.CopyTo(fileStream);
            fileStream.Close();
        };

        return exportStream.ToArray();
    }

    // https://stackoverflow.com/questions/18145667/how-can-i-reverse-the-byte-order-of-an-int
    public static uint ReverseBytes(uint value)
    {
        return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
            (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
    }

    // https://stackoverflow.com/questions/18145667/how-can-i-reverse-the-byte-order-of-an-int
    public static uint ReverseBytes(byte[] valueBase)
    {
        return ReverseBytes(BitConverter.ToUInt32(valueBase));
    }
}
