using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using BARSBundler.Core.Filetypes;
using BARSBundler.Models;
using BARSBundler.ViewModels;

namespace BARSBundler.Services;

public class NodeService
{
    public static ObservableCollection<Node> ReloadNode(ObservableCollection<Node> Nodes, ref BARSFile barsData, bool sortNodes = false)
    {
        Nodes.Clear();
        
        // Re-order here to sort effectively
        if (sortNodes)
        {
            barsData.EntryArray = barsData.EntryArray.OrderBy(path => path.Bamta.Path).ToArray();
        }

        int entryIndex = 0;
        foreach (BARSFile.BarsEntry file in barsData.EntryArray)
        {
            Nodes.Add(new BARSEntryNode(file.Bamta.Path, entryIndex, new ObservableCollection<Node>()
            {
                new AMTANode("Metadata (AMTA)", entryIndex),
                new BWAVNode("Song (BWAV)", entryIndex)
            }));
            entryIndex++;
        }

        return Nodes;
    }
    
    public static async Task<BARSFile.BarsEntry> CreateNewEntry()
    {
        AMTAFile bametaData = await BarsEntryService.CreateBameta();
        BWAVFile bwavData = await BarsEntryService.CreateBwav();

        if (bametaData.Info.Magic != 1096043841 || bwavData.Header.Magic != 1447122754) 
            return new BARSFile.BarsEntry() { BamtaOffset = 0xDEADBEEF }; //null check!

        BARSFile.BarsEntry newEntry = new BARSFile.BarsEntry();
        newEntry.Bamta = bametaData;
        newEntry.Bwav = bwavData;

        return newEntry;
    }
}