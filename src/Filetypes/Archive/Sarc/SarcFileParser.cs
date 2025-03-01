using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NintendAUX.Filetypes;
using NintendAUX.Utilities;

namespace NintendAUX.Parsers.SARC;

/// <summary>
///     A class for parsing SARC archives.
/// </summary>
public class SarcFileParser : SarcUtils.IFileParser<SarcFile>, SarcUtils.IAlignedArchiveParser
{
    #region IAlignedArchiveParser interface

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public List<SarcUtils.AlignmentInfo> CheckAlignment(Stream fileStream, SarcUtils.AlignmentTable table)
    {
        return CheckAlignmentStatic(fileStream, table);
    }

    #endregion

    #region public methods

    /// <inheritdoc cref="SarcUtils.IFileParser.CanParse" />
    /// <exception cref="ArgumentNullException"></exception>
    public static bool CanParseStatic(Stream fileStream)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));
#else
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
#endif

        using var reader = new FileReader(fileStream, true);
        return CanParse(reader);
    }

    /// <inheritdoc cref="SarcUtils.IAlignedArchiveParser.CheckAlignment" />
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public static List<SarcUtils.AlignmentInfo> CheckAlignmentStatic(Stream fileStream, SarcUtils.AlignmentTable table)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));
#else
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
#endif

        using var reader = new FileReader(fileStream);
        if (!CanParse(reader)) throw new InvalidDataException("File is not a SARC file.");

        //parse file metadata and header
        GetMetaData(reader, out _, out _, out var sfatOffset, out var dataOffset);

        //parse files
        if (reader.ReadStringAt(sfatOffset, 4, Encoding.ASCII) != "SFAT")
            throw new InvalidDataException("Missing SFAT section.");
        var sfatHeaderLength = reader.ReadUInt16();
        var fileCount = reader.ReadUInt16();
        reader.JumpTo(sfatOffset + sfatHeaderLength);

        var infos = new List<SarcUtils.AlignmentInfo>(fileCount);
        var nameOffsets = new int[fileCount];
        for (var i = 0; i < fileCount; ++i)
        {
            reader.JumpTo(sfatOffset + sfatHeaderLength + i * 16);

            var fileNameHash = reader.ReadBytes(4);
            var fileNameData = reader.ReadUInt32();
            var fileOffset = reader.ReadInt32();
            var endOfFile = reader.ReadInt32();

            infos.Add(new SarcUtils.AlignmentInfo
            {
                Name = fileNameHash.ToHexString(true),
                DataStart = dataOffset + fileOffset,
                DataEnd = dataOffset + endOfFile,
                Alignment = table.Default,
                ExpectedDataStart = dataOffset + fileOffset,
                Padding = 0
            });
            nameOffsets[i] = fileNameData > 0 ? ((int)fileNameData & 0x00FFFFFF) * 4 : -1;
        }

        //parse file names
        if (reader.ReadStringAt(sfatOffset + sfatHeaderLength + fileCount * 16, 4, Encoding.ASCII) != "SFNT")
            throw new InvalidDataException("Missing SFNT section.");
        var nameOffset = reader.Position + reader.ReadUInt16() - 4;
        var maxAlignment = 0;
        for (var i = 0; i < nameOffsets.Length; ++i)
        {
            var offset = nameOffsets[i];
            if (offset < 0) continue;

            var fileName = reader.ReadTerminatedStringAt(nameOffset + offset);
            var alignment = table.GetFromName(fileName);
            if (alignment > maxAlignment) maxAlignment = alignment;

            infos[i].Name = fileName;
            infos[i].Alignment = alignment;
        }

        //calculate aligned data starts
        infos.Sort((i1, i2) => i1.DataStart.CompareTo(i2.DataStart));
        var currentDataOffset = reader.Position;
        for (var i = 0; i < infos.Count; ++i)
        {
            var info = infos[i];
            var alignment = i == 0 ? maxAlignment : info.Alignment;
            info.ExpectedDataStart = currentDataOffset += BinaryUtilities.GetOffset(currentDataOffset, alignment);
            info.Padding = info.DataStart -
                           (i > 0 ? infos[i - 1].DataStart + infos[i - 1].DataLength : reader.Position);
            currentDataOffset += info.DataLength;
        }

        return infos;
    }

    #endregion

    #region IFileParser interface

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    public bool CanParse(Stream fileStream)
    {
        return CanParseStatic(fileStream);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    public SarcFile Parse(Stream fileStream)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(fileStream, nameof(fileStream));
#else
        if (fileStream is null) throw new ArgumentNullException(nameof(fileStream));
#endif

        using var reader = new FileReader(fileStream);
        if (!CanParse(reader)) throw new InvalidDataException("File is not a SARC file.");

        //parse file metadata and header
        GetMetaData(reader, out _, out var version, out var sfatOffset, out var dataOffset);

        var sarcFile = new SarcFile
        {
            BigEndian = reader.IsBigEndian,
            Version = version,
            HasFileNames = true
        };

        //parse files
        if (reader.ReadStringAt(sfatOffset, 4, Encoding.ASCII) != "SFAT")
            throw new InvalidDataException("Missing SFAT section.");
        var sfatHeaderLength = reader.ReadUInt16();
        var fileCount = reader.ReadUInt16();
        sarcFile.HashKey = reader.ReadInt32();
        reader.JumpTo(sfatOffset + sfatHeaderLength);

        var files = new List<SarcContent>(fileCount);
        var nameOffsets = new int[fileCount];
        for (var i = 0; i < fileCount; ++i)
        {
            reader.JumpTo(sfatOffset + sfatHeaderLength + i * 16);

            var fileNameHash = reader.ReadBytes(4);
            var fileNameData = reader.ReadUInt32();
            var fileOffset = reader.ReadInt32();
            var endOfFile = reader.ReadInt32();

            var file = new SarcContent
            {
                Name = fileNameHash.ToHexString(true),
                Data = reader.ReadBytesAt(dataOffset + fileOffset, endOfFile - fileOffset)
            };

            files.Add(file);

            if (fileNameData > 0)
            {
                nameOffsets[i] = ((int)fileNameData & 0x00FFFFFF) * 4;
            }
            else
            {
                nameOffsets[i] = -1;
                sarcFile.HasFileNames = false;
            }
        }

        //parse file names
        if (reader.ReadStringAt(sfatOffset + sfatHeaderLength + fileCount * 16, 4, Encoding.ASCII) != "SFNT")
            throw new InvalidDataException("Missing SFNT section.");
        var nameOffset = reader.Position + reader.ReadUInt16() - 4;
        for (var i = 0; i < nameOffsets.Length; ++i)
        {
            var offset = nameOffsets[i];
            if (offset < 0) continue;
            files[i].Name = reader.ReadTerminatedStringAt(nameOffset + offset);
        }

        sarcFile.Files = files;
        return sarcFile;
    }

    #endregion

    #region private methods

    //verifies that the file is a SARC archive
    private static bool CanParse(FileReader reader)
    {
        return reader.BaseStream.Length > 4 && reader.ReadStringAt(0, 4, Encoding.ASCII) == "SARC";
    }

    //parses meta data
    private static void GetMetaData(FileReader reader, out uint fileSize, out int version, out int sfatOffset,
        out uint dataOffset)
    {
        if (reader.ReadUInt16At(6) == 0xFFFE) reader.IsBigEndian = true;

        sfatOffset = reader.ReadUInt16At(0x04);
        fileSize = reader.ReadUInt32At(0x08);
        dataOffset = reader.ReadUInt32();
        version = reader.ReadUInt16();
    }

    #endregion
}