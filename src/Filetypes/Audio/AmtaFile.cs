using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Data;
using NintendAUX.Filetypes.Generic;
using NintendAUX.Utilities;
using NintendAUX.ViewModels;

namespace NintendAUX.Filetypes.Audio;

public struct AmtaFile
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

    public struct AMTAMarker
    {
        public uint ID;
        public uint NameOffset;
        public uint Start;
        public uint Length;

        public string Name;
    }

    public struct AMTAMarkerTable
    {
        public uint MarkerCount;
        public AMTAMarker[] Markers;
    }

    private readonly long BaseAddress;
    public AMTAInfo Info;
    public AMTAMarkerTable MarkerTable;

    public string Path;
    public byte[] Data;

    public AmtaFile(ref FileReader amtaReader)
    {
        BaseAddress = amtaReader.Position;

        Info = amtaReader.ReadStruct<AMTAInfo>();

        if (!MiscUtilities.CheckMagic(Info.Magic, InputFileType.Amta))
            new DataValidationException("This is not an AMTA file!").CreateExceptionDialog();

        if (Info.MarkerOffset != 0)
        {
            amtaReader.Position = BaseAddress + Info.MarkerOffset;
            MarkerTable.MarkerCount = amtaReader.ReadUInt32();
            MarkerTable.Markers = new AMTAMarker[MarkerTable.MarkerCount];

            var markerPositions = new long[MarkerTable.MarkerCount];

            for (var i = 0; i < MarkerTable.MarkerCount; i++)
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

            for (var i = 0; i < MarkerTable.MarkerCount; i++)
            {
                amtaReader.Position = markerPositions[i] + MarkerTable.Markers[i].NameOffset;
                MarkerTable.Markers[i].Name = amtaReader.ReadTerminatedString();
            }
        }

        amtaReader.Position = BaseAddress + Marshal.OffsetOf<AMTAInfo>("PathOffset") + Info.PathOffset;

        Path = amtaReader.ReadTerminatedString();

        var foundAMTA = false;
        while (!foundAMTA)
        {
            if (amtaReader.Position >= amtaReader.BaseStream.Length - 4)
            {
                amtaReader.Position += 4;
                break; // Found end of stream, just go with it
            }

            var amtaData = amtaReader.ReadBytes(4);
            amtaReader.Position -= 3;

            if (amtaData[0] != 0x41 && amtaData[0] != 0x42) continue; // A|B
            if (amtaData[1] != 0x4d && amtaData[1] != 0x57) continue; // M|W
            if (amtaData[2] != 0x54 && amtaData[2] != 0x41) continue; // T|A
            if (amtaData[3] != 0x41 && amtaData[3] != 0x56) continue; // A|V

            foundAMTA = true;
            amtaReader.Position -= 1; // To reset the stream if found
        }


        var amtaLength = Convert.ToInt32(amtaReader.Position - BaseAddress);
        amtaReader.Position = BaseAddress;

        Data = amtaReader.ReadBytes(amtaLength);
    }

    public AmtaFile(byte[] data)
    {
        FileReader amtaReader = new(new MemoryStream(data));
        this = new AmtaFile(ref amtaReader);
    }

    public AmtaFile(Stream stream)
    {
        FileReader amtaReader = new(stream);
        this = new AmtaFile(ref amtaReader);
    }

    public static byte[] Save(AmtaFile amtaData)
    {
        using MemoryStream saveStream = new();
        var amtaWriter = new FileWriter(saveStream);

        var BaseAddress = amtaWriter.Position;

        amtaWriter.Write(amtaData.Data);
        amtaWriter.Position = BaseAddress;

        amtaWriter.Write(MemoryMarshal.AsBytes(new Span<AMTAInfo>(ref amtaData.Info)));
        amtaWriter.Position = BaseAddress;

        if (amtaData.Info.MarkerOffset != 0)
        {
            // find earliest name offset
            var earliestNameOffset = 0xFFFFFFFFFFFF;
            for (var i = 0; i < amtaData.MarkerTable.MarkerCount; i++)
                if (amtaData.Info.MarkerOffset + i * 16 + 4 + amtaData.MarkerTable.Markers[i].NameOffset <
                    earliestNameOffset)
                    earliestNameOffset = amtaData.Info.MarkerOffset + i * 16 + 4 +
                                         amtaData.MarkerTable.Markers[i].NameOffset;

            List<long> markerNameOffsets = new();
            List<long> markerNameOffsetOffsets = new(); // lol

            amtaWriter.Position = amtaData.Info.MarkerOffset;
            amtaWriter.Write(amtaData.MarkerTable.MarkerCount);
            for (var i = 0; i < amtaData.MarkerTable.MarkerCount; i++)
            {
                amtaWriter.Write(amtaData.MarkerTable.Markers[i].ID);
                markerNameOffsetOffsets.Add(amtaWriter.Position);
                amtaWriter.Write((uint)0x00);
                amtaWriter.Write(amtaData.MarkerTable.Markers[i].Start);
                amtaWriter.Write(amtaData.MarkerTable.Markers[i].Length);
            }

            amtaWriter.Align(4);

            amtaWriter.Position = earliestNameOffset;
            foreach (var marker in amtaData.MarkerTable.Markers)
            {
                markerNameOffsets.Add(amtaWriter.Position);
                amtaWriter.Write(marker.Name);
                amtaWriter.Write((byte)0x0); //terminating string
            }

            var PathAddress = amtaWriter.Position - Marshal.OffsetOf<AMTAInfo>("PathOffset");
            amtaWriter.Write(amtaData.Path);
            amtaWriter.Write(0x00); // Null termination
            amtaWriter.WriteAt(Marshal.OffsetOf<AMTAInfo>("PathOffset"), (uint)PathAddress);

            for (var i = 0; i < markerNameOffsets.Count; i++)
            {
                var markerOffset = (uint)markerNameOffsets[i] - (uint)markerNameOffsetOffsets[i];
                amtaWriter.WriteAt(markerNameOffsetOffsets[i], markerOffset);
            }
        }
        else
        {
            // Since no marker table means this is the last thing in the file we're good just adding this at a hard offset
            amtaWriter.Position = amtaData.Info.PathOffset + Marshal.OffsetOf<AMTAInfo>("PathOffset") + BaseAddress;
            amtaWriter.Write(amtaData.Path);
            amtaWriter.Write(0x00); // Null termination
        }
        
        amtaWriter.WriteAt(Marshal.OffsetOf<AMTAInfo>("Size"), (uint)amtaWriter.Position);

        return saveStream.ToArray();
    }
}