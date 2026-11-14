using System.Runtime.CompilerServices;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 函数声明语法节点到运行时可调用值的适配工厂（阶段 2 语法/运行时分界）。
/// </summary>
public static class FunctionSyntaxRuntimeFactory
{
    private static readonly ConditionalWeakTable<FunctionSyntax, FuncLangValue> FuncCache = new();

    /// <summary>获取函数语法节点对应的运行时函数值（同一语法节点返回同一实例）。</summary>
    public static FuncLangValue GetOrCreateFunc(FunctionSyntax syntax) =>
        FuncCache.GetValue(syntax, CreateFunc);

    /// <summary>创建普通函数值，并复制装饰器与文档注释。</summary>
    public static FuncLangValue CreateFunc(FunctionSyntax syntax)
    {
        var func = new FuncLangValue(
            syntax.Id,
            syntax.Parameters,
            syntax.Body,
            syntax.GenericParameters,
            syntax.Position);

        if (syntax.Decorators is not null) func.Decorators = syntax.Decorators;
        if (syntax.DocComment is not null) func.DocComment = syntax.DocComment;
        return func;
    }

    /// <summary>创建异步函数值，并复制装饰器与文档注释。</summary>
    public static AsyncFuncLangValue CreateAsync(FunctionSyntax syntax)
    {
        var func = new AsyncFuncLangValue(syntax.Id, syntax.Parameters, syntax.Body, syntax.Position);
        if (syntax.Decorators is not null) func.Decorators = syntax.Decorators;
        if (syntax.DocComment is not null) func.DocComment = syntax.DocComment;
        return func;
    }
}

/// <summary>
/// <see cref="FuncInit"/> 的运行时可调用值访问入口；按语法节点缓存以保持原对象身份。
/// </summary>
public static partial class FuncInitRuntimeExtensions
{
    /// <summary>获取该函数声明的运行时函数值（同一语法节点返回同一实例）。</summary>
    public static FuncLangValue GetFuncValue(this FuncInit init) =>
        FunctionSyntaxRuntimeFactory.GetOrCreateFunc(init.Syntax);
}

/// <summary>
/// <see cref="NativeStatement"/> 的函数签名访问入口。
/// </summary>
public static class NativeStatementRuntimeExtensions
{
    /// <summary>获取 native 声明的运行时函数签名（无签名时返回 null）。</summary>
    public static FuncLangValue? GetFuncValue(this NativeStatement node) =>
        node.Function is null ? null : FunctionSyntaxRuntimeFactory.GetOrCreateFunc(node.Function);
}

/// <summary>
/// <see cref="AsyncFuncInit"/> 的运行时可调用值访问入口；按语法节点缓存以保持原对象身份。
/// </summary>
public static partial class AsyncFuncInitRuntimeExtensions
{
    private static readonly ConditionalWeakTable<FunctionSyntax, AsyncFuncLangValue> Cache = new();

    /// <summary>获取该异步函数声明的运行时函数值（同一语法节点返回同一实例）。</summary>
    public static AsyncFuncLangValue GetAsyncFuncValue(this AsyncFuncInit init) =>
        Cache.GetValue(init.Syntax, FunctionSyntaxRuntimeFactory.CreateAsync);
}
