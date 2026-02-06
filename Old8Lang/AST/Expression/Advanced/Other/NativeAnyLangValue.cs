using Old8Lang.AST.Visitor;
using System.Reflection;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;
using System.Collections.Concurrent;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 原生映射 适用于有构造函数的类
/// 也可以直接包装已有的 .NET 对象实例
/// </summary>
public class NativeAnyLangValue : ImportInfo
{
    private Type? ClassType { get; set; }
    public readonly string ClassName;

    // 注册名称（用于别名功能，如果为null则使用ClassName）
    public readonly string RegisterName;

    private readonly string? _dllName;
    private readonly string? _path;

    private ConstructorInfo? Constructor { get; set; }
    private object? InstanceObj { get; set; }

    // 成员信息缓存 - 按类型缓存，所有同类型实例共享
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MemberInfo?>> MemberCache = new();

    /// <summary>
    /// 获取缓存的成员信息（属性、字段或方法）
    /// </summary>
    private MemberInfo? GetCachedMember(string memberName)
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

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            // 使用缓存获取成员
            var member = GetCachedMember(id.IdName);

            // 尝试访问属性
            if (member is PropertyInfo prop)
            {
                try
                {
                    // 如果有实例对象，访问实例成员；否则访问静态成员
                    var value = prop.GetValue(InstanceObj);
                    return ObjToValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationError(this, $"访问属性 '{id.IdName}' 失败: {ex.Message}");
                }
            }

            // 尝试访问字段
            if (member is FieldInfo field)
            {
                try
                {
                    // 如果有实例对象，访问实例成员；否则访问静态成员
                    var value = field.GetValue(InstanceObj);
                    return ObjToValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationError(this, $"访问字段 '{id.IdName}' 失败: {ex.Message}");
                }
            }

            throw new AttributeError(this, id.IdName, ClassName);
        }

        if (dotExpression is Instance instance)
        {
            // 使用缓存获取方法
            var member = GetCachedMember(instance.Id.IdName);
            if (member is not MethodInfo method)
                throw new AttributeError(this, instance.Id.IdName, ClassName);

            try
            {
                // 处理命名参数：将命名参数重新排序为位置参数
                List<LangExpression> orderedArgs;
                if (instance.NamedArgs is { Count: > 0 })
                {
                    orderedArgs = ReorderArgumentsWithNamedParameters(
                        method,
                        instance.Ids,
                        instance.NamedArgs,
                        instance.Position);
                }
                else
                {
                    orderedArgs = instance.Ids;
                }

                // 计算参数值
                var arguments = orderedArgs.Select(arg => arg.Run(manager)).ToList();

                // 将 Old8Lang 值转换为 .NET 对象
                var nativeArguments = arguments.Select(ValueToObj).ToArray();

                // 使用委托缓存优化方法调用
                var result = MethodInvokerCache.Invoke(method, InstanceObj, nativeArguments);

                // 将结果转换为 Old8Lang 值
                return ObjToValue(result);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationError(this, $"调用方法 '{instance.Id.IdName}' 失败: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(this, $"调用方法 '{instance.Id.IdName}' 失败: {ex.Message}");
            }
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 如果已经有实例对象（通过第二个构造函数创建），则直接返回
        if (InstanceObj is not null && ClassType is not null)
        {
            manager.Clone();
            return this;
        }

        // 否则从 DLL 加载类型
        if (_path is null || _dllName is null)
        {
            throw new InvalidOperationError(this, "无法加载类型：缺少 DLL 路径或名称");
        }

        var assembly = Assembly.LoadFile(_path);

        // ClassName 可能是简单类名（如 "Container"）或完整类型名（如 "FirstUI.Layout.Container"）
        // 先尝试使用 ClassName 直接获取类型
        ClassType = assembly.GetType(ClassName);

        // 如果失败，尝试拼接 DllName 和 ClassName
        if (ClassType is null && _dllName is not null)
        {
            ClassType = assembly.GetType($"{_dllName}.{ClassName}");
        }

        if (ClassType is null)
        {
            throw new InvalidOperationError(this, $"无法加载类型：在程序集中找不到类型 '{ClassName}' 或 '{_dllName}.{ClassName}'");
        }

        var constructors = ClassType.GetConstructors();
        if (constructors is { Length: > 0 })
            Constructor = constructors[0];
        manager.Clone();
        return this;
    }

    public void New(object[] pa)
    {
        InstanceObj = Constructor is not null ? Constructor.Invoke(pa) : Activator.CreateInstance(ClassType!)!;
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
        throw new NotSupportedException("NativeAnyLangValue 暂不支持 Visitor 模式访问");
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表（用于原生方法调用）
    /// </summary>
    /// <param name="method">原生方法信息</param>
    /// <param name="positionalArgs">位置参数列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    /// <returns>重新排序后的参数列表</returns>
    private List<LangExpression> ReorderArgumentsWithNamedParameters(
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
    private void ValidateNamedArguments(List<NamedArgument> namedArgs, SourcePosition callPosition)
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