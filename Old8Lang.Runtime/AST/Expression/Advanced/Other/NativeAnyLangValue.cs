using Old8Lang.AST.Visitor;
using System.Reflection;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using System.Collections.Concurrent;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 原生映射 适用于有构造函数的类
/// 也可以直接包装已有的 .NET 对象实例
/// </summary>
public partial class NativeAnyLangValue : ImportInfo
{
    internal Type? ClassType { get; set; }
    public readonly string ClassName;

    // 注册名称（用于别名功能，如果为null则使用ClassName）
    public readonly string RegisterName;

    internal readonly string? _dllName;
    internal readonly string? _path;

    internal ConstructorInfo? Constructor { get; set; }
    internal object? InstanceObj { get; set; }

    // 成员信息缓存 - 按类型缓存，所有同类型实例共享
    internal static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MemberInfo?>> MemberCache = new();

    /// <summary>
    /// 获取缓存的成员信息（属性、字段或方法）
    /// </summary>
    internal MemberInfo? GetCachedMember(string memberName)
    {
        if (ClassType is null) return null;

        // 获取或创建该类型的缓存字典
        var typeCache = MemberCache.GetOrAdd(ClassType, _ => new ConcurrentDictionary<string, MemberInfo?>());

        // 从缓存中获取或查询成员
        return typeCache.GetOrAdd(memberName, name =>
        {
            // 依次尝试：属性 -> 字段 -> 方法
            MemberInfo? member = ClassType.GetProperty(name);
            if (member is not null) return member;

            member = ClassType.GetField(name);
            if (member is not null) return member;

            member = ClassType.GetMethod(name);
            return member; // 如果都找不到，返回 null
        });
    }

    /// <summary>
    /// 从 DLL 导入类型的构造函数
    /// </summary>
    public NativeAnyLangValue(string dllName, string className, string path, string? registerName = null)
    {
        _dllName = dllName;
        ClassName = className;
        _path = path;
        RegisterName = registerName ?? className;
    }

    /// <summary>
    /// 直接包装已有对象实例的构造函数
    /// </summary>
    public NativeAnyLangValue(object nativeObject, SourcePosition position = default) : base(position)
    {
        InstanceObj = nativeObject ?? throw new ArgumentNullException(nameof(nativeObject));
        ClassType = nativeObject.GetType();
        ClassName = ClassType.Name;
        RegisterName = ClassName;
        _dllName = null;
        _path = null;
    }


    /// <summary>
    /// 创建一个共享类型信息、但拥有独立实例槽位的新持有者（模块导出的类每次实例化都需要独立对象）。
    /// </summary>
    internal NativeAnyLangValue CreateInstanceHolder() =>
        new(_dllName ?? string.Empty, ClassName, _path ?? string.Empty, RegisterName)
        {
            ClassType = ClassType,
            Constructor = Constructor
        };

    public void New(object[] pa)
    {
        var constructor = SelectConstructor(pa, out var converted) ?? Constructor;
        InstanceObj = constructor is not null
            ? constructor.Invoke(converted ?? pa)
            : Activator.CreateInstance(ClassType!)!;
    }

    /// <summary>
    /// 按实参个数与可转换性选择构造函数（构造函数在初始化时只固定了一个，带参构造需要重新匹配）。
    /// </summary>
    private System.Reflection.ConstructorInfo? SelectConstructor(object[] args, out object?[]? converted)
    {
        converted = null;
        if (Constructor is { } fixedCtor && fixedCtor.GetParameters().Length == args.Length)
            return fixedCtor;
        if (ClassType is null)
            return null;

        foreach (var candidate in ClassType.GetConstructors())
        {
            var parameters = candidate.GetParameters();
            if (parameters.Length != args.Length)
                continue;

            var values = new object?[args.Length];
            var matched = true;
            for (var index = 0; index < args.Length && matched; index++)
            {
                var type = parameters[index].ParameterType;
                var value = args[index];
                if (value is null)
                {
                    matched = !type.IsValueType;
                    continue;
                }

                if (type.IsInstanceOfType(value))
                {
                    values[index] = value;
                    continue;
                }

                try
                {
                    values[index] = value is IConvertible && typeof(IConvertible).IsAssignableFrom(type)
                        ? Convert.ChangeType(value, type, System.Globalization.CultureInfo.InvariantCulture)
                        : throw new InvalidCastException();
                }
                catch (Exception)
                {
                    matched = false;
                }
            }

            if (!matched)
                continue;

            converted = values;
            return candidate;
        }

        return null;
    }

    /// <summary>
    /// 获取包装的 .NET 对象
    /// </summary>
    public object? GetNativeObject() => InstanceObj;

    /// <summary>
    /// 获取对象的类型信息
    /// </summary>
    public Type? GetNativeType() => ClassType;

    /// <summary>
    /// 类型转换为字符串
    /// </summary>
    public override string TypeToString()
    {
        return InstanceObj is not null
            ? $"NativeObject<{ClassName}>"
            : $"NativeType<{ClassName}>";
    }

    /// <summary>
    /// 显示字符串
    /// </summary>
    public override string ToDisplayString()
    {
        return InstanceObj is not null
            ? $"NativeObject({InstanceObj})"
            : $"NativeType({ClassName})";
    }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString()
    {
        return InstanceObj?.ToString() ?? ClassName;
    }

    /// <summary>
    /// 获取值的实际.NET对象
    /// </summary>
    public override object GetValue()
    {
        return InstanceObj ?? new object();
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is NativeAnyLangValue other)
        {
            // 如果都有实例对象，比较实例
            if (InstanceObj is not null && other.InstanceObj is not null)
            {
                return InstanceObj.Equals(other.InstanceObj);
            }
            // 如果都是类型引用，比较类型
            if (InstanceObj is null && other.InstanceObj is null)
            {
                return ClassName == other.ClassName;
            }
        }
        return false;
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        return ((Old8Lang.AST.Visitor.IRuntimeVisitor<TResult>)visitor).VisitNativeAnyLangValue(this);
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表（用于原生方法调用）
    /// </summary>
    /// <param name="method">原生方法信息</param>
    /// <param name="positionalArgs">位置参数列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    /// <returns>重新排序后的参数列表</returns>
    internal List<LangExpression> ReorderArgumentsWithNamedParameters(
        MethodInfo method,
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPosition)
    {
        var parameters = method.GetParameters();

        if (parameters.Length == 0)
        {
            if (namedArgs.Count > 0)
            {
                throw new ArgumentError(callPosition,
                    $"方法 '{method.Name}' 不接受任何参数，但提供了命名参数");
            }

            return positionalArgs;
        }

        // 1. 验证命名参数的合法性
        ValidateNamedArguments(namedArgs, callPosition);

        // 2. 创建参数槽位数组
        var paramSlots = new LangExpression?[parameters.Length];
        var parameterFilled = new bool[parameters.Length];

        // 3. 填充位置参数
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            if (i >= parameters.Length)
            {
                throw new ArgumentError(callPosition,
                    $"方法 '{method.Name}' 期望最多 {parameters.Length} 个参数，但位置参数提供了 {positionalArgs.Count} 个");
            }

            paramSlots[i] = positionalArgs[i];
            parameterFilled[i] = true;
        }

        // 4. 填充命名参数
        foreach (var namedArg in namedArgs)
        {
            // 查找参数索引
            int paramIndex = -1;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Name == namedArg.Name)
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
            {
                throw new ArgumentError(namedArg.Position,
                    $"方法 '{method.Name}' 没有名为 '{namedArg.Name}' 的参数");
            }

            // 检查是否已经通过位置参数提供
            if (parameterFilled[paramIndex])
            {
                throw new ArgumentError(namedArg.Position,
                    $"参数 '{namedArg.Name}' 已经通过位置参数提供，不能重复指定");
            }

            paramSlots[paramIndex] = namedArg.Value;
            parameterFilled[paramIndex] = true;
        }

        // 5. 填充默认参数值或验证必需参数
        for (int i = 0; i < parameters.Length; i++)
        {
            if (!parameterFilled[i])
            {
                // 检查是否有默认值
                if (parameters[i].HasDefaultValue)
                {
                    // 使用默认值 - 将 .NET 默认值转换为 Old8Lang 表达式
                    var defaultValue = parameters[i].DefaultValue;
                    paramSlots[i] = ObjToValue(defaultValue);
                }
                else if (parameters[i].IsOptional)
                {
                    // 可选参数但没有默认值，使用 null
                    paramSlots[i] = new NullLangValue(callPosition);
                }
                else
                {
                    throw new ArgumentError(callPosition,
                        $"方法 '{method.Name}' 的必需参数 '{parameters[i].Name}' (第{i + 1}个参数) 未提供值");
                }
            }
        }

        // 6. 转换为列表并返回
        var result = new List<LangExpression>(paramSlots.Length);
        for (int i = 0; i < paramSlots.Length; i++)
        {
            if (paramSlots[i] is null)
            {
                throw new ArgumentError(callPosition,
                    $"内部错误：方法 '{method.Name}' 的参数槽位 {i} 未被填充");
            }

            result.Add(paramSlots[i]!);
        }

        return result;
    }

    /// <summary>
    /// 验证命名参数的合法性
    /// </summary>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    internal void ValidateNamedArguments(List<NamedArgument> namedArgs, SourcePosition callPosition)
    {
        // 检查命名参数是否重复
        var seenNames = new HashSet<string>();
        foreach (var namedArg in namedArgs.Where(namedArg => !seenNames.Add(namedArg.Name)))
        {
            throw new ArgumentError(namedArg.Position,
                $"命名参数 '{namedArg.Name}' 重复指定");
        }
    }
}
