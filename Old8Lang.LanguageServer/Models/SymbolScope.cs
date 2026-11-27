namespace Old8Lang.LanguageServer.Models;

/// <summary>
/// 词法作用域种类
/// </summary>
public enum ScopeKind
{
    Global,
    Class,
    Function,
    Method,
    Block,
    Loop,
    Catch,
    Using,
    Lambda
}

/// <summary>
/// 词法作用域节点，形成从最内层块指向全局作用域的作用域链。
/// 所有位置均采用 LSP 0-based (Line, Column) 约定。
/// </summary>
public class SymbolScope
{
    /// <summary>作用域类型</summary>
    public required ScopeKind Kind { get; init; }

    /// <summary>父作用域（全局作用域为 null）</summary>
    public SymbolScope? Parent { get; init; }

    /// <summary>子作用域列表</summary>
    public List<SymbolScope> Children { get; } = [];

    /// <summary>作用域在源码中的起止范围（0-based，包含起止点）</summary>
    public required SourceLocation Range { get; init; }

    /// <summary>当前作用域中生效的符号表（按名称索引，首次/主声明）</summary>
    public Dictionary<string, SymbolInfo> Symbols { get; } = new(StringComparer.Ordinal);

    /// <summary>当前作用域内的全部显式声明记录（保留同名重复声明，供重复定义诊断使用）</summary>
    public List<SymbolInfo> AllDeclarations { get; } = [];

    /// <summary>关联的宿主符号（类作用域对应类符号，函数/方法作用域对应函数/方法符号）</summary>
    public SymbolInfo? OwnerSymbol { get; init; }

    /// <summary>是否为函数/全局边界（变量赋值默认不会跨越函数边界声明到外层）</summary>
    public bool IsFunctionBoundary =>
        Kind is ScopeKind.Global or ScopeKind.Function or ScopeKind.Method or ScopeKind.Lambda;

    /// <summary>
    /// 在当前作用域声明一个符号。
    /// </summary>
    public void Declare(SymbolInfo symbol, bool recordDuplicateCheck = true)
    {
        symbol.DeclaringScope = this;
        if (recordDuplicateCheck)
        {
            AllDeclarations.Add(symbol);
        }

        // 保留首个声明或由显式重新声明覆盖（重复定义会在 AllDeclarations 中被诊断发现）
        Symbols[symbol.Name] = symbol;
    }

    /// <summary>
    /// 判断指定 0-based 位置是否落在本作用域范围内。
    /// </summary>
    public bool ContainsPosition(int line, int column)
    {
        if (Kind == ScopeKind.Global)
        {
            return true;
        }

        if (line < Range.Line || line > Range.EndLine)
        {
            return false;
        }

        if (line == Range.Line && column < Range.Column)
        {
            return false;
        }

        if (line == Range.EndLine && column > Range.EndColumn)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 查找包含指定 0-based 位置的最内层作用域。
    /// </summary>
    public SymbolScope FindInnermostScope(int line, int column)
    {
        SymbolScope? bestMatch = null;

        foreach (var child in Children)
        {
            if (!child.ContainsPosition(line, column))
            {
                continue;
            }

            var candidate = child.FindInnermostScope(line, column);
            if (bestMatch == null || IsNarrowerRange(candidate.Range, bestMatch.Range))
            {
                bestMatch = candidate;
            }
        }

        return bestMatch ?? this;
    }

    /// <summary>
    /// 沿作用域链向上解析符号；若指定位置，局部/块变量仅在声明之后（或声明位置本身）可见。
    /// </summary>
    public SymbolInfo? Resolve(string name, int? line = null, int? column = null)
    {
        var current = this;

        while (current != null)
        {
            if (current.Symbols.TryGetValue(name, out var symbol))
            {
                if (IsVisibleAtPosition(current, symbol, line, column))
                {
                    return symbol;
                }
            }

            // 在类的实例方法内允许裸名称访问类自身成员（如 return value）
            if (current.Kind == ScopeKind.Class && current.OwnerSymbol != null)
            {
                if (current.OwnerSymbol.Members.TryGetValue(name, out var member))
                {
                    return member;
                }
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// 在当前函数/方法/Lambda 作用域链内查找已声明的局部变量或参数（不跨函数边界到全局）。
    /// 用于判断块内赋值 `x <- expr` 是重新赋值还是新局部声明。
    /// </summary>
    public SymbolInfo? ResolveInCurrentFunction(string name)
    {
        var current = this;
        while (current != null)
        {
            if (current.Symbols.TryGetValue(name, out var symbol))
            {
                return symbol;
            }

            if (current.IsFunctionBoundary)
            {
                break;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>
    /// 获取当前位置可见的全部符号（内层同名符号遮蔽外层）。
    /// </summary>
    public List<SymbolInfo> GetVisibleSymbols(int line, int column)
    {
        var scope = FindInnermostScope(line, column);
        var result = new List<SymbolInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        var enclosingMethod = scope.FindEnclosingMethod();
        var enclosingClass = scope.FindEnclosingClass();

        var current = scope;
        while (current != null)
        {
            if (current.Kind == ScopeKind.Class && current.OwnerSymbol != null)
            {
                if (enclosingMethod != null)
                {
                    var isStaticMethod = enclosingMethod.IsStatic;

                    if (!isStaticMethod && seen.Add("this"))
                    {
                        result.Add(CreateThisSymbol(current.OwnerSymbol));
                    }

                    if (!isStaticMethod && !string.IsNullOrEmpty(current.OwnerSymbol.BaseClassName) && seen.Add("super"))
                    {
                        result.Add(CreateSuperSymbol(current.OwnerSymbol));
                    }

                    foreach (var member in current.OwnerSymbol.Members.Values)
                    {
                        if (isStaticMethod && !member.IsStatic)
                        {
                            continue;
                        }

                        if (seen.Add(member.Name))
                        {
                            // 类方法体内直接可见的字段按 Field 呈现（与成员访问的 Property 保持区分）
                            if (member.Kind == SymbolKind.Property)
                            {
                                result.Add(new SymbolInfo
                                {
                                    Name = member.Name,
                                    Kind = SymbolKind.Field,
                                    Type = member.Type,
                                    Location = member.Location,
                                    Documentation = member.Documentation,
                                    Parent = member.Parent,
                                    AccessModifier = member.AccessModifier,
                                    IsStatic = member.IsStatic,
                                    DeclaringScope = member.DeclaringScope
                                });
                            }
                            else
                            {
                                result.Add(member);
                            }
                        }
                    }
                }

                current = current.Parent;
                continue;
            }

            foreach (var symbol in current.Symbols.Values)
            {
                if (!IsVisibleAtPosition(current, symbol, line, column))
                {
                    continue;
                }

                if (seen.Add(symbol.Name))
                {
                    result.Add(symbol);
                }
            }

            current = current.Parent;
        }

        return result;
    }

    /// <summary>查找所在类符号（若当前处于类体内）</summary>
    public SymbolInfo? FindEnclosingClass()
    {
        var current = this;
        while (current != null)
        {
            if (current.Kind == ScopeKind.Class && current.OwnerSymbol != null)
            {
                return current.OwnerSymbol;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>查找所在方法符号（若当前处于类方法体内）</summary>
    public SymbolInfo? FindEnclosingMethod()
    {
        var current = this;
        while (current != null)
        {
            if (current.Kind == ScopeKind.Method && current.OwnerSymbol != null)
            {
                return current.OwnerSymbol;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>查找所在函数或方法符号</summary>
    public SymbolInfo? FindEnclosingCallable()
    {
        var current = this;
        while (current != null)
        {
            if (current.Kind is ScopeKind.Function or ScopeKind.Method && current.OwnerSymbol != null)
            {
                return current.OwnerSymbol;
            }

            current = current.Parent;
        }

        return null;
    }

    /// <summary>遍历本作用域及所有后代作用域</summary>
    public IEnumerable<SymbolScope> EnumerateAllScopes()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var descendant in child.EnumerateAllScopes())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>创建 `this` 伪符号</summary>
    public static SymbolInfo CreateThisSymbol(SymbolInfo classSymbol) => new()
    {
        Name = "this",
        Kind = SymbolKind.Keyword,
        Type = classSymbol.Name,
        Location = classSymbol.Location,
        Parent = classSymbol,
        Documentation = $"当前 `{classSymbol.Name}` 实例引用"
    };

    /// <summary>创建 `super` 伪符号</summary>
    public static SymbolInfo CreateSuperSymbol(SymbolInfo classSymbol) => new()
    {
        Name = "super",
        Kind = SymbolKind.Keyword,
        Type = classSymbol.BaseClassName,
        Location = classSymbol.Location,
        Parent = classSymbol,
        Documentation = $"父类 `{classSymbol.BaseClassName}` 引用"
    };

    private static bool IsVisibleAtPosition(SymbolScope scope, SymbolInfo symbol, int? line, int? column)
    {
        if (!line.HasValue)
        {
            return true;
        }

        // 函数、类、枚举、方法、属性、参数在所属作用域内可前向可见
        if (symbol.Kind is SymbolKind.Function or SymbolKind.Class or SymbolKind.Interface or SymbolKind.Enum
            or SymbolKind.Method or SymbolKind.Property or SymbolKind.Field or SymbolKind.Parameter
            or SymbolKind.EnumMember)
        {
            return true;
        }

        // 全局顶层变量在同文件内也允许函数体前向引用；只有同在全局作用域且尚未声明时受行序限制
        if (scope.Kind == ScopeKind.Global)
        {
            return true;
        }

        if (symbol.Location.Line < line.Value)
        {
            return true;
        }

        if (symbol.Location.Line == line.Value)
        {
            return !column.HasValue || symbol.Location.Column <= column.Value;
        }

        return false;
    }

    private static bool IsNarrowerRange(SourceLocation candidate, SourceLocation current)
    {
        var candidateSpan = (candidate.EndLine - candidate.Line) * 100_000 + (candidate.EndColumn - candidate.Column);
        var currentSpan = (current.EndLine - current.Line) * 100_000 + (current.EndColumn - current.Column);
        return candidateSpan < currentSpan;
    }
}
