using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace OldStudio.Views;

public partial class VariateManagerView : UserControl
{

    public VariateManagerView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}