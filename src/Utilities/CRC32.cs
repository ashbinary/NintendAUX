using System.Text;

public class CRC32
{
    private static readonly uint[] Table = GenerateTable();

    private static uint[] GenerateTable()
    {
        var table = new uint[256];
        const uint polynomial = 0xEDB88320;

        for (uint i = 0; i < 256; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++) crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            table[i] = crc;
        }

        return table;
    }

    public static uint Compute(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var crc = 0xFFFFFFFF;

        foreach (var b in bytes) crc = (crc >> 8) ^ Table[(crc ^ b) & 0xFF];

        return ~crc; // Convert to signed int
    }
}