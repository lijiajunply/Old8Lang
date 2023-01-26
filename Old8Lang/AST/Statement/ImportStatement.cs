using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class ImportStatement : OldStatement
{
    public string ImportString { get; set; }

    public ImportStatement(string importString)
    {
        ImportString = importString;
    }

    public override void Run(ref VariateManager Manager)
    {
        //查找
        var path = Manager.Path+ImportString;
        Manager    = APIs.CslyUsing(APIs.FromDirectory(path)).Manager;
        Manager.Init();
    }

    public override string ToString() => $"import {ImportString}";
}