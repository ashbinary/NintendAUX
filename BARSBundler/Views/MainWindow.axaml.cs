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
        var barsFile = await OpenFile(new FilePickerOpenOptions
        {
            Title = "Open BARS File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(".bars files") { Patterns = ["*.bars.zs", "*.bars"] }]
        });
        
        if (barsFile != null)
        {
            LoadBars(barsFile);
        }
    }

    public async void LoadTotkDict(object sender, RoutedEventArgs e)
    {
        var zstdPack = await OpenFile(new FilePickerOpenOptions
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

    public async void SaveDecompressedBARSFile(object sender, RoutedEventArgs e) => SaveBARSFile(false);
    public async void SaveCompressedBARSFile(object sender, RoutedEventArgs e) => SaveBARSFile(true);

    public async void SaveBARSFile(bool compressFile)
    {
        var barsFile = await SaveFile(new FilePickerSaveOptions()
        {
            Title = "Save BARS File",
            DefaultExtension = compressFile ? "bars.zs" : "bars",
            SuggestedFileName = Model.BarsFilePath
        });
        
        if (barsFile != null)
        {
            using var stream = await barsFile.OpenWriteAsync();
            
            // Re-order here, so the saver will not have issues in-game
            currentBARS.Metadata = currentBARS.Metadata.OrderBy(path => CRC32.Compute(path.Path)).ToList();

            byte[] savedBars = BARSFile.SoftSave(currentBARS);
            if (compressFile) savedBars = ZSTDUtils.CompressZSTDBytes(savedBars, Model.ZsdicLoaded);
            stream.Write(savedBars);
            stream.Flush();
        }
    }

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

            var sillyMetadata = currentBARS.Metadata[nodeIndex];
            currentBARS.Metadata.RemoveAt(nodeIndex);
            sillyMetadata.Path = newName;
            currentBARS.Metadata.Insert(nodeIndex, sillyMetadata);
        }
    }

    public void ChangeDisplayedInfo(object? sender, SelectionChangedEventArgs e)
    {
        if (inDeletion || !Model.BarsLoaded) return;
        
        currentNode = ((TreeView)sender).SelectedItem; // dude
        Model.TextData = InfoParser.ParseData(currentBARS.Metadata[currentNode.ID], currentBARS.Tracks[currentNode.ID]);
        
        nodeIndex = currentNode.ID;
        
    }

    public void ExitApplication(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    public void DeleteBARSEntry(object? sender, RoutedEventArgs e)
    {
        inDeletion = true; 
        int nodeIndex = ((BARSEntryNode)currentNode).ID;
        Model.Nodes.RemoveAt(nodeIndex);
        
        currentBARS.Metadata.RemoveAt(nodeIndex);
        currentBARS.Tracks.RemoveAt(nodeIndex);
        
        ReloadNode();
        inDeletion = false;
        Console.WriteLine("removed node");
    }

    public async void ExtractAsBwav(object? sender, RoutedEventArgs e)
    {
        IStorageFile fileData = await SaveFile(
            new FilePickerSaveOptions()
            {
                Title = "Save .bwav File",
                DefaultExtension = "bwav",
                SuggestedFileName = currentBARS.Metadata[currentNode.ID].Path + ".bwav"
            }
        );

        using Stream bwavStream = await fileData.OpenWriteAsync();
            bwavStream.Write(BWAVFile.Save(currentBARS.Tracks[currentNode.ID]));
            bwavStream.Flush();
    }
    
    public async void ExtractAsBameta(object? sender, RoutedEventArgs e)
    {
        IStorageFile fileData = await SaveFile(
            new FilePickerSaveOptions()
            {
                Title = "Save .bameta File",
                DefaultExtension = "bameta",
                SuggestedFileName = currentBARS.Metadata[currentNode.ID].Path + ".bameta"
            }
        );

        using Stream amtaStream = await fileData.OpenWriteAsync();
        amtaStream.Write(AMTAFile.Save(currentBARS.Metadata[currentNode.ID]));
        amtaStream.Flush();
    }

    public async void ExtractAll(object? sender, RoutedEventArgs e)
    {
        IStorageFolder folderData = await OpenFolder(
            new FolderPickerOpenOptions()
            {
                Title = "Open Folder to Save Files In",
                AllowMultiple = false
            }
        );

        if (folderData == null) return;

        for (int i = 0; i < currentBARS.Metadata.Count; i++)
        {
            IStorageFolder dataFolder = await folderData.CreateFolderAsync(currentBARS.Metadata[i].Path);
            
            IStorageFile amtaData = await dataFolder.CreateFileAsync(currentBARS.Metadata[i].Path + ".bameta");
            using Stream amtaStream = await amtaData.OpenWriteAsync();
            amtaStream.Write(AMTAFile.Save(currentBARS.Metadata[i]));
            amtaStream.Flush();
            
            IStorageFile bwavData = await dataFolder.CreateFileAsync(currentBARS.Metadata[i].Path + ".bwav");
            using Stream bwavStream = await bwavData.OpenWriteAsync();
            bwavStream.Write(BWAVFile.Save(currentBARS.Tracks[i]));
            bwavStream.Flush();
        }
    }

    public void ReloadNode()
    {
        Model.Nodes.Clear();
        
        // Re-order here to sort effectively
        if (Model.SortNodes)
            currentBARS.Metadata = currentBARS.Metadata.OrderBy(path => path.Path).ToList();

        int entryIndex = 0;
        foreach (AMTAFile file in currentBARS.Metadata)
        {
            Model.Nodes.Add(new BARSEntryNode(file.Path, entryIndex, new ObservableCollection<Node>()
            {
                new AMTANode("Metadata (AMTA)", entryIndex),
                new BWAVNode("Song (BWAV)", entryIndex)
            }));
            entryIndex++;
        }
    }

    public async void AddNewNode(object? sender, RoutedEventArgs e)
    {
        AMTAFile bametaData = await CreateBameta();
        BWAVFile bwavData = await CreateBwav();

        if (bametaData.Info.Magic != 1096043841 || bwavData.Header.Magic != 1447122754) 
            return;
            
        currentBARS.Metadata.Add(bametaData);
        currentBARS.Tracks.Add(bwavData);
        
        ReloadNode();
    }

    public async Task<AMTAFile> CreateBameta()
    {
        IStorageFile bametaFile = await OpenFile(
            new FilePickerOpenOptions()
            {
                Title = "Open .bameta File",
                FileTypeFilter = [new FilePickerFileType(".bameta files") { Patterns = ["*.bameta"] }],
                AllowMultiple = false
            }
        );
        
        if (bametaFile != null)
            return new AMTAFile(await bametaFile.OpenReadAsync()); 
        
        return new AMTAFile();
    }

    public async void ReplaceBwav(object sender, RoutedEventArgs e)
    {
        var newBwav = await CreateBwav();

        if (newBwav.Header.Magic == 1447122754)
        {
            currentBARS.Tracks[nodeIndex] = newBwav;
        }
    }
    
    public async void ReplaceBameta(object sender, RoutedEventArgs e)
    {
        var newBameta = await CreateBameta();

        if (newBameta.Info.Magic == 1096043841)
        {
            currentBARS.Metadata[nodeIndex] = newBameta;
        }
    }
    
    public async Task<BWAVFile> CreateBwav()
    {
        IStorageFile bwavFile = await OpenFile(
            new FilePickerOpenOptions()
            {
                Title = "Open .bwav File",
                FileTypeFilter = [new FilePickerFileType(".bwav files") { Patterns = ["*.bwav"] }],
                AllowMultiple = false
            }
        );
        
        if (bwavFile != null)
            return new BWAVFile(await bwavFile.OpenReadAsync());
        
        return new BWAVFile();
    }
    
    // ---------------------- General File Saving/Opening Dialogs -----------------------
    
    public async Task<IStorageFile> SaveFile(FilePickerSaveOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.SaveFilePickerAsync(options);
            if (files != null)
                return files;
        }

        return null;
    }
    
    public async Task<IStorageFolder> OpenFolder(FolderPickerOpenOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.OpenFolderPickerAsync(options);
            if (files.Count > 0)
                return files[0];
        }

        return null;
    }
    
    public async Task<IStorageFile> OpenFile(FilePickerOpenOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.OpenFilePickerAsync(options);
            if (files.Count > 0)
                return files[0];
        }

        return null;
    }
}