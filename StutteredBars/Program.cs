namespace StutteredBars;

using StutteredBars.Filetypes;

class Program
{
    public static void Main(string[] args)
    {
        BARSFile bars = new BARSFile(File.ReadAllBytes("BgmLobbyVersus.Product.920.bars"));
        AMTAFile amta = new AMTAFile(File.ReadAllBytes("BGM_LobbyVersus_Gambit_SquidSquad_04.bameta"));
        BWAVFile bwav = new BWAVFile(File.ReadAllBytes("BGM_Versus_Fes_SAND_3Idol.bwav"));

        File.WriteAllBytes("BGM_Versus_Fes_SAND_3Idol_New.bwav", BWAVFile.Save(bwav));
        File.WriteAllBytes("BGM_LobbyVersus_Gambit_SquidSquad_04_New.bameta", AMTAFile.Save(amta));
        Console.WriteLine("complete");
    }
}