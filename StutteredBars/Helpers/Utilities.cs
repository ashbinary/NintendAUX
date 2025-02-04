using System.Runtime.CompilerServices;
using System.Text;

namespace StutteredBars.Helpers;

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
