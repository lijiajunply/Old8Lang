using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 扩展方法声明的运行时适配与类型解析（阶段 2 语法/运行时分界）。
/// </summary>
public static class ExtensionDeclarationRuntimeExtensions
{
    private static readonly ConditionalWeakTable<ExtensionDeclaration, List<FuncLangValue>> Cache = new();

    /// <summary>获取扩展方法对应的运行时函数值列表（同一语法节点返回同一列表与实例）。</summary>
    public static List<FuncLangValue> GetExtensionMethods(this ExtensionDeclaration node) =>
        Cache.GetValue(node, static decl =>
            decl.ExtensionMethods.Select(FunctionSyntaxRuntimeFactory.GetOrCreateFunc).ToList());

    /// <summary>
    /// 解析目标类型名称到 .NET Type。
    /// </summary>
    public static Type? ResolveTargetType(string typeName)
    {
        // 内置类型映射到 Old8Lang 的包装类型
        return typeName.ToLower() switch
        {
            "string" => typeof(StringLangValue),
            "int" => typeof(IntLangValue),
            "double" => typeof(DoubleLangValue),
            "bool" => typeof(BoolLangValue),
            "char" => typeof(CharLangValue),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "decimal" => typeof(decimal),
            "object" => typeof(object),
            "list" => typeof(ListLangValue),
            "array" => typeof(Array),
            "dict" => typeof(DictionaryLangValue),
            _ => Type.GetType(typeName) // 尝试通过完全限定名解析
        };
    }
}
