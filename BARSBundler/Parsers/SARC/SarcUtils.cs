using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace BARSBundler.Parsers.SARC;

public class SarcUtils
{
    public interface IVariableEndianFile
    {
        /// <summary>
        /// Whether the file is encoded in big endian.
        /// </summary>
        public bool BigEndian { get; set; }
    }
    
    public class AlignmentTable : IEnumerable<KeyValuePair<string, int>>
    {
        #region private members
        private readonly Dictionary<string, int> _alignment = new(StringComparer.OrdinalIgnoreCase);
        private int _defaultValue = 8;
        #endregion

        #region public properties
        /// <summary>
        /// Gets or sets the default alignment in bytes for file extensions not defined in the table.
        /// Defaults to 8.
        /// </summary>
        public int Default
        {
            get => _defaultValue;
            set
            {
                if (value == 0) _defaultValue = 1;
                _defaultValue = Math.Abs(value);
            }
        }

        /// <summary>
        /// Gets the number of alignment mappings in this table.
        /// </summary>
        public int Count => _alignment.Count;
        #endregion

        #region public methods
        /// <summary>
        /// Adds a new alignment in bytes for a given file extension.
        /// File extensions are case-insensitive.
        /// </summary>
        /// <param name="extension">The file extension to add.</param>
        /// <param name="alignment">The alignment in bytes.</param>
        /// <returns><see langword="true"/> if the alignment was added successfully; otherwise <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool Add(string extension, int alignment)
        {
            #if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(extension, nameof(extension));
            #else
            if (extension is null) throw new ArgumentNullException(nameof(extension));
            #endif

            return _alignment.TryAdd(extension.StartsWith('.') ? extension : '.' + extension, alignment == 0 ? 1 : Math.Abs(alignment));
        }

        /// <summary>
        /// Determines whether an alignment exists for a given file extension.
        /// </summary>
        /// <param name="extension">The file extension to find.</param>
        /// <returns><see langword="true"/> if the alignment was found; otherwise <see langword="false"/>.</returns>
        public bool Contains(string extension) => _alignment.ContainsKey(extension);

        /// <summary>
        /// Removes the alignment for a given file extension.
        /// </summary>
        /// <param name="extension">The file extension to remove.</param>
        /// <returns><see langword="true"/> if the alignment was found and removed successfully; otherwise <see langword="false"/>.</returns>
        public bool Remove(string extension) => _alignment.Remove(extension.StartsWith('.') ? extension : '.' + extension);

        /// <summary>
        /// Removes the alignments for all file extensions.
        /// </summary>
        public void Clear() => _alignment.Clear();

        /// <summary>
        /// Gets the alignment in bytes for a given file extension.
        /// Falls back to the value specified in <see cref="Default"/> if the extension was not found.
        /// </summary>
        /// <param name="extension">The file extension to find.</param>
        /// <returns>The alignment in bytes for that file extension.</returns>
        public int Get(string extension) => _alignment.GetValueOrDefault(extension, Default);

        /// <summary>
        /// Attempts to get the alignment in bytes for a given file extension.
        /// </summary>
        /// <param name="extension">The file extension to find.</param>
        /// <param name="alignment">The alignment in bytes for that file extension.</param>
        /// <returns><see langword="true"/> if the alignment was found; otherwise <see langword="false"/>.</returns>
        public bool TryGet(string extension, out int alignment) => _alignment.TryGetValue(extension, out alignment);

        /// <summary>
        /// Gets the alignment in bytes for a given file name. Each '.' in the name will be considered as extension.
        /// Falls back to the value specified in <see cref="Default"/> if the extension was not found.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns>The alignment in bytes for that file extension.</returns>
        public int GetFromName(string fileName)
        {
            #if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(fileName, nameof(fileName));
            #else
            if (fileName is null) throw new ArgumentNullException(nameof(fileName));
            #endif

            fileName = Path.GetFileName(fileName);

            var index = 0;
            while (index < fileName.Length)
            {
                if (fileName[index] == '.' && _alignment.TryGetValue(fileName[index..], out var value))
                {
                    return value;
                }

                ++index;
            }

            return Default;
        }
        #endregion

        #region IEnumerable interface
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _alignment.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }

    public interface IFileParser
    {
        /// <summary>
        /// Validates whether the given stream can be parsed with this parser instance.
        /// </summary>
        /// <param name="fileStream">The stream to check.</param>
        /// <returns><see langword="true"/> if can be parsed; otherwise <see langword="false"/>.</returns>
        public bool CanParse(Stream fileStream);
    }

    /// <summary>
    /// The interface for generic file parsers.
    /// </summary>
    public interface IFileParser<out T> : IFileParser where T : class
    {
        /// <summary>
        /// Parses a file stream to a file format.
        /// </summary>
        /// <param name="fileStream">The stream to parse.</param>
        /// <returns>The parsed file format.</returns>
        public T Parse(Stream fileStream);
    }

    public interface IAlignedArchiveParser
    {
        /// <summary>
        /// Gets a list of alignment data of all files contained in the archive.
        /// This can be used to figure out the correct alignment values.
        /// </summary>
        /// <param name="fileStream">The stream to parse</param>
        /// <param name="table">The alignment table to use.</param>
        /// <returns>A collection of <see cref="AlignmentInfo"/> entries for all containing files.</returns>
        public List<AlignmentInfo> CheckAlignment(Stream fileStream, AlignmentTable table);
    }
    
    public class AlignmentInfo
    {
        /// <summary>
        /// The name of the file.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// The absolute start position of the file data.
        /// </summary>
        public required long DataStart { get; set; }

        /// <summary>
        /// The absolute end position of the file data.
        /// </summary>
        public required long DataEnd { get; set; }

        /// <summary>
        /// The length of the file data.
        /// </summary>
        public long DataLength => DataEnd - DataStart;

        /// <summary>
        /// The padding between the previous file data and the start of this file data.
        /// </summary>
        public required long Padding { get; set; }

        /// <summary>
        /// The used alignment value from a <see cref="AlignmentTable"/>.
        /// </summary>
        public required int Alignment { get; set; }

        /// <summary>
        /// The expected/aligned start position of the file data.
        /// </summary>
        public required long ExpectedDataStart { get; set; }

        /// <summary>
        /// Determines whether the used alignment value matches the data start position.
        /// </summary>
        public bool IsValid => DataStart == ExpectedDataStart;
    }


}