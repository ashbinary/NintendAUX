using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
    private bool _isEditable;
    private int _nodeIndex;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = Model;
        AllArea.AddHandler(DragDrop.DropEvent, DragFileOn);
    }

    private MainWindowViewModel Model => ViewModelLocator.Model;

    public void SortNodes(object sender, RoutedEventArgs e)
    {
        Model.SortNodes = true;
        NodeService.UpdateNodeArray();
    }

    private async void DragFileOn(object? sender, DragEventArgs e)
    {
        var pathLocalPath = e.Data?.GetFiles()?.FirstOrDefault()?.Path.LocalPath;
        if (pathLocalPath != null)
        {
            var draggedFile = await
                StorageProvider.TryGetFileFromPathAsync(pathLocalPath);
            await SetupFileData(draggedFile);
        }
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

    private async Task SetupFileData(IStorageFile? inputData)
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
        _isEditable = true;
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

    private async Task SaveFile(bool compressed)
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

        var textBox = sender as TextBox;

        textBox.IsVisible = false;
        if (textBox.Text != null)
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

    private void SaveRenamedBarsEntry(string newName)
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

        var inputBars = Model.InputFile.AsBarsFile();

        if (inputBars.EntryArray != null && _nodeIndex >= 0 && _nodeIndex < inputBars.EntryArray.Count)
        {
            var entry = inputBars.EntryArray[_nodeIndex];
            entry.Bamta.Path = newName;
            inputBars.EntryArray[_nodeIndex] = entry;
        }
    }

    public void ChangeDisplayedInfo(object? sender, SelectionChangedEventArgs e)
    {
        if (!_isEditable || !Model.FileLoaded) return;

        Model.SelectedNode = TreeView.SelectedItem as Node;
        if (Model.SelectedNode == null) return;

        _nodeIndex = Model.SelectedNode.ID;

        // The UI will automatically update based on the type of the selected node
        // through the IsVisible bindings in the XAML
    }

    public void ExitApplication(object? sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    public void DeleteBarsEntry(object? sender, RoutedEventArgs e)
    {
        _isEditable = false;
        Model.InputFile.RemoveEntryAt(Model.SelectedNode.ID);
        NodeService.UpdateNodeArray();
        _isEditable = true;
    }

    public async void ExtractAll(object? sender, RoutedEventArgs e)
    {
        await FileExtractService.ExtractAllEntries(Model.InputFile.AsBarsFile());
    }

    public async void AddNewNode(object? sender, RoutedEventArgs e)
    {
        await NodeService.CreateNewEntry();
    }

    public async void ReplaceBwav(object sender, RoutedEventArgs e)
    {
        await EntryReplaceService.ReplaceBwav(_nodeIndex);
    }

    public async void ReplaceBameta(object sender, RoutedEventArgs e)
    {
        await EntryReplaceService.ReplaceBameta(_nodeIndex);
    }

    public async void ExtractAsBwav(object? sender, RoutedEventArgs e)
    {
        await FileExtractService.ExtractBwavWithDialog(Model.InputFile.AsBarsFile().EntryArray[Model.SelectedNode.ID]);
    }

    public async void ExtractAsBameta(object? sender, RoutedEventArgs e)
    {
        await FileExtractService.ExtractBametaWithDialog(Model.InputFile.AsBarsFile()
            .EntryArray[Model.SelectedNode.ID]);
    }

    private async Task ExtractFileAsWav(BwavFile bwavFile)
    {
        var selectedId = Model.SelectedNode.ID;

        string fileName = "";

        switch (Model.InputType)
        {
            case InputFileType.Bars: 
                fileName = Model.InputFile.AsBarsFile().EntryArray[selectedId].Bamta.Path;
                break;
            case InputFileType.Bwav:
                fileName = Path.GetFileNameWithoutExtension(Model.InputFileName);
                break;
            default:
                await new DataException("Failed input type check!").CreateExceptionDialog();
                break;
        }

        var fileData = await FileDialogService.SaveFile(new FilePickerSaveOptions
        {
            SuggestedFileName = fileName,
            DefaultExtension = "wav"
        });
        
        if (fileData == null) return;

        Task<AudioChannel[]> audioData = Model.SelectedNode switch
        {
            BWAVNode => PcmService.DecodeChannels(bwavFile.ChannelInfoArray),
            BWAVStereoChannelNode => PcmService.DecodeChannels(bwavFile.ChannelInfoArray[selectedId..(selectedId + 2)]),
            BWAVChannelNode => PcmService.DecodeMonoChannel(bwavFile.ChannelInfoArray[selectedId]),
            _ => await new DataValidationException("Found illegal channel.").CreateExceptionDialog()
        };

        var wavData = ConversionUtilities.CreateWavData(
            ref bwavFile.ChannelInfoArray[Model.SelectedNode.GetType() == typeof(BWAVNode) ? 0 : selectedId],
            await audioData);

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