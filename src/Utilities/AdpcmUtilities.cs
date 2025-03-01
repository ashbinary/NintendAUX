using System;

namespace NintendAUX.Utilities;

public static class AdpcmUtilities
{
    public static sbyte[] NibbleToSbyte => [0, 1, 2, 3, 4, 5, 6, 7, -8, -7, -6, -5, -4, -3, -2, -1];
    
    public static int DivideByRoundUp(this int value, int divisor)
    {
        return (int) Math.Ceiling((double) value / divisor);
    }
    
    public static byte GetHighNibble(byte value) => (byte) (value >> 4 & 15);

    public static byte GetLowNibble(byte value) => (byte) (value & 15U);
}
