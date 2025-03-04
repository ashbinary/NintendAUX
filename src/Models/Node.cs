using System.Collections.ObjectModel;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;

namespace NintendAUX.Models;

public class Node
{
    // Base for other node classes, should not be used outside of here
    protected Node(string title, int id)
    {
        Title = title;
        ID = id;
    }

    public Node()
    {
        Title = string.Empty;
        ID = 0;
    }

    public string Title { get; protected set; }
    public int ID { get; protected set; }
}

public class AMTANode : Node
{
    public AmtaNodeData Data;

    public AMTANode(int id, AmtaFile.AMTAInfo info)
        : base("Metadata (AMTA)", id)
    {
        Data = new AmtaNodeData(info);
    }
}

public class BWAVNode : Node
{
    public BwavNodeData Data;

    public BWAVNode(string title, int id, BwavFile.BwavHeader header, ObservableCollection<Node> channels,
        InputFileType ctx)
        : base(title, id)
    {
        Data = new BwavNodeData(header);
        Channels = channels;
        InBarsFile = ctx == InputFileType.Bars ? true : false;
    }

    public bool InBarsFile { get; }

    public ObservableCollection<Node>? Channels { get; }
}

public class BWAVChannelNode : Node
{
    public BwavChannelNodeData Data;

    public BWAVChannelNode(int id, BwavFile.ResBwavChannelInfo channelInfo, bool isParentPrefetch)
        : base(string.Empty, id)
    {
        Title = $"Channel #{id + 1} ({channelInfo.ChannelPan})";
        Data = new BwavChannelNodeData(channelInfo, isParentPrefetch);
    }

    public BWAVChannelNode(string title, int id, BwavFile.ResBwavChannelInfo channelInfo, bool isParentPrefetch)
        : base(title, id)
    {
        Data = new BwavChannelNodeData(channelInfo, isParentPrefetch);
    }
}

public class BWAVStereoChannelNode : Node
{
    public BWAVStereoChannelNode(int stereoId, int channelId, BwavFile.ResBwavChannelInfo[] channelInfo,
        bool isParentPrefetch)
    {
        Title = $"Stereo Channel #{stereoId + 1}";
        ID = channelId;
        Channels = new ObservableCollection<BWAVChannelNode>();

        for (var i = 0; i < channelInfo.Length; i++)
            Channels.Add(new BWAVChannelNode($"{channelInfo[i].ChannelPan} Channel", channelId + i, channelInfo[i],
                isParentPrefetch));
    }

    public ObservableCollection<BWAVChannelNode>? Channels { get; }
}

public class BARSEntryNode : Node
{
    public BarsEntryNodeData Data;

    public BARSEntryNode(string title, int id, ObservableCollection<Node>? subNodes, BarsFile.BarsEntry entry)
        : base(title, id)
    {
        Data = new BarsEntryNodeData(entry);
        SubNodes = subNodes ?? new ObservableCollection<Node>();
    }

    public ObservableCollection<Node>? SubNodes { get; }
}