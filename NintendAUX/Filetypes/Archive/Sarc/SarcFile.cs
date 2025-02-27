using System.Collections.Generic;

namespace NintendAUX.Parsers.SARC;

/// <summary>
///     A class holding information about a SARC archive file.
/// </summary>
public class SarcFile : SarcUtils.IVariableEndianFile
{
    /// <summary>
    ///     Gets the version of the SARC file.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    ///     Gets the hash key used for hashing file names.
    /// </summary>
    public int HashKey { get; set; }

    /// <summary>
    ///     Whether the files have names.
    /// </summary>
    public bool HasFileNames { get; set; }

    /// <summary>
    ///     The list of files contained in the SARC file.
    /// </summary>
    public List<SarcContent> Files { get; set; } = [];

    /// <inheritdoc />
    public bool BigEndian { get; set; }
}

public class SarcContent
{
    /// <summary>
    ///     The full name and path of the file.
    ///     Defaults to the file name hash if the SARC file doesn't contain a SFNT entry.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///     The content of the file as <see cref="byte" /> array.
    /// </summary>
    public required byte[] Data { get; set; }
}