using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BARSBundler.Core.Helpers;

internal static class BinaryUtils
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(int length, int alignment) => (-length % alignment + alignment) % alignment;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(long length, int alignment) => ((int) (-length % alignment) + alignment) % alignment;
}

internal static class EncodingExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMinByteCount(this Encoding encoding) => encoding.GetByteCount("\0");
}

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