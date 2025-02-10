namespace StutteredBars.Frontend.Parsers.SARC;

/// <summary>
/// A class holding information about a file inside a SARC file.
/// </summary>
public class SarcContent
{
    /// <summary>
    /// The full name and path of the file.
    /// Defaults to the file name hash if the SARC file doesn't contain a SFNT entry.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// The content of the file as <see cref="byte"/> array.
    /// </summary>
    public required byte[] Data { get; set; }
}