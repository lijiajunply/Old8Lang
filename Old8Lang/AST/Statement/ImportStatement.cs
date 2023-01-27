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
            if (Manager.LangInfo.ImPortTable.Keys.Any(x => i == x) || APIs.ImportInstall(i))
            {
                var path = Manager.LangInfo.ImportPath+i;
                var a    = new Interpreter(path,false,Manager.LangInfo);
                a.Use();
                var manager = a.GetVariateManager();
                foreach (var valueType in manager.AnyInfo)
                    Manager.AddClassAndFunc((valueType as FuncValue).Id,valueType);
            }
        }
    }

    public override string ToString() => $"import {APIs.ArrayToString(ImportString)}";
}