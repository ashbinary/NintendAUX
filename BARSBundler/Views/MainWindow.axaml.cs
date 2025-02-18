using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using BARSBundler.Core.Filetypes;
using BARSBundler.Compression;
using BARSBundler.Models;
using BARSBundler.Parsers;
using BARSBundler.Services;
using BARSBundler.ViewModels;

namespace BARSBundler.Views;

public partial class MainWindow : Window
{
    public BARSFile currentBARS;
    private dynamic currentNode;
    private int nodeIndex = 0;

    // Used for when an entry is deleted to prevent crashes.
    private bool inDeletion = false;
    
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
        AllArea.AddHandler(DragDrop.DropEvent, DragFileOn);
    }
    
    public MainWindowViewModel Model => (MainWindowViewModel)DataContext;

    public void DragFileOn(object sender, DragEventArgs e)
    {
        if (e.Data?.GetFiles()?.FirstOrDefault() is IStorageFile storageFile)
        {
            LoadBars(storageFile);
        }
    }

    public void SortNodes(object sender, RoutedEventArgs e)
    {
        Model.SortNodes = true;
        ReloadNode();
    }

    public async void OpenBARSFile(object sender, RoutedEventArgs e)
    {
        var barsFile = await FileDialogService.OpenFile(new FilePickerOpenOptions
        {
            Title = "Open BARS File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(".bars files") { Patterns = ["*.bars.zs", "*.bars"] }]
        });
        
        if (barsFile != null)
            LoadBars(barsFile);
    }

    public async void LoadTotkDict(object sender, RoutedEventArgs e)
    {
        var zstdPack = await FileDialogService.OpenFile(new FilePickerOpenOptions
        {
            Title = "Open ZSTD Dictionary",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("ZSTD Dictionaries") { Patterns = ["*.pack.zs", "*.pack", "*.zsdic"] }]
        });
        
        if (zstdPack != null)
        {
            ZSDic.LoadDictionary(File.ReadAllBytes(zstdPack.Path.LocalPath));
            Model.ZsdicLoaded = true;
        }
    }

    public void LoadBars(IStorageFile barsFile)
    {
        var barsData = File.ReadAllBytes(barsFile.Path.LocalPath);
        Console.WriteLine(barsData.Take(4).ToArray().Last());
        if (!barsData.Take(4).ToArray().SequenceEqual(Utilities.BARSHeader)) barsData = barsData.DecompressZSTDBytes(Model.ZsdicLoaded);
        currentBARS = new BARSFile(barsData);
        Model.BarsFilePath = barsFile.Name;
        ReloadNode();
        Model.BarsLoaded = true;
    }

    public async void SaveDecompressedBARSFile(object sender, RoutedEventArgs e) => await SaveBARSFile(false);
    public async void SaveCompressedBARSFile(object sender, RoutedEventArgs e) => await SaveBARSFile(true);
    
    public async Task SaveBARSFile(bool compressed) => await BarsHandlingService.SaveBARSFile(currentBARS, compressed, Model.BarsFilePath, Model.ZsdicLoaded);

    public void RenameBARSFile(object sender, RoutedEventArgs e)
    {
        TreeViewItem currentItem = (TreeViewItem)treeView.GetVisualDescendants().OfType<TreeViewItem>()
            .FirstOrDefault(tvi => tvi.DataContext == currentNode);
        if (currentNode.Type == NodeType.BARSEntry)
        {
            foreach (TreeViewItem item in treeView.GetVisualDescendants().OfType<TreeViewItem>())
                if (item.DataContext != currentNode) item.IsEnabled = false;
            
            var stackPanel = currentItem.GetLogicalChildren().ElementAt(0).GetLogicalChildren();
            
            var textData = (TextBox)stackPanel.ElementAt(0);
            textData.IsVisible = true;

            var nameTag = (TextBlock)stackPanel.ElementAt(1);
            nameTag.IsVisible = false;
        }
    }

    public void HideTextBox(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            (sender as TextBox).IsVisible = false;
            SaveRenamedBarsFile((sender as TextBox).Text);
            (sender as TextBox).Text = null;
        }
    }

    public void SaveRenamedBarsFile(string newName)
    {
        TreeViewItem currentItem = (TreeViewItem)treeView.GetVisualDescendants().OfType<TreeViewItem>()
            .FirstOrDefault(tvi => tvi.DataContext == currentNode);
        if (currentNode.Type == NodeType.BARSEntry)
        {
            foreach (TreeViewItem item in treeView.GetVisualDescendants().OfType<TreeViewItem>())
                item.IsEnabled = true;

            var stackPanel = currentItem.GetLogicalChildren().ElementAt(0).GetLogicalChildren();
            
            var textData = (TextBox)stackPanel.ElementAt(0);
            textData.IsVisible = false;

            var nameTag = (TextBlock)stackPanel.ElementAt(1);
            nameTag.IsVisible = true;
            nameTag.Text = newName;

            currentBARS.EntryArray[nodeIndex].Bamta.Path = newName;
        }
    }

    public void ChangeDisplayedInfo(object? sender, SelectionChangedEventArgs e)
    {
        if (inDeletion || !Model.BarsLoaded) return;
        
        currentNode = ((TreeView)sender).SelectedItem; // dude
        Model.TextData = InfoParser.ParseData(currentBARS.EntryArray[currentNode.ID].Bamta, currentBARS.EntryArray[currentNode.ID].Bwav);
        
        nodeIndex = currentNode.ID;
        
    }

    public void ExitApplication(object? sender, RoutedEventArgs e) => Environment.Exit(0);

    public void DeleteBARSEntry(object? sender, RoutedEventArgs e)
    {
        inDeletion = true; 
        
        Utilities.RemoveAt(ref currentBARS.EntryArray, (currentNode as BARSEntryNode).ID);
        ReloadNode();
        
        inDeletion = false;
    }

    public async void ExtractAsBwav(object? sender, RoutedEventArgs e) => 
        await FileExtractService.ExtractBwavWithDialog(currentBARS.EntryArray[currentNode.ID]);
    
    public async void ExtractAsBameta(object? sender, RoutedEventArgs e) => 
        await FileExtractService.ExtractBametaWithDialog(currentBARS.EntryArray[currentNode.ID]);

    public async void ExtractAll(object? sender, RoutedEventArgs e) =>
        await FileExtractService.ExtractAllEntries(currentBARS);

    public void ReloadNode() => Model.Nodes = NodeService.ReloadNode(Model.Nodes, ref currentBARS, Model.SortNodes);

    public async void AddNewNode(object? sender, RoutedEventArgs e)
    {
        BARSFile.BarsEntry entry = await NodeService.CreateNewEntry();
        if (entry.BamtaOffset == 0xDEADBEEF) return;
        Utilities.AddToEnd(ref currentBARS.EntryArray, entry);
        ReloadNode();
    }

    public async void ReplaceBwav(object sender, RoutedEventArgs e)
    {
        var newBwav = await BarsEntryService.CreateBwav();
        if (newBwav.Header.Magic == 1447122754)
            currentBARS.EntryArray[nodeIndex].Bwav = newBwav;
    }
    
    public async void ReplaceBameta(object sender, RoutedEventArgs e)
    {
        var newBameta = await BarsEntryService.CreateBameta();
        if (newBameta.Info.Magic == 1096043841)
            currentBARS.EntryArray[nodeIndex].Bamta = await BarsEntryService.CreateBameta();
    }
    
}