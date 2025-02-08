namespace StutteredBars.Frontend.Models;

using System.Collections.ObjectModel;

public interface Node
{
    public string Title { get; }
    public NodeType Type { get; }
}

public enum NodeType
{
    BARSRoot,
    BARSEntry,
    BWAV,
    AMTA
}

public class AMTANode : Node
{
    //public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public NodeType Type { get; }
    
    public AMTANode(string title)
    {
        Title = "Metadata (AMTA)";
        Type = NodeType.AMTA;
    }
}

public class BWAVNode : Node
{
    //public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public NodeType Type { get; }
    
    public BWAVNode(string title)
    {
        Title = title;
        Type = NodeType.BWAV;
    }
}

public class BARSEntryNode : Node
{
    public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public NodeType Type { get; }
    
    public BARSEntryNode(string title, ObservableCollection<Node>? subNodes)
    {
        Title = title;
        Type = NodeType.BARSEntry;
        SubNodes = subNodes;
    }
}

public class BARSRootNode : Node
{
    public ObservableCollection<Node>? SubNodes { get; }
    public string Title { get; }
    public NodeType Type { get; }
    
    public BARSRootNode(string title, ObservableCollection<Node>? subNodes)
    {
        Title = title;
        Type = NodeType.BARSRoot;
        SubNodes = subNodes;
    }
}

