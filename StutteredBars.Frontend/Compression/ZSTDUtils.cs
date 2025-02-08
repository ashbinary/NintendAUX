﻿using System;
using ZstdSharp;
using System.IO;

namespace StutteredBars.Frontend.Compression;

public static class ZSTDUtils
{
    public static Span<byte> CompressZSTD(this byte[] data)
    {
        using Compressor compressor = new(19);
        return compressor.Wrap(data);
    }

    public static Span<byte> DecompressZSTD(this byte[] data)
    {
        using Decompressor decompressor = new();
        return decompressor.Unwrap(data);
    }
    
    public static byte[] DecompressZSTDBytes(this byte[] data) => DecompressZSTD(data).ToArray();
    public static byte[] CompressZSTDBytes(this byte[] data) => CompressZSTD(data).ToArray();
    
    public static byte[] DecompressZSTDStream(this Stream data) => DecompressZSTD(data.ReadAllBytes()).ToArray();
    public static byte[] CompressZSTDStream(this Stream data) => DecompressZSTD(data.ReadAllBytes()).ToArray();
    
    public static byte[] ReadAllBytes(this Stream inStream)
    {
        if (inStream is MemoryStream inMemoryStream)
            return inMemoryStream.ToArray();

        using (var outStream = new MemoryStream())
        {
            inStream.CopyTo(outStream);
            return outStream.ToArray();
        }
    }
}