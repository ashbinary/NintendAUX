using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NintendAUX.Views.Controls;

public partial class InfoBlock : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<InfoBlock, string>(nameof(Label));

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<InfoBlock, string>(nameof(Value));

    public InfoBlock()
    {
        InitializeComponent();
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}