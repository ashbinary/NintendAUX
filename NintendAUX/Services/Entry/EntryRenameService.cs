using System.Linq;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace NintendAUX.Services.Entry;

public class EntryRenameService
{
    public static TreeViewItem? GetTreeViewItemForNode(ref TreeView treeView, dynamic node)
    {
        return treeView.GetVisualDescendants()
            .OfType<TreeViewItem>()
            .FirstOrDefault(tvi => tvi.DataContext == node);
    }

    public static void SetTreeViewItemsEnabled(ref TreeView treeView, bool enabled)
    {
        foreach (var item in treeView.GetVisualDescendants().OfType<TreeViewItem>()) item.IsEnabled = enabled;
    }

    public static (TextBox textBox, TextBlock nameBlock) GetNodeControls(TreeViewItem item)
    {
        var stackPanel = item.GetLogicalChildren().ElementAt(0)?.GetLogicalChildren().ToList();

        return (
            textBox: stackPanel[0] as TextBox,
            nameBlock: stackPanel[1] as TextBlock
        );
    }
}