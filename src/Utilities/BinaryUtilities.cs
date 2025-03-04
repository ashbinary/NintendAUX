using System.Runtime.CompilerServices;

namespace NintendAUX.Utilities;

public static class BinaryUtilities
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetOffset(long length, int alignment)
    {
        return ((int)(-length % alignment) + alignment) % alignment;
    }
}