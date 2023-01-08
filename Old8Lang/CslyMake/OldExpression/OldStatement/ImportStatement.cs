using Old8Lang.CslyMake.OldLandParser;

namespace Old8Lang.CslyMake.OldExpression;

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
        var path = APIs.ImportSearch(ImportString);
        //
        var a = APIs.CslyUsing(APIs.FromDirectory(path));
        //
        Manager = a.Manager;
        Manager.Init();
    }
}