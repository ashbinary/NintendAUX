namespace StutteredBars;

using StutteredBars.Filetypes;

class Program
{
    public static void Main(string[] args)
    {
        BARSFile bars = new BARSFile(File.ReadAllBytes("BgmVersusFest_SAND.bars"));
        AMTAFile amta = new AMTAFile(File.ReadAllBytes("BGM_Versus_Fes_SAND_3Idol.bameta"));
        BWAVFile bwav = new BWAVFile(File.ReadAllBytes("BGM_Versus_Fes_SAND_3Idol.bwav"));

        foreach (AMTAFile amtaList in bars.Metadata)
        {
            if (amtaList.Info.MarkerOffset != 0)
                Console.WriteLine(amtaList.Path);         
        }

        Console.WriteLine("complete");
    }
}