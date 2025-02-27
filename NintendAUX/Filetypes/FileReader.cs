using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NintendAUX.Utilities;

namespace NintendAUX.Filetypes;

/// <summary>
///     A class for reading binary data from a <see cref="Stream" />.
/// </summary>
public sealed class FileReader : IDisposable
{
    #region constructor

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileReader" /> class.
    /// </summary>
    /// <param name="stream">The <see cref="Stream" /> to read from.</param>
    /// <param name="leaveOpen">Whether to leave the stream open when this <see cref="FileReader" /> instance is disposed.</param>
    public FileReader(Stream stream, bool leaveOpen = false)
    {
        _buffer = new byte[8];
        _leaveOpen = leaveOpen;
        _reader = LittleEndianReader;
        _readBytes = BitConverter.IsLittleEndian ? InternalReadBytes : InternalReadBytesReverse;

        BaseStream = stream;
        Position = 0;
    }

    #endregion

    #region IDisposable interface

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        if (!_leaveOpen) BaseStream.Dispose();
        _disposed = true;
    }

    #endregion

    #region private members

    private static readonly LittleEndianBinaryReader LittleEndianReader = new();
    private static readonly BigEndianBinaryReader BigEndianReader = new();
    private readonly byte[] _buffer;
    private readonly bool _leaveOpen;
    private IBinaryReader _reader;
    private Action<int, int> _readBytes;
    private bool _isBigEndian;
    private bool _disposed;

    #endregion

    #region public properties

    /// <summary>
    ///     Whether the stream encoding should be interpreted as big endian.
    /// </summary>
    public bool IsBigEndian
    {
        get => _isBigEndian;
        set
        {
            _isBigEndian = value;
            _reader = value ? BigEndianReader : LittleEndianReader;
            _readBytes = value == BitConverter.IsLittleEndian ? InternalReadBytesReverse : InternalReadBytes;
        }
    }

    /// <summary>
    ///     Gets or sets the current position of the stream.
    /// </summary>
    public long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    /// <summary>
    ///     The base stream used for reading.
    /// </summary>
    public Stream BaseStream { get; }

    #endregion

    #region public methods

    /// <summary>
    ///     Skips a number of bytes relative to the current position.
    /// </summary>
    /// <param name="count">The number of bytes to skip.</param>
    public void Skip(long count)
    {
        Position += count;
    }

    /// <summary>
    ///     Jumps to an absolute position in the stream.
    /// </summary>
    /// <param name="position">The position to jump to.</param>
    public void JumpTo(long position)
    {
        Position = position;
    }

    /// <summary>
    ///     Aligns the current position to the next valid alignment value.
    /// </summary>
    /// <param name="alignment">The alignment to use.</param>
    public void Align(int alignment)
    {
        Position += BinaryUtilities.GetOffset(Position, alignment);
    }

    #region ReadBytes

    /// <summary>
    ///     Reads a number of bytes from the stream.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    public byte[] ReadBytes(int length)
    {
        if (length < 1) return [];

        var bytes = new byte[length];
        _ = BaseStream.Read(bytes, 0, length);
        return bytes;
    }

    /// <summary>
    ///     Reads a number of bytes from the stream into a given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The offset into the buffer from where to start.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The number of actual bytes that have been read.</returns>
    public int ReadBytes(byte[] buffer, int offset, int count)
    {
        return BaseStream.Read(buffer, offset, count);
    }

    /// <summary>
    ///     Reads a number of bytes from the stream into a given buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read into.</param>
    /// <returns>The number of actual bytes that have been read.</returns>
    public int ReadBytes(Span<byte> buffer)
    {
        return BaseStream.Read(buffer);
    }

    /// <summary>
    ///     Reads a number of bytes from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="length">The number of bytes to read.</param>
    public byte[] ReadBytesAt(long position, int length)
    {
        Position = position;
        return ReadBytes(length);
    }

    /// <summary>
    ///     Reads a number of bytes from the stream at a given position into a buffer.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <param name="offset">The offset into the buffer from where to start.</param>
    /// <param name="count">The number of bytes to read.</param>
    /// <returns>The number of actual bytes that have been read.</returns>
    public int ReadBytesAt(long position, byte[] buffer, int offset, int count)
    {
        Position = position;
        return ReadBytes(buffer, offset, count);
    }

    /// <summary>
    ///     Reads a number of bytes from the stream into a given buffer.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="buffer">The buffer to read into.</param>
    /// <returns>The number of actual bytes that have been read.</returns>
    public int ReadBytesAt(long position, Span<byte> buffer)
    {
        Position = position;
        return ReadBytes(buffer);
    }

    #endregion

    #region ReadSByte

    /// <summary>
    ///     Reads a <see cref="sbyte" /> value from the stream.
    /// </summary>
    public sbyte ReadSByte()
    {
        return (sbyte)BaseStream.ReadByte();
    }

    /// <summary>
    ///     Reads a <see cref="sbyte" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public sbyte ReadSByteAt(long position)
    {
        Position = position;
        return ReadSByte();
    }

    [Obsolete]
    internal sbyte ReadSByte(int length)
    {
        if (length == 1) return (sbyte)BaseStream.ReadByte();

        _ = BaseStream.Read(_buffer, 0, length);
        return (sbyte)_buffer[IsBigEndian ? 0 : length - 1];
    }

    [Obsolete]
    internal sbyte ReadSByteAt(long position, int length)
    {
        Position = position;
        return ReadSByte(length);
    }

    #endregion

    #region ReadByte

    /// <summary>
    ///     Reads a <see cref="byte" /> value from the stream.
    /// </summary>
    public byte ReadByte()
    {
        return (byte)BaseStream.ReadByte();
    }

    /// <summary>
    ///     Reads a <see cref="byte" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public byte ReadByteAt(long position)
    {
        Position = position;
        return ReadByte();
    }

    [Obsolete]
    internal byte ReadByte(int length)
    {
        if (length == 1) return (byte)BaseStream.ReadByte();

        _ = BaseStream.Read(_buffer, 0, length);
        return _buffer[IsBigEndian ? 0 : length - 1];
    }

    [Obsolete]
    internal byte ReadByteAt(long position, int length)
    {
        Position = position;
        return ReadByte(length);
    }

    #endregion

    #region ReadInt16

    /// <summary>
    ///     Reads a <see cref="short" /> value from the stream.
    /// </summary>
    public short ReadInt16()
    {
        _ = BaseStream.Read(_buffer, 0, 2);
        return _reader.ReadInt16(_buffer);
    }

    /// <summary>
    ///     Reads a <see cref="short" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public short ReadInt16At(long position)
    {
        Position = position;
        return ReadInt16();
    }

    [Obsolete]
    internal short ReadInt16(int length)
    {
        if (length == 2) return ReadInt16();

        _readBytes(length, 2);
        return _reader.ReadInt16(_buffer);
    }

    [Obsolete]
    internal short ReadInt16At(long position, int length)
    {
        Position = position;
        return ReadInt16(length);
    }

    #endregion

    #region ReadUInt16

    /// <summary>
    ///     Reads an <see cref="ushort" /> value from the stream.
    /// </summary>
    public ushort ReadUInt16()
    {
        _ = BaseStream.Read(_buffer, 0, 2);
        return _reader.ReadUInt16(_buffer);
    }

    /// <summary>
    ///     Reads an <see cref="ushort" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public ushort ReadUInt16At(long position)
    {
        Position = position;
        return ReadUInt16();
    }

    [Obsolete]
    internal ushort ReadUInt16(int length)
    {
        if (length == 2) return ReadUInt16();

        _readBytes(length, 2);
        return _reader.ReadUInt16(_buffer);
    }

    [Obsolete]
    internal ushort ReadUInt16At(long position, int length)
    {
        Position = position;
        return ReadUInt16(length);
    }

    #endregion

    #region ReadInt32

    /// <summary>
    ///     Reads an <see cref="int" /> value from the stream.
    /// </summary>
    public int ReadInt32()
    {
        _ = BaseStream.Read(_buffer, 0, 4);
        return _reader.ReadInt32(_buffer);
    }

    /// <summary>
    ///     Reads an <see cref="int" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public int ReadInt32At(long position)
    {
        Position = position;
        return ReadInt32();
    }

    [Obsolete]
    internal int ReadInt32(int length)
    {
        if (length == 4) return ReadInt32();

        _readBytes(length, 4);
        return _reader.ReadInt32(_buffer);
    }

    [Obsolete]
    internal int ReadInt32At(long position, int length)
    {
        Position = position;
        return ReadInt32(length);
    }

    #endregion

    #region ReadUInt32

    /// <summary>
    ///     Reads an <see cref="uint" /> value from the stream.
    /// </summary>
    public uint ReadUInt32()
    {
        _ = BaseStream.Read(_buffer, 0, 4);
        return _reader.ReadUInt32(_buffer);
    }

    /// <summary>
    ///     Reads an <see cref="uint" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public uint ReadUInt32At(long position)
    {
        Position = position;
        return ReadUInt32();
    }

    [Obsolete]
    internal uint ReadUInt32(int length)
    {
        if (length == 4) return ReadUInt32();

        _readBytes(length, 4);
        return _reader.ReadUInt32(_buffer);
    }

    [Obsolete]
    internal uint ReadUInt32At(long position, int length)
    {
        Position = position;
        return ReadUInt32(length);
    }

    #endregion

    #region ReadInt64

    /// <summary>
    ///     Reads a <see cref="long" /> value from the stream.
    /// </summary>
    public long ReadInt64()
    {
        _ = BaseStream.Read(_buffer, 0, 8);
        return _reader.ReadInt64(_buffer);
    }

    /// <summary>
    ///     Reads a <see cref="long" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public long ReadInt64At(long position)
    {
        Position = position;
        return ReadInt64();
    }

    [Obsolete]
    internal long ReadInt64(int length)
    {
        if (length == 8) return ReadInt64();

        _readBytes(length, 8);
        return _reader.ReadInt64(_buffer);
    }

    [Obsolete]
    internal long ReadInt64At(long position, int length)
    {
        Position = position;
        return ReadInt64(length);
    }

    #endregion

    #region ReadUInt64

    /// <summary>
    ///     Reads an <see cref="ulong" /> value from the stream.
    /// </summary>
    public ulong ReadUInt64()
    {
        _ = BaseStream.Read(_buffer, 0, 8);
        return _reader.ReadUInt64(_buffer);
    }

    /// <summary>
    ///     Reads an <see cref="ulong" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public ulong ReadUInt64At(long position)
    {
        Position = position;
        return ReadUInt64();
    }

    [Obsolete]
    internal ulong ReadUInt64(int length)
    {
        if (length == 8) return ReadUInt64();

        _readBytes(length, 8);
        return _reader.ReadUInt64(_buffer);
    }

    [Obsolete]
    internal ulong ReadUInt64At(long position, int length)
    {
        Position = position;
        return ReadUInt64(length);
    }

    #endregion

    #region ReadSingle

    /// <summary>
    ///     Reads a <see cref="float" /> value from the stream.
    /// </summary>
    public float ReadSingle()
    {
        _ = BaseStream.Read(_buffer, 0, 4);
        return _reader.ReadSingle(_buffer);
    }

    /// <summary>
    ///     Reads a <see cref="float" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public float ReadSingleAt(long position)
    {
        Position = position;
        return ReadSingle();
    }

    [Obsolete]
    internal float ReadSingle(int length)
    {
        if (length == 4) return ReadSingle();

        _readBytes(length, 4);
        return _reader.ReadSingle(_buffer);
    }

    [Obsolete]
    internal float ReadSingleAt(long position, int length)
    {
        Position = position;
        return ReadSingle(length);
    }

    #endregion

    #region ReadDouble

    /// <summary>
    ///     Reads a <see cref="double" /> value from the stream.
    /// </summary>
    public double ReadDouble()
    {
        _ = BaseStream.Read(_buffer, 0, 8);
        return _reader.ReadDouble(_buffer);
    }

    /// <summary>
    ///     Reads a <see cref="double" /> value from the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    public double ReadDoubleAt(long position)
    {
        Position = position;
        return ReadDouble();
    }

    [Obsolete]
    internal double ReadDouble(int length)
    {
        if (length == 8) return ReadDouble();

        _readBytes(length, 8);
        return _reader.ReadDouble(_buffer);
    }

    [Obsolete]
    internal double ReadDoubleAt(long position, int length)
    {
        Position = position;
        return ReadDouble(length);
    }

    #endregion

    #region ReadHexString

    /// <summary>
    ///     Reads a number of bytes from the stream and converts them into a hex-encoded string.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    public string ReadHexString(int length)
    {
#if NET5_0_OR_GREATER
        Span<byte> bytes = stackalloc byte[length];
        _ = BaseStream.Read(bytes);
        if (!IsBigEndian) bytes.Reverse();
        return Convert.ToHexString(bytes);
#else
        var bytes = new byte[length];
        _ = BaseStream.Read(bytes);
        if (!IsBigEndian) Array.Reverse(bytes);
        return BitConverter.ToString(bytes).Replace("-", string.Empty);
#endif
    }

    /// <summary>
    ///     Reads a number of bytes from the stream at a given position and converts them into a hex-encoded string.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="length">The number of bytes to read.</param>
    public string ReadHexStringAt(long position, int length)
    {
        Position = position;
        return ReadHexString(length);
    }

    #endregion

    #region ReadString

    /// <summary>
    ///     Reads a <see cref="string" /> value from the stream using <see cref="Encoding.UTF8" /> as encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    public string ReadString(int length)
    {
        return ReadString(length, Encoding.UTF8);
    }

    /// <summary>
    ///     Reads a <see cref="string" /> value from the stream using the given encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <param name="encoding">The encoding to use.</param>
    public string ReadString(int length, Encoding encoding)
    {
        Span<byte> bytes = stackalloc byte[length];
        _ = BaseStream.Read(bytes);
        return encoding.GetString(bytes).TrimEnd('\0');
    }

    /// <summary>
    ///     Reads a <see cref="string" /> value from the stream at a given position using <see cref="Encoding.UTF8" /> as
    ///     encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="length">The number of bytes to read.</param>
    public string ReadStringAt(long position, int length)
    {
        return ReadStringAt(position, length, Encoding.UTF8);
    }

    /// <summary>
    ///     Reads a <see cref="double" /> value from the stream at a given position using the given encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <param name="encoding">The encoding to use.</param>
    public string ReadStringAt(long position, int length, Encoding encoding)
    {
        Position = position;
        return ReadString(length, encoding);
    }

    #endregion

    #region ReadTerminatedString

    /// <summary>
    ///     Reads a null-terminated <see cref="string" /> value from the stream using <see cref="Encoding.UTF8" /> as encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="maxLength">The maximum number of bytes to read.</param>
    public string ReadTerminatedString(int maxLength = -1)
    {
        return ReadTerminatedString(Encoding.UTF8, maxLength);
    }

    /// <summary>
    ///     Reads a null-terminated <see cref="string" /> value from the stream using the given encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="maxLength">The maximum number of bytes to read.</param>
    public string ReadTerminatedString(Encoding encoding, int maxLength = -1)
    {
        var bytes = new List<byte>(maxLength > 0 ? maxLength : 256);
        var nullByteLength = encoding.GetMinByteCount();

        var nullCount = 0;
        do
        {
            var value = (byte)BaseStream.ReadByte();
            nullCount = value == 0x00 ? nullCount + 1 : 0;
            bytes.Add(value);
        } while (bytes.Count != maxLength && nullCount < nullByteLength);

        //return whatever we have
        if (bytes.Count == maxLength) return encoding.GetString([..bytes]).TrimEnd('\0');

        //append enough null bytes to ensure we have a full null-byte to trim
        for (var i = 0; i < nullByteLength - 1; ++i) bytes.Add(0x00);
        return encoding.GetString([..bytes])[..^1].TrimEnd('\0');
    }

    /// <summary>
    ///     Reads a null-terminated <see cref="string" /> value from the stream at a given position using
    ///     <see cref="Encoding.UTF8" /> as encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="maxLength">The maximum number of bytes to read.</param>
    /// <returns></returns>
    public string ReadTerminatedStringAt(long position, int maxLength = -1)
    {
        return ReadTerminatedStringAt(position, Encoding.UTF8, maxLength);
    }

    /// <summary>
    ///     Reads a null-terminated <see cref="string" /> value from the stream at a given position using the given encoding.
    ///     Trailing null-bytes are removed.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start reading from.</param>
    /// <param name="encoding">The encoding to use.</param>
    /// <param name="maxLength">The maximum number of bytes to read.</param>
    /// <returns></returns>
    public string ReadTerminatedStringAt(long position, Encoding encoding, int maxLength = -1)
    {
        Position = position;
        return ReadTerminatedString(encoding, maxLength);
    }

    #endregion

    #endregion

    #region private methods

    //reads an array of raw bytes from the stream
    private void InternalReadBytes(int length, int padding)
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _ = BaseStream.Read(_buffer, 0, length);
    }

    private void InternalReadBytesReverse(int length, int padding)
    {
        Array.Clear(_buffer, 0, _buffer.Length);
        _ = BaseStream.Read(_buffer, padding - length, length);
    }

    #endregion

    #region helper classes

    private interface IBinaryReader
    {
        public short ReadInt16(ReadOnlySpan<byte> buffer);
        public ushort ReadUInt16(ReadOnlySpan<byte> buffer);
        public int ReadInt32(ReadOnlySpan<byte> buffer);
        public uint ReadUInt32(ReadOnlySpan<byte> buffer);
        public long ReadInt64(ReadOnlySpan<byte> buffer);
        public ulong ReadUInt64(ReadOnlySpan<byte> buffer);
        public float ReadSingle(ReadOnlySpan<byte> buffer);
        public double ReadDouble(ReadOnlySpan<byte> buffer);
    }

    private class LittleEndianBinaryReader : IBinaryReader
    {
        public short ReadInt16(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(buffer);
        }

        public ushort ReadUInt16(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(buffer);
        }

        public int ReadInt32(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }

        public uint ReadUInt32(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(buffer);
        }

        public long ReadInt64(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(buffer);
        }

        public ulong ReadUInt64(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt64LittleEndian(buffer);
        }

        public float ReadSingle(ReadOnlySpan<byte> buffer)
        {
#if NET5_0_OR_GREATER
            return BinaryPrimitives.ReadSingleLittleEndian(buffer);
#else
            var tmpValue = BinaryPrimitives.ReadInt32LittleEndian(buffer);
            return BitConverter.Int32BitsToSingle(tmpValue);
#endif
        }

        public double ReadDouble(ReadOnlySpan<byte> buffer)
        {
#if NET5_0_OR_GREATER
            return BinaryPrimitives.ReadDoubleLittleEndian(buffer);
#else
            var tmpValue = BinaryPrimitives.ReadInt64LittleEndian(buffer);
            return BitConverter.Int64BitsToDouble(tmpValue);
#endif
        }
    }

    private class BigEndianBinaryReader : IBinaryReader
    {
        public short ReadInt16(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt16BigEndian(buffer);
        }

        public ushort ReadUInt16(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public int ReadInt32(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt32BigEndian(buffer);
        }

        public uint ReadUInt32(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public long ReadInt64(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadInt64BigEndian(buffer);
        }

        public ulong ReadUInt64(ReadOnlySpan<byte> buffer)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(buffer);
        }

        public float ReadSingle(ReadOnlySpan<byte> buffer)
        {
#if NET5_0_OR_GREATER
            return BinaryPrimitives.ReadSingleBigEndian(buffer);
#else
            var tmpValue = BinaryPrimitives.ReadInt32BigEndian(buffer);
            return BitConverter.Int32BitsToSingle(tmpValue);
#endif
        }

        public double ReadDouble(ReadOnlySpan<byte> buffer)
        {
#if NET5_0_OR_GREATER
            return BinaryPrimitives.ReadDoubleBigEndian(buffer);
#else
            var tmpValue = BinaryPrimitives.ReadInt64BigEndian(buffer);
            return BitConverter.Int64BitsToDouble(tmpValue);
#endif
        }
    }

    #endregion
}