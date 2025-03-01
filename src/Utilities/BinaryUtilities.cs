using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using NintendAUX.ViewModels;

namespace NintendAUX.Utilities;

public static class BinaryUtilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(long length, int alignment)
    {
        return ((int)(-length % alignment) + alignment) % alignment;
    }

    public static byte[] ReadExactly(this Stream stream, int count)
    {
        var buffer = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                new EndOfStreamException().CreateExceptionDialog();
            offset += read;
        }

        Debug.Assert(offset == count);
        return buffer;
    }

    public static int BinarySearch<T, K>(ReadOnlySpan<T> arr, K v) where T : IComparable<K>
    {
        var start = 0;
        var end = arr.Length - 1;

        while (start <= end)
        {
            var mid = (start + end) / 2;
            var entry = arr[mid];
            var cmp = entry.CompareTo(v);

            if (cmp == 0)
                return mid;
            if (cmp > 0)
                end = mid - 1;
            else /* if (cmp < 0) */
                start = mid + 1;
        }

        return ~start;
    }
}