using System.Reflection;
using System.Reflection.Emit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Old8Lang.AST.Statement;

namespace Old8Lang.Compiler;

public static class Compiler
{
    public static Action Compile(BlockStatement statement, string path, MiniInterpreter i)
    {
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path ,Interpreter = i};
        statement.GenerateIL(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
        var oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
        return oldLangRun;
    }

    public static Action Compile(string path, MiniInterpreter i)
    {
        var statement = i.Build(Apis.FromFile(path));
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path ,Interpreter = i};
        statement.GenerateIL(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ret);
        var oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
        return oldLangRun;
    }

    public static void CompileByCode(BlockStatement statement)
    {
        var code = statement.ToCode();
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        // 创建编译器参数
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        // 创建编译
        var compilation = CSharpCompilation.Create(
            "HelloWorld",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication));

        // 生成 IL 代码
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            // 处理编译错误
            foreach (var diagnostic in result.Diagnostics)
            {
                Console.WriteLine(diagnostic.ToString());
            }
        }
        else
        {
            // 输出 IL 代码
            ms.Seek(0, SeekOrigin.Begin);
            var ilCode = ms.ToArray();
            File.WriteAllBytes("HelloWorld.dll", ilCode);
            Console.WriteLine("中间代码生成成功");
            var assembly = Assembly.LoadFrom("HelloWorld.dll");

            // 获取入口点类型和方法
            var entryPointType = assembly.GetType("Program");
            var mainMethod = entryPointType?.GetMethod("Main");

            // 调用 Main 方法
            mainMethod?.Invoke(null, [Array.Empty<string>()]);
        }
    }
}