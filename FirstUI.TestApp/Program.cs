// See https://aka.ms/new-console-template for more information

using Old8Lang.FirstUI;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Layout;

namespace FirstUI.TestApp;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = FirstUIBinding.CreateApp();

        app.Run(() => new Column(children: [new Text("asdf")]));
    }
}