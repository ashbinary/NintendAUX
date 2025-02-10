using BARSBundler.Core.Filetypes;

namespace BARSBundler.Core;

using Core.Filetypes;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Loading BARS file");
        AMTAFile bars = new AMTAFile(File.ReadAllBytes("C:\\Users\\Ash\\Documents\\Data\\BGM_Versus_Fes_Thunder_05\\BGM_Versus_Fes_Thunder_05_Resave.bameta"));
        Console.WriteLine("Idk if imma see you again");
        //File.WriteAllBytes("C:\\Users\\Ash\\Documents\\Data\\BGM_Versus_Fes_Thunder_05\\BGM_Versus_Fes_Thunder_05_Resave.bameta", AMTAFile.Save(bars));
    }

    public static void ResizeAndAdd<T>(ref T[] array, T data)
    {
        Array.Resize(ref array, array.Length + 1);
        array[array.Length - 1] = data;
    }
}