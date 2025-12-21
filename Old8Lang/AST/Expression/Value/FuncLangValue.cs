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

    public FuncLangValue(LangId? id, List<LangId> ids, BlockStatement blockStatement,
        SourcePosition position = default,
        bool isLambda = false) :
        base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
        IsLambda = isLambda;
    }

    public FuncLangValue(string idName, MethodInfo methodInfo, FuncLangValue? func = null,
        SourcePosition position = default) : base(position)
    {
        Id = new LangId(idName);
        Method = methodInfo;
        Func = func;
        IsLambda = false; // 原生方法不是Lambda表达式
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
                    var generatorClosure = new FuncLangValue(Id, Ids, BlockStatement, Position, IsLambda)
                    {
                        // 生成器需要独立的作用域副本，使用深拷贝
                        CapturedScope = manager.Clone()
                    };

                    return new GeneratorLangValue(generatorClosure, Position);
                }

                // 没有参数的生成器函数
                var noParamClosure = new FuncLangValue(Id, Ids, BlockStatement, Position, IsLambda)
                {
                    // 生成器需要独立的作用域副本，使用深拷贝
                    CapturedScope = manager.Clone()
                };

                return new GeneratorLangValue(noParamClosure, Position);
            }

            var closureFunc = new FuncLangValue(Id, Ids, BlockStatement, Position, IsLambda)
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


    public LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> ids, object? obj = null)
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
                invoke = Method?.Invoke(obj, finalParams);
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

            // 只检查最大参数数量，允许实际参数少于期望参数（如果有默认参数）
            if (actualParams > expectedParams)
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
        try
        {
            // 入栈：记录函数调用
            Old8Exception.PushCallStack(Id?.IdName ?? "anonymous", Position);

            // 如果有捕获的作用域（闭包），使用捕获的作用域而不是调用时的作用域
            // 这样函数体就能访问定义时的外部变量
            VariateManager executionManager;
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

            executionManager.IsFunc = true; // 设置为函数上下文
            executionManager.CurrentFunctionReturnType = Id?.AssumptionType; // 设置当前函数的返回类型注解（Id.AssumptionType包含了返回类型）

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

            // 移除子作用域，但是要注意，在init方法中使用this关键字设置的值已经被保存到实例中了
            // 所以这里移除子作用域不会影响实例的状态
            executionManager.RemoveChildren();

            return result;
        }
        finally
        {
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
            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
            var delegateKey = $"{methodName}${paramTypeNames}";
            local.DelegateVar.TryAdd(delegateKey, dynamicMethod);

            // 同时存储函数的参数列表信息，用于支持默认参数
            if (Ids != null)
            {
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
    private void ValidateParameterTypes(List<LangExpression> argumentExpressions, List<LangValueType> argumentValues)
    {
        if (Ids == null) return;

        // 使用全局类型检查器进行验证
        TypeChecker.ValidateParameterTypes(
            argumentExpressions.Cast<IOldLangTree>().ToList(),
            argumentValues,
            Ids);
    }

    /// <summary>
    /// 统一的参数处理方法：计算、验证、处理默认值，并返回最终参数值列表
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

        // 2. 验证参数类型匹配（仅在有类型注解时进行检查）
        ValidateParameterTypes(argumentExpressions, paramValues);

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
}