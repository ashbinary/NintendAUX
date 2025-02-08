namespace StutteredBars.Frontend.Models;

using System.Collections.ObjectModel;

public class Node
{
    public string Title { get; }
    public NodeType Type { get; }
    public int ID { get; }

    public Node() // never should be created
    {
        Title = string.Empty;
        Type = NodeType.Unknown;
        ID = 0;
    }
}

public enum NodeType
{
    BARSRoot,
    BARSEntry,
    BWAV,
    AMTA,
    Unknown
}

public class AMTANode : Node
{
    //public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public int ID { get; }
    public NodeType Type { get; }
    
    public AMTANode(string title, int id)
    {
        Title = "Metadata (AMTA)";
        ID = id;
        Type = NodeType.AMTA;
    }
}

public class BWAVNode : Node
{
    //public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public int ID { get; }
    public NodeType Type { get; }
    
    public BWAVNode(string title, int id)
    {
        Title = title;
        ID = id;
        Type = NodeType.BWAV;
    }
}

public class BARSEntryNode : Node
{
    public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public int ID { get; }
    public NodeType Type { get; }
    
    public BARSEntryNode(string title, int id, ObservableCollection<Node>? subNodes)
    {
        Title = title;
        ID = id;
        Type = NodeType.BARSEntry;
        SubNodes = subNodes;
    }
}


