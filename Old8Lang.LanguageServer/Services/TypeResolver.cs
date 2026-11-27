using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.LanguageServer.Models;
using Old8Lang.LangParser;
using Old8Lang.Syntax;

namespace Old8Lang.LanguageServer.Services;

/// <summary>
/// 成员访问权限上下文
/// </summary>
public enum MemberAccessLevel
{
    /// <summary>类外部（只能访问 public）</summary>
    External,
    /// <summary>子类内部（可访问 public 与 protected）</summary>
    Protected,
    /// <summary>本类内部（可访问 public、protected、private）</summary>
    Private
}

/// <summary>
/// 成员解析结果（含声明该成员的类，用于区分继承成员与自身成员）
/// </summary>
public sealed record ResolvedMember(SymbolInfo Member, SymbolInfo DeclaringClass);

/// <summary>
/// 统一的类型与成员解析服务：负责类型名归一化、继承/Mixin/接口成员聚合、访问权限判断、
/// 赋值兼容性检查以及 AST 表达式类型推断。
/// </summary>
public static class TypeResolver
{
    private static readonly HashSet<string> DynamicTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "any", "var", "object", ""
    };

    private static readonly HashSet<string> PrimitiveTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "int", "double", "string", "bool", "char", "void", "null"
    };

    private static readonly HashSet<string> BuiltInCollectionTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "list", "array", "dict", "dictionary", "tuple", "function", "task", "module"
    };

    /// <summary>
    /// 归一化类型名：去除前导 <c>static </c>、可空后缀 <c>?</c> 和泛型参数 <c>&lt;...&gt;</c>。
    /// </summary>
    public static string NormalizeTypeName(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            return string.Empty;
        }

        var trimmed = rawType.Trim();
        if (trimmed.StartsWith("static ", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring("static ".Length).Trim();
        }

        // 如果传入的是函数/方法签名，先提取返回类型
        var arrowIndex = trimmed.LastIndexOf("->", StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            trimmed = trimmed.Substring(arrowIndex + 2).Trim();
        }

        // 剥离尾部可空标记
        while (trimmed.EndsWith('?'))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 1).Trim();
        }

        // 剥离泛型实参，例如 Box<int> -> Box, list<string> -> list
        var genericIndex = trimmed.IndexOf('<');
        if (genericIndex > 0)
        {
            trimmed = trimmed.Substring(0, genericIndex).Trim();
        }

        return trimmed;
    }

    /// <summary>
    /// 从方法/函数签名中提取原始返回类型（保留可空与联合类型以便类型检查）。
    /// </summary>
    public static string? ExtractRawReturnType(string? signature)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return null;
        }

        var arrowIndex = signature.LastIndexOf("->", StringComparison.Ordinal);
        if (arrowIndex >= 0)
        {
            return signature.Substring(arrowIndex + 2).Trim();
        }

        var trimmed = signature.Trim();
        if (trimmed.StartsWith("static ", StringComparison.Ordinal))
        {
            trimmed = trimmed.Substring("static ".Length).Trim();
        }

        return trimmed;
    }

    /// <summary>提取返回类型（与 <see cref="ExtractRawReturnType"/> 等价）</summary>
    public static string? ExtractReturnType(string? signature) => ExtractRawReturnType(signature);

    /// <summary>判断是否为动态/未定型类型</summary>
    public static bool IsDynamicType(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return true;
        }

        return DynamicTypes.Contains(NormalizeTypeName(typeName));
    }

    /// <summary>判断是否为语言内置类型</summary>
    public static bool IsBuiltInType(string? typeName)
    {
        var normalized = NormalizeTypeName(typeName);
        return PrimitiveTypes.Contains(normalized) ||
               BuiltInCollectionTypes.Contains(normalized) ||
               DynamicTypes.Contains(normalized);
    }

    /// <summary>
    /// 按类型名查找类/接口/枚举符号（优先当前文档符号表，其次工作区索引）。
    /// </summary>
    public static SymbolInfo? ResolveTypeSymbol(
        string? typeName,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        string? preferredUri = null)
    {
        var normalized = NormalizeTypeName(typeName);
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        if (localSymbols != null &&
            localSymbols.TryGetValue(normalized, out var localSymbol) &&
            IsTypeDeclaration(localSymbol))
        {
            return localSymbol;
        }

        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(normalized, preferredUri, out var workspaceSymbol, out _) &&
            workspaceSymbol != null &&
            IsTypeDeclaration(workspaceSymbol))
        {
            return workspaceSymbol;
        }

        return null;
    }

    /// <summary>判断符号是否为类/接口/枚举类型声明</summary>
    public static bool IsTypeDeclaration(SymbolInfo symbol) =>
        symbol.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Enum;

    /// <summary>判断符号是否为类型声明（类/接口/枚举）</summary>
    public static bool IsTypeSymbol(SymbolInfo symbol) => IsTypeDeclaration(symbol);

    /// <summary>
    /// 收集类的全部成员（包含基类继承链、Mixin、接口成员；派生类成员覆盖基类同名成员）。
    /// </summary>
    public static List<SymbolInfo> GetAllMembers(
        SymbolInfo typeSymbol,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        bool staticOnly = false,
        MemberAccessLevel? accessLevel = null,
        string? preferredUri = null)
    {
        var result = new List<SymbolInfo>();
        var seenMembers = new HashSet<string>(StringComparer.Ordinal);
        var visitedTypes = new HashSet<string>(StringComparer.Ordinal);

        CollectMembersRecursive(
            typeSymbol,
            isDirectType: true,
            localSymbols,
            workspaceIndex,
            preferredUri,
            staticOnly,
            accessLevel,
            seenMembers,
            visitedTypes,
            result);

        return result;
    }

    /// <summary>
    /// 在类及其继承链/Mixin/接口中按名称查找成员。
    /// </summary>
    public static (SymbolInfo Member, SymbolInfo DeclaringClass)? FindMember(
        SymbolInfo typeSymbol,
        string memberName,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        string? preferredUri = null)
    {
        var visitedTypes = new HashSet<string>(StringComparer.Ordinal);
        return FindMemberRecursive(typeSymbol, memberName, localSymbols, workspaceIndex, preferredUri, visitedTypes);
    }

    /// <summary>
    /// 计算在 <paramref name="enclosingClass"/> 内访问 <paramref name="targetClass"/> 成员的权限级别。
    /// </summary>
    public static MemberAccessLevel GetAccessLevel(
        SymbolInfo? enclosingClass,
        SymbolInfo targetClass,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        string? preferredUri = null)
    {
        if (enclosingClass == null)
        {
            return MemberAccessLevel.External;
        }

        if (string.Equals(enclosingClass.Name, targetClass.Name, StringComparison.Ordinal))
        {
            return MemberAccessLevel.Private;
        }

        if (IsDerivedFrom(enclosingClass, targetClass.Name, localSymbols, workspaceIndex, preferredUri))
        {
            return MemberAccessLevel.Protected;
        }

        return MemberAccessLevel.External;
    }

    /// <summary>判断某访问修饰符是否在允许级别内可见</summary>
    public static bool IsMemberAccessible(AccessModifier modifier, MemberAccessLevel accessLevel, bool isInherited)
    {
        return modifier switch
        {
            AccessModifier.Public => true,
            AccessModifier.Protected => accessLevel is MemberAccessLevel.Protected or MemberAccessLevel.Private,
            AccessModifier.Private => accessLevel == MemberAccessLevel.Private && !isInherited,
            _ => true
        };
    }

    /// <summary>判断 <paramref name="derived"/> 是否直接或间接继承/实现/混入 <paramref name="baseName"/>。</summary>
    public static bool IsDerivedFrom(
        SymbolInfo derived,
        string baseName,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        string? preferredUri = null)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return IsDerivedFromRecursive(derived, baseName, localSymbols, workspaceIndex, preferredUri, visited);
    }

    /// <summary>
    /// 判断源类型是否可赋值给目标类型（用于语义诊断的类型不匹配检测）。
    /// </summary>
    public static bool IsAssignableTo(
        string? sourceType,
        string? targetType,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        string? preferredUri = null,
        HashSet<string>? genericParameters = null)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || string.IsNullOrWhiteSpace(targetType))
        {
            return true;
        }

        var srcRaw = sourceType.Trim();
        var dstRaw = targetType.Trim();

        if (srcRaw.StartsWith("static ", StringComparison.Ordinal))
        {
            srcRaw = srcRaw.Substring("static ".Length).Trim();
        }
        if (dstRaw.StartsWith("static ", StringComparison.Ordinal))
        {
            dstRaw = dstRaw.Substring("static ".Length).Trim();
        }

        // 泛型参数名（如 T）在静态检查中放行
        var srcNorm = NormalizeTypeName(srcRaw);
        var dstNorm = NormalizeTypeName(dstRaw);
        if (IsDynamicType(srcNorm) || IsDynamicType(dstNorm))
        {
            return true;
        }

        if (genericParameters != null &&
            (genericParameters.Contains(srcNorm) || genericParameters.Contains(dstNorm)))
        {
            return true;
        }

        // 联合类型目标：A | B —— 任一分支兼容即可
        if (dstRaw.Contains('|'))
        {
            var branches = dstRaw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return branches.Any(branch =>
                IsAssignableTo(srcRaw, branch, localSymbols, workspaceIndex, preferredUri, genericParameters));
        }

        // 联合类型源：A | B —— 所有分支都兼容目标（保守：只要存在兼容分支也不误报）
        if (srcRaw.Contains('|'))
        {
            var branches = srcRaw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return branches.Any(branch =>
                IsAssignableTo(branch, dstRaw, localSymbols, workspaceIndex, preferredUri, genericParameters));
        }

        // 交叉类型目标：A & B —— 满足所有分支
        if (dstRaw.Contains('&'))
        {
            var branches = dstRaw.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return branches.All(branch =>
                IsAssignableTo(srcRaw, branch, localSymbols, workspaceIndex, preferredUri, genericParameters));
        }

        // null 可赋给可空类型或非基础值类型的引用/类/集合类型
        if (string.Equals(srcNorm, "null", StringComparison.OrdinalIgnoreCase))
        {
            if (dstRaw.EndsWith('?'))
            {
                return true;
            }

            // Old8Lang 中类、字符串、集合都允许赋 null；纯基础值类型 int/double/bool/char 在非可空时不允许 null
            return !dstNorm.Equals("int", StringComparison.OrdinalIgnoreCase) &&
                   !dstNorm.Equals("double", StringComparison.OrdinalIgnoreCase) &&
                   !dstNorm.Equals("bool", StringComparison.OrdinalIgnoreCase) &&
                   !dstNorm.Equals("char", StringComparison.OrdinalIgnoreCase);
        }

        // 大小写不敏感匹配基础与集合别名（dict/dictionary）
        if (string.Equals(srcNorm, dstNorm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if ((srcNorm.Equals("dict", StringComparison.OrdinalIgnoreCase) && dstNorm.Equals("dictionary", StringComparison.OrdinalIgnoreCase)) ||
            (srcNorm.Equals("dictionary", StringComparison.OrdinalIgnoreCase) && dstNorm.Equals("dict", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 数值宽化：int -> double
        if (srcNorm.Equals("int", StringComparison.OrdinalIgnoreCase) &&
            dstNorm.Equals("double", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 枚举成员底层可当 int 使用
        var srcSym = ResolveTypeSymbol(srcNorm, localSymbols, workspaceIndex, preferredUri);
        var dstSym = ResolveTypeSymbol(dstNorm, localSymbols, workspaceIndex, preferredUri);

        if (srcSym is { Kind: SymbolKind.Enum } && dstNorm.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (dstSym is { Kind: SymbolKind.Enum } && srcNorm.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 继承 / 接口 / Mixin 子类型兼容
        if (srcSym != null && IsDerivedFrom(srcSym, dstNorm, localSymbols, workspaceIndex, preferredUri))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 基于作用域链与 AST 推断表达式类型。
    /// </summary>
    public static string? InferExpressionType(
        LangExpression? expr,
        SymbolScope? scope,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex = null,
        string? preferredUri = null)
    {
        if (expr == null)
        {
            return null;
        }

        switch (expr)
        {
            case ScalarLiteralExpression scalar:
                return scalar.Kind switch
                {
                    LiteralKind.Int => "int",
                    LiteralKind.Double => "double",
                    LiteralKind.String => "string",
                    LiteralKind.Char => "char",
                    LiteralKind.Bool => "bool",
                    LiteralKind.Null => "null",
                    LiteralKind.Void => "void",
                    _ => null
                };

            case IntLangValue:
                return "int";
            case DoubleLangValue:
                return "double";
            case StringLangValue:
            case StringTemplateValue:
                return "string";
            case CharLangValue:
                return "char";
            case BoolLangValue:
                return "bool";
            case NullLangValue:
                return "null";

            case ListLiteralExpression:
            case ListLangValue:
            case ListComprehension:
            case LinqExpression:
                return "list";

            case ArrayLiteralExpression:
            case ArrayLangValue:
            case RangeLiteralExpression:
                return "array";

            case DictionaryLiteralExpression:
            case DictionaryLangValue:
                return "dict";

            case TupleLiteralExpression:
            case TupleLangValue:
                return "tuple";

            case LambdaExpression:
            case FuncLangValue:
                return "function";

            case ThisExpression:
                return scope?.FindEnclosingClass()?.Name;

            case SuperExpression:
                return scope?.FindEnclosingClass()?.BaseClassName;

            case AwaitExpression awaitExpr:
                return InferExpressionType(awaitExpr.Expression, scope, localSymbols, workspaceIndex, preferredUri);

            case TernaryExpression ternary:
            {
                var trueType = InferExpressionType(ternary.TrueExpression, scope, localSymbols, workspaceIndex, preferredUri);
                var falseType = InferExpressionType(ternary.FalseExpression, scope, localSymbols, workspaceIndex, preferredUri);
                if (string.IsNullOrEmpty(trueType) || trueType == "null") return falseType;
                if (string.IsNullOrEmpty(falseType) || falseType == "null") return trueType;
                return string.Equals(trueType, falseType, StringComparison.OrdinalIgnoreCase) ? trueType : $"{trueType} | {falseType}";
            }

            case GenericInstanceExpression genericInstance:
            {
                if (genericInstance.BaseExpression is LangId baseId)
                {
                    var sym = ResolveIdentifier(baseId.IdName, scope, localSymbols, workspaceIndex, preferredUri);
                    if (sym is { Kind: SymbolKind.Function })
                    {
                        return ExtractRawReturnType(sym.Type);
                    }

                    return baseId.IdName;
                }

                break;
            }

            case Instance instance:
                return ResolveCallableReturnOrClassName(instance.Id.IdName, scope, localSymbols, workspaceIndex, preferredUri);

            case CallExpression call:
                return ResolveCallableReturnOrClassName(call.Id.IdName, scope, localSymbols, workspaceIndex, preferredUri);

            case FunctionCallExpression funcCall:
            {
                if (funcCall.FunctionExpression is Operation { Opera: LangTokenType.Dot } dotExpr)
                {
                    return InferDotAccessType(dotExpr, scope, localSymbols, workspaceIndex, preferredUri);
                }

                if (funcCall.FunctionExpression is LangId funcId)
                {
                    return ResolveCallableReturnOrClassName(funcId.IdName, scope, localSymbols, workspaceIndex, preferredUri);
                }

                break;
            }

            case Operation { Opera: LangTokenType.Dot } dotOp:
                return InferDotAccessType(dotOp, scope, localSymbols, workspaceIndex, preferredUri);

            case Operation { Opera: LangTokenType.As } asOp:
                if (asOp.Right is LangId targetTypeId)
                {
                    return targetTypeId.IdName;
                }
                break;

            case Operation op:
                return InferOperationType(op, scope, localSymbols, workspaceIndex, preferredUri);

            case LangId langId:
            {
                var sym = ResolveIdentifier(langId.IdName, scope, localSymbols, workspaceIndex, preferredUri);
                if (sym == null)
                {
                    return null;
                }

                return sym.Kind switch
                {
                    SymbolKind.Class or SymbolKind.Interface or SymbolKind.Enum => sym.Name,
                    SymbolKind.Function or SymbolKind.Method => "function",
                    SymbolKind.EnumMember => sym.Parent?.Name ?? "int",
                    _ => ExtractRawReturnType(sym.Type)
                };
            }
        }

        return null;
    }

    private static string? InferDotAccessType(
        Operation dotOp,
        SymbolScope? scope,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri)
    {
        var leftType = InferExpressionType(dotOp.Left, scope, localSymbols, workspaceIndex, preferredUri);
        if (string.IsNullOrEmpty(leftType))
        {
            return null;
        }

        var memberName = dotOp.Right switch
        {
            LangId id => id.IdName,
            Instance inst => inst.Id.IdName,
            CallExpression call => call.Id.IdName,
            GenericInstanceExpression { BaseExpression: LangId gId } => gId.IdName,
            FunctionCallExpression { FunctionExpression: LangId fId } => fId.IdName,
            _ => null
        };

        if (string.IsNullOrEmpty(memberName))
        {
            return null;
        }

        var classSymbol = ResolveTypeSymbol(leftType, localSymbols, workspaceIndex, preferredUri);
        if (classSymbol == null)
        {
            // 常见内置方法返回类型
            return InferBuiltInMemberType(NormalizeTypeName(leftType), memberName);
        }

        var resolved = FindMember(classSymbol, memberName, localSymbols, workspaceIndex, preferredUri);
        if (resolved == null)
        {
            return null;
        }

        return resolved.Value.Member.Kind == SymbolKind.EnumMember
            ? classSymbol.Name
            : ExtractRawReturnType(resolved.Value.Member.Type);
    }

    private static string? InferBuiltInMemberType(string receiverType, string memberName)
    {
        return memberName switch
        {
            "ToStr" => "string",
            "ToInt" => "int",
            "ToDouble" => "double",
            "ToBool" => "bool",
            "ToChar" => "char",
            "Count" or "Length" or "IndexOf" => "int",
            "IsEmpty" or "Contains" or "StartsWith" or "EndsWith" or "Any" or "All" or "IsSorted" => "bool",
            "ToList" => "list",
            "ToArray" => "array",
            "ToUpper" or "ToLower" or "Trim" or "Substring" or "Replace" or "Join" when receiverType == "string" => "string",
            _ => null
        };
    }

    private static string? InferOperationType(
        Operation op,
        SymbolScope? scope,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri)
    {
        switch (op.Opera)
        {
            case LangTokenType.Equals:
            case LangTokenType.NotEquals:
            case LangTokenType.LessThan:
            case LangTokenType.LessThanEquals:
            case LangTokenType.GreaterThan:
            case LangTokenType.GreaterThanEquals:
            case LangTokenType.And:
            case LangTokenType.Or:
            case LangTokenType.Not:
            case LangTokenType.In:
            case LangTokenType.Is:
                return "bool";

            case LangTokenType.Plus:
            {
                var left = NormalizeTypeName(InferExpressionType(op.Left, scope, localSymbols, workspaceIndex, preferredUri));
                var right = NormalizeTypeName(InferExpressionType(op.Right, scope, localSymbols, workspaceIndex, preferredUri));
                if (left == "string" || right == "string") return "string";
                if (left == "double" || right == "double") return "double";
                if (left == "int" && right == "int") return "int";
                if (left == "list" && right == "list") return "list";
                if (left == "array" && right == "array") return "array";
                return null;
            }

            case LangTokenType.Minus:
            case LangTokenType.Star:
            case LangTokenType.Slash:
            case LangTokenType.Percent:
            case LangTokenType.Caret:
            {
                if (op.Left == null && op.Right != null)
                {
                    return NormalizeTypeName(InferExpressionType(op.Right, scope, localSymbols, workspaceIndex, preferredUri));
                }

                var left = NormalizeTypeName(InferExpressionType(op.Left, scope, localSymbols, workspaceIndex, preferredUri));
                var right = NormalizeTypeName(InferExpressionType(op.Right, scope, localSymbols, workspaceIndex, preferredUri));
                if (left == "double" || right == "double") return "double";
                if (left == "int" && right == "int") return "int";
                return null;
            }
        }

        return null;
    }

    private static string? ResolveCallableReturnOrClassName(
        string name,
        SymbolScope? scope,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri)
    {
        var sym = ResolveIdentifier(name, scope, localSymbols, workspaceIndex, preferredUri);
        if (sym != null)
        {
            if (sym.Kind is SymbolKind.Class or SymbolKind.Interface or SymbolKind.Enum)
            {
                return sym.Name;
            }

            if (sym.Kind is SymbolKind.Function or SymbolKind.Method)
            {
                return ExtractRawReturnType(sym.Type);
            }
        }

        // 未在符号表中找到且首字母大写时，保留类构造推断约定（如尚未定义的类 User()）
        if (!string.IsNullOrEmpty(name) && char.IsUpper(name[0]))
        {
            return name;
        }

        return null;
    }

    private static SymbolInfo? ResolveIdentifier(
        string name,
        SymbolScope? scope,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri)
    {
        var fromScope = scope?.Resolve(name);
        if (fromScope != null)
        {
            return fromScope;
        }

        if (localSymbols != null && localSymbols.TryGetValue(name, out var local))
        {
            return local;
        }

        if (workspaceIndex != null &&
            workspaceIndex.TryResolveSymbol(name, preferredUri, out var wsSymbol, out _) &&
            wsSymbol != null)
        {
            return wsSymbol;
        }

        return null;
    }

    private static void CollectMembersRecursive(
        SymbolInfo typeSymbol,
        bool isDirectType,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri,
        bool staticOnly,
        MemberAccessLevel? maxAccessLevel,
        HashSet<string> seenMembers,
        HashSet<string> visitedTypes,
        List<SymbolInfo> output)
    {
        if (!visitedTypes.Add(typeSymbol.Name))
        {
            return;
        }

        foreach (var member in typeSymbol.Members.Values)
        {
            if (staticOnly && !member.IsStatic && member.Kind != SymbolKind.EnumMember)
            {
                continue;
            }

            if (maxAccessLevel.HasValue &&
                !IsMemberAccessible(member.AccessModifier, maxAccessLevel.Value, isInherited: !isDirectType))
            {
                continue;
            }

            if (seenMembers.Add(member.Name))
            {
                output.Add(member);
            }
        }

        // 1. 优先沿父类继承链向上收集
        if (!string.IsNullOrEmpty(typeSymbol.BaseClassName))
        {
            var baseClass = ResolveTypeSymbol(typeSymbol.BaseClassName, localSymbols, workspaceIndex, preferredUri);
            if (baseClass != null)
            {
                CollectMembersRecursive(
                    baseClass,
                    isDirectType: false,
                    localSymbols,
                    workspaceIndex,
                    preferredUri,
                    staticOnly,
                    maxAccessLevel,
                    seenMembers,
                    visitedTypes,
                    output);
            }
        }

        // 2. 收集 Mixin 成员
        foreach (var mixinName in typeSymbol.MixinNames)
        {
            var mixin = ResolveTypeSymbol(mixinName, localSymbols, workspaceIndex, preferredUri);
            if (mixin != null)
            {
                CollectMembersRecursive(
                    mixin,
                    isDirectType: false,
                    localSymbols,
                    workspaceIndex,
                    preferredUri,
                    staticOnly,
                    maxAccessLevel,
                    seenMembers,
                    visitedTypes,
                    output);
            }
        }

        // 3. 收集接口成员（含接口默认方法）
        foreach (var ifaceName in typeSymbol.InterfaceNames)
        {
            var iface = ResolveTypeSymbol(ifaceName, localSymbols, workspaceIndex, preferredUri);
            if (iface != null)
            {
                CollectMembersRecursive(
                    iface,
                    isDirectType: false,
                    localSymbols,
                    workspaceIndex,
                    preferredUri,
                    staticOnly,
                    maxAccessLevel,
                    seenMembers,
                    visitedTypes,
                    output);
            }
        }
    }

    private static (SymbolInfo Member, SymbolInfo DeclaringClass)? FindMemberRecursive(
        SymbolInfo typeSymbol,
        string memberName,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri,
        HashSet<string> visitedTypes)
    {
        if (!visitedTypes.Add(typeSymbol.Name))
        {
            return null;
        }

        if (typeSymbol.Members.TryGetValue(memberName, out var member))
        {
            return (member, typeSymbol);
        }

        if (!string.IsNullOrEmpty(typeSymbol.BaseClassName))
        {
            var baseClass = ResolveTypeSymbol(typeSymbol.BaseClassName, localSymbols, workspaceIndex, preferredUri);
            if (baseClass != null)
            {
                var found = FindMemberRecursive(baseClass, memberName, localSymbols, workspaceIndex, preferredUri, visitedTypes);
                if (found != null) return found;
            }
        }

        foreach (var mixinName in typeSymbol.MixinNames)
        {
            var mixin = ResolveTypeSymbol(mixinName, localSymbols, workspaceIndex, preferredUri);
            if (mixin != null)
            {
                var found = FindMemberRecursive(mixin, memberName, localSymbols, workspaceIndex, preferredUri, visitedTypes);
                if (found != null) return found;
            }
        }

        foreach (var ifaceName in typeSymbol.InterfaceNames)
        {
            var iface = ResolveTypeSymbol(ifaceName, localSymbols, workspaceIndex, preferredUri);
            if (iface != null)
            {
                var found = FindMemberRecursive(iface, memberName, localSymbols, workspaceIndex, preferredUri, visitedTypes);
                if (found != null) return found;
            }
        }

        return null;
    }

    private static bool IsDerivedFromRecursive(
        SymbolInfo current,
        string targetName,
        Dictionary<string, SymbolInfo>? localSymbols,
        WorkspaceIndex? workspaceIndex,
        string? preferredUri,
        HashSet<string> visited)
    {
        if (!visited.Add(current.Name))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(current.BaseClassName))
        {
            if (string.Equals(current.BaseClassName, targetName, StringComparison.Ordinal))
            {
                return true;
            }

            var baseSym = ResolveTypeSymbol(current.BaseClassName, localSymbols, workspaceIndex, preferredUri);
            if (baseSym != null && IsDerivedFromRecursive(baseSym, targetName, localSymbols, workspaceIndex, preferredUri, visited))
            {
                return true;
            }
        }

        foreach (var iface in current.InterfaceNames)
        {
            if (string.Equals(iface, targetName, StringComparison.Ordinal))
            {
                return true;
            }

            var ifaceSym = ResolveTypeSymbol(iface, localSymbols, workspaceIndex, preferredUri);
            if (ifaceSym != null && IsDerivedFromRecursive(ifaceSym, targetName, localSymbols, workspaceIndex, preferredUri, visited))
            {
                return true;
            }
        }

        foreach (var mixin in current.MixinNames)
        {
            if (string.Equals(mixin, targetName, StringComparison.Ordinal))
            {
                return true;
            }

            var mixinSym = ResolveTypeSymbol(mixin, localSymbols, workspaceIndex, preferredUri);
            if (mixinSym != null && IsDerivedFromRecursive(mixinSym, targetName, localSymbols, workspaceIndex, preferredUri, visited))
            {
                return true;
            }
        }

        return false;
    }
}
