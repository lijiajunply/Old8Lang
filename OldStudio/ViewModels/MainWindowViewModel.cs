using AvaloniaGraphControl;

namespace OldStudio.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private Graph _graph;
    public Graph MyGraph
    {
        get => _graph;
        set => SetField(ref _graph, value);
    }

    public MainWindowViewModel()
    {
        var graph = new Graph();
        graph.Edges.Add(new Edge("A", "B"));
        graph.Edges.Add(new Edge("A", "D"));
        graph.Edges.Add(new Edge("A", "E"));
        graph.Edges.Add(new Edge("B", "C"));
        graph.Edges.Add(new Edge("B", "D"));
        graph.Edges.Add(new Edge("D", "A"));
        graph.Edges.Add(new Edge("D", "E"));
        _graph = MyGraph = graph;
    }
}