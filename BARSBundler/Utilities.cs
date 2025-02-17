using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BARSBundler.Parsers.SARC;

namespace BARSBundler;

public static class Utilities
{
    public static byte[] ReadExactly(this System.IO.Stream stream, int count)
    {
        byte[] buffer = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buffer, offset, count - offset);
            if (read == 0)
                throw new System.IO.EndOfStreamException();
            offset += read;
        }
        System.Diagnostics.Debug.Assert(offset == count);
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
    
    public static uint Hash(string str, uint key)
    {
        var bytes = Encoding.UTF8.GetBytes(str);

        uint hash = 0;
        foreach (var b in bytes)
        {
            hash = hash * key + b;
        }

        return hash;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToHexString(this byte[]? data) => data.ToHexString(false);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToHexString(this byte[]? data, bool prefix)
    {
        if (data is null) return string.Empty;

        #if NET5_0_OR_GREATER
        var result = Convert.ToHexString(data);
        #else
        var result = BitConverter.ToString(data).Replace("-", string.Empty);
        #endif

        if (prefix && result.Length > 0) return "0x" + result;
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(int length, int alignment) => (-length % alignment + alignment) % alignment;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(long length, int alignment) => ((int) (-length % alignment) + alignment) % alignment;
    
    public static int GetSarcFileIndex(this SarcFile sarc, string path)
    {
        for (int i = 0; i < sarc.Files.Count; i++)
        {
            if (sarc.Files[i].Name == path)
                return i;
        }

        return -1; // Unable to find file in SARC.
    }

    public static byte[] BWAVHeader => [0x42, 0x57, 0x41, 0x56];
    public static byte[] BARSHeader => [0x42, 0x41, 0x52, 0x53];
    public static byte[] AMTAHeader => [0x41, 0x4D, 0x54, 0x41];
    public static byte[] SARCHeader => [0x53, 0x41, 0x52, 0x43];
    public static byte[] ZSDicHeader => [0x37, 0xA4, 0x30, 0xEC];
    
    public static void RemoveAt<T>(ref T[] arr, int index)
    {
        for (int a = index; a < arr.Length - 1; a++)
        {
            // moving elements downwards, to fill the gap at [index]
            arr[a] = arr[a + 1];
        }
        // finally, let's decrement Array's size by one
        Array.Resize(ref arr, arr.Length - 1);
    }
    
    public static void AddToEnd<T>(ref T[] arr, T item)
    {
        Array.Resize(ref arr, arr.Length + 1); // Increase size by 1

        arr[arr.Length - 1] = item; // Insert new item at the given index
    }
}