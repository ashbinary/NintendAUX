using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using NintendAUX.Filetypes.Archive;
using NintendAUX.Filetypes.Audio;
using NintendAUX.Filetypes.Generic;
using NintendAUX.Models;
using NintendAUX.Services;
using NintendAUX.Services.Audio;
using NintendAUX.Services.Entry;
using NintendAUX.Utilities;
using NintendAUX.ViewModels;

namespace NintendAUX.Views;

public partial class MainWindow : Window
{
    private bool isEditable;
    private int nodeIndex;

    private MainWindowViewModel Model => ViewModelLocator.Model;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = Model;
        AllArea.AddHandler(DragDrop.DropEvent, DragFileOn);
    }

    public void SortNodes(object sender, RoutedEventArgs e)
    {
        Model.SortNodes = true;
        NodeService.UpdateNodeArray();
    }

    public async void DragFileOn(object sender, DragEventArgs e)
    {
        IStorageFile draggedFile = await 
            StorageProvider.TryGetFileFromPathAsync(e.Data?.GetFiles()?.FirstOrDefault()?.Path.LocalPath);
        await SetupFileData(draggedFile);
    }

    public async void OpenFile(object sender, RoutedEventArgs e)
    {
        var inputData = await FileDialogService.OpenFile(new FilePickerOpenOptions
        {
            Title = "Open Sound / Sound Archive File",
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Sound Archives") { Patterns = ["*.bars.zs", "*.bars", "*.bwav"] }]
        });
        
        await SetupFileData(inputData);
    }

    private async Task SetupFileData(IStorageFile inputData)
    {
        if (inputData is null) return;

        Model.InputType = Path.GetExtension(inputData.Name) switch
        {
            ".bars" or ".zs" => InputFileType.Bars,
            ".bwav" => InputFileType.Bwav,
            _ => await new DataException("This is an invalid file!").CreateExceptionDialog()
        };

        await FileLoadingService.LoadFile(inputData, Model.InputType);
        NodeInfoPanel.Height = 375;
        isEditable = true;
    }

    public async void LoadTotkDict(object sender, RoutedEventArgs e)
    {
        await FileLoadingService.LoadTotkDict();
    }

    public async void SaveDecompressedFile(object sender, RoutedEventArgs e)
    {
        await SaveFile(false);
    }

    public async void SaveCompressedFile(object sender, RoutedEventArgs e)
    {
        await SaveFile(true);
    }

    public async Task SaveFile(bool compressed)
    {
        await FileSavingService.SaveFile(await FileDialogService.SaveFile(
                new FilePickerSaveOptions
                {
                    Title = "Pick File to Save",
                    DefaultExtension = FileSavingService.GetExtensionType(compressed),
                    SuggestedFileName = Model.InputFileName
                }), compressed
        );
    }

    public void HideTextBox(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        TextBox textBox = sender as TextBox;

        textBox.IsVisible = false;
        SaveRenamedBarsEntry(textBox.Text);
        textBox.Text = null;
    }

    public void RenameBarsEntry(object sender, RoutedEventArgs e)
    {
        if (Model.SelectedNode.GetType() != typeof(BARSEntryNode)) return;

        var currentItem = EntryRenameService.GetTreeViewItemForNode(ref TreeView, Model.SelectedNode);
        if (currentItem == null) return;

        (TextBox textBox, TextBlock nameBlock) controls = EntryRenameService.GetNodeControls(currentItem);

        EntryRenameService.SetTreeViewItemsEnabled(ref TreeView, false);
        currentItem.IsEnabled = true;

        controls.textBox.IsVisible = true;
        controls.nameBlock.IsVisible = false;
    }

    public void SaveRenamedBarsEntry(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || Model.SelectedNode.GetType() != typeof(BARSEntryNode)) return;

        var currentItem = EntryRenameService.GetTreeViewItemForNode(ref TreeView, Model.SelectedNode);
        if (currentItem == null) return;

        (TextBox textBox, TextBlock nameBlock) controls = EntryRenameService.GetNodeControls(currentItem);

        EntryRenameService.SetTreeViewItemsEnabled(ref TreeView, true);

        controls.textBox.IsVisible = false;
        controls.textBox.Text = null;

        controls.nameBlock.IsVisible = true;
        controls.nameBlock.Text = newName;

        var InputBars = Model.InputFile.AsBarsFile();

        if (InputBars.EntryArray != null && nodeIndex >= 0 && nodeIndex < InputBars.EntryArray.Count)
        {
            var entry = InputBars.EntryArray[nodeIndex];
            entry.Bamta.Path = newName;
            InputBars.EntryArray[nodeIndex] = entry;
        }
    }

    public void ChangeDisplayedInfo(object? sender, SelectionChangedEventArgs e)
    {
        if (!isEditable || !Model.FileLoaded) return;

        Model.SelectedNode = TreeView.SelectedItem as Node;
        if (Model.SelectedNode == null) return;
        
        nodeIndex = Model.SelectedNode.ID;
        
        // The UI will automatically update based on the type of the selected node
        // through the IsVisible bindings in the XAML
    }

    public void ExitApplication(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    public void DeleteBARSEntry(object? sender, RoutedEventArgs e)
    {
        isEditable = false;
        Model.InputFile.RemoveEntryAt(Model.SelectedNode.ID);
        NodeService.UpdateNodeArray();
        isEditable = true;
    }

    public async void ExtractAll(object? sender, RoutedEventArgs e)
    {
        await FileExtractService.ExtractAllEntries(Model.InputFile.AsBarsFile());
    }

    public async void AddNewNode(object? sender, RoutedEventArgs e)
    {
        var entry = await NodeService.CreateNewEntry();
        if (entry.BamtaOffset == 0xDEADBEEF) return;
        // Add the new entry to the List
        Model.InputFile.AsBarsFile().EntryArray.Add(entry);
        NodeService.UpdateNodeArray();
    }

    public async void ReplaceBwav(object sender, RoutedEventArgs e)
    {
        await EntryReplaceService.ReplaceBwav(nodeIndex);
    }

    public async void ReplaceBameta(object sender, RoutedEventArgs e)
    {
        await EntryReplaceService.ReplaceBameta(nodeIndex);
    }

    public async void ExtractAsBwav(object? sender, RoutedEventArgs e)
    {
        await FileExtractService.ExtractBwavWithDialog(Model.InputFile.AsBarsFile().EntryArray[Model.SelectedNode.ID]);
    }

    public async void ExtractAsBameta(object? sender, RoutedEventArgs e)
    {
        await FileExtractService.ExtractBametaWithDialog(Model.InputFile.AsBarsFile().EntryArray[Model.SelectedNode.ID]);
    }
    
    public async Task ExtractFileAsWav(BwavFile bwavFile)
    {
        var selectedId = Model.SelectedNode.ID;
        
        var fileData = await FileDialogService.SaveFile(new FilePickerSaveOptions()
        {
            SuggestedFileName = Path.GetFileNameWithoutExtension(Model.InputFileName),
            DefaultExtension = "wav"
        });

        Task<AudioChannel[]> audioData = Model.SelectedNode switch
        {
            BWAVNode node => PcmService.DecodeChannels(bwavFile.ChannelInfoArray),
            BWAVStereoChannelNode stereoNode => PcmService.DecodeChannels(bwavFile.ChannelInfoArray[selectedId..(selectedId + 2)]),
            BWAVChannelNode channelNode => PcmService.DecodeMonoChannel(bwavFile.ChannelInfoArray[selectedId]),
            
        };
        
        WavFile wavData = ConversionUtilities.CreateWavData(
            ref bwavFile.ChannelInfoArray[Model.SelectedNode.GetType() == typeof(BWAVNode) ? 0 : selectedId], await audioData);

        await FileExtractService.ExtractFile(wavData, fileData, WavFile.Write);
    }

    public async void ExtractAsWav(object? sender, RoutedEventArgs e)
    {
        switch (Model.InputType)
        {
            case InputFileType.Bars:
                await ExtractFileAsWav(Model.InputFile.AsBarsFile().EntryArray[Model.SelectedNode.ID].Bwav);
                break;
            case InputFileType.Bwav:
                await ExtractFileAsWav(Model.InputFile.AsBwavFile());
                break;
            default:
                await new NotImplementedException().CreateExceptionDialog();
                break;
        }
    }
}