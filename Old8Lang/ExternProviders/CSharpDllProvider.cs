using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;

namespace Old8Lang.ExternProviders;

/// <summary>
/// C# DLL 提供者
/// 支持从 .NET 程序集导入静态方法
/// 语法: extern "C#:System" Math { func Pow(x:double, y:double) -> double }
/// </summary>
public class CSharpDllProvider : IExternProvider
{
    /// <summary>
    /// 支持编译模式
    /// </summary>
    public bool SupportsCompilation => true;

    /// <summary>
    /// 解释模式：加载 C# 方法
    /// </summary>
    public void LoadFunctions(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        VariateManager manager)
    {
        // 解析程序集名称和类名
        var (assemblyName, className) = ParseSource(source);

        // 加载程序集
        Assembly assembly;
        try
        {
            assembly = AssemblyTypeCache.GetOrLoadAssembly(assemblyName);
        }
        catch (Exception ex)
        {
            throw new ImportError(default(SourcePosition), assemblyName,
                $"无法加载 C# 程序集 '{assemblyName}': {ex.Message}");
        }

        // 获取类型
        var type = AssemblyTypeCache.FindType(assembly, className);
        if (type == null)
        {
            throw new InvalidOperationError(new SourcePosition(0, 0),
                $"在程序集 '{assemblyName}' 中找不到类型 '{className}'");
        }

        // 导入函数
        foreach (var funcDecl in functions)
        {
            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            try
            {
                // 获取方法
                var methodInfo = FindMethod(type, funcDecl);

                if (methodInfo == null)
                {
                    throw new InvalidOperationError(new SourcePosition(0, 0),
                        $"在类型 '{className}' 中找不到方法 '{funcDecl.FunctionName}'");
                }

                // 创建包装函数（使用简单的构造函数）
                var funcValue = new FuncLangValue(targetName, methodInfo);

                manager.AddClassAndFunc(funcValue);
            }
            catch (Exception ex) when (ex is not InvalidOperationError)
            {
                throw new InvalidOperationError(new SourcePosition(0, 0),
                    $"导入 C# 方法 '{funcDecl.FunctionName}' 失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 编译模式：生成 IL 代码
    /// </summary>
    public void GenerateIL(
        string source,
        List<ExternFunctionDeclaration> functions,
        CallingConventionType defaultCallingConvention,
        ILGenerator ilGenerator,
        LocalManager localManager)
    {
        // 解析程序集名称和类名
        var (assemblyName, className) = ParseSource(source);

        // 加载程序集
        var assembly = AssemblyTypeCache.GetOrLoadAssembly(assemblyName);

        // 获取类型
        var type = AssemblyTypeCache.FindType(assembly, className);
        if (type == null)
        {
            throw new InvalidOperationError(new SourcePosition(0, 0),
                $"找不到类型 '{className}'");
        }

        foreach (var funcDecl in functions)
        {
            var targetName = funcDecl.Alias ?? funcDecl.FunctionName;

            try
            {
                // 获取方法
                var methodInfo = FindMethod(type, funcDecl);

                if (methodInfo == null)
                {
                    throw new InvalidOperationError(new SourcePosition(0, 0),
                        $"找不到方法 '{funcDecl.FunctionName}'");
                }

                // 构建参数类型签名
                var signature = funcDecl.FunctionSignature?.FuncValue;
                var paramTypes = signature?.Ids?
                    .Select(p => ConvertOld8TypeToCSharpType(p.AssumptionType))
                    .ToArray() ?? [];
                var paramTypeNames = string.Join("_", paramTypes.Select(t => t.Name));
                var delegateKey = $"{targetName}${paramTypeNames}";

                // 注册到局部变量管理器
                localManager.DelegateVar.Add(delegateKey, methodInfo);

                // 同时注册不带签名的键
                if (!localManager.DelegateVar.ContainsKey(targetName))
                {
                    localManager.DelegateVar.Add(targetName, methodInfo);
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationError)
            {
                throw new InvalidOperationError(new SourcePosition(0, 0),
                    $"编译 C# 方法 '{funcDecl.FunctionName}' 失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 解析源字符串，提取程序集名称和类名
    /// 格式: "C#:AssemblyName" ClassName 或 "C#:Namespace.ClassName" 或 "dotnetdll:path/to/file.dll" ClassName
    /// </summary>
    private (string assemblyName, string className) ParseSource(string source)
    {
        // 移除前缀
        string content;
        if (source.StartsWith("C#:", StringComparison.OrdinalIgnoreCase))
            content = source.Substring("C#:".Length);
        else if (source.StartsWith("cs:", StringComparison.OrdinalIgnoreCase))
            content = source.Substring("cs:".Length);
        else if (source.StartsWith("csharp:", StringComparison.OrdinalIgnoreCase))
            content = source.Substring("csharp:".Length);
        else if (source.StartsWith("dotnetdll:", StringComparison.OrdinalIgnoreCase))
            content = source.Substring("dotnetdll:".Length);
        else
            content = source;

        // 解析程序集名称和类名
        // 格式可以是:
        // 1. "System" Math -> assemblyName="System", className="Math" (需要从 extern 语句中获取类名)
        // 2. "System.Math" -> assemblyName="System", className="Math"
        // 3. "mscorlib" System.Math -> assemblyName="mscorlib", className="System.Math"

        // 尝试将内容作为完整的类型名解析
        // 例如: "System.Math" -> 程序集名为包含该类型的程序集
        var parts = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 2)
        {
            // 格式: "AssemblyName ClassName"
            return (parts[0], parts[1]);
        }
        else if (parts.Length == 1)
        {
            // 格式: "Namespace.ClassName" 或 "AssemblyName"
            // 尝试从已加载的程序集中查找类型
            var typeName = parts[0];

            // 检查是否是完整的类型名（包含命名空间）
            if (typeName.Contains('.'))
            {
                // 尝试从标准程序集中查找
                var type = Type.GetType(typeName) ??
                           Type.GetType($"{typeName}, System.Runtime") ??
                           Type.GetType($"{typeName}, mscorlib");

                if (type != null)
                {
                    return (type.Assembly.GetName().Name ?? "System.Runtime", typeName);
                }

                // 如果找不到，假设最后一部分是类名，其余是命名空间
                var lastDot = typeName.LastIndexOf('.');
                var namespacePart = typeName.Substring(0, lastDot);
                var classNamePart = typeName.Substring(lastDot + 1);

                // 尝试推断程序集名称
                return (namespacePart.Split('.')[0], typeName);
            }
            else
            {
                // 只有类名，假设在 System 命名空间
                return ("System.Runtime", typeName);
            }
        }

        throw new FormatError(new SourcePosition(0, 0),
            $"无效的 C# extern 源格式: '{source}'。期望格式: 'C#:AssemblyName ClassName' 或 'C#:Namespace.ClassName'");
    }

    /// <summary>
    /// 在类型中查找方法
    /// </summary>
    private MethodInfo? FindMethod(Type type, ExternFunctionDeclaration funcDecl)
    {
        var methodName = funcDecl.FunctionName;

        // 获取参数类型
        var signature = funcDecl.FunctionSignature?.FuncValue;
        var paramTypes = signature?.Ids?
            .Select(p => ConvertOld8TypeToCSharpType(p.AssumptionType))
            .ToArray();

        // 使用缓存查找方法
        return AssemblyTypeCache.FindMethod(type, methodName, paramTypes);
    }

    /// <summary>
    /// 将 Old8Lang 类型转换为 C# 类型
    /// </summary>
    private Type ConvertOld8TypeToCSharpType(string? old8Type)
    {
        return old8Type?.ToLower() switch
        {
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "float" => typeof(float),
            "bool" => typeof(bool),
            "string" => typeof(string),
            "void" => typeof(void),
            "object" => typeof(object),
            "char" => typeof(char),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "uint" => typeof(uint),
            "ulong" => typeof(ulong),
            "ushort" => typeof(ushort),
            null => typeof(object),
            _ => typeof(object)
        };
    }
}
