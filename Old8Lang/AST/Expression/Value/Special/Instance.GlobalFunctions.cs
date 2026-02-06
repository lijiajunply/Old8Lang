using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Instance 类的全局函数集成部分
/// </summary>
public partial class Instance
{
    /// <summary>
    /// 尝试通过全局函数注册器执行函数（解释器模式）
    /// </summary>
    /// <returns>如果找到并执行了全局函数返回 true，否则返回 false</returns>
    private bool TryExecuteGlobalFunction(VariateManager manager, out LangValueType? result)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        // 获取重载组
        var overloadGroup = GlobalFunctionRegistry.Instance.GetOverloadGroup(Id.IdName);
        if (overloadGroup is not null)
        {
            // 处理命名参数：将命名参数重新排序为位置参数
            List<LangExpression> orderedArgs;

            // 如果有命名参数，需要先解析一个重载来确定参数顺序
            if (NamedArgs is { Count: > 0 })
            {
                // 使用第一个重载来处理命名参数
                var firstOverload = overloadGroup.Overloads.FirstOrDefault();
                if (firstOverload == null)
                {
                    result = null;
                    return false;
                }
                orderedArgs = ReorderGlobalFunctionArguments(firstOverload, Ids, NamedArgs, Position);
            }
            else
            {
                orderedArgs = Ids;
            }

            // 在解释器模式下，基于运行时参数值解析重载
            var globalFunc = ResolveOverloadForInterpreter(overloadGroup, orderedArgs, manager);
            if (globalFunc is not null)
            {
                result = globalFunc.Execute(orderedArgs, manager, Position);
                return true;
            }
        }

        result = null;
        return false;
    }

    /// <summary>
    /// 尝试通过全局函数注册器生成 IL 代码（编译器模式）
    /// </summary>
    /// <returns>如果找到并生成了 IL 代码返回 true，否则返回 false</returns>
    private bool TryGenerateGlobalFunctionIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        // 使用重载解析查找最匹配的全局函数
        var globalFunc = GlobalFunctionRegistry.Instance.ResolveFunction(Id.IdName, Ids, local);
        if (globalFunc is not null)
        {
            // 处理命名参数：将命名参数重新排序为位置参数
            List<LangExpression> orderedArgs;
            if (NamedArgs is { Count: > 0 })
            {
                orderedArgs = ReorderGlobalFunctionArguments(globalFunc, Ids, NamedArgs, Position);
            }
            else
            {
                orderedArgs = Ids;
            }

            globalFunc.GenerateIl(orderedArgs, ilGenerator, local, Position);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 尝试通过全局函数注册器获取返回类型（编译器模式）
    /// </summary>
    /// <returns>如果找到了全局函数返回其返回类型，否则返回 null</returns>
    private Type? TryGetGlobalFunctionReturnType(LocalManager local)
    {
        // 确保全局函数已初始化
        GlobalFunctionInitializer.EnsureInitialized();

        // 使用重载解析查找最匹配的全局函数
        var globalFunc = GlobalFunctionRegistry.Instance.ResolveFunction(Id.IdName, Ids, local);
        if (globalFunc is not null)
        {
            // 处理命名参数：将命名参数重新排序为位置参数
            List<LangExpression> orderedArgs;
            if (NamedArgs is { Count: > 0 })
            {
                orderedArgs = ReorderGlobalFunctionArguments(globalFunc, Ids, NamedArgs, Position);
            }
            else
            {
                orderedArgs = Ids;
            }

            return globalFunc.GetReturnType(orderedArgs, local);
        }

        return null;
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表（用于全局函数调用）
    /// </summary>
    /// <param name="globalFunc">全局函数</param>
    /// <param name="positionalArgs">位置参数列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    /// <returns>重新排序后的参数列表</returns>
    private List<LangExpression> ReorderGlobalFunctionArguments(
        IGlobalFunction globalFunc,
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPosition)
    {
        // 检查全局函数是否支持命名参数
        if (globalFunc.ParameterNames is null || globalFunc.ParameterNames.Length == 0)
        {
            throw new ArgumentError(callPosition,
                $"全局函数 '{globalFunc.Names[0]}' 不支持命名参数。" +
                $"该函数只能使用位置参数调用。");
        }

        var parameterNames = globalFunc.ParameterNames;

        // 1. 验证命名参数的合法性
        ValidateNamedArguments(namedArgs, callPosition);

        // 2. 创建参数槽位数组
        var paramSlots = new LangExpression?[parameterNames.Length];
        var parameterFilled = new bool[parameterNames.Length];

        // 3. 填充位置参数
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            if (i >= parameterNames.Length)
            {
                throw new ArgumentError(callPosition,
                    $"全局函数 '{globalFunc.Names[0]}' 期望最多 {parameterNames.Length} 个参数，但位置参数提供了 {positionalArgs.Count} 个");
            }

            paramSlots[i] = positionalArgs[i];
            parameterFilled[i] = true;
        }

        // 4. 填充命名参数
        foreach (var namedArg in namedArgs)
        {
            // 查找参数索引
            int paramIndex = -1;
            for (int i = 0; i < parameterNames.Length; i++)
            {
                if (parameterNames[i] == namedArg.Name)
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
            {
                throw new ArgumentError(namedArg.Position,
                    $"全局函数 '{globalFunc.Names[0]}' 没有名为 '{namedArg.Name}' 的参数。" +
                    $"可用的参数名称: {string.Join(", ", parameterNames)}");
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

        // 5. 验证所有必需参数都已提供
        // 注意：全局函数可能有可选参数（MinParameterCount < MaxParameterCount）
        int requiredCount = globalFunc.MinParameterCount;
        for (int i = 0; i < requiredCount; i++)
        {
            if (!parameterFilled[i])
            {
                throw new ArgumentError(callPosition,
                    $"全局函数 '{globalFunc.Names[0]}' 的必需参数 '{parameterNames[i]}' (第{i + 1}个参数) 未提供值");
            }
        }

        // 6. 转换为列表并返回（只返回已填充的参数）
        var result = new List<LangExpression>();
        for (int i = 0; i < paramSlots.Length; i++)
        {
            if (paramSlots[i] is not null)
            {
                result.Add(paramSlots[i]!);
            }
            else if (i < requiredCount)
            {
                // 必需参数未填充，这不应该发生（前面已经检查过）
                throw new ArgumentError(callPosition,
                    $"内部错误：全局函数 '{globalFunc.Names[0]}' 的参数槽位 {i} 未被填充");
            }
            // 可选参数未填充，不添加到结果中
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

    /// <summary>
    /// 在解释器模式下基于运行时参数值解析重载
    /// </summary>
    private IGlobalFunction? ResolveOverloadForInterpreter(OverloadGroup overloadGroup, List<LangExpression> parameters, VariateManager manager)
    {
        if (overloadGroup.Overloads.Count == 0)
            return null;

        // 如果只有一个重载，直接返回
        if (overloadGroup.Overloads.Count == 1)
            return overloadGroup.Overloads[0];

        // 先执行参数表达式获取运行时值
        var paramValues = parameters.Select(p => p.Run(manager)).ToList();

        // 获取参数的 Old8Lang 类型名称
        var paramTypes = paramValues.Select(v => OverloadResolver.GetRuntimeValueType(v)).ToList();

        // 计算每个重载的匹配分数
        var candidates = new List<(IGlobalFunction func, int score)>();

        foreach (var overload in overloadGroup.Overloads)
        {
            var score = OverloadResolver.CalculateTotalMatchScore(overload, paramTypes);
            if (score >= 0)
            {
                candidates.Add((overload, score));
            }
        }

        if (candidates.Count == 0)
        {
            // 没有精确匹配，尝试找一个可以接受参数数量的重载
            foreach (var overload in overloadGroup.Overloads)
            {
                if (OverloadResolver.CanAcceptParameters(overload, paramTypes))
                {
                    return overload;
                }
            }
            return null;
        }

        // 选择分数最高的重载
        return candidates.OrderByDescending(c => c.score).First().func;
    }
}
