namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 符号信息 - 用于符号表
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// 符号名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 符号类型（变量、函数、类等）
    /// </summary>
    public required SymbolKind Kind { get; set; }

    /// <summary>
    /// 符号的类型（数据类型）
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 符号定义的位置
    /// </summary>
    public required SourceLocation Location { get; set; }

    /// <summary>
    /// 符号的文档注释
    /// </summary>
    public string? Documentation { get; set; }

    /// <summary>
    /// 符号的引用位置列表
    /// </summary>
    public List<SourceLocation> References { get; set; } = [];

    /// <summary>
    /// 成员列表（仅对类有效）
    /// 存储类的方法和属性
    /// </summary>
    public Dictionary<string, SymbolInfo> Members { get; set; } = new();

    /// <summary>
    /// 父符号（用于成员追溯到所属类）
    /// </summary>
    public SymbolInfo? Parent { get; set; }

    /// <summary>
    /// 访问修饰符
    /// </summary>
    public AccessModifier AccessModifier { get; set; } = AccessModifier.Public;

    /// <summary>
    /// 是否为静态成员
    /// </summary>
    public bool IsStatic { get; set; }

    /// <summary>
    /// 函数/方法的参数列表（仅对函数和方法有效）
    /// 存储参数符号信息，包括参数名称、类型和位置
    /// </summary>
    public List<SymbolInfo> Parameters { get; set; } = [];

    /// <summary>
    /// 调用的其他函数列表 (用于调用层次)
    /// 存储此函数/方法调用的其他函数的名称和位置
    /// </summary>
    public List<CallSite> Calls { get; set; } = [];

    /// <summary>
    /// 被哪些函数调用 (用于调用层次)
    /// 存储调用此函数/方法的其他函数的名称和位置
    /// </summary>
    public List<CallSite> CalledBy { get; set; } = [];

    /// <summary>
    /// 声明该符号所在的词法作用域（为空或 Global 表示顶层符号）
    /// </summary>
    public SymbolScope? DeclaringScope { get; set; }

    /// <summary>
    /// 是否为顶层全局符号
    /// </summary>
    public bool IsTopLevel => DeclaringScope == null || DeclaringScope.Kind == ScopeKind.Global;

    /// <summary>
    /// 父类名称（仅对类有效）
    /// </summary>
    public string? BaseClassName { get; set; }

    /// <summary>
    /// 实现的接口名称列表（对接口而言为父接口列表）
    /// </summary>
    public List<string> InterfaceNames { get; set; } = [];

    /// <summary>
    /// 混入的 Mixin 名称列表
    /// </summary>
    public List<string> MixinNames { get; set; } = [];

    /// <summary>是否为接口声明</summary>
    public bool IsInterface { get; set; }

    /// <summary>是否为抽象类</summary>
    public bool IsAbstract { get; set; }

    /// <summary>是否为 Mixin 声明</summary>
    public bool IsMixin { get; set; }

    /// <summary>变量/字段/参数是否带有显式类型注解（用于区分显式注解类型不匹配诊断）</summary>
    public bool HasExplicitTypeAnnotation { get; set; }

    /// <summary>泛型参数名列表（仅对泛型类/泛型函数有效）</summary>
    public List<string> GenericParameters { get; set; } = [];

    /// <summary>参数是否有默认值（仅对 Parameter 有效）</summary>
    public bool HasDefaultValue { get; set; }

    /// <summary>参数是否为 params 可变参数（仅对 Parameter 有效）</summary>
    public bool IsParams { get; set; }
}

/// <summary>
/// 调用点信息
/// </summary>
public class CallSite
{
    /// <summary>
    /// 被调用/调用者的符号名称
    /// </summary>
    public required string SymbolName { get; set; }

    /// <summary>
    /// 调用发生的位置
    /// </summary>
    public required SourceLocation Location { get; set; }
}

/// <summary>
/// 符号类型
/// </summary>
public enum SymbolKind
{
    Variable,
    Function,
    Class,
    Method,
    Property,
    Parameter,
    Constant,
    Field,
    Keyword,
    Enum,
    EnumMember,
    Interface,
    Module
}

/// <summary>
/// 源代码位置
/// </summary>
public class SourceLocation
{
    public required string Uri { get; set; }
    public required int Line { get; set; }
    public required int Column { get; set; }
    public int EndLine { get; set; }
    public int EndColumn { get; set; }
}

/// <summary>
/// 访问修饰符
/// </summary>
public enum AccessModifier
{
    Public,
    Private,
    Protected
}
