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
        BARSFile barsResaved = new BARSFile(File.ReadAllBytes("BgmVersusFest_SAND_Resaved.bars"));

        var serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

        File.WriteAllText("Bars.yaml", serializer.Serialize(bars));
        File.WriteAllText("BarsResaved.yaml", serializer.Serialize(barsResaved));

        BWAVFile lastBossBwav = new BWAVFile(File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bwav"));
        File.WriteAllBytes("BGM_Jukebox_BlitzOLB_Resave.bwav", BWAVFile.Save(lastBossBwav));

        AMTAFile lastBossAmta = new AMTAFile(File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bameta"));
        File.WriteAllBytes("BGM_Jukebox_BlitzOLB_Resave.bameta", AMTAFile.Save(lastBossAmta));
        //AMTAFile lastbossAmta = new AMTAFile(File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bameta"));

        ResizeAndAdd(ref bars.Tracks, lastBossBwav);
        ResizeAndAdd(ref bars.Metadata, lastBossAmta);

        File.WriteAllBytes("BgmVersusFest_SAND_Resaved.bars", BARSFile.SoftSave(bars));
    }

    public static void ResizeAndAdd<T>(ref T[] array, T data)
    {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = data;
    }
}