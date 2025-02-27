using System;
using System.IO;
using ZstdSharp;

namespace NintendAUX.Filetypes.Compression;

public static class ZSTDUtils
{
    public static Span<byte> CompressZSTD(this byte[] data, bool usesDict = false)
    {
        using Compressor compressor = new(19);
        if (usesDict) compressor.LoadDictionary(ZSDic.ZSDicFile);
        return compressor.Wrap(data);
    }

    public static Span<byte> DecompressZSTD(this byte[] data, bool usesDict = true)
    {
        using Decompressor decompressor = new();
        if (usesDict) decompressor.LoadDictionary(ZSDic.ZSDicFile);
        return decompressor.Unwrap(data);
    }

    public static byte[] DecompressZSTDBytes(this byte[] data, bool usesDict)
    {
        return DecompressZSTD(data).ToArray();
    }

    public static byte[] CompressZSTDBytes(this byte[] data, bool usesDict)
    {
        return CompressZSTD(data).ToArray();
    }

    public static byte[] DecompressZSTDStream(this Stream data, bool usesDict)
    {
        return DecompressZSTD(data.ReadAllBytes()).ToArray();
    }

    public static byte[] CompressZSTDStream(this Stream data, bool usesDict)
    {
        return DecompressZSTD(data.ReadAllBytes()).ToArray();
    }

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