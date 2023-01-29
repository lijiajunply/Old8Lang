using Old8Lang.AST.Expression.Value;
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
        if (Manager.LangInfo.LibInfos.Any(x => ImportString == x.LibName))
        {
            var b = Manager.LangInfo.LibInfos.Where(x => x.LibName == ImportString).Select(x => x.IsDir).ToArray()[0];
            var path = Manager.LangInfo.ImportPath+ImportString+(b?"":".ws");
            var a    = new Interpreter(path,b,Manager.LangInfo);
            a.Run();
            var manager = a.GetVariateManager();
            foreach (var valueType in manager.AnyInfo)
                Manager.AddClassAndFunc((valueType as FuncValue).Id,valueType);
            return;
        }
        if (APIs.ImportInstall(ImportString))
        {
            var b = Manager.LangInfo.LibInfos.Where(x => x.LibName == ImportString).Select(x => x.IsDir).ToArray()[0];
            var path = Manager.LangInfo.ImportPath+ImportString+".ws";
            var a    = new Interpreter(path,b,Manager.LangInfo);
            a.Run();
            var manager = a.GetVariateManager();
            foreach (var valueType in manager.AnyInfo)
                Manager.AddClassAndFunc((valueType as FuncValue).Id,valueType);
            return;
        }
        string dic = Path.GetDirectoryName(Manager.Path);
        if (File.Exists(dic+"/"+ImportString+".ws"))
        {
            var a = new Interpreter(dic+"/"+ImportString+".ws",false,Manager.LangInfo);
            a.Run();
            var manager = a.GetVariateManager();
            foreach (var valueType in manager.AnyInfo)
            {
                if (valueType is FuncValue func)
                {
                    Manager.AddClassAndFunc(func.Id,valueType);
                    continue;
                }
                if (valueType is AnyValue any)
                {
                    Manager.AddClassAndFunc(any.Id,valueType);
                }
            }
        }
        
    }

    public override string ToString() => $"import {ImportString}";
}