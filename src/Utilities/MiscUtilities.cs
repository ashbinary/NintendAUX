using System;
using System.Runtime.CompilerServices;
using System.Text;
using NintendAUX.Filetypes.Generic;
using NintendAUX.Parsers.SARC;

namespace NintendAUX;

public static class MiscUtilities
{
    private static byte[] BWAVHeader => [0x42, 0x57, 0x41, 0x56];
    private static byte[] BARSHeader => [0x42, 0x41, 0x52, 0x53];
    private static byte[] AMTAHeader => [0x41, 0x4D, 0x54, 0x41];
    private static byte[] SARCHeader => [0x53, 0x41, 0x52, 0x43];
    private static byte[] ZSDicHeader => [0x37, 0xA4, 0x30, 0xEC];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToHexString(this byte[]? data)
    {
        return data.ToHexString(false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToHexString(this byte[]? data, bool prefix)
    {
        if (data is null) return string.Empty;
        var result = Convert.ToHexString(data);

        if (prefix && result.Length > 0) return "0x" + result;
        return result;
    }

    public static int GetSarcFileIndex(this SarcFile sarc, string path)
    {
        for (var i = 0; i < sarc.Files.Count; i++)
            if (sarc.Files[i].Name == path)
                return i;

        return -1; // Unable to find file in SARC.
    }

    public static bool CheckMagic(ref byte[] data, InputFileType fileType)
    {
        if (data.Length < 4) return false;

        ReadOnlySpan<byte> magic = data.AsSpan(0, 4);

        return fileType switch
        {
            InputFileType.Bwav => magic.SequenceEqual(BWAVHeader),
            InputFileType.Bars => magic.SequenceEqual(BARSHeader),
            InputFileType.Sarc => magic.SequenceEqual(SARCHeader),
            InputFileType.ZsDic => magic.SequenceEqual(ZSDicHeader),
            _ => false
        };
    }

    public static byte[] ToByteArray(this short[] shortArray)
    {
        if (shortArray == null)
            throw new ArgumentNullException(nameof(shortArray));

        var byteArray = new byte[shortArray.Length * sizeof(short)];
        Buffer.BlockCopy(shortArray, 0, byteArray, 0, byteArray.Length);
        return byteArray;
    }

    public static bool CheckMagic(uint magic, InputFileType fileType)
    {
        return magic switch
        {
            1447122754 => fileType == InputFileType.Bwav,
            1096043841 => fileType == InputFileType.Amta,
            _ => false
        };
    }
}

internal static class EncodingExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMinByteCount(this Encoding encoding)
    {
        return encoding.GetByteCount("\0");
    }
}