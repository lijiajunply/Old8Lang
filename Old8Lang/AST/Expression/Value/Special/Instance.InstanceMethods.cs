using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Instance 类的实例方法系统集成部分
/// </summary>
public partial class Instance
{
    /// <summary>
    /// 尝试通过实例方法注册器执行方法
    /// </summary>
    private bool TryExecuteInstanceMethod(LangValueType instance, VariateManager manager, out LangValueType? result)
    {
        result = null;

        // 确保实例方法系统已初始化
        InstanceMethodInitializer.EnsureInitialized();

        // 查找实例方法（使用重载解析）
        var instanceType = instance.GetType();

        // 先尝试不使用重载解析（向后兼容）
        var method = InstanceMethodRegistry.Instance.ResolveMethod(
            instanceType,
            Id.IdName,
            Ids,
            null); // 解释模式下没有 LocalManager

        if (method == null)
        {
            return false;
        }

        try
        {
            // 处理命名参数：重排序参数列表
            var orderedParameters = ReorderInstanceMethodArguments(method, Ids, NamedArgs);

            // 执行方法
            result = method.Execute(instance, orderedParameters, manager, Position);
            return true;
        }
        catch (Exception ex)
        {
            // 如果是 Old8Exception，直接抛出
            if (ex is Old8Exception)
            {
                throw;
            }

            // 否则包装为 RuntimeError
            throw new InvalidOperationError(Position, $"执行实例方法 '{Id.IdName}' 时发生错误: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试通过实例方法注册器生成 IL 代码
    /// </summary>
    private bool TryGenerateInstanceMethodIl(LangExpression instance, ILGenerator ilGenerator, LocalManager local)
    {
        // 确保实例方法系统已初始化
        InstanceMethodInitializer.EnsureInitialized();

        // 获取实例类型
        Type instanceType;
        try
        {
            // 尝试从 LocalManager 获取类型信息
            if (instance is LangValueType valueType)
            {
                instanceType = valueType.GetType();
            }
            else
            {
                // 无法确定类型，返回 false
                return false;
            }
        }
        catch
        {
            // 如果无法获取类型，返回 false
            return false;
        }

        // 查找实例方法（使用重载解析）
        var method = InstanceMethodRegistry.Instance.ResolveMethod(
            instanceType,
            Id.IdName,
            Ids,
            local); // 编译模式下使用 LocalManager 进行类型推断

        if (method == null)
        {
            return false;
        }

        try
        {
            // 处理命名参数：重排序参数列表
            var orderedParameters = ReorderInstanceMethodArguments(method, Ids, NamedArgs);

            // 生成 IL 代码
            method.GenerateIl(instance, orderedParameters, ilGenerator, local, Position);
            return true;
        }
        catch (Exception ex)
        {
            // 如果是 Old8Exception，直接抛出
            if (ex is Old8Exception)
            {
                throw;
            }

            // 否则包装为 CompilerException
            throw new CompilerException($"生成实例方法 '{Id.IdName}' 的 IL 代码时发生错误: {ex.Message}", Position);
        }
    }

    /// <summary>
    /// 重排序实例方法参数以支持命名参数
    /// </summary>
    private List<LangExpression> ReorderInstanceMethodArguments(
        IInstanceMethod instanceMethod,
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs)
    {
        // 如果没有命名参数，直接返回位置参数
        if (namedArgs == null || namedArgs.Count == 0)
        {
            return positionalArgs;
        }

        // 如果方法不支持命名参数，抛出错误
        if (instanceMethod.ParameterNames == null || instanceMethod.ParameterNames.Length == 0)
        {
            throw new ArgumentError(Position,
                $"方法 '{instanceMethod.Names[0]}' 不支持命名参数");
        }

        // 创建参数槽位数组
        var paramSlots = new LangExpression?[instanceMethod.ParameterNames.Length];
        var parameterFilled = new bool[instanceMethod.ParameterNames.Length];

        // 填充位置参数
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            if (i >= paramSlots.Length)
            {
                throw new ArgumentError(Position,
                    $"方法 '{instanceMethod.Names[0]}' 的位置参数过多");
            }

            paramSlots[i] = positionalArgs[i];
            parameterFilled[i] = true;
        }

        // 填充命名参数
        foreach (var namedArg in namedArgs)
        {
            int paramIndex = Array.IndexOf(instanceMethod.ParameterNames, namedArg.Name);
            if (paramIndex == -1)
            {
                throw new ArgumentError(namedArg.Position,
                    $"方法 '{instanceMethod.Names[0]}' 没有名为 '{namedArg.Name}' 的参数");
            }

            if (parameterFilled[paramIndex])
            {
                throw new ArgumentError(namedArg.Position,
                    $"参数 '{namedArg.Name}' 已经通过位置参数提供");
            }

            paramSlots[paramIndex] = namedArg.Value;
            parameterFilled[paramIndex] = true;
        }

        // 验证所有必需参数都已提供
        for (int i = 0; i < instanceMethod.MinParameterCount; i++)
        {
            if (!parameterFilled[i])
            {
                throw new ArgumentError(Position,
                    $"方法 '{instanceMethod.Names[0]}' 的必需参数 '{instanceMethod.ParameterNames[i]}' 未提供");
            }
        }

        // 返回重排序后的参数列表（只包含已填充的参数）
        var result = new List<LangExpression>();
        for (int i = 0; i < paramSlots.Length; i++)
        {
            if (paramSlots[i] != null)
            {
                result.Add(paramSlots[i]!);
            }
        }

        return result;
    }
}
