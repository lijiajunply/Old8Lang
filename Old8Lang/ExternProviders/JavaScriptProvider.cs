using System.Reflection.Emit;
using Jint;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.ExternProviders;

/// <summary>
/// JavaScript 语言提供者
/// 使用 Jint 引擎执行 JavaScript 代码
/// </summary>
public class JavaScriptProvider : IExternProvider
{
    /// <summary>
    /// 不支持编译模式（JavaScript 需要动态运行时）
    /// </summary>
    public bool SupportsCompilation => false;

    /// <summary>
    /// 解释模式：加载 JavaScript 函数
    /// </summary>
    public void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager)
    {
        // 解析脚本路径
        var scriptPath = source.StartsWith("js:")
            ? source.Substring("js:".Length)
            : source;

        // 解析脚本路径（支持相对路径）
        string fullPath;
        if (Path.IsPathRooted(scriptPath))
        {
            fullPath = scriptPath;
        }
        else
        {
            // 相对路径优先从当前工作目录解析
            var cwdPath = Path.Combine(Directory.GetCurrentDirectory(), scriptPath);
            if (File.Exists(cwdPath))
            {
                fullPath = cwdPath;
            }
            else
            {
                // 如果当前目录找不到,尝试从脚本文件所在目录解析
                var baseDir = manager.Path != null && File.Exists(manager.Path)
                    ? Path.GetDirectoryName(manager.Path) ?? Directory.GetCurrentDirectory()
                    : manager.Path ?? Directory.GetCurrentDirectory();

                fullPath = Path.Combine(baseDir, scriptPath);
            }
        }

        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationError(default(SourcePosition),
                $"JavaScript 脚本文件不存在：{fullPath}");
        }

        // 创建 Jint 引擎并执行脚本
        var engine = new Engine();

        try
        {
            var code = File.ReadAllText(fullPath);
            engine.Execute(code);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(default(SourcePosition),
                $"JavaScript 脚本执行失败：{ex.Message}");
        }

        // 导入函数
        foreach (var funcDecl in functions)
        {
            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            // 检查 JavaScript 函数是否存在
            var jsFunction = engine.GetValue(funcDecl.FunctionName);
            if (jsFunction.IsUndefined())
            {
                throw new InvalidOperationError(default(SourcePosition),
                    $"JavaScript 模块中找不到函数：{funcDecl.FunctionName}");
            }

            // 检查是否为函数（Jint 中函数也是对象）
            if (!jsFunction.IsObject())
            {
                throw new InvalidOperationError(default(SourcePosition),
                    $"JavaScript 对象 {funcDecl.FunctionName} 不是函数");
            }

            // 创建包装函数
            var wrapperFunc = CreateJavaScriptFunctionWrapper(engine, funcDecl, targetName);
            manager.AddClassAndFunc(wrapperFunc);
        }
    }

    /// <summary>
    /// 编译模式：不支持
    /// </summary>
    public void GenerateIL(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        ILGenerator ilGenerator,
        LocalManager localManager)
    {
        throw new NotSupportedException("JavaScript extern 函数不支持编译模式，仅支持解释模式执行。");
    }

    /// <summary>
    /// 创建 JavaScript 函数包装器
    /// </summary>
    private JavaScriptFunctionLangValue CreateJavaScriptFunctionWrapper(
        Engine engine,
        ExternFunctionDeclaration funcDecl,
        string targetName)
    {
        var signature = funcDecl.FunctionSignature?.FuncLangValue;
        var parameters = signature?.Ids ?? new List<LangId>();
        var returnType = signature?.Id?.AssumptionType;

        // 创建包装函数
        var funcValue = new JavaScriptFunctionLangValue(targetName, engine, funcDecl.FunctionName, parameters, returnType);

        return funcValue;
    }
}
