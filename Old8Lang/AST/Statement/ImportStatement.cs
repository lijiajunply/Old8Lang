using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class ImportStatement(string importString) : OldStatement
{
    private string ImportString { get; } = importString;

    public override void Run(ref VariateManager Manager)
    {
        if (Manager.LangInfo!.LibInfos.Any(x => ImportString == x.LibName))
        {
            var b = Manager.LangInfo.LibInfos.Where(x => x.LibName == ImportString).Select(x => x.IsDir).ToArray()[0];
            var path = Path.Combine(Manager.LangInfo.ImportPath, ImportString + (b ? "" : ".ws"));
            var previousPath = Manager.Path;
            Manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = Manager.Interpreter?.Build(code: code);
            a?.ImportRun(ref Manager);
            Manager.Path = previousPath;
            return;
        }

        if (Apis.ImportInstall(ImportString))
        {
            var b = Manager.LangInfo.LibInfos.Where(x => x.LibName == ImportString).Select(x => x.IsDir).ToArray()[0];
            var path = Manager.LangInfo.ImportPath + ImportString + ".ws";
            var previousPath = Manager.Path;
            Manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = Manager.Interpreter?.Build(code: code);
            a?.ImportRun(ref Manager);
            Manager.Path = previousPath;
            return;
        }

        var dic = Path.GetDirectoryName(Manager.Path)!;
        if (!File.Exists(dic + "/" + ImportString + ".ws")) return;

        var filePath = dic + "/" + ImportString + ".ws";
        var PreviousPath = Manager.Path;
        Manager.Path = filePath;
        var result = Manager.Interpreter?.Build(code: Apis.FromFile(filePath));
        result?.ImportRun(ref Manager);
        Manager.Path = PreviousPath;
    }

    public override string ToString() => $"using {ImportString}";
}