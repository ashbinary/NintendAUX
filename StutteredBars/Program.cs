using StutteredBars.Filetypes;

namespace StutteredBars;
class Program
{
    public static void Main(string[] args)
    {
        BARSFile bars = new BARSFile(File.ReadAllBytes("BgmVersusFest_SAND.bars"));
        Console.WriteLine("complete");
    }
}