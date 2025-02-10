using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ImportStatement(string importString) : OldStatement
{
    public override void Run(VariateManager Manager)
    {
        if (Manager.LangInfo!.LibInfos.Any(x => importString == x.LibName))
        {
            var b = Manager.LangInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            var path = Path.Combine(Manager.LangInfo.ImportPath, importString + (b ? "" : ".ws"));
            var previousPath = Manager.Path;
            Manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = Manager.Interpreter.Build(code: code);
            a.ImportRun(Manager);
            Manager.Path = previousPath;
            return;
        }

        if (Apis.ImportInstall(importString))
        {
            var b = Manager.LangInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            var path = Manager.LangInfo.ImportPath + importString + ".ws";
            var previousPath = Manager.Path;
            Manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = Manager.Interpreter.Build(code: code);
            a.ImportRun(Manager);
            Manager.Path = previousPath;
            return;
        }

        var dic = Path.GetDirectoryName(Manager.Path)!;
        if (!File.Exists(dic + "/" + importString + ".ws")) return;

        var filePath = dic + "/" + importString + ".ws";
        var PreviousPath = Manager.Path;
        Manager.Path = filePath;
        var result = Manager.Interpreter.Build(code: Apis.FromFile(filePath));
        result.ImportRun(Manager);
        Manager.Path = PreviousPath;
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        var langInfo = Apis.ReadJson();
        if (langInfo.LibInfos.Any(x => importString == x.LibName))
        {
            var b = langInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            var path = Path.Combine(langInfo.ImportPath, importString + (b ? "" : ".ws"));
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            //var a = Interpreter.Build(code: code);

            var pPath = local.FilePath;
            local.FilePath = path;
            var block = local.Interpreter?.Build(code: code);
            block?.GenerateImportIL(ilGenerator, local);
            local.FilePath = pPath;
            return;
        }
        
        var dic = Path.GetDirectoryName(local.FilePath)!;
        if (!File.Exists(dic + "/" + importString + ".ws")) return;

        var filePath = dic + "/" + importString + ".ws";
        var result = local.Interpreter?.Build(code: Apis.FromFile(filePath));
        result?.GenerateImportIL(ilGenerator, local);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"using {importString}";
}