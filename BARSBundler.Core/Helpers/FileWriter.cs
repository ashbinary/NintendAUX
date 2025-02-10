using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using BARSBundler.Core.Helpers;

namespace BARSBundler.Core;

/// <summary>
/// A class for writing binary data to a <see cref="Stream"/>.
/// </summary>
public sealed class FileWriter : IDisposable
{
    #region private members
    private static readonly LittleEndianBinaryWriter LittleEndianWriter = new();
    private static readonly BigEndianBinaryWriter BigEndianWriter = new();
    private readonly byte[] _buffer;
    private readonly bool _leaveOpen;
    private IBinaryWriter _writer;
    private bool _isBigEndian;
    private bool _disposed;
    #endregion

    #region constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="FileWriter"/> class.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to write to.</param>
    /// <param name="leaveOpen">Whether to leave the stream open when this <see cref="FileWriter"/> instance is disposed.</param>
    public FileWriter(Stream stream, bool leaveOpen = false)
    {
        _buffer = new byte[8];
        _leaveOpen = leaveOpen;
        _writer = LittleEndianWriter;

        BaseStream = stream;
        Position = 0;
    }
    #endregion

    #region public properties
    /// <summary>
    /// Whether the stream encoding should be interpreted as big endian.
    /// </summary>
    public bool IsBigEndian
    {
        get => _isBigEndian;
        set
        {
            _isBigEndian = value;
            _writer = value ? BigEndianWriter : LittleEndianWriter;
        }
    }

    /// <summary>
    /// Gets or sets the current position of the stream.
    /// </summary>
    public long Position
    {
        get => BaseStream.Position;
        set
        {
            if (AutoExpand && value > BaseStream.Length)
            {
                var diff = value - BaseStream.Length;
                BaseStream.Position = BaseStream.Length;
                Pad(diff);
            }
            else BaseStream.Position = value;
        }
    }

    /// <summary>
    /// The base stream used for writing.
    /// </summary>
    public Stream BaseStream { get; }

    /// <summary>
    /// Whether the base stream will be expanded (padded with null-bytes) if accessing a position beyond the current length.
    /// </summary>
    public bool AutoExpand { get; set; }
    #endregion

    #region public methods
    /// <summary>
    /// Skips a number of bytes relative to the current position.
    /// </summary>
    /// <param name="count">The number of bytes to skip.</param>
    public void Skip(long count) => Position += count;

    /// <summary>
    /// Jumps to an absolute position in the stream.
    /// </summary>
    /// <param name="position">The position to jump to.</param>
    public void JumpTo(long position) => Position = position;

    /// <summary>
    /// Appends the given number of null-bytes to the stream.
    /// </summary>
    /// <param name="count">The number of bytes to append.</param>
    public void Pad(long count) => Pad(count, 0);

    /// <summary>
    /// Appends the given number of specific bytes to the stream.
    /// </summary>
    /// <param name="count">The number of bytes to append.</param>
    /// <param name="value">The value to append.</param>
    public void Pad(long count, byte value)
    {
        if (count < ushort.MaxValue)
        {
            Span<byte> buffer = stackalloc byte[(int) count];
            if (value == 0) buffer.Clear();
            else buffer.Fill(value);
            BaseStream.Write(buffer);
        }
        else
        {
            Span<byte> buffer = stackalloc byte[ushort.MaxValue];
            if (value == 0) buffer.Clear();
            else buffer.Fill(value);
            while (count > buffer.Length)
            {
                BaseStream.Write(buffer);
                count -= buffer.Length;
            }
            BaseStream.Write(buffer.ToArray(), 0, (int) count);
        }
    }

    /// <summary>
    /// Aligns the current position to the next valid alignment value.
    /// This will append the missing alignment as a number of null-bytes to the stream.
    /// </summary>
    /// <param name="alignment">The alignment to use.</param>
    public void Align(int alignment) => Align(alignment, 0);

    /// <summary>
    /// Aligns the current position to the next valid alignment value.
    /// This will append the missing alignment as a number of specific bytes to the stream.
    /// </summary>
    /// <param name="alignment">The alignment to use.</param>
    /// <param name="value">The value to append.</param>
    public void Align(int alignment, byte value)
    {
        var offset = BinaryUtils.GetOffset(Position, alignment);
        if (offset > 0) Pad(offset, value);
    }

    #region Write(byte[])
    /// <summary>
    /// Writes the given bytes to the stream.
    /// </summary>
    /// <param name="value">The bytes to write.</param>
    public void Write(byte[] value)
    {
        if (value.Length == 0) return;
        BaseStream.Write(value);
    }

    /// <summary>
    /// Writes the given buffer to the stream.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    /// <param name="offset">The offset into the buffer from where to start.</param>
    /// <param name="count">The number of bytes to write.</param>
    public void Write(byte[] buffer, int offset, int count)
    {
        if (buffer.Length == 0) return;
        BaseStream.Write(buffer, offset, count);
    }

    /// <summary>
    /// Writes the given buffer to the stream.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    public void Write(ReadOnlySpan<byte> buffer)
    {
        if (buffer.Length == 0) return;
        BaseStream.Write(buffer);
    }

    /// <summary>
    /// Writes the given bytes to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The bytes to write.</param>
    public void WriteAt(long position, byte[] value)
    {
        Position = position;
        Write(value);
    }

    /// <summary>
    /// Writes the given buffer to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="buffer">The buffer to read from.</param>
    /// <param name="offset">The offset into the buffer from where to start.</param>
    /// <param name="count">The number of bytes to write.</param>
    public void WriteAt(long position, byte[] buffer, int offset, int count)
    {
        Position = position;
        Write(buffer);
    }

    /// <summary>
    /// Writes the given buffer to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="buffer">The buffer to read from.</param>
    public void WriteAt(long position, ReadOnlySpan<byte> buffer)
    {
        Position = position;
        Write(buffer);
    }
    #endregion

    #region Write(sbyte)
    /// <summary>
    /// Writes a given <see cref="sbyte"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(sbyte value) => BaseStream.WriteByte((byte) value);

    /// <summary>
    /// Writes a given <see cref="sbyte"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, sbyte value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(byte)
    /// <summary>
    /// Writes a given <see cref="byte"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(byte value) => BaseStream.WriteByte(value);

    /// <summary>
    /// Writes a given <see cref="byte"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, byte value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(short)
    /// <summary>
    /// Writes a given <see cref="short"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(short value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 2);
    }

    /// <summary>
    /// Writes a given <see cref="short"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, short value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(ushort)
    /// <summary>
    /// Writes a given <see cref="ushort"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(ushort value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 2);
    }

    /// <summary>
    /// Writes a given <see cref="ushort"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, ushort value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(int)
    /// <summary>
    /// Writes a given <see cref="int"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(int value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 4);
    }

    /// <summary>
    /// Writes a given <see cref="int"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, int value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(uint)
    /// <summary>
    /// Writes a given <see cref="uint"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(uint value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 4);
    }

    /// <summary>
    /// Writes a given <see cref="uint"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, uint value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(long)
    /// <summary>
    /// Writes a given <see cref="long"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(long value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 8);
    }

    /// <summary>
    /// Writes a given <see cref="long"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, long value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(ulong)
    /// <summary>
    /// Writes a given <see cref="ulong"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(ulong value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 8);
    }

    /// <summary>
    /// Writes a given <see cref="ulong"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, ulong value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(float)
    /// <summary>
    /// Writes a given <see cref="float"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(float value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 4);
    }

    /// <summary>
    /// Writes a given <see cref="float"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, float value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(double)
    /// <summary>
    /// Writes a given <see cref="double"/> value to the stream.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(double value)
    {
        _writer.Write(_buffer, value);
        BaseStream.Write(_buffer, 0, 8);
    }

    /// <summary>
    /// Writes a given <see cref="double"/> value to the stream at a given position.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, double value)
    {
        Position = position;
        Write(value);
    }
    #endregion

    #region Write(string)
    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream using <see cref="Encoding.UTF8"/> as encoding.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void Write(string value) => Write(value, Encoding.UTF8);

    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream using the given encoding.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void Write(string value, Encoding encoding) => BaseStream.Write(encoding.GetBytes(value));

    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream at a given position using <see cref="Encoding.UTF8"/> as encoding.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteAt(long position, string value)
    {
        Position = position;
        Write(value, Encoding.UTF8);
    }

    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream at a given position using the given encoding.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void WriteAt(long position, string value, Encoding encoding)
    {
        Position = position;
        Write(value, encoding);
    }
    #endregion

    #region WriteTerminated(string)
    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream using <see cref="Encoding.UTF8"/> as encoding.
    /// A null-byte is appended at the end of the string.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteTerminated(string value) => Write(value + "\0", Encoding.UTF8);

    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream using the given encoding.
    /// A null-byte is appended at the end of the string.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void WriteTerminated(string value, Encoding encoding) => Write(value + "\0", encoding);

    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream at a given position using <see cref="Encoding.UTF8"/> as encoding.
    /// A null-byte is appended at the end of the string.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    public void WriteTerminatedAt(long position, string value)
    {
        Position = position;
        WriteTerminated(value, Encoding.UTF8);
    }

    /// <summary>
    /// Writes a given <see cref="string"/> value to the stream at a given position using the given encoding.
    /// A null-byte is appended at the end of the string.
    /// </summary>
    /// <param name="position">The absolute position in the stream to start writing at.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="encoding">The encoding to use.</param>
    public void WriteTerminatedAt(long position, string value, Encoding encoding)
    {
        Position = position;
        WriteTerminated(value, encoding);
    }
    #endregion
    #endregion

    #region IDisposable interface
    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        if (!_leaveOpen) BaseStream.Dispose();
        _disposed = true;
    }
    #endregion

    #region helper classes
    private interface IBinaryWriter
    {
        public void Write(Span<byte> buffer, short value);
        public void Write(Span<byte> buffer, ushort value);
        public void Write(Span<byte> buffer, int value);
        public void Write(Span<byte> buffer, uint value);
        public void Write(Span<byte> buffer, long value);
        public void Write(Span<byte> buffer, ulong value);
        public void Write(Span<byte> buffer, float value);
        public void Write(Span<byte> buffer, double value);
    }

    private class LittleEndianBinaryWriter : IBinaryWriter
    {
        public void Write(Span<byte> buffer, short value) => BinaryPrimitives.WriteInt16LittleEndian(buffer, value);

        public void Write(Span<byte> buffer, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);

        public void Write(Span<byte> buffer, int value) => BinaryPrimitives.WriteInt32LittleEndian(buffer, value);

        public void Write(Span<byte> buffer, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);

        public void Write(Span<byte> buffer, long value) => BinaryPrimitives.WriteInt64LittleEndian(buffer, value);

        public void Write(Span<byte> buffer, ulong value) => BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);

        public void Write(Span<byte> buffer, float value)
        {
            #if NET5_0_OR_GREATER
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            #else
            var tmpValue = BitConverter.SingleToInt32Bits(value);
            BinaryPrimitives.WriteInt64LittleEndian(buffer, tmpValue);
            #endif
        }

        public void Write(Span<byte> buffer, double value)
        {
            #if NET5_0_OR_GREATER
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            #else
            var tmpValue = BitConverter.DoubleToInt64Bits(value);
            BinaryPrimitives.WriteInt64LittleEndian(buffer, tmpValue);
            #endif
        }
    }

    private class BigEndianBinaryWriter : IBinaryWriter
    {
        public void Write(Span<byte> buffer, short value) => BinaryPrimitives.WriteInt16BigEndian(buffer, value);

        public void Write(Span<byte> buffer, ushort value) => BinaryPrimitives.WriteUInt16BigEndian(buffer, value);

        public void Write(Span<byte> buffer, int value) => BinaryPrimitives.WriteInt32BigEndian(buffer, value);

        public void Write(Span<byte> buffer, uint value) => BinaryPrimitives.WriteUInt32BigEndian(buffer, value);

        public void Write(Span<byte> buffer, long value) => BinaryPrimitives.WriteInt64BigEndian(buffer, value);

        public void Write(Span<byte> buffer, ulong value) => BinaryPrimitives.WriteUInt64BigEndian(buffer, value);

        public void Write(Span<byte> buffer, float value)
        {
            #if NET5_0_OR_GREATER
            BinaryPrimitives.WriteSingleBigEndian(buffer, value);
            #else
            var tmpValue = BitConverter.SingleToInt32Bits(value);
            BinaryPrimitives.WriteInt32BigEndian(buffer, tmpValue);
            #endif
        }

        public void Write(Span<byte> buffer, double value)
        {
            #if NET5_0_OR_GREATER
            BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
            #else
            var tmpValue = BitConverter.DoubleToInt64Bits(value);
            BinaryPrimitives.WriteInt64BigEndian(buffer, tmpValue);
            #endif
        }
    }
    #endregion
}