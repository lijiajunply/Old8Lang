using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaGraphControl;

namespace OldStudio.Views;

public partial class GraphView : Window
{
    public GraphView(Graph graph)
    {
        InitializeComponent();
        GraphPanel = this.FindControl<GraphPanel>("GraphPanel");
        GraphPanel.Graph = graph;
    }
    
    public GraphView()
    {
        InitializeComponent();
        GraphPanel = this.FindControl<GraphPanel>("GraphPanel");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}