using CommunityToolkit.Mvvm.ComponentModel;

namespace NintendAUX.ViewModels;

public partial class ErrorHandlerViewModel : ViewModelBase
{
    [ObservableProperty] private string _errorDetail;
    [ObservableProperty] private string _errorText;

    public ErrorHandlerViewModel()
    {
        _errorText = string.Empty;
        _errorDetail = string.Empty;
    }
}