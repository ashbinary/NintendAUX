namespace StutteredBars;

using StutteredBars.Filetypes;

class Program
{
    public static void Main(string[] args)
    {
        BARSFile bars = new BARSFile(File.ReadAllBytes("BgmLobbyVersus.Product.920.bars"));
        AMTAFile amta = new AMTAFile(File.ReadAllBytes("BGM_Versus_Fes_SAND_3Idol.bameta"));
        BWAVFile bwav = new BWAVFile(File.ReadAllBytes("BGM_Versus_Fes_SAND_3Idol.bwav"));

        foreach (AMTAFile amtaList in bars.Metadata)
        {
            if (amtaList.Info.TagOffset != 0)
                Console.WriteLine(amtaList.Path);         
        }

        byte[] bwavSaved = BWAVFile.Save(bwav);
        File.WriteAllBytes("BGM_Versus_Fes_SAND_3Idol_New.bwav", bwavSaved);
        Console.WriteLine("complete");
    }
}