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
    public static async Task CreateNewEntry()
    {
        var bametaData = await EntryCreateService.CreateBameta();
        var bwavData = await EntryCreateService.CreateBwav();

        if (!MiscUtilities.CheckMagic(bametaData.Info.Magic, InputFileType.Amta) || 
            !MiscUtilities.CheckMagic(bwavData.Header.Magic, InputFileType.Bwav))
            return;

        var newEntry = new BarsFile.BarsEntry();
        newEntry.Bamta = bametaData;
        newEntry.Bwav = bwavData;

        ViewModelLocator.Model.InputFile.AsBarsFile().EntryArray.Add(newEntry);
        UpdateNodeArray();
    }

    private static ObservableCollection<Node> CreateChannelNodes(
        BwavFile.ResBwavChannelInfo[] channelInfoArray, uint isParentPrefetch)
    {
        var channels = new ObservableCollection<Node>();

        var currentChannel = 0;
        for (var i = 0; i < channelInfoArray.Length; i++)
        {
            if (channelInfoArray[i].ChannelPan == BwavFile.BwavChannelPan.Left &&
                channelInfoArray[i + 1].ChannelPan == BwavFile.BwavChannelPan.Right)
            {
                channels.Add(new BWAVStereoChannelNode(currentChannel, i, channelInfoArray[i..(i + 2)],
                    isParentPrefetch != 0));
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

        barsFile.EntryArray = ViewModelLocator.Model.SortNodes
            ? barsFile.EntryArray.OrderBy(path => path.Bamta.Path).ToList()
            : barsFile.EntryArray;

        for (var i = 0; i < barsFile.EntryArray.Count; i++)
        {
            var channels = CreateChannelNodes(barsFile.EntryArray[i].Bwav.ChannelInfoArray,
                barsFile.EntryArray[i].Bwav.Header.IsPrefetch);
            nodes.Add(new BARSEntryNode(barsFile.EntryArray[i].Bamta.Path, i, new ObservableCollection<Node>
            {
                new AMTANode(i, barsFile.EntryArray[i].Bamta.Info),
                new BWAVNode("Song (BWAV)", i, barsFile.EntryArray[i].Bwav.Header, channels, ViewModelLocator.Model.InputType)
            }, barsFile.EntryArray[i]));
        }
    }

    private static void UpdateBwavNodes(BwavFile bwavFile, ObservableCollection<Node> nodes, string fileName)
    {
        nodes.Clear();
        var channels = CreateChannelNodes(bwavFile.ChannelInfoArray, bwavFile.Header.IsPrefetch);
        nodes.Add(new BWAVNode(Path.GetFileNameWithoutExtension(fileName), 0, bwavFile.Header, channels,
            ViewModelLocator.Model.InputType));
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
                throw new ArgumentException($"Unsupported input type: {model.InputType}");
                break;
        }

        model.FileLoaded = true;
        model.ArchiveLoaded = model.InputType == InputFileType.Bars;
    }
}