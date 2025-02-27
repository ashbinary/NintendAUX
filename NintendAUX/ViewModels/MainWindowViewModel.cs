using System.Collections.ObjectModel;
using NintendAUX.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using NintendAUX.Filetypes.Generic;

namespace NintendAUX.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private bool _archiveLoaded; // Used specifically for disabling Save Compressed with BWAV 
    [ObservableProperty] private string _barsFilePath = "";
    [ObservableProperty] private bool _fileLoaded; // Enables all options once file is setup
    [ObservableProperty] private AudioFile _inputFile;
    [ObservableProperty] private string _inputFileName;
    [ObservableProperty] private InputFileType _inputType;
    [ObservableProperty] private Node _selectedNode;
    [ObservableProperty] private bool _sortNodes;
    [ObservableProperty] private bool _zsdicLoaded;

    public MainWindowViewModel()
    {
        SelectedNode = new Node();
    }

    // Initialize the collection directly since it's observable
    public ObservableCollection<Node> Nodes { get; } = [];
}