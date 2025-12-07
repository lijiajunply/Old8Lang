using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class ImportStatement(string importString, SourcePosition position = default) : OldStatement(position)
{
    public override void Run(VariateManager manager)
    {
        if (manager.LangInfo!.LibInfos.Any(x => importString == x.LibName))
        {
            var b = manager.LangInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = importString;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(manager.LangInfo.ImportPath, fileName);
            var previousPath = manager.Path;
            manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = manager.Interpreter.Build(code: code);
            a.ImportRun(manager);
            manager.Path = previousPath;
            return;
        }

        if (Apis.ImportInstall(importString))
        {
            var b = manager.LangInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = importString;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(manager.LangInfo.ImportPath, fileName);
            var previousPath = manager.Path;
            manager.Path = path;
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            var a = manager.Interpreter.Build(code: code);
            a.ImportRun(manager);
            manager.Path = previousPath;
            return;
        }

        var dic = Path.GetDirectoryName(manager.Path)!;
        // 检查文件扩展名，只支持.old8和.ol
        var fileNameLocal = importString;
        var extLocal = Path.GetExtension(fileNameLocal).ToLower();
        if (extLocal != ".old8" && extLocal != ".ol")
        {
            fileNameLocal += ".old8"; // 默认使用.old8扩展名
        }

        var filePath = Path.Combine(dic, fileNameLocal);
        if (!File.Exists(filePath)) return;

        var managerPath = manager.Path;
        manager.Path = filePath;
        var result = manager.Interpreter.Build(code: Apis.FromFile(filePath));
        result.ImportRun(manager);
        manager.Path = managerPath;
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        var langInfo = Apis.ReadJson();
        if (langInfo.LibInfos.Any(x => importString == x.LibName))
        {
            var b = langInfo.LibInfos.Where(x => x.LibName == importString).Select(x => x.IsDir).ToArray()[0];
            // 检查文件扩展名，只支持.old8和.ol
            var fileName = importString;
            var ext = Path.GetExtension(fileName).ToLower();
            if (!b && ext != ".old8" && ext != ".ol")
            {
                fileName += ".old8"; // 默认使用.old8扩展名
            }

            var path = Path.Combine(langInfo.ImportPath, fileName);
            var code = b ? Apis.FromDirectory(path) : Apis.FromFile(path);
            //var a = Interpreter.Build(code: code);

            var pPath = local.FilePath;
            local.FilePath = path;
            var block = local.Interpreter?.Build(code: code);
            block?.GenerateImportIl(ilGenerator, local);
            local.FilePath = pPath;
            return;
        }

        var dic = Path.GetDirectoryName(local.FilePath)!;
        // 检查文件扩展名，只支持.old8和.ol
        var fileNameLocal = importString;
        var extLocal = Path.GetExtension(fileNameLocal).ToLower();
        if (extLocal != ".old8" && extLocal != ".ol")
        {
            fileNameLocal += ".old8"; // 默认使用.old8扩展名
        }

        var filePath = Path.Combine(dic, fileNameLocal);
        if (!File.Exists(filePath)) return;

        var result = local.Interpreter?.Build(code: Apis.FromFile(filePath));
        result?.GenerateImportIl(ilGenerator, local);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"using {importString}";
}