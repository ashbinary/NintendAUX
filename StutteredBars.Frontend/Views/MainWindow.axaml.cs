using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Platform.Storage;
using StutteredBars.Filetypes;
using StutteredBars.Frontend.Compression;
using StutteredBars.Frontend.Models;
using StutteredBars.Frontend.Parsers;
using StutteredBars.Frontend.ViewModels;
using YamlDotNet.Serialization.NodeDeserializers;

namespace StutteredBars.Frontend.Views;

public partial class MainWindow : Window
{
    BARSFile currentBARS;
    private dynamic currentNode;

    // Used for when an entry is deleted to prevent crashes.
    private bool inDeletion = false;
    
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainWindowViewModel();
    }
    
    public MainWindowViewModel Model => (MainWindowViewModel)DataContext;

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
            var barsData = File.ReadAllBytes(barsFile.Path.LocalPath);
            if (barsData.Take(4) != Utilities.BARSHeader) barsData = barsData.DecompressZSTDBytes();
            currentBARS = new BARSFile(barsData);
            Model.BarsFilePath = barsFile.Name;
            ReloadNode();
            Model.BarsLoaded = true;
        }
    }

    public async void SaveDecompressedBARSFile(object sender, RoutedEventArgs e) => SaveBARSFile(false);
    public async void SaveCompressedBARSFile(object sender, RoutedEventArgs e) => SaveBARSFile(true);

    public async void SaveBARSFile(bool compressFile)
    {
        var barsFile = await SaveFile(new FilePickerSaveOptions()
        {
            Title = "Save BARS File",
            DefaultExtension = compressFile ? ".bars.zs" : ".bars",
            SuggestedFileName = Model.BarsFilePath
        });
        
        if (barsFile != null)
        {
            using var stream = await barsFile.OpenWriteAsync();

            byte[] savedBars = BARSFile.SoftSave(currentBARS);
            if (compressFile) savedBars = ZSTDUtils.CompressZSTDBytes(savedBars);
            stream.Write(savedBars);
            stream.Flush();
        }
    }
    
    public async void SaveBARSFileCompressed(object? sender, RoutedEventArgs e)
    {
        var barsFile = await SaveFile(new FilePickerSaveOptions()
        {
            Title = "Save BARS File",
            DefaultExtension = ".bars",
            SuggestedFileName = Model.BarsFilePath
        });
        
        if (barsFile != null)
        {
            using var stream = await barsFile.OpenWriteAsync();
            stream.Write(ZSTDUtils.CompressZSTD(BARSFile.SoftSave(currentBARS)));
            stream.Flush();
        }
    }

    public void ChangeDisplayedInfo(object? sender, SelectionChangedEventArgs e)
    {
        if (inDeletion || !Model.BarsLoaded) return;
        
        currentNode = ((TreeView)sender).SelectedItem; // dude
        Model.TextData = InfoParser.ParseData(currentBARS.Metadata[currentNode.ID], currentBARS.Tracks[currentNode.ID]);
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
                DefaultExtension = ".bwav",
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
                DefaultExtension = ".bameta",
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

        if (bametaData.Info.Magic != 1096043841 || bwavData.Header.Magic != 1447122754) throw new Exception("fart");
            
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
        else
            return new AMTAFile();
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
        else
            return new BWAVFile();
    }
    
    // ---------------------- General File Saving/Opening Dialogs -----------------------
    
    public async Task<IStorageFile> SaveFile(FilePickerSaveOptions options)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var storageProvider = desktop.MainWindow?.StorageProvider;
            var files = await storageProvider.SaveFilePickerAsync(options);
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
            return files[0];
        }

        return null;
    }
}