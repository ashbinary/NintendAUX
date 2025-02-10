using System.IO;
using System.Linq;
using StutteredBars.Frontend.Parsers;
using StutteredBars.Frontend.Parsers.SARC;

namespace StutteredBars.Frontend.Compression;

public static class ZSDic
{
    public static byte[] ZSDicFile;

    public static void LoadDictionary(byte[] zsdicData)
    {
        if (zsdicData.Take(4).ToArray().SequenceEqual(Utilities.ZSDicHeader))
        {
            ZSDicFile = zsdicData;
        }
        else
        {
            if (!zsdicData.Take(4).ToArray().SequenceEqual(Utilities.SARCHeader))
            {
                zsdicData = ZSTDUtils.DecompressZSTDBytes(zsdicData, false);
            }
            
            SarcFileParser parser = new SarcFileParser();
            SarcFile zsdicPack = parser.Parse(new MemoryStream(zsdicData));

            ZSDicFile = zsdicPack.Files[zsdicPack.GetSarcFileIndex("zs.zsdic")].Data;   
        }
    }
    
}