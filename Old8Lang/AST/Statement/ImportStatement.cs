using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class ImportStatement : OldStatement
{
    public string[] ImportString { get; set; }

    public ImportStatement(string[] importString)
    {
        ImportString = importString;
    }

    public override void Run(ref VariateManager Manager)
    {
        //查找
        foreach (var i in ImportString)
        {
            var path = "/home/luckyfish/RiderProjects/Old8Lang/Old8LangLib/OldLib/"+i;
            var a    = new Interpreter(path,false);
            a.Use();
            var manager = a.GetVariateManager();
            foreach (var valueType in manager.AnyInfo)
                Manager.AddClassAndFunc((valueType as FuncValue).Id,valueType);
        }
    }

    public override string ToString() => $"import {APIs.ArrayToString(ImportString)}";
}