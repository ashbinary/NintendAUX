using System.IO;
using NintendAUX.Filetypes.Generic;
using NintendAUX.Parsers.SARC;

namespace NintendAUX.Filetypes.Compression;

public static class ZSDic
{
    public static byte[] ZSDicFile; // Statically store the ZsDic here because only one needs to exist at any point

    public static void LoadDictionary(byte[] zsdicData)
    {
        if (MiscUtilities.CheckMagic(ref zsdicData, InputFileType.ZsDic))
        {
            ZSDicFile = zsdicData;
        }
        else
        {
            if (!MiscUtilities.CheckMagic(ref zsdicData, InputFileType.Sarc))
                zsdicData = zsdicData.DecompressZSTDBytes(false);

            var parser = new SarcFileParser();
            var zsdicPack = parser.Parse(new MemoryStream(zsdicData));

            ZSDicFile = zsdicPack.Files[zsdicPack.GetSarcFileIndex("zs.zsdic")].Data;
        }
    }
}