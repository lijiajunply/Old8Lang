using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;
using System.Collections.Concurrent;
using System.Reflection;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 原生静态类 映射
/// </summary>
/// <param name="className"></param>
/// <param name="classType"></param>
public class NativeStaticAny(string className, Type classType) : ImportInfo
{
    public readonly string ClassName = className;

    // 成员信息缓存 - 按类型缓存，所有同类型实例共享
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, MemberInfo?>> MemberCache = new();

    /// <summary>
    /// 获取缓存的成员信息（属性、字段或方法）
    /// </summary>
    private MemberInfo? GetCachedMember(string memberName)
    {
        // 获取或创建该类型的缓存字典
        var typeCache = MemberCache.GetOrAdd(classType, _ => new ConcurrentDictionary<string, MemberInfo?>());

        // 从缓存中获取或查询成员
        return typeCache.GetOrAdd(memberName, name =>
        {
            // 依次尝试：属性 -> 字段 -> 方法
            MemberInfo? member = classType.GetProperty(name);
            if (member is not null) return member;

            member = classType.GetField(name);
            if (member is not null) return member;

            member = classType.GetMethod(name);
            return member; // 如果都找不到，返回 null
        });
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
                return ObjToValue(prop.GetValue(null)!);
            }

            // 尝试访问字段
            if (member is FieldInfo field)
            {
                return ObjToValue(field.GetValue(null)!);
            }

            throw new AttributeError(this, id.IdName, ClassName);
        }

        if (dotExpression is Instance instance)
        {
            // 使用缓存获取方法
            var member = GetCachedMember(instance.Id.IdName);
            if (member is not MethodInfo method)
                throw new AttributeError(this, instance.Id.IdName, ClassName);

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
            var a = Apis.ListToObjects(arguments.OfType<LangValueType>().ToList()).ToArray();

            // 使用委托缓存优化静态方法调用
            var invoke = MethodInvokerCache.Invoke(method, null, a);
            return ObjToValue(invoke!);
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("NativeStaticAny 暂不支持 Visitor 模式访问");
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表（用于原生静态方法调用）
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