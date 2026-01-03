// See https://aka.ms/new-console-template for more information

using Old8Lang.FirstUI;
using Old8Lang.FirstUI.Basic;
using Old8Lang.FirstUI.Core;
using Old8Lang.FirstUI.Layout;

namespace FirstUI.TestApp;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var app = FirstUIBinding.CreateApp();

        app.Run(RunSimpleTest, "asdf");
    }

    public static WidgetBase RunSimpleTest()
    {
        return new Column([
            new Text("计数器应用")
        ]);
    }
}