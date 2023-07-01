using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.TextMate.Grammars;
using AvaloniaGraphControl;
using sly.parser.generator.visitor.dotgraph;

namespace OldStudio.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        InfoBlock = this.FindControl<TextBlock>("InfoBlock");
        Editor = this.FindControl<TextEditor>("Editor");
        GraphPanel = this.FindControl<GraphPanel>("GraphPanel");
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
        GraphPanel.Graph = ToGraph(i.Graph);
        InfoBlock.Text += "error:\n";
        i.GetError().ForEach(x => InfoBlock.Text += x);
        InfoBlock.Text += "info:\n";
        InfoBlock.Text += i.GetTime();
    }

    private Graph ToGraph(DotGraph dotGraph)
    {
        var a = new Graph();
        dotGraph.Dump();
        var b = dotGraph.FindRoots();
        var node = b.First();
        GetEdge(ref a,ref dotGraph,node);
        return a;
    }

    private void GetEdge(ref Graph graph,ref DotGraph dotGraph, DotNode node)
    {
        foreach (var arrow in dotGraph.FindEgdes(node))
        {
            graph.Edges.Add(new Edge(
                    new MyNode(arrow.Destination),
                    new MyNode(arrow.Source)
                )
            );
            if (arrow.Destination == null)
                return;
            GetEdge(ref graph,ref dotGraph,arrow.Destination);
        }
    }
    
    private class MyNode
    {
        private DotNode DotNode { get; set; }

        public MyNode(DotNode dotNode)
        {
            DotNode = dotNode;
        }

        public override bool Equals(object? obj)
        {
            if (obj is not MyNode node) return false;
            return node.DotNode == DotNode;
        }

        protected bool Equals(MyNode other)
        {
            return DotNode.Equals(other.DotNode);
        }

        public override string ToString() => DotNode.Label;
    }
    
}