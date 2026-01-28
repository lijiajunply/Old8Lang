using System.Reflection;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Utilities;
// ReSharper disable CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 执行方法
/// </summary>
public partial class FuncLangValue
{
    public override LangValueType Run(VariateManager manager)
    {
        // 如果这个函数没有方法引用（即是 Old8Lang 函数而非原生方法）
        if (Method is null && Ids is not null)
        {
            // 对于生成器函数，我们需要特殊处理
            if (ContainsYieldStatement(BlockStatement))
            {
                // 检查是否有参数需要处理
                if (Ids.Count > 0)
                {
                    // 对于生成器函数，我们需要创建一个闭包，捕获当前作用域
                    var generatorClosure =
                        new FuncLangValue(Id, Ids, BlockStatement, GenericParameters, Position, IsLambda)
                        {
                            // 生成器需要独立的作用域副本，使用深拷贝
                            CapturedScope = manager.Clone()
                        };

                    return new GeneratorLangValue(generatorClosure, Position);
                }

                // 没有参数的生成器函数
                var noParamClosure = new FuncLangValue(Id, Ids, BlockStatement, GenericParameters, Position, IsLambda)
                {
                    // 生成器需要独立的作用域副本，使用深拷贝
                    CapturedScope = manager.Clone()
                };

                return new GeneratorLangValue(noParamClosure, Position);
            }

            var closureFunc = new FuncLangValue(Id, Ids, BlockStatement, GenericParameters, Position, IsLambda)
            {
                // 使用深拷贝创建独立的作用域副本
                // 这样即使外层函数返回，lambda 仍然可以访问捕获的变量
                CapturedScope = manager.Clone()
            };

            return closureFunc;
        }

        // 原生方法或其他情况直接返回自身
        return this;
    }

    /// <summary>
    /// 创建一个带有捕获作用域的函数副本
    /// 用于模块导入时，确保函数可以访问模块作用域中的变量
    /// </summary>
    /// <param name="manager">要捕获的作用域</param>
    /// <returns>带有捕获作用域的函数副本</returns>

    public FuncLangValue CreateWithCapturedScope(VariateManager manager)
    {
        // 如果是原生方法，直接返回自身（原生方法不需要捕获作用域）
        if (Method is not null)
        {
            return this;
        }

        // 创建带有捕获作用域的函数副本
        return new FuncLangValue(Id, Ids ?? [], BlockStatement, GenericParameters, Position, IsLambda)
        {
            CapturedScope = manager.Clone()
        };
    }

    /// <summary>
    /// 执行生成器函数，返回下一个值
    /// </summary>
    /// <param name="variateManagerFunc">变量管理器</param>
    /// <param name="ids">参数列表</param>
    /// <param name="obj">对象实例</param>
    /// <returns>生成器的下一个值</returns>

    public LangValueType RunGenerator(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        // 创建一个新的变量管理器来执行生成器
        var generatorManager = new VariateManager
        {
            LangInfo = variateManagerFunc.LangInfo,
            Path = variateManagerFunc.Path,
            Interpreter = variateManagerFunc.Interpreter,
            IsFunc = true
        };

        // 处理参数
        if (Ids is not null && Ids.Count != 0)
        {
            var paramValues = ProcessAndValidateParameters(ids, variateManagerFunc);

            // 设置参数值到生成器的变量管理器
            for (var i = 0; i < Ids.Count; i++)
            {
                generatorManager.SetParameter(Ids[i], paramValues[i]);
            }
        }

        // 运行函数体
        BlockStatement.Run(generatorManager);

        return generatorManager.Result;
    }


    /// <summary>
    /// 执行函数调用，支持命名参数
    /// </summary>
    /// <param name="variateManagerFunc">调用时的变量管理器</param>
    /// <param name="positionalArgs">位置参数表达式列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置信息</param>
    /// <param name="obj">对象实例（方法调用时使用）</param>
    /// <returns>函数执行结果</returns>

    public virtual LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> positionalArgs,
        List<NamedArgument>? namedArgs, SourcePosition callPosition, object? obj = null)
    {
        // 如果没有命名参数，使用原有的逻辑
        if (namedArgs is null || namedArgs.Count == 0)
        {
            return Run(variateManagerFunc, positionalArgs, obj);
        }

        // 如果是原生 .NET 方法，需要特殊处理
        if (Method is not null)
        {
            var reorderedArgs = ReorderNativeMethodArguments(positionalArgs, namedArgs, callPosition, obj);
            return Run(variateManagerFunc, reorderedArgs, obj);
        }

        // Old8Lang 函数：将命名参数转换为位置参数
        var reorderedOld8Args = ReorderArgumentsWithNamedParameters(positionalArgs, namedArgs, callPosition);

        // 使用重新排序后的参数调用原有方法
        return Run(variateManagerFunc, reorderedOld8Args, obj);
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表
    /// </summary>
    /// <param name="positionalArgs">位置参数列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    /// <returns>重新排序后的参数列表</returns>

    private List<LangExpression> ReorderArgumentsWithNamedParameters(
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPosition)
    {
        if (Ids is null || Ids.Count == 0)
        {
            if (namedArgs.Count > 0)
            {
                throw new ArgumentError(callPosition,
                    $"函数 '{Id?.IdName ?? "匿名函数"}' 不接受任何参数，但提供了命名参数");
            }

            return positionalArgs;
        }

        // 1. 验证命名参数的合法性
        ValidateNamedArguments(namedArgs, callPosition);

        // 2. 创建参数槽位数组
        var paramSlots = new LangExpression?[Ids.Count];
        var parameterFilled = new bool[Ids.Count];

        // 3. 填充位置参数
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            if (i >= Ids.Count)
            {
                throw new ArgumentError(callPosition,
                    $"函数 '{Id?.IdName}' 期望最多 {Ids.Count} 个参数，但位置参数提供了 {positionalArgs.Count} 个");
            }

            paramSlots[i] = positionalArgs[i];
            parameterFilled[i] = true;
        }

        // 4. 填充命名参数
        foreach (var namedArg in namedArgs)
        {
            // 查找参数索引
            int paramIndex = -1;
            for (int i = 0; i < Ids.Count; i++)
            {
                if (Ids[i].IdName == namedArg.Name)
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
            {
                throw new ArgumentError(namedArg.Position,
                    $"函数 '{Id?.IdName}' 没有名为 '{namedArg.Name}' 的参数");
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
        for (int i = 0; i < Ids.Count; i++)
        {
            if (!parameterFilled[i])
            {
                // 检查是否有默认值
                if (Ids[i].DefaultValue is not null)
                {
                    // 使用默认值
                    paramSlots[i] = Ids[i].DefaultValue;
                }
                else if (Ids[i].IsParams)
                {
                    // params 参数，创建空数组
                    paramSlots[i] = new ArrayLangValue(new List<LangExpression>(), elementType: null, Ids[i].Position);
                }
                else
                {
                    throw new ArgumentError(callPosition,
                        $"函数 '{Id?.IdName}' 的必需参数 '{Ids[i].IdName}' (第{i + 1}个参数) 未提供值");
                }
            }
        }

        // 6. 转换为列表并返回
        // 所有槽位都应该已经被填充，不应该有 null
        var result = new List<LangExpression>(paramSlots.Length);
        for (int i = 0; i < paramSlots.Length; i++)
        {
            if (paramSlots[i] is null)
            {
                throw new ArgumentError(callPosition,
                    $"内部错误：函数 '{Id?.IdName}' 的参数槽位 {i} 未被填充");
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

        // 检查是否尝试对 params 参数使用命名参数
        if (Ids is not null)
        {
            foreach (var paramsName in from t in Ids
                     where t.IsParams
                     select t.IdName
                     into paramsName
                     where namedArgs.Any(na => na.Name == paramsName)
                     select paramsName)
            {
                throw new ArgumentError(callPosition,
                    $"不支持对 params 参数 '{paramsName}' 使用命名参数");
            }
        }
    }

    /// <summary>
    /// 为原生 .NET 方法重新排序参数（支持命名参数）
    /// </summary>
    /// <param name="positionalArgs">位置参数列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    /// <param name="obj">对象实例（方法调用时使用）</param>
    /// <returns>重新排序后的参数列表</returns>
    private List<LangExpression> ReorderNativeMethodArguments(
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPosition,
        object? obj)
    {
        if (Method is null)
        {
            throw new InvalidOperationError(callPosition, "方法引用为空");
        }

        // 获取方法的所有参数
        var methodParams = Method.GetParameters();
        var paramStartIndex = obj is not null ? 1 : 0; // 如果有 this 参数，跳过第一个参数
        var effectiveParams = methodParams.Skip(paramStartIndex).ToArray();

        // 1. 验证命名参数的合法性
        var seenNames = new HashSet<string>();
        foreach (var namedArg in namedArgs)
        {
            if (!seenNames.Add(namedArg.Name))
            {
                throw new ArgumentError(namedArg.Position,
                    $"命名参数 '{namedArg.Name}' 重复指定");
            }
        }

        // 2. 创建参数槽位数组
        var paramSlots = new LangExpression?[effectiveParams.Length];
        var parameterFilled = new bool[effectiveParams.Length];

        // 3. 填充位置参数
        for (int i = 0; i < positionalArgs.Count; i++)
        {
            if (i >= effectiveParams.Length)
            {
                throw new ArgumentError(callPosition,
                    $"方法 '{Method.Name}' 期望最多 {effectiveParams.Length} 个参数，但位置参数提供了 {positionalArgs.Count} 个");
            }

            paramSlots[i] = positionalArgs[i];
            parameterFilled[i] = true;
        }

        // 4. 填充命名参数
        foreach (var namedArg in namedArgs)
        {
            // 查找参数索引
            int paramIndex = -1;
            for (int i = 0; i < effectiveParams.Length; i++)
            {
                if (effectiveParams[i].Name == namedArg.Name)
                {
                    paramIndex = i;
                    break;
                }
            }

            if (paramIndex == -1)
            {
                throw new ArgumentError(namedArg.Position,
                    $"方法 '{Method.Name}' 没有名为 '{namedArg.Name}' 的参数");
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
        for (int i = 0; i < effectiveParams.Length; i++)
        {
            if (!parameterFilled[i])
            {
                // 检查是否有默认值
                if (effectiveParams[i].HasDefaultValue)
                {
                    // 对于有默认值的参数，我们不需要在这里填充
                    // 因为 Run 方法会处理默认值
                    // 但我们需要标记这个槽位为已填充（使用 null 占位）
                    paramSlots[i] = null;
                }
                else
                {
                    throw new ArgumentError(callPosition,
                        $"方法 '{Method.Name}' 的必需参数 '{effectiveParams[i].Name}' (第{i + 1}个参数) 未提供值");
                }
            }
        }

        // 6. 转换为列表并返回（过滤掉 null 占位符）
        var result = new List<LangExpression>();
        for (int i = 0; i < paramSlots.Length; i++)
        {
            if (paramSlots[i] is not null)
            {
                result.Add(paramSlots[i]!);
            }
        }

        return result;
    }


    public virtual LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        if (Method is not null)
        {
            // 获取方法的所有参数
            var methodParams = Method.GetParameters();
            var expectedParams = methodParams.Length;
            if (obj is not null) expectedParams--; // 如果有this参数，减去1
            var actualParams = ids.Count;

            // 检查参数数量是否匹配，考虑可选参数
            if (actualParams > expectedParams)
            {
                throw new ArgumentError(Position,
                    $"方法 '{Method.Name}' 期望最多 {expectedParams} 个参数，但实际提供了 {actualParams} 个参数");
            }

            // 检查是否所有缺失的参数都是可选参数
            if (actualParams < expectedParams)
            {
                // 计算缺失参数的起始索引（考虑 this 参数）
                var startIndex = obj is not null ? actualParams + 1 : actualParams;

                // 检查从 actualParams 开始的所有参数是否都有默认值
                for (int i = startIndex; i < methodParams.Length; i++)
                {
                    if (!methodParams[i].HasDefaultValue)
                    {
                        throw new ArgumentError(Position,
                            $"方法 '{Method.Name}' 的参数 '{methodParams[i].Name}' 缺少实参且没有默认值");
                    }
                }
            }

            var values = ids.Select(expr => expr.Run(variateManagerFunc)).ToList();
            var convertedValues = Apis.ListToObjects(values).ToArray();

            // 根据目标参数类型转换参数
            var adjustedParams = new object?[convertedValues.Length];
            var paramStartIndex = obj is not null ? 1 : 0; // 如果有 this 参数，跳过第一个参数

            for (int i = 0; i < convertedValues.Length; i++)
            {
                var methodParamIndex = i + paramStartIndex;
                var targetType = methodParams[methodParamIndex].ParameterType;
                var value = convertedValues[i];

                // 如果目标类型是数组，且值是 List<object>，进行转换
                if (targetType.IsArray && value is List<object> list)
                {
                    var elementType = targetType.GetElementType()!;
                    var array = Array.CreateInstance(elementType, list.Count);
                    for (int j = 0; j < list.Count; j++)
                    {
                        array.SetValue(Convert.ChangeType(list[j], elementType), j);
                    }

                    adjustedParams[i] = array;
                }
                // 如果值为 null 且目标是可空类型，直接赋 null
                else if (value is null && targetType.IsGenericType &&
                         targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    adjustedParams[i] = null;
                }
                // 如果值不为 null 且类型不匹配，尝试类型转换
                else if (value is not null && !targetType.IsInstanceOfType(value))
                {
                    try
                    {
                        // 处理可空类型
                        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
                        adjustedParams[i] = Convert.ChangeType(value, underlyingType);
                    }
                    catch (InvalidCastException)
                    {
                        // 如果转换失败，保持原值，让反射调用时报错
                        adjustedParams[i] = value;
                    }
                }
                else
                {
                    adjustedParams[i] = value;
                }
            }

            // 如果有可选参数缺失，需要填充默认值
            object?[] finalParams;
            if (actualParams < expectedParams)
            {
                finalParams = new object?[expectedParams];
                Array.Copy(adjustedParams, finalParams, actualParams);

                // 填充默认值
                var startIndex = obj is not null ? actualParams + 1 : actualParams;
                for (int i = startIndex; i < methodParams.Length; i++)
                {
                    var targetIndex = obj is not null ? i - 1 : i;
                    finalParams[targetIndex] = methodParams[i].DefaultValue;
                }
            }
            else
            {
                finalParams = adjustedParams;
            }

            object? invoke;
            try
            {
                // 入栈：记录函数调用
                Old8Exception.PushCallStack(Method?.Name ?? "Unknown", Position);

                // 使用委托缓存优化反射调用性能
                invoke = Method is not null ? MethodInvokerCache.Invoke(Method, obj, finalParams) : null;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                // 转换 .NET 异常为 Old8Lang 异常
                var innerException = ex.InnerException;

                // 获取当前方法的调用栈信息
                var currentCallStack = new List<CallStackFrame>(Old8Exception.CurrentCallStack);

                // 基础异常信息
                string errorMessage = innerException.Message;
                string errorCode = "RUNTIME_ERROR";

                // 直接创建 Old8Exception，保留原始异常作为 innerException
                throw new Old8Exception(
                    errorCode,
                    errorMessage,
                    Position,
                    null,
                    null,
                    null,
                    null,
                    innerException);
            }
            finally
            {
                // 出栈：函数调用结束
                Old8Exception.PopCallStack();
            }

            if (invoke is null)
                return new VoidLangValue();

            var manager = new VariateManager();
            var convertedValue = ObjToValue(invoke);
            manager.Init(new Dictionary<string, LangValueType> { { "base", convertedValue } });
            manager.IsClass = false;
            manager.Result = convertedValue;
            _func?.Run(manager, ids);
            return manager.Result;
        }

        // 检查参数数量是否匹配，但允许省略带默认参数的实参
        if (Ids is not null)
        {
            var expectedParams = Ids.Count;
            var actualParams = ids.Count;

            // 检查是否有 params 参数
            var hasParamsParameter = Ids.Any(id => id.IsParams);

            // 如果有 params 参数，允许传入任意数量的参数（大于等于普通参数的数量）
            // 如果没有 params 参数，只检查最大参数数量，允许实际参数少于期望参数（如果有默认参数）
            if (!hasParamsParameter && actualParams > expectedParams)
            {
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 期望最多 {expectedParams} 个参数，但实际提供了 {actualParams} 个参数");
            }
        }

        // 检查是否是生成器函数
        if (IsGenerator)
        {
            // 对于生成器函数，我们需要特殊处理
            // 生成器函数在调用时，应该返回一个GeneratorLangValue对象
            // 这个对象包含了生成器函数的引用和调用时的参数值

            // 使用统一的参数处理方法
            var paramValues = ProcessAndValidateParameters(ids, variateManagerFunc);

            // 注意：新架构不再使用 LocalState，而是通过 ParameterValues 传递参数
            // GeneratorLangValue.Run 方法会在创建状态机时处理参数设置

            // 对于生成器函数，返回GeneratorLangValue对象，而不是直接执行
            var generator = new GeneratorLangValue(this, Position);

            // 将参数值设置到生成器的 ParameterValues 中
            if (Ids is { Count: > 0 })
            {
                for (var i = 0; i < Ids.Count; i++)
                {
                    var paramId = Ids[i];
                    var paramValue = paramValues[i];
                    generator.SetParameter(paramId.IdName, paramValue);
                }
            }

            return generator;
        }

        // 调用方法体
        // 递归深度检查
        variateManagerFunc.RecursionDepth++;

        // 提前声明executionManager以便在finally中使用
        VariateManager? executionManager = null;

        try
        {
            // 入栈：记录函数调用
            Old8Exception.PushCallStack(Id?.IdName ?? "anonymous", Position);

            // 如果有捕获的作用域（闭包），使用捕获的作用域而不是调用时的作用域
            // 这样函数体就能访问定义时的外部变量
            if (CapturedScope is not null)
            {
                // 使用捕获的作用域作为基础
                executionManager = CapturedScope;
                // 增加递归深度
                executionManager.RecursionDepth = variateManagerFunc.RecursionDepth;
            }
            else
            {
                // 没有捕获作用域，使用调用时的作用域
                executionManager = variateManagerFunc;
            }

            executionManager.AddChildren();

            // 保存当前的函数返回类型，避免嵌套调用时被覆盖
            var originalReturnType = executionManager.CurrentFunctionReturnType;
            var originalTypeArgumentMapping = executionManager.CurrentFunctionTypeArgumentMapping;

            // 保存当前的IsReturn标志，防止函数调用影响生成器上下文
            var originalIsReturn = executionManager.IsReturn;

            // 保存并清除生成器上下文，防止函数内部错误地操作生成器状态
            // 函数应该以标准模式执行，即使它是在生成器中被调用的
            var originalGeneratorContext = executionManager.GeneratorContext;
            executionManager.GeneratorContext = null;

            executionManager.IsFunc = true; // 设置为函数上下文
            executionManager.CurrentFunctionReturnType = Id?.AssumptionType; // 设置当前函数的返回类型注解（Id.AssumptionType包含了返回类型）

            // 设置当前函数的泛型类型参数映射
            // 优先使用已有的映射（从泛型类实例传递过来），否则使用函数自己的泛型参数映射（泛型函数）
            if (originalTypeArgumentMapping is null && TypeArgumentMapping is not null)
            {
                executionManager.CurrentFunctionTypeArgumentMapping = TypeArgumentMapping;
            }

            // 将静态成员添加到方法的变量管理器中
            var thisValue = executionManager.GetValue(new LangId("this"));
            if (thisValue is AnyLangValue)
            {
                // 将类的静态成员添加到方法的变量管理器中
                foreach (var importInfo in executionManager.ImportInfos)
                {
                    if (importInfo is TypeTemplate typeTemplate)
                    {
                        foreach (var staticMember in typeTemplate.StaticVariates)
                        {
                            executionManager.Set(staticMember.Key, staticMember.Value.Run(executionManager));
                        }
                    }
                }
            }

            if (Ids is not null && Ids.Count != 0)
            {
                // 首次调用时初始化默认参数值缓存
                if (CachedDefaultValues is null && Ids.Any(id => id.DefaultValue is not null))
                {
                    InitializeDefaultValueCache(executionManager);
                }

                // 使用统一的参数处理方法
                var paramValues = ProcessAndValidateParameters(ids, variateManagerFunc, executionManager);

                // 然后将所有参数值（包括默认参数）设置到函数的变量管理器中
                // 使用SetParameter确保参数在当前作用域中创建新变量，保持递归调用中的独立性
                for (var i = 0; i < Ids.Count; i++)
                {
                    executionManager.SetParameter(Ids[i], paramValues[i]);
                }
            }

            // 保持函数上下文标志，确保变量遮蔽正常工作
            // executionManager.IsFunc = true; // 已经设置为true，不要重置

            // 运行方法体
            BlockStatement.Run(executionManager);

            // 保存返回值
            var result = executionManager.Result;

            // 重置return标志为原始值，确保函数调用不会影响外部上下文
            // 特别重要：在生成器上下文中，函数的return不应该导致生成器退出
            executionManager.IsReturn = originalIsReturn;

            // 恢复生成器上下文
            executionManager.GeneratorContext = originalGeneratorContext;

            // 恢复原始的函数返回类型，避免嵌套调用时的类型污染
            executionManager.CurrentFunctionReturnType = originalReturnType;
            executionManager.CurrentFunctionTypeArgumentMapping = originalTypeArgumentMapping;

            return result;
        }
        finally
        {
            // 在移除子作用域之前执行所有defer语句（此时变量仍然可见）
            // 必须在finally块中执行，以确保即使函数抛出异常，defer也能执行
            if (executionManager is not null)
            {
                executionManager.ExecuteDefers();

                // 移除子作用域，但是要注意，在init方法中使用this关键字设置的值已经被保存到实例中了
                // 所以这里移除子作用域不会影响实例的状态
                executionManager.RemoveChildren();
            }

            // 确保递归深度总是被递减
            variateManagerFunc.RecursionDepth--;
            // 出栈：函数调用结束
            Old8Exception.PopCallStack();
        }
    }


}
