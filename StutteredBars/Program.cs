namespace StutteredBars;

using StutteredBars.Filetypes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Loading BARS file");
        BARSFile bars = new BARSFile(File.ReadAllBytes("BgmVersusFest_SAND.bars"));

        BWAVFile lastBossBwav = new BWAVFile(File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bwav"));
        AMTAFile lastbossAmta = new AMTAFile(File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bameta"));

        ResizeAndAdd(ref bars.Tracks, lastBossBwav);
        ResizeAndAdd(ref bars.Metadata, lastbossAmta);

        File.WriteAllBytes("BgmVersusFest_SAND_Gamblitz.bars", BARSFile.SoftSave(bars));
    }

    public static void ResizeAndAdd<T>(ref T[] array, T data)
    {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = data;
    }
}