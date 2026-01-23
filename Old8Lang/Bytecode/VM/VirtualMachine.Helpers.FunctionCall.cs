using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 函数调用
/// </summary>
public partial class VirtualMachine
{
    private object? CallNativeFunction(string funcName, object?[] args)
    {
        // 首先尝试从全局函数注册表中查找
        var globalFunction = GlobalFunctionRegistry.Instance.TryGetFunction(funcName);
        if (globalFunction != null)
        {
            try
            {
                return globalFunction.ExecuteInVM(args);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(new SourcePosition(), $"调用全局函数 {funcName} 时发生错误: {ex.Message}");
            }
        }

        // 处理特殊的辅助函数（不在全局函数注册表中）
        switch (funcName)
        {
            case "System.String::Concat":
            {
                // 字符串拼接
                if (args.Length > 0 && args[0] is object[] array)
                {
                    return string.Concat(array.Select(ToString));
                }

                return string.Concat(args.Select(ToString));
            }

            case "CheckRange":
                // 参数: value, start, end, includeStart, includeEnd
                if (args.Length >= 5)
                {
                    double value = Convert.ToDouble(args[0]);
                    double start = Convert.ToDouble(args[1]);
                    double end = Convert.ToDouble(args[2]);
                    bool includeStart = Convert.ToBoolean(args[3]);
                    bool includeEnd = Convert.ToBoolean(args[4]);

                    bool inRange = true;
                    if (includeStart)
                        inRange &= value >= start;
                    else
                        inRange &= value > start;

                    if (includeEnd)
                        inRange &= value <= end;
                    else
                        inRange &= value < end;

                    return inRange;
                }

                return false;

            case "FlattenTuple":
                // 展平元组为列表
                if (args.Length > 0 && args[0] is TupleLangValue tuple)
                {
                    return FlattenTupleHelper(tuple);
                }

                return new List<object?>();

            case "GetCount":
                // 获取集合元素数量
                if (args.Length > 0)
                {
                    return args[0] switch
                    {
                        string str => str.Length,
                        Array array => array.Length,
                        IList list => list.Count,
                        _ => 0
                    };
                }

                return 0;

            case "ResourceManagerTryDispose":
                if (args.Length > 0)
                {
                    int resourceId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.TryDispose(resourceId);
                }

                return null;

            default:
                throw new MethodNotFoundError(new SourcePosition(), funcName);
        }
    }

    /// <summary>
    /// 对数组执行切片操作
    /// </summary>

    private object?[] ArrangeArgumentsWithNamed(FunctionMetadata function, object?[] positionalArgs,
        string[] namedArgNames, object?[] namedArgValues)
    {
        int paramCount = function.Parameters.Count;
        var args = new object?[paramCount];
        var filled = new bool[paramCount]; // 跟踪哪些参数位置已被填充

        // 首先填充位置参数
        for (int i = 0; i < positionalArgs.Length; i++)
        {
            if (i >= paramCount)
            {
                throw new ArgumentError(new SourcePosition(), $"函数 {function.Name} 期望 {paramCount} 个参数，但提供了过多的参数");
            }

            args[i] = positionalArgs[i];
            filled[i] = true;
        }

        // 然后根据命名参数填充剩余位置
        for (int i = 0; i < namedArgNames.Length; i++)
        {
            string paramName = namedArgNames[i];
            object? paramValue = namedArgValues[i];

            // 查找参数在函数参数列表中的位置
            int paramIndex = function.Parameters.IndexOf(paramName);
            if (paramIndex == -1)
            {
                throw new ArgumentError(new SourcePosition(), $"函数 {function.Name} 没有名为 '{paramName}' 的参数");
            }

            // 检查该位置是否已被位置参数占用
            if (filled[paramIndex])
            {
                throw new ArgumentError(new SourcePosition(), $"参数 '{paramName}' 已通过位置参数提供");
            }

            args[paramIndex] = paramValue;
            filled[paramIndex] = true;
        }

        // 检查是否所有参数都已提供，如果没有则使用默认值
        for (int i = 0; i < paramCount; i++)
        {
            if (!filled[i])
            {
                // 参数未提供，检查是否有默认值
                if (i < function.DefaultValues.Count && function.DefaultValues[i] != null)
                {
                    // 使用默认值
                    args[i] = function.DefaultValues[i];
                    filled[i] = true;
                }
                else
                {
                    // 没有默认值，抛出错误
                    throw new ArgumentError(new SourcePosition(), $"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                }
            }
        }

        return args;
    }

    // ===== Task 管理 =====

    /// <summary>
    /// 注册 Task 并返回 ID
    /// </summary>

}
