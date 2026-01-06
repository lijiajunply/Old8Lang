using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using Old8Lang.Utilities;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 函数 ，作为一种变量存在
/// </summary>
public class FuncLangValue : ImportInfo
{
    public readonly LangId? Id;
    public readonly BlockStatement BlockStatement = new([]);

    public readonly List<LangId>? Ids;

    public readonly MethodInfo? Method;

    private readonly FuncLangValue? Func;

    // 闭包环境：捕获的作用域，用于支持闭包变量访问
    private VariateManager? CapturedScope { get; init; }

    // 函数类型：区分普通方法和Lambda表达式
    private bool IsLambda { get; init; }

    // 默认参数值缓存：缓存常量表达式的默认值，避免重复求值
    private Dictionary<int, LangValueType>? CachedDefaultValues { get; set; }

    /// <summary>
    /// 泛型参数列表
    /// 例如: func map<T, U>(...) 中的 [T, U]
    /// </summary>
    public readonly List<GenericParameter>? GenericParameters;

    /// <summary>
    /// 是否为泛型函数
    /// </summary>
    public bool IsGeneric => GenericParameters is { Count: > 0 };

    /// <summary>
    /// 当前实例的类型参数映射（用于泛型实例化）
    /// 例如: map<int, string> 时为 {"T": int, "U": string}
    /// </summary>
    public Dictionary<string, ITypeInfo>? TypeArgumentMapping { get; set; }

    /// <summary>
    /// 文档注释内容
    /// 存储通过 /// 语法编写的函数文档注释（结构化）
    /// </summary>
    public DocCommentInfo? DocComment { get; set; }

    public FuncLangValue(
        LangId? id,
        List<LangId> ids,
        BlockStatement blockStatement,
        List<GenericParameter>? genericParameters = null,
        SourcePosition position = default,
        bool isLambda = false) :
        base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
        GenericParameters = genericParameters;
        IsLambda = isLambda;
    }

    public FuncLangValue(string idName, MethodInfo methodInfo, FuncLangValue? func = null,
        SourcePosition position = default) : base(position)
    {
        Id = new LangId(idName);
        Method = methodInfo;
        Func = func;
        IsLambda = false; // 原生方法不是Lambda表达式
        GenericParameters = null; // 原生方法暂不支持泛型
    }

    /// <summary>
    /// 检查函数是否是生成器函数（包含yield语句）
    /// </summary>
    private bool IsGenerator => ContainsYieldStatement(BlockStatement);

    /// <summary>
    /// 递归检查语句是否包含yield语句
    /// </summary>
    private bool ContainsYieldStatement(OldStatement stmt)
    {
        if (stmt is YieldStatement)
            return true;

        // 特殊处理 TryStatement，因为它的 Count 返回 0
        if (stmt is TryStatement tryStmt)
        {
            // 使用公开属性访问块
            // 检查 try 块
            if (ContainsYieldStatement(tryStmt.TryBlock))
                return true;

            // 检查 catch 块
            foreach (var (_, _, catchBlock) in tryStmt.CatchBlocks)
            {
                if (ContainsYieldStatement(catchBlock))
                    return true;
            }

            // 检查 finally 块
            if (tryStmt.FinallyBlock != null && ContainsYieldStatement(tryStmt.FinallyBlock))
                return true;

            return false;
        }

        // 检查块语句中的子语句
        for (int i = 0; i < stmt.Count; i++)
        {
            var child = stmt[i];
            if (child != null && ContainsYieldStatement(child))
                return true;
        }

        return false;
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 如果这个函数没有方法引用（即是 Old8Lang 函数而非原生方法）
        if (Method == null && Ids != null)
        {
            // 对于生成器函数，我们需要特殊处理
            if (ContainsYieldStatement(BlockStatement))
            {
                // 检查是否有参数需要处理
                if (Ids.Count > 0)
                {
                    // 对于生成器函数，我们需要创建一个闭包，捕获当前作用域
                    var generatorClosure = new FuncLangValue(Id, Ids, BlockStatement, GenericParameters, Position, IsLambda)
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
        if (Method != null)
        {
            return this;
        }

        // 创建带有捕获作用域的函数副本
        return new FuncLangValue(Id, Ids ?? new List<LangId>(), BlockStatement, GenericParameters, Position, IsLambda)
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
        if (Ids != null && Ids.Count != 0)
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
        if (namedArgs == null || namedArgs.Count == 0)
        {
            return Run(variateManagerFunc, positionalArgs, obj);
        }

        // 将命名参数转换为位置参数
        var reorderedArgs = ReorderArgumentsWithNamedParameters(positionalArgs, namedArgs, callPosition, variateManagerFunc);

        // 使用重新排序后的参数调用原有方法
        return Run(variateManagerFunc, reorderedArgs, obj);
    }

    /// <summary>
    /// 将位置参数和命名参数重新排序为完整的位置参数列表
    /// </summary>
    /// <param name="positionalArgs">位置参数列表</param>
    /// <param name="namedArgs">命名参数列表</param>
    /// <param name="callPosition">调用位置</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>重新排序后的参数列表</returns>
    private List<LangExpression> ReorderArgumentsWithNamedParameters(
        List<LangExpression> positionalArgs,
        List<NamedArgument> namedArgs,
        SourcePosition callPosition,
        VariateManager manager)
    {
        if (Ids == null || Ids.Count == 0)
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
                if (Ids[i].DefaultValue != null)
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
            if (paramSlots[i] == null)
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
        foreach (var namedArg in namedArgs)
        {
            if (seenNames.Contains(namedArg.Name))
            {
                throw new ArgumentError(namedArg.Position,
                    $"命名参数 '{namedArg.Name}' 重复指定");
            }
            seenNames.Add(namedArg.Name);
        }

        // 检查是否尝试对 params 参数使用命名参数
        if (Ids != null)
        {
            for (int i = 0; i < Ids.Count; i++)
            {
                if (Ids[i].IsParams)
                {
                    var paramsName = Ids[i].IdName;
                    if (namedArgs.Any(na => na.Name == paramsName))
                    {
                        throw new ArgumentError(callPosition,
                            $"不支持对 params 参数 '{paramsName}' 使用命名参数");
                    }
                }
            }
        }
    }

    public virtual LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
    {
        if (Method != null)
        {
            // 获取方法的所有参数
            var methodParams = Method.GetParameters();
            var expectedParams = methodParams.Length;
            if (obj != null) expectedParams--; // 如果有this参数，减去1
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
                var startIndex = obj != null ? actualParams + 1 : actualParams;

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
            var paramStartIndex = obj != null ? 1 : 0; // 如果有 this 参数，跳过第一个参数

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
                else if (value == null && targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    adjustedParams[i] = null;
                }
                // 如果值不为 null 且类型不匹配，尝试类型转换
                else if (value != null && !targetType.IsAssignableFrom(value.GetType()))
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
                var startIndex = obj != null ? actualParams + 1 : actualParams;
                for (int i = startIndex; i < methodParams.Length; i++)
                {
                    var targetIndex = obj != null ? i - 1 : i;
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
                if (Method != null)
                {
                    invoke = MethodInvokerCache.Invoke(Method, obj, finalParams);
                }
                else
                {
                    invoke = null;
                }
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
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
            Func?.Run(manager, ids);
            return manager.Result;
        }

        // 检查参数数量是否匹配，但允许省略带默认参数的实参
        if (Ids != null)
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
            if (CapturedScope != null)
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

            executionManager.IsFunc = true; // 设置为函数上下文
            executionManager.CurrentFunctionReturnType = Id?.AssumptionType; // 设置当前函数的返回类型注解（Id.AssumptionType包含了返回类型）

            // 设置当前函数的泛型类型参数映射
            // 优先使用已有的映射（从泛型类实例传递过来），否则使用函数自己的泛型参数映射（泛型函数）
            if (originalTypeArgumentMapping == null && TypeArgumentMapping != null)
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

            if (Ids != null && Ids.Count != 0)
            {
                // 首次调用时初始化默认参数值缓存
                if (CachedDefaultValues == null && Ids.Any(id => id.DefaultValue != null))
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

            // 重置return标志，确保函数调用不会影响外部上下文
            executionManager.IsReturn = false;

            // 恢复原始的函数返回类型，避免嵌套调用时的类型污染
            executionManager.CurrentFunctionReturnType = originalReturnType;
            executionManager.CurrentFunctionTypeArgumentMapping = originalTypeArgumentMapping;

            return result;
        }
        finally
        {
            // 在移除子作用域之前执行所有defer语句（此时变量仍然可见）
            // 必须在finally块中执行，以确保即使函数抛出异常，defer也能执行
            if (executionManager != null)
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

    public override Type OutputType(LocalManager local)
    {
        var idType = Id?.OutputType(local);
        if (idType != null && idType != typeof(object)) return idType;
        var a = GetItemType(BlockStatement, local);
        return a;
    }

    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.OutputType(local);
                if (varType != null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.OutputType(local);
            }

            if (item == null || item.Count == 0)
            {
                continue;
            }

            var innerType = GetItemType(item, local);
            if (innerType != typeof(void))
            {
                return innerType;
            }
        }

        return typeof(void);
    }

    public override string ToString()
    {
        if (Method != null)
        {
            return $"{Method}";
        }

        var paramList = Ids != null ? string.Join(", ", Ids) : string.Empty;
        return $"func {Id}({paramList}) \n {{ {BlockStatement} }}";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 如果是.NET方法，直接加载方法引用
        if (Method != null)
        {
            // 对于实例方法，需要先加载对象实例到堆栈上
            // 这里假设Method已经是正确的委托类型
        }

        // 如果是Old8Lang函数，直接返回，因为函数调用是通过Instance类处理的
        // 不需要在这里加载函数委托
    }

    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 【新增】Lambda表达式类型注解验证
        if (IsLambda || Id == null)
        {
            ValidateLambdaTypeAnnotations(local, idName);
        }

        // Lambda表达式需要特殊处理：编译成Delegate
        // 普通方法：编译成DynamicMethod

        // Lambda表达式没有函数名(Id == null)，使用变量名作为方法名
        var methodName = Id?.IdName ?? idName;

        // 如果已经是编译好的方法，直接注册
        if (Method != null)
        {
            local.DelegateVar.Add(methodName, Method);
            return;
        }

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = Ids!.Select(item => item.OutputType(funcLocal)).ToArray();

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < Ids!.Count; i++)
        {
            var id = Ids[i];
            var paramType = parameterTypes[i];
            funcLocal.LocalVarTypes[id.IdName] = paramType;
        }

        // 获取返回类型
        var returnType = GetItemType(BlockStatement, funcLocal);

        // 根据函数类型选择不同的处理方式
        if (IsLambda || Id == null)
        {
            // Lambda表达式处理：编译成Delegate

            // 定义新的方法
            var dynamicMethod = new DynamicMethod(
                methodName,
                returnType,
                parameterTypes,
                true
            );

            // 创建方法的 IL 发射器
            var methodIl = dynamicMethod.GetILGenerator();

            // 处理参数
            for (var i = 0; i < Ids!.Count; i++)
            {
                var id = Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType);
                funcLocal.AddLocalVar(id.IdName, localVar);
                // 加载参数并存储到局部变量
                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }

            // 生成方法体的 IL 代码
            BlockStatement.GenerateIl(methodIl, funcLocal);

            // 检查函数体的最后一个语句是否是 ReturnStatement
            var lastStatement = BlockStatement.Count > 0
                ? BlockStatement[^1]
                : null;

            // 确保方法有正确的返回值
            if (lastStatement is not ReturnStatement)
            {
                if (returnType == typeof(void))
                {
                    methodIl.Emit(OpCodes.Ret);
                }
                else
                {
                    // 对于有返回值的Lambda表达式，确保返回默认值
                    if (returnType.IsValueType)
                    {
                        // 根据返回类型生成不同的默认值
                        if (returnType == typeof(int))
                        {
                            methodIl.Emit(OpCodes.Ldc_I4_0);
                        }
                        else if (returnType == typeof(double))
                        {
                            methodIl.Emit(OpCodes.Ldc_R8, 0.0);
                        }
                        else if (returnType == typeof(bool))
                        {
                            methodIl.Emit(OpCodes.Ldc_I4_0);
                        }
                        else
                        {
                            // 对于其他值类型，初始化并加载默认值
                            var defaultValueLocal = methodIl.DeclareLocal(returnType);
                            methodIl.Emit(OpCodes.Initobj, returnType);
                            methodIl.Emit(OpCodes.Ldloc, defaultValueLocal);
                        }
                    }
                    else
                    {
                        // 引用类型返回null
                        methodIl.Emit(OpCodes.Ldnull);
                    }

                    methodIl.Emit(OpCodes.Ret);
                }
            }

            // 注册Lambda表达式到DelegateVar
            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
            var delegateKey = $"{methodName}${paramTypeNames}";
            local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
        }
        else
        {
            // 普通方法处理：编译成DynamicMethod

            // 定义新的方法
            var dynamicMethod = new DynamicMethod(
                methodName,
                returnType,
                parameterTypes,
                true
            );

            // 创建方法的 IL 发射器
            var methodIl = dynamicMethod.GetILGenerator();

            // 处理参数
            for (var i = 0; i < Ids!.Count; i++)
            {
                var id = Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType);
                funcLocal.AddLocalVar(id.IdName, localVar);
                // 加载参数并存储到局部变量
                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }

            // 生成方法体的 IL 代码
            BlockStatement.GenerateIl(methodIl, funcLocal);

            // 检查函数体的最后一个语句是否是 ReturnStatement
            var lastStatement = BlockStatement.Count > 0
                ? BlockStatement[^1]
                : null;

            // 确保方法有正确的返回值
            if (lastStatement is not ReturnStatement)
            {
                if (returnType == typeof(void))
                {
                    methodIl.Emit(OpCodes.Ret);
                }
                else
                {
                    // 对于有返回值的方法，确保返回默认值
                    if (returnType.IsValueType)
                    {
                        // 根据返回类型生成不同的默认值
                        if (returnType == typeof(int))
                        {
                            methodIl.Emit(OpCodes.Ldc_I4_0);
                        }
                        else if (returnType == typeof(double))
                        {
                            methodIl.Emit(OpCodes.Ldc_R8, 0.0);
                        }
                        else if (returnType == typeof(bool))
                        {
                            methodIl.Emit(OpCodes.Ldc_I4_0);
                        }
                        else
                        {
                            // 对于其他值类型，初始化并加载默认值
                            var defaultValueLocal = methodIl.DeclareLocal(returnType);
                            methodIl.Emit(OpCodes.Initobj, returnType);
                            methodIl.Emit(OpCodes.Ldloc, defaultValueLocal);
                        }
                    }
                    else
                    {
                        // 引用类型返回null
                        methodIl.Emit(OpCodes.Ldnull);
                    }

                    methodIl.Emit(OpCodes.Ret);
                }
            }

            // 将方法注册到本地变量管理器的DelegateVar中
            // 对于泛型函数，需要同时注册泛型版本和特化版本
            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
            
            if (IsGeneric)
            {
                // 注册泛型函数的基础版本（不带类型签名）
                local.DelegateVar.TryAdd(methodName, dynamicMethod);
                
                // 也注册带类型签名的版本，确保兼容性
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
            }
            else
            {
                // 普通函数只注册带类型签名的版本
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
            }

            // 同时存储函数的参数列表信息，用于支持默认参数
            if (Ids != null)
            {
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.FuncParameters.TryAdd(delegateKey, Ids);
            }
        }
    }

    /// <summary>
    /// 检查表达式是否为常量表达式（可以安全缓存）
    /// </summary>
    private static bool IsConstantExpression(LangExpression? expr)
    {
        if (expr == null) return false;

        return expr switch
        {
            // 字面量都是常量
            IntLangValue => true,
            DoubleLangValue => true,
            StringLangValue => true,
            BoolLangValue => true,
            CharLangValue => true,
            NullLangValue => true,

            // 算术运算：如果操作数都是常量，结果也是常量
            Operation op => IsConstantExpression(op.Left) && IsConstantExpression(op.Right),

            // 其他情况（变量、函数调用等）不是常量
            _ => false
        };
    }

    /// <summary>
    /// 初始化默认参数值缓存
    /// </summary>
    private void InitializeDefaultValueCache(VariateManager manager)
    {
        if (Ids == null || Ids.Count == 0) return;

        for (int i = 0; i < Ids.Count; i++)
        {
            var param = Ids[i];
            if (param.DefaultValue != null && IsConstantExpression(param.DefaultValue))
            {
                // 延迟初始化缓存字典
                CachedDefaultValues ??= new Dictionary<int, LangValueType>();

                // 预先求值并缓存
                var defaultValue = param.DefaultValue.Run(manager);
                CachedDefaultValues[i] = defaultValue;
            }
        }
    }

    /// <summary>
    /// 验证Lambda表达式的类型注解完整性（编译模式要求）
    /// </summary>
    private void ValidateLambdaTypeAnnotations(LocalManager local, string variableName)
    {
        // 验证Lambda参数的类型注解
        if (Ids != null)
        {
            for (int i = 0; i < Ids.Count; i++)
            {
                var param = Ids[i];
                if (string.IsNullOrEmpty(param.AssumptionType))
                {
                    var errorMsg =
                        $"[编译模式错误] Lambda表达式 '{variableName}' 的参数 '{param.IdName}' (第{i + 1}个参数) 缺少类型注解\n\n" +
                        $"编译模式下Lambda表达式的所有参数必须显式声明类型注解。\n\n" +
                        $"修复示例：\n" +
                        $"  {variableName} <- ({param.IdName}:int, ...) -> {{ ... }}\n" +
                        $"  {variableName} <- ({param.IdName}:int, ...) -> expression\n\n" +
                        $"支持的类型：int, double, string, bool, char, list<T>, array<T>";
                    local.ReportError(errorMsg, param.Position);
                }
            }
        }

        // 注意：Lambda返回类型允许推断，不需要强制声明
    }

    /// <summary>
    /// 验证函数调用时的参数类型匹配
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="argumentValues">计算后的参数值列表</param>
    /// <param name="executionManager">执行管理器，用于获取泛型类型参数映射</param>
    private void ValidateParameterTypes(
        List<LangExpression> argumentExpressions,
        List<LangValueType> argumentValues,
        VariateManager? executionManager = null)
    {
        if (Ids == null) return;

        // 从执行管理器获取泛型类型映射（泛型类的方法）
        // 如果没有，则使用函数自身的泛型映射（泛型函数）
        var typeMapping = executionManager?.CurrentFunctionTypeArgumentMapping ?? TypeArgumentMapping;

        // 使用全局类型检查器进行验证
        TypeChecker.ValidateParameterTypes(
            argumentExpressions.Cast<IOldLangTree>().ToList(),
            argumentValues,
            Ids,
            typeMapping);
    }

    /// <summary>
    /// 统一的参数处理方法：计算、验证、处理默认值和 params 参数，并返回最终参数值列表
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="variManager">外部变量管理器，用于计算参数值</param>
    /// <param name="executionManager">执行管理器，用于获取缓存的默认值</param>
    /// <returns>处理完成的参数值列表</returns>
    private List<LangValueType> ProcessAndValidateParameters(
        List<LangExpression> argumentExpressions,
        VariateManager variManager,
        VariateManager? executionManager = null)
    {
        if (Ids == null) return new List<LangValueType>();

        // 1. 计算所有传入参数的值
        var paramValues = argumentExpressions.Select(expr => expr.Run(variManager)).ToList();

        // 检查是否有 params 参数
        var paramsIndex = -1;
        for (int i = 0; i < Ids.Count; i++)
        {
            if (Ids[i].IsParams)
            {
                paramsIndex = i;
                break;
            }
        }

        // 如果有 params 参数，需要特殊处理
        if (paramsIndex >= 0)
        {
            // params 参数之前的普通参数数量
            var regularParamCount = paramsIndex;

            // 检查是否提供了足够的参数
            if (paramValues.Count < regularParamCount)
            {
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 至少需要 {regularParamCount} 个参数，但实际提供了 {paramValues.Count} 个参数");
            }

            // 2. 验证普通参数的类型匹配（仅在有类型注解时进行检查）
            if (regularParamCount > 0)
            {
                var regularArgExpressions = argumentExpressions.Take(regularParamCount).ToList();
                var regularParamValues = paramValues.Take(regularParamCount).ToList();
                ValidateParameterTypes(regularArgExpressions, regularParamValues, executionManager);
            }

            // 3. 处理 params 参数：将剩余的参数打包成数组
            var paramsValues = paramValues.Skip(regularParamCount).ToList();

            // 创建 ArrayLangValue
            var paramsArrayValue = new ArrayLangValue(paramsValues);

            // 替换 paramValues：保留普通参数 + params 数组
            var finalParamValues = paramValues.Take(regularParamCount).ToList();
            finalParamValues.Add(paramsArrayValue);

            return finalParamValues;
        }

        // 没有 params 参数，使用原有逻辑
        // 2. 验证参数类型匹配（仅在有类型注解时进行检查）
        ValidateParameterTypes(argumentExpressions, paramValues, executionManager);

        // 3. 处理默认参数，补全缺失的参数值
        for (var i = paramValues.Count; i < Ids.Count; i++)
        {
            var parameter = Ids[i];
            if (parameter.DefaultValue != null)
            {
                // 优先使用缓存的默认值（如果提供了执行管理器）
                if (executionManager != null && CachedDefaultValues?.TryGetValue(i, out var cachedValue) == true)
                {
                    paramValues.Add(cachedValue);
                }
                else
                {
                    // 非常量表达式，需要每次计算
                    var defaultValueManager = executionManager ?? variManager;
                    var defaultValue = parameter.DefaultValue.Run(defaultValueManager);
                    paramValues.Add(defaultValue);
                }
            }
            else
            {
                // 没有默认参数且没有传入参数，抛出错误
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 的参数 '{parameter.IdName}' 缺少实参且没有默认值");
            }
        }

        return paramValues;
    }

    /// <summary>
    /// 实例化泛型函数
    /// </summary>
    public FuncLangValue InstantiateGeneric(
        Dictionary<string, ITypeInfo> typeArguments,
        TypeAnnotationManager typeAnnotationManager)
    {
        if (!IsGeneric)
        {
            throw new InvalidOperationException($"函数 {Id?.IdName} 不是泛型函数");
        }

        // 验证类型参数数量
        if (typeArguments.Count != GenericParameters!.Count)
        {
            throw new ArgumentException(
                $"类型参数数量不匹配：期望 {GenericParameters.Count} 个，实际 {typeArguments.Count} 个");
        }

        // 验证约束（如果有）
        foreach (var genericParam in GenericParameters)
        {
            if (genericParam.HasConstraints && typeArguments.TryGetValue(genericParam.Name, out var actualType))
            {
                foreach (var constraintName in genericParam.Constraints!)
                {
                    var constraintType = typeAnnotationManager.GetTypeFamily().GetType(constraintName);
                    if (constraintType != null && !actualType.IsCompatibleWith(constraintType))
                    {
                        throw new ArgumentException(
                            $"类型 {actualType.Name} 不满足约束 {constraintName}");
                    }
                }
            }
        }

        // 创建实例化的FuncLangValue（复制所有字段）
        var instantiated = new FuncLangValue(
            id: Id,
            ids: Ids!,
            blockStatement: BlockStatement,
            genericParameters: GenericParameters,
            position: Position,
            isLambda: IsLambda
        );

        // 设置类型参数映射
        instantiated.TypeArgumentMapping = typeArguments;

        // 复制闭包环境（如果有）
        if (CapturedScope != null)
        {
            typeof(FuncLangValue)
                .GetProperty("CapturedScope", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .SetValue(instantiated, CapturedScope);
        }

        return instantiated;
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("FuncLangValue 暂不支持 Visitor 模式访问");
    }
}