using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.TextMate.Grammars;
using AvaloniaGraphControl;
using OldStudio.Models;
using sly.parser.generator.visitor.dotgraph;

namespace OldStudio.Views;

public partial class MainWindow : Window
{
    private Graph? Graph { get; set; }

    public MainWindow()
    {
        InitializeComponent();
        View = this.FindControl<VariateManagerView>("View");
        InfoBlock = this.FindControl<TextBlock>("InfoBlock");
        Editor = this.FindControl<TextEditor>("Editor");
        var _registryOptions = new RegistryOptions(ThemeName.DarkPlus);

        var _textMateInstallation = Editor.InstallTextMate(_registryOptions);

        _textMateInstallation.SetGrammar(
            _registryOptions.GetScopeByLanguageId(_registryOptions.GetLanguageByExtension(".cs").Id));
    }

    private void RunClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(Editor.Text)) return;
        var i = new Old8Lang.CslyParser.Interpreter(Editor.Text, false);
        i.ParserRun(true);
        i.GetVariateManager();
        Graph = ToGraph(i.Graph);
        View.DataContext = i.GetVariateManager();
        InfoBlock.Text = "";
        InfoBlock.Text += "error:\n";
        if (i.GetError().Count == 0)
            InfoBlock.Text += "null\n";
        else
            i.GetError().ForEach(x => InfoBlock.Text += x+"\n");
        InfoBlock.Text += "info:\n";
        InfoBlock.Text += i.GetTime();
    }

    private Graph ToGraph(DotGraph dotGraph)
    {
        var a = new Graph();
        dotGraph.Dump();
        var b = dotGraph.FindRoots();
        var node = b.First();
        GetEdge(ref a, ref dotGraph, node);
        return a;
    }

    private void GetEdge(ref Graph graph, ref DotGraph dotGraph, DotNode node)
    {
        foreach (var arrow in dotGraph.FindEgdes(node))
        {
            graph.Edges.Add(new Edge(
                    new NodeModel(arrow.Destination),
                    new NodeModel(arrow.Source)
                )
            );
            if (arrow.Destination == null)
                return;
            GetEdge(ref graph, ref dotGraph, arrow.Destination);
        }
    }


    private void ShowClick(object? sender, RoutedEventArgs e)
    {
        if (Graph != null)
        {
            var window = new GraphView(Graph);
            window.Show(this);
        }
    }
}