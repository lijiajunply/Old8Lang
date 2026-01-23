using System.Reflection;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

// ReSharper disable CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 函数 ，作为一种变量存在
/// </summary>
public partial class FuncLangValue : ImportInfo
{
    public readonly LangId? Id;
    public readonly BlockStatement BlockStatement = new([]);

    public readonly List<LangId>? Ids;

    public readonly MethodInfo? Method;

    private readonly FuncLangValue? _func;

    // 闭包环境：捕获的作用域，用于支持闭包变量访问
    internal VariateManager? CapturedScope { get; init; }

    /// <summary>
    /// 获取捕获的作用域（用于装饰器等场景）
    /// </summary>
    internal VariateManager? GetCapturedScope() => CapturedScope;

    // 函数类型：区分普通方法和Lambda表达式
    private bool IsLambda { get; init; }

    // 默认参数值缓存：缓存常量表达式的默认值，避免重复求值
    private Dictionary<int, LangValueType>? CachedDefaultValues { get; set; }

    /// <summary>
    /// 泛型参数列表
    /// 例如: func map&lt;T, U>(...) 中的 [T, U]
    /// </summary>
    public readonly List<GenericParameter>? GenericParameters;

    /// <summary>
    /// 是否为泛型函数
    /// </summary>
    public bool IsGeneric => GenericParameters is { Count: > 0 };

    /// <summary>
    /// 当前实例的类型参数映射（用于泛型实例化）
    /// 例如: map&lt;int, string> 时为 {"T": int, "U": string}
    /// </summary>
    public Dictionary<string, ITypeInfo>? TypeArgumentMapping { get; set; }

    /// <summary>
    /// 文档注释内容
    /// 存储通过 /// 语法编写的函数文档注释（结构化）
    /// </summary>
    public DocCommentInfo? DocComment { get; set; }

    /// <summary>
    /// 装饰器列表
    /// 存储应用于此函数的装饰器（从上到下的顺序）
    /// </summary>
    public List<FunctionDecorator>? Decorators { get; set; }

    public FuncLangValue(
        LangId? id,
        List<LangId> ids,
        BlockStatement blockStatement,
        List<GenericParameter>? genericParameters = null,
        SourcePosition position = default,
        bool isLambda = false) :
        base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
        GenericParameters = genericParameters;
        IsLambda = isLambda;
    }

    public FuncLangValue(string idName, MethodInfo methodInfo, FuncLangValue? func = null,
        SourcePosition position = default) : base(position)
    {
        Id = new LangId(idName);
        Method = methodInfo;
        _func = func;
        IsLambda = false; // 原生方法不是Lambda表达式
        GenericParameters = null; // 原生方法暂不支持泛型
    }

    /// <summary>
    /// 检查函数是否是生成器函数（包含yield语句）
    /// </summary>
}
