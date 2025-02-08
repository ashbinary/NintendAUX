namespace StutteredBars.Frontend;

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

    public static byte[] BWAVHeader => [0x42, 0x57, 0x41, 0x56];
    public static byte[] BARSHeader => [0x42, 0x41, 0x72, 0x73];
    public static byte[] AMTAHeader => [0x41, 0x4D, 0x54, 0x41];
}