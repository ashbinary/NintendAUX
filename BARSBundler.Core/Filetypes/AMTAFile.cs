using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BARSBundler.Core.Filetypes.AMTA;
using BARSBundler.Core.Helpers;
using Microsoft.VisualBasic;

namespace BARSBundler.Core.Filetypes;

// Currently not used.
// Almost fully functional, but has issues regarding Instrument offsets.
// SimpleAMTA is used instead.
public struct FullAMTAFile
{
    [StructLayout(LayoutKind.Sequential, Size = 49)]
    public struct AMTAInfo
    {
        public uint Magic;
        public ushort Endianness;
        public byte MinorVersion;
        public byte MajorVersion;
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
    public AMTATagTable TagTable;

    public string Path;

    public FullAMTAFile(ref FileReader amtaReader)
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
            MarkerTable = new AMTAMarkerTable(ref amtaReader);
        }

        if (Info.MinfOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.MinfOffset;
            Minf = new MINFFile(ref amtaReader);
        }

        if (Info.TagOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.TagOffset;
            TagTable = new AMTATagTable(ref amtaReader);
        }

        Path = amtaReader.ReadTerminatedStringAt(BaseAddress + 36 + Info.PathOffset);

    }

    public FullAMTAFile(byte[] data)
    {
        FileReader barsReader = new(new MemoryStream(data));
        this = new FullAMTAFile(ref barsReader);
    }

    public static byte[] Save(FullAMTAFile amtaData)
    {
        using MemoryStream saveStream = new();
        FileWriter amtaWriter = new FileWriter(saveStream);

        amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AMTAInfo>(ref amtaData.Info)));
        
        foreach (AmtaSourceInfo sourceData in amtaData.SourceInfo)
        {
            amtaWriter.Write(sourceData.ChannelCount);
            for (int j = 0; j < sourceData.ChannelInfo.Length; j++)
                amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AmtaChannelInfo>(ref sourceData.ChannelInfo[j])));
        }

        amtaWriter.Pad(2);

        if (amtaData.Info.DataOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.DataOffset;
            amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AMTAData>(ref amtaData.Data)));
        }

        if (amtaData.Info.MarkerOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.MarkerOffset;
            amtaWriter.Write(AMTAMarkerTable.Save(amtaData.MarkerTable));
        }

        if (amtaData.Info.MinfOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.MinfOffset;
            amtaWriter.Write(MINFFile.Save(amtaData.Minf));
        }

        if (amtaData.Info.TagOffset != 0)
        {
            amtaWriter.Position = amtaData.Info.TagOffset;
            amtaWriter.Write(AMTATagTable.Save(amtaData.TagTable));
        }

        amtaWriter.WriteTerminatedAt(Marshal.OffsetOf<AMTAInfo>("PathOffset") + amtaData.Info.PathOffset, amtaData.Path);

        return saveStream.ToArray();
    }
}

public struct AMTAFile
{
    [StructLayout(LayoutKind.Sequential, Size = 49)]
    public struct AMTAInfo
    {
        public uint Magic;
        public ushort Endianness;
        public byte MinorVersion;
        public byte MajorVersion;
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
    
    public struct AMTAMarker {
        public uint ID;
        public uint NameOffset;
        public uint Start;
        public uint Length;

        public string Name;
    };

    public struct AMTAMarkerTable
    {
        public uint MarkerCount;
        public AMTAMarker[] Markers;
    }

    private long BaseAddress;
    public AMTAInfo Info;
    public AMTAMarkerTable MarkerTable;
    
    public string Path;
    private int PathLength;
    public byte[] Data;
    
    public AMTAFile(ref FileReader amtaReader)
    {   
        BaseAddress = amtaReader.Position;

        Info = amtaReader.ReadStruct<AMTAInfo>();
        
        if (Info.MarkerOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.MarkerOffset;
            MarkerTable.MarkerCount = amtaReader.ReadUInt32();  
            MarkerTable.Markers = new AMTAMarker[MarkerTable.MarkerCount];

            long[] markerPositions = new long[MarkerTable.MarkerCount];

            for (int i = 0; i < MarkerTable.MarkerCount; i++)
            {
                markerPositions[i] = amtaReader.Position + 4;
                MarkerTable.Markers[i] = new AMTAMarker
                {
                    ID = amtaReader.ReadUInt32(),
                    NameOffset = amtaReader.ReadUInt32(),
                    Start = amtaReader.ReadUInt32(),
                    Length = amtaReader.ReadUInt32()
                };
            }

            for (int i = 0; i < MarkerTable.MarkerCount; i++)
            {
                amtaReader.Position = markerPositions[i] + MarkerTable.Markers[i].NameOffset;
                MarkerTable.Markers[i].Name = amtaReader.ReadTerminatedString();
            }
        }
        
        amtaReader.Position = BaseAddress + Marshal.OffsetOf<AMTAInfo>("PathOffset") + Info.PathOffset;

        Path = amtaReader.ReadTerminatedString();
        PathLength = Path.Length;

        bool foundAMTA = false;
        while (!foundAMTA)
        {
            if (amtaReader.Position >= amtaReader.BaseStream.Length - 4)
            {
                amtaReader.Position += 4;
                break; // Found end of stream, just go with it
            }
            byte[] amtaData = amtaReader.ReadBytes(4);
            amtaReader.Position -= 3;

            if (amtaData[0] != 0x41 && amtaData[0] != 0x42) continue; // A|B
            if (amtaData[1] != 0x4d && amtaData[1] != 0x57) continue; // M|W
            if (amtaData[2] != 0x54 && amtaData[2] != 0x41) continue; // T|A
            if (amtaData[3] != 0x41 && amtaData[3] != 0x56) continue; // A|V

            foundAMTA = true;
            amtaReader.Position -= 1; // To reset the stream if found
        }


        int amtaLength = Convert.ToInt32(amtaReader.Position - BaseAddress);
        amtaReader.Position = BaseAddress;

        Data = amtaReader.ReadBytes(amtaLength);
    }

    public AMTAFile(byte[] data)
    {
        FileReader amtaReader = new(new MemoryStream(data));
        this = new AMTAFile(ref amtaReader);
    }

    public AMTAFile(Stream stream)
    {
        FileReader amtaReader = new(stream);
        this = new AMTAFile(ref amtaReader);
    }

    public static byte[] Save(AMTAFile amtaData)
    {
        using MemoryStream saveStream = new();
        FileWriter amtaWriter = new FileWriter(saveStream);
        
        long BaseAddress = amtaWriter.Position;

        amtaWriter.Write(amtaData.Data);
        amtaWriter.Position = BaseAddress;

        amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AMTAInfo>(ref amtaData.Info)));
        amtaWriter.Position = BaseAddress;
        
        if (amtaData.Info.MarkerOffset != 0)
        {
            // find earliest name offset
            long earliestNameOffset = 0xFFFFFFFFFFFF;
            for (int i = 0; i < amtaData.MarkerTable.MarkerCount; i++)
            {
                if (amtaData.Info.MarkerOffset + (i * 16) + 4 + amtaData.MarkerTable.Markers[i].NameOffset < earliestNameOffset)
                    earliestNameOffset = amtaData.Info.MarkerOffset + (i * 16) + 4 + amtaData.MarkerTable.Markers[i].NameOffset;
            }
            
            List<long> markerNameOffsets = new();
            List<long> markerNameOffsetOffsets = new(); // lol

            amtaWriter.Position = amtaData.Info.MarkerOffset;
            amtaWriter.Write(amtaData.MarkerTable.MarkerCount);
            for (int i = 0; i < amtaData.MarkerTable.MarkerCount; i++)
            {
                amtaWriter.Write(amtaData.MarkerTable.Markers[i].ID);
                markerNameOffsetOffsets.Add(amtaWriter.Position);
                amtaWriter.Write((uint)0x00);
                amtaWriter.Write(amtaData.MarkerTable.Markers[i].Start);
                amtaWriter.Write(amtaData.MarkerTable.Markers[i].Length);
            }

            amtaWriter.Align(4);

            amtaWriter.Position = earliestNameOffset;
            foreach (AMTAMarker marker in amtaData.MarkerTable.Markers)
            {
                markerNameOffsets.Add(amtaWriter.Position);
                amtaWriter.Write(marker.Name);
                amtaWriter.Write((byte)0x0); //terminating string
            }

            long PathAddress = amtaWriter.Position - Marshal.OffsetOf<AMTAInfo>("PathOffset");
            amtaWriter.Write(amtaData.Path);
            amtaWriter.Pad(4);
            amtaWriter.WriteAt(Marshal.OffsetOf<AMTAInfo>("PathOffset"), (uint)PathAddress);

            for (int i = 0; i < markerNameOffsets.Count; i++)
            {
                uint markerOffset = (uint)markerNameOffsets[i] - (uint)markerNameOffsetOffsets[i];
                amtaWriter.WriteAt(markerNameOffsetOffsets[i], markerOffset);
            }
        }
        else
        {
            // Since no marker table means this is the last thing in the file we're good just adding this at a hard offset
            amtaWriter.Position = amtaData.Info.PathOffset + Marshal.OffsetOf<AMTAInfo>("PathOffset") + BaseAddress;
            amtaWriter.Write(amtaData.Path);
            amtaWriter.Pad(4);
        }

        return saveStream.ToArray();
    }
}