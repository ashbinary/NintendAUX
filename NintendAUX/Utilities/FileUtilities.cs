using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NintendAUX.Filetypes;

namespace NintendAUX.Utilities;

public static class FileUtilities
{
    public static T ReadStruct<T>(this FileReader fileReader) where T : struct
    {
        return MemoryMarshal.AsRef<T>(fileReader.ReadBytes(Unsafe.SizeOf<T>()));
    }

    public static void WriteStruct<T>(this FileWriter fileWriter, T value) where T : struct
    {
        fileWriter.Write(MemoryMarshal.AsBytes(new Span<T>(ref value)));
    }
}