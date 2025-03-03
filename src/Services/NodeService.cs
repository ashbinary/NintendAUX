using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using NintendAUX.Models;
using NintendAUX.Services.Entry;
using NintendAUX.ViewModels;

namespace NintendAUX.Services;

public class NodeService
{
    public static async Task<BarsFile.BarsEntry> CreateNewEntry()
    {
        var bametaData = await EntryCreateService.CreateBameta();
        var bwavData = await EntryCreateService.CreateBwav();

        if (bametaData.Info.Magic != 1096043841 || bwavData.Header.Magic != 1447122754)
            return new BarsFile.BarsEntry { BamtaOffset = 0xDEADBEEF }; //null check!

        var newEntry = new BarsFile.BarsEntry();
        newEntry.Bamta = bametaData;
        newEntry.Bwav = bwavData;

        return newEntry;
    }

    private static ObservableCollection<Node> CreateChannelNodes(
        BwavFile.ResBwavChannelInfo[] channelInfoArray, uint isParentPrefetch)
    {
        var channels = new ObservableCollection<Node>();

        int currentChannel = 0;
        for (var i = 0; i < channelInfoArray.Length; i++)
        {
            if (channelInfoArray[i].ChannelPan == BwavFile.BwavChannelPan.Left &&
                channelInfoArray[i + 1].ChannelPan == BwavFile.BwavChannelPan.Right)
            {
                channels.Add(new BWAVStereoChannelNode(currentChannel, i, channelInfoArray[i..(i+2)], isParentPrefetch != 0));
                i++;
            }
            else
            {
                channels.Add(new BWAVChannelNode(currentChannel, channelInfoArray[i], isParentPrefetch != 0));
            }

            currentChannel++;
        }

        return channels;
    }

    private static void UpdateBarsNodes(BarsFile barsFile, ObservableCollection<Node> nodes)
    {
        nodes.Clear();

        var sortedEntries = ViewModelLocator.Model.SortNodes
            ? barsFile.EntryArray.OrderBy(path => path.Bamta.Path).ToList()
            : barsFile.EntryArray;

        for (var i = 0; i < sortedEntries.Count; i++)
        {
            var channels = CreateChannelNodes(sortedEntries[i].Bwav.ChannelInfoArray, sortedEntries[i].Bwav.Header.IsPrefetch);
            nodes.Add(new BARSEntryNode(sortedEntries[i].Bamta.Path, i, new ObservableCollection<Node>
            {
                new AMTANode(i, sortedEntries[i].Bamta.Info),
                new BWAVNode("Song (BWAV)", i, sortedEntries[i].Bwav.Header, channels, ViewModelLocator.Model.InputType)
            }, sortedEntries[i]));
        }
    }

    private static void UpdateBwavNodes(BwavFile bwavFile, ObservableCollection<Node> nodes, string fileName)
    {
        nodes.Clear();
        var channels = CreateChannelNodes(bwavFile.ChannelInfoArray, bwavFile.Header.IsPrefetch);
        nodes.Add(new BWAVNode(Path.GetFileNameWithoutExtension(fileName), 0, bwavFile.Header, channels, ViewModelLocator.Model.InputType));
    }

    public static void UpdateNodeArray()
    {
        var model = ViewModelLocator.Model;

        switch (model.InputType)
        {
            case InputFileType.Bars:
                UpdateBarsNodes(model.InputFile.AsBarsFile(), model.Nodes);
                break;

            case InputFileType.Bwav:
                UpdateBwavNodes(model.InputFile.AsBwavFile(), model.Nodes, model.InputFileName);
                break;

            default:
                new ArgumentException($"Unsupported input type: {model.InputType}").CreateExceptionDialog();
                break;
        }

        model.FileLoaded = true;
        model.ArchiveLoaded = model.InputType == InputFileType.Bars;
    }
}