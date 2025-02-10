using System.Collections.Generic;

namespace StutteredBars.Frontend.Parsers.SARC;

/// <summary>
/// A class holding information about a SARC archive file.
/// </summary>
public class SarcFile : SarcUtils.IVariableEndianFile
{
    /// <inheritdoc/>
    public bool BigEndian { get; set; }

    /// <summary>
    /// Gets the version of the SARC file.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets the hash key used for hashing file names.
    /// </summary>
    public int HashKey { get; set; }

    /// <summary>
    /// Whether the files have names.
    /// </summary>
    public bool HasFileNames { get; set; }

    /// <summary>
    /// The list of files contained in the SARC file.
    /// </summary>
    public List<SarcContent> Files { get; set; } = [];
}