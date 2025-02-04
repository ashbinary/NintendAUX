namespace StutteredBars;
class Program
{
    public static void Main(string[] args)
    {
        BARSParser parser = new BARSParser();
        BARSFile barsFile = parser.ReadBARSFile("Fart.bars");

        for (int i = 0; i < barsFile.FileDataHash.Length; i++)
        {
            if (Array.IndexOf(barsFile.ReserveData.FileHashes, barsFile.FileDataHash[i]) != -1) continue;

            Console.WriteLine(barsFile.AmtaList[i].Path);
        }

        byte[] octaLastBoss = File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bwav");
        byte[] octaLastBameta = File.ReadAllBytes("BGM_Jukebox_Blitz_Octa_LastBoss.bameta");

        Array.Resize(ref barsFile.BwavList, barsFile.BwavList.Length + 1);
        barsFile.BwavList[^1] = new BARSFile.Bwav() { Data = octaLastBoss };

        Array.Resize(ref barsFile.AmtaData, barsFile.AmtaData.Length + 1);
        barsFile.AmtaData[^1] = new BARSFile.Amta() { Data = octaLastBameta };

        Array.Resize(ref barsFile.AmtaList, barsFile.AmtaList.Length + 1);
        barsFile.AmtaList[^1] = new BARSFile.AmtaDetail();

        barsFile.Header.FileCount += 1;

        // To do: Allow proper amta porting lol
        barsFile.AmtaList[^1].Path = "BGM_Jukebox_Blitz_Octa_LastBoss";
        
        BARSSaver saver = new BARSSaver();
        byte[] barsData = saver.SaveBARSFile(barsFile);
        File.WriteAllBytes("BgmFestSandNew2.bin", barsData);
    }
}