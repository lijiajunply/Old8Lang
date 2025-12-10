using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.Error;

namespace Old8Lang.Compiler;

public static class Compiler
{
    public static Action Compile(BlockStatement statement, string path, IMiniInterpreter i)
    {
        Console.WriteLine($"[编译调试] 开始编译: {path}");
        Console.WriteLine($"[编译调试] 语句类型: {statement.GetType().Name}");
        
        var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
        var ilGenerator = dynamicMethod.GetILGenerator();
        var local = new LocalManager() { FilePath = path ,Interpreter = i};
        
        try
        {
            Console.WriteLine($"[编译调试] 开始生成IL代码");
            statement.GenerateIl(ilGenerator, local);
            Console.WriteLine($"[编译调试] IL代码生成完成");
            
            ilGenerator.Emit(OpCodes.Ret);
            
            Console.WriteLine($"[编译调试] 创建委托");
            var oldLangRun = (Action)dynamicMethod.CreateDelegate(typeof(Action));
            Console.WriteLine($"[编译调试] 编译成功");
            
            return oldLangRun;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[编译错误] {path}");
            Console.Error.WriteLine($"[错误类型] {ex.GetType().Name}");
            Console.Error.WriteLine($"[错误信息] {ex.Message}");
            Console.Error.WriteLine($"[堆栈跟踪] {ex.StackTrace}");
            
            // 尝试获取更详细的位置信息
            if (ex is InvalidOperationError invalidOpError)
            {
                Console.Error.WriteLine($"[位置信息] {invalidOpError.Position}");
                if (invalidOpError.SourceContext != null && invalidOpError.SourceContext.Length > 0)
                {
                    Console.Error.WriteLine($"[上下文]");
                    foreach (var line in invalidOpError.SourceContext)
                    {
                        Console.Error.WriteLine($"  {line}");
                    }
                }
                Console.Error.WriteLine($"[建议] {invalidOpError.Suggestion}");
            }
            else if (ex is CompilerException compilerEx)
            {
                Console.Error.WriteLine($"[位置信息] {compilerEx.Position}");
            }
            
            throw;
        }
    }

    public static Action Compile(string path, IMiniInterpreter i)
    {
        Console.WriteLine($"[编译调试] 开始编译文件: {path}");
        
        try
        {
            Console.WriteLine($"[编译调试] 解析代码");
            var statement = i.Build(Apis.FromFile(path));
            Console.WriteLine($"[编译调试] 解析完成");
            
            return Compile(statement, path, i);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"\n[编译错误] {path}");
            Console.Error.WriteLine($"[错误类型] {ex.GetType().Name}");
            Console.Error.WriteLine($"[错误信息] {ex.Message}");
            Console.Error.WriteLine($"[堆栈跟踪] {ex.StackTrace}");
            
            // 尝试获取更详细的位置信息
            if (ex is SyntaxError syntaxError)
            {
                Console.Error.WriteLine($"[位置信息] {syntaxError.Position}");
                if (syntaxError.SourceContext != null && syntaxError.SourceContext.Length > 0)
                {
                    Console.Error.WriteLine($"[上下文]");
                    foreach (var line in syntaxError.SourceContext)
                    {
                        Console.Error.WriteLine($"  {line}");
                    }
                }
                Console.Error.WriteLine($"[建议] {syntaxError.Suggestion}");
            }
            
            throw;
        }
    }
}