using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using BARSBundler.Models;

namespace BARSBundler.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private Node _selectedNode;
    public Node SelectedNode 
    {
        get => _selectedNode; 
        set => SetProperty(ref _selectedNode, value);
    }
    
    [ObservableProperty] private bool barsLoaded = false;
    [ObservableProperty] private bool zsdicLoaded = false;

    [ObservableProperty] private string textData = "                    Load a .bars file to continue!";
    [ObservableProperty] private string barsFilePath = "";
    
    public ObservableCollection<Node> Nodes { get; set; }

    public MainWindowViewModel()
    {
        SelectedNode = new Node();
        Nodes = new ObservableCollection<Node>();
    }
}