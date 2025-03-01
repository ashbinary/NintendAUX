using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NintendAUX.ViewModels;

public partial class ErrorHandlerViewModel : ViewModelBase
{
    [ObservableProperty] private string _errorText;
    [ObservableProperty] private string _errorDetail;
    
    public ErrorHandlerViewModel()
    {
        _errorText = string.Empty;
        _errorDetail = string.Empty;
    }
}