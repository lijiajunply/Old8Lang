using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 实例，a(b,c)，支持命名参数
/// </summary>
public partial class Instance : LangValueType
{
    public readonly List<LangExpression> Ids;
    public readonly LangId Id;
    public readonly List<NamedArgument> NamedArgs;

    public Instance(LangId langId, List<LangExpression> ids, SourcePosition position = default)
        : base(position)
    {
        Id = langId;
        Ids = ids;
        NamedArgs = new List<NamedArgument>();
    }

    public Instance(LangId langId, List<LangExpression> ids, List<NamedArgument> namedArgs, SourcePosition position = default)
        : base(position)
    {
        Id = langId;
        Ids = ids;
        NamedArgs = namedArgs;
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 首先尝试通过全局函数注册器执行
        if (TryExecuteGlobalFunction(manager, out var globalFuncResult))
        {
            return globalFuncResult!;
        }

        LangValueType result;

        // 计算总参数数量（位置参数 + 命名参数）
        var totalArgCount = Ids.Count + (NamedArgs?.Count ?? 0);

        // 获取所有可能的匹配函数（函数名和参数数量匹配）
        var matchingFunctions = manager.ImportInfos
            .Where(x => (x is FuncLangValue func && func.Id!.IdName == Id.IdName && func.Ids?.Count == totalArgCount)
                        || (x is AsyncFuncLangValue asyncFunc && asyncFunc.Id!.IdName == Id.IdName &&
                            asyncFunc.Ids?.Count == totalArgCount))
            .ToList();

        if (matchingFunctions.Count > 0)
        {
            // 查找最匹配的函数
            object? bestMatch = null;

            // 如果有多个重载，选择第一个（参数数量已经匹配）
            // 命名参数的类型检查将在FuncLangValue.Run中进行
            if (matchingFunctions.Count == 1)
            {
                bestMatch = matchingFunctions[0];
            }
            else if (matchingFunctions.Count > 1 && Ids.Count > 0)
            {
                // 有多个重载且有位置参数时，进行类型匹配
                // 计算位置参数的实际值和类型
                var paramValues = Ids.Select(t => t.Run(manager)).ToList();

                foreach (var func in matchingFunctions)
                {
                    if (func is FuncLangValue { Ids: not null } funcValue)
                    {
                        bool isMatch = true;
                        bool isExactMatch = true;

                        // 只检查位置参数的类型是否匹配
                        for (int i = 0; i < Ids.Count && i < funcValue.Ids.Count; i++)
                        {
                            var paramType = funcValue.Ids[i].AssumptionType;
                            var argValue = paramValues[i];

                            // 如果参数没有类型注解，视为匹配
                            if (string.IsNullOrEmpty(paramType))
                            {
                                isExactMatch = false;
                                continue;
                            }

                            // 检查参数类型是否匹配
                            string argTypeName = argValue.GetType().Name;
                            if (argTypeName.StartsWith(paramType, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            // 类型不匹配
                            isMatch = false;
                            break;
                        }

                        if (isMatch)
                        {
                            if (isExactMatch)
                            {
                                // 找到精确匹配，直接使用
                                bestMatch = funcValue;
                                break;
                            }
                            else if (bestMatch == null)
                            {
                                // 记录第一个匹配的函数
                                bestMatch = funcValue;
                            }
                        }
                    }
                    else if (func is AsyncFuncLangValue asyncFuncValue)
                    {
                        // 异步函数暂时只检查数量
                        bestMatch = asyncFuncValue;
                        break;
                    }
                }

                // 如果没有找到匹配的，使用第一个
                if (bestMatch == null && matchingFunctions.Count > 0)
                {
                    bestMatch = matchingFunctions[0];
                }
            }
            else
            {
                // 只有命名参数或者只有一个匹配的函数，直接使用第一个
                bestMatch = matchingFunctions[0];
            }

            // 如果找到匹配的函数，调用它
            if (bestMatch != null)
            {
                if (bestMatch is AsyncFuncLangValue asyncFunc)
                {
                    // 先调用 Run() 捕获闭包（可能返回AsyncFuncLangValue或AsyncGeneratorLangValue）
                    var closedFunc = asyncFunc.Run(manager);

                    // 如果是异步生成器，需要设置参数
                    if (closedFunc is AsyncGeneratorLangValue asyncGen)
                    {
                        // 计算参数值
                        var paramValuesCopy = Ids.Select(t => t.Run(manager)).ToList();

                        // 处理默认参数，补全缺失的参数值
                        if (asyncFunc.Ids is { Count: > 0 })
                        {
                            for (var i = paramValuesCopy.Count; i < asyncFunc.Ids.Count; i++)
                            {
                                var id = asyncFunc.Ids[i];
                                if (id.DefaultValue != null)
                                {
                                    var defaultValue = id.DefaultValue.Run(manager);
                                    paramValuesCopy.Add(defaultValue);
                                }
                            }
                        }

                        // 将参数值设置到异步生成器
                        if (asyncFunc.Ids is { Count: > 0 })
                        {
                            for (var i = 0; i < asyncFunc.Ids.Count; i++)
                            {
                                var paramId = asyncFunc.Ids[i];
                                var paramValue = paramValuesCopy[i];
                                asyncGen.SetParameter(paramId.IdName, paramValue);
                            }
                        }

                        result = asyncGen;
                    }
                    else if (closedFunc is AsyncFuncLangValue closedAsyncFunc)
                    {
                        // 如果是异步函数，调用 RunAsync()
                        result = closedAsyncFunc.RunAsync(manager, Ids);
                    }
                    else
                    {
                        // 不应该到达这里
                        result = closedFunc;
                    }
                }
                else if (bestMatch is FuncLangValue funcValue)
                {
                    // 如果是泛型函数且未实例化，尝试自动推断类型参数
                    if (funcValue.IsGeneric && funcValue.TypeArgumentMapping == null)
                    {
                        if (manager.Interpreter == null)
                        {
                            throw new InvalidOperationError(this, "无法执行泛型类型推断：解释器未初始化");
                        }

                        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
                        var inference = new TypeSystem.GenericTypeInference(typeAnnotationManager);
                        var inferredTypes = inference.InferFunctionTypeArguments(funcValue, Ids, manager, Position);

                        if (inferredTypes != null)
                        {
                            // 使用推断出的类型实例化泛型函数
                            var instantiatedFunc = funcValue.InstantiateGeneric(inferredTypes, typeAnnotationManager);
                            result = instantiatedFunc.Run(manager, Ids, NamedArgs, Position);
                        }
                        else
                        {
                            // 无法推断类型，抛出错误
                            throw new InvalidOperationError(this,
                                $"无法推断泛型函数 '{funcValue.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{funcValue.Id?.IdName}<类型>(...)");
                        }
                    }
                    else
                    {
                        // 找到匹配的重载函数，直接调用
                        result = funcValue.Run(manager, Ids, NamedArgs, Position);
                    }
                }
                else
                {
                    // 其他类型的 ImportInfo，使用单参数 Run
                    result = ((ImportInfo)bestMatch).Run(manager);
                }
            }
            else
            {
                // 没有找到匹配的函数，使用原来的方式查找
                var idResult = Id.Run(manager);
                result = idResult;

                // 后续处理...
                if (idResult is TypeTemplate typeTemplate)
                {
                    // 创建类的实例（使用 V2 架构）
                    var instance = typeTemplate.CreateInstanceV2(manager);

                    // 初始化实例，设置Interpreter
                    instance.Init(manager.Interpreter);

                    // 调用 init 构造函数
                    instance.CallInit(Ids, manager);

                    result = instance;
                }
                // 如果idResult是FuncLangValue，则调用它
                else if (idResult is FuncLangValue funcValue)
                {
                    // 如果是泛型函数且未实例化，尝试自动推断类型参数
                    if (funcValue.IsGeneric && funcValue.TypeArgumentMapping == null)
                    {
                        if (manager.Interpreter == null)
                        {
                            throw new InvalidOperationError(this, "无法执行泛型类型推断：解释器未初始化");
                        }

                        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
                        var inference = new TypeSystem.GenericTypeInference(typeAnnotationManager);
                        var inferredTypes = inference.InferFunctionTypeArguments(funcValue, Ids, manager, Position);

                        if (inferredTypes != null)
                        {
                            // 使用推断出的类型实例化泛型函数
                            var instantiatedFunc = funcValue.InstantiateGeneric(inferredTypes, typeAnnotationManager);
                            result = instantiatedFunc.Run(manager, Ids, NamedArgs, Position);
                        }
                        else
                        {
                            // 无法推断类型，抛出错误
                            throw new InvalidOperationError(this,
                                $"无法推断泛型函数 '{funcValue.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{funcValue.Id?.IdName}<类型>(...)");
                        }
                    }
                    else
                    {
                        // 直接调用函数，参数表达式会在函数体内执行
                        result = funcValue.Run(manager, Ids, NamedArgs, Position);
                    }
                }
                // 如果idResult是AsyncFuncLangValue，则调用它
                else if (idResult is AsyncFuncLangValue asyncFuncValue)
                {
                    // 先调用 Run() 捕获闭包，然后调用返回的副本的 RunAsync()
                    var closedAsyncFunc = (AsyncFuncLangValue)asyncFuncValue.Run(manager);
                    result = closedAsyncFunc.RunAsync(manager, Ids);
                }
                // 如果idResult是TaskStaticMethodWrapper，则调用它
                else if (idResult is TaskStaticMethodWrapper taskMethodWrapper)
                {
                    // 执行静态方法
                    var args = Ids.Select(id => id.Run(manager)).ToList();
                    result = taskMethodWrapper.Invoke(args, Position);
                }
                // 如果idResult是ThreadStaticMethodWrapper，则调用它
                else if (idResult is ThreadStaticMethodWrapper threadMethodWrapper)
                {
                    // 执行静态方法
                    var args = Ids.Select(id => id.Run(manager)).ToList();
                    result = threadMethodWrapper.Invoke(args, Position);
                }
            }
        }
        else
        {
            // 如果没有找到重载函数，使用原来的方式查找
            var idResult = Id.Run(manager);
            result = idResult;

            // 如果idResult是TypeTemplate，则创建其实例
            if (idResult is TypeTemplate typeTemplate)
            {
                // 创建类的实例（使用 V2 架构）
                var instance = typeTemplate.CreateInstanceV2(manager);

                // 初始化实例，设置Interpreter
                instance.Init(manager.Interpreter);

                // 调用 init 构造函数
                instance.CallInit(Ids, manager);

                result = instance;
            }
            // 如果idResult是FuncLangValue，则调用它
            else if (idResult is FuncLangValue funcValue)
            {
                // 如果是泛型函数且未实例化,尝试自动推断类型参数
                if (funcValue.IsGeneric && funcValue.TypeArgumentMapping == null)
                {
                    if (manager.Interpreter == null)
                    {
                        throw new InvalidOperationError(this, "无法执行泛型类型推断：解释器未初始化");
                    }

                    var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
                    var inference = new TypeSystem.GenericTypeInference(typeAnnotationManager);
                    var inferredTypes = inference.InferFunctionTypeArguments(funcValue, Ids, manager, Position);

                    if (inferredTypes != null)
                    {
                        // 使用推断出的类型实例化泛型函数
                        var instantiatedFunc = funcValue.InstantiateGeneric(inferredTypes, typeAnnotationManager);
                        result = instantiatedFunc.Run(manager, Ids, NamedArgs, Position);
                    }
                    else
                    {
                        // 无法推断类型，抛出错误
                        throw new InvalidOperationError(this,
                            $"无法推断泛型函数 '{funcValue.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{funcValue.Id?.IdName}<类型>(...)");
                    }
                }
                else
                {
                    // 直接调用函数，参数表达式会在函数体内执行
                    result = funcValue.Run(manager, Ids, NamedArgs, Position);
                }
            }
            // 如果idResult是NativeDelegateFuncLangValue，则调用它
            else if (idResult is NativeDelegateFuncLangValue nativeDelegate)
            {
                result = nativeDelegate.Run(manager, Ids);
            }
            // 如果idResult是AsyncFuncLangValue，则调用它
            else if (idResult is AsyncFuncLangValue asyncFuncValue)
            {
                // 先调用 Run() 捕获闭包，然后调用返回的副本的 RunAsync()
                var closedAsyncFunc = (AsyncFuncLangValue)asyncFuncValue.Run(manager);
                result = closedAsyncFunc.RunAsync(manager, Ids);
            }
            // 如果idResult是TaskStaticMethodWrapper，则调用它
            else if (idResult is TaskStaticMethodWrapper taskMethodWrapper)
            {
                // 执行静态方法
                var args = Ids.Select(id => id.Run(manager)).ToList();
                result = taskMethodWrapper.Invoke(args, Position);
            }
            // 如果idResult是ThreadStaticMethodWrapper，则调用它
            else if (idResult is ThreadStaticMethodWrapper threadMethodWrapper)
            {
                // 执行静态方法
                var args = Ids.Select(id => id.Run(manager)).ToList();
                result = threadMethodWrapper.Invoke(args, Position);
            }
        }

        // 注意：TypeTemplate.CreateInstanceV2 路径已经处理了构造函数调用
        // 移除了这里的重复 init 调用逻辑，避免构造函数被调用两次

        if (result is NativeAnyLangValue nativeAnyValue)
        {
            List<LangValueType> a = [];
            a.AddRange(Ids.Select(id => id.Run(manager)));
            nativeAnyValue.New([.. Apis.ListToObjects(a)]);
            result = nativeAnyValue;
        }

        return result;
    }

    public LangValueType FromClassToResult(LangValueType baseLangValue, VariateManager? manager = null)
    {
        var type = baseLangValue.GetType();
        MethodInfo? m = null;

        // 设置执行上下文，以便扩展方法可以访问当前的 VariateManager
        if (manager != null)
        {
            ValueFunctions.ExecutionContext.SetCurrentManager(manager);
        }

        // 对于具有扩展方法的类型，优先查找扩展方法而不是实例方法
        // 这样可以避免找到 TaskLangValue.Then 实例方法而不是扩展方法
        if (baseLangValue is DictionaryLangValue or ListLangValue or TaskLangValue or ThreadLangValue or StringLangValue
            or TupleLangValue or ArrayLangValue or CharLangValue)
        {
            type = baseLangValue switch
            {
                DictionaryLangValue => typeof(DictionaryValueFuncStatic),
                ListLangValue => typeof(ListValueFuncStatic),
                TaskLangValue => typeof(TaskValueFuncStatic),
                ThreadLangValue => typeof(ThreadValueFuncStatic),
                StringLangValue => typeof(StringValueFuncStatic),
                TupleLangValue => typeof(TupleValueFuncStatic),
                ArrayLangValue => typeof(ArrayValueFuncStatic),
                CharLangValue => typeof(CharValueFuncStatic),
                _ => null
            };

            // 根据参数数量查找正确的重载
            var allMethods = type?.GetMethods().Where(x => x.Name == Id.IdName).ToArray();
            if (allMethods is { Length: > 0 })
            {
                // 预期参数数量 = 传入参数数量 + 1 (扩展方法的第一个参数是baseLangValue)
                var expectedParamCount = Ids.Count + 1;

                // 首先查找精确匹配的参数数量
                m = allMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount);

                // 如果没找到，查找有可选参数的方法
                if (m == null)
                {
                    m = allMethods.FirstOrDefault(x =>
                    {
                        var parameters = x.GetParameters();
                        if (parameters.Length < expectedParamCount) return false;

                        // 检查除了第一个参数（baseLangValue）之外，剩余的参数是否都是可选的
                        for (int i = expectedParamCount; i < parameters.Length; i++)
                        {
                            if (!parameters[i].IsOptional && !parameters[i].HasDefaultValue)
                                return false;
                        }

                        return true;
                    });
                }

                // 如果还是没找到，使用第一个方法
                m ??= allMethods[0];
            }
        }

        // 如果没有找到扩展方法，尝试在类型本身上查找
        if (m == null)
        {
            type = baseLangValue.GetType();
            // 根据参数数量查找正确的重载
            var allInstanceMethods = type.GetMethods().Where(x => x.Name == Id.IdName).ToArray();
            if (allInstanceMethods is { Length: > 0 })
            {
                // 对于实例方法，预期参数数量 = 传入参数数量
                var expectedParamCount = Ids.Count;
                m = allInstanceMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount) ??
                    allInstanceMethods[0];
            }
        }

        // 如果还是没找到，尝试 ValueTypeFuncStatic
        if (m == null)
        {
            type = Type.GetType("Old8Lang.AST.Expression.ValueFunctions.ValueTypeFuncStatic");
            m = type?.GetMethod(Id.IdName);
        }

        // 如果找不到方法，抛出异常
        if (m == null)
        {
            throw new AttributeError(baseLangValue, Id.IdName, baseLangValue.GetType().Name);
        }

        var os = new List<object>();

        // 检查方法是否需要参数
        var parameters = m?.GetParameters() ?? Array.Empty<ParameterInfo>();

        // 对于静态方法（扩展方法），第一个参数是 baseLangValue
        if (m?.IsStatic == true && parameters.Length > 0)
        {
            os.Add(baseLangValue);
        }

        // 只添加与方法参数数量匹配的参数
        for (int i = 0; i < Ids.Count && os.Count < parameters.Length; i++)
        {
            // 对于实例方法，第一个参数已经是实例本身，所以跳过
            if (m?.IsStatic == false && i == 0 && parameters.Length > 0)
            {
                continue;
            }

            // 运行表达式获取参数值
            // 如果参数已经是 LangValueType，则直接使用；否则调用 Run
            // 对于 TaskLangValue 和 ThreadLangValue，优先使用 ExternalManager
            LangValueType argValue;
            if (Ids[i] is LangValueType langValue)
            {
                argValue = langValue;
            }
            else if (baseLangValue is TaskLangValue { ExternalManager: not null } taskLangValue)
            {
                argValue = Ids[i].Run(taskLangValue.ExternalManager);
            }
            else if (baseLangValue is ThreadLangValue { ExternalManager: not null } threadLangValue)
            {
                argValue = Ids[i].Run(threadLangValue.ExternalManager);
            }
            else if (baseLangValue is FuncLangValue funcLangValue)
            {
                argValue = funcLangValue;
            }
            else
            {
                // 使用传入的 manager 参数来运行表达式，而不是 null
                argValue = Ids[i].Run(manager ?? new VariateManager());
            }

            os.Add(argValue);
        }

        // 补充缺失的参数，包括可选参数的默认值和 SourcePosition 参数
        if (os.Count < parameters.Length)
        {
            for (int i = os.Count; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(SourcePosition))
                {
                    // 使用 Instance 的位置信息
                    os.Add(Position);
                }
                else if (parameters[i].IsOptional || parameters[i].HasDefaultValue)
                {
                    // 使用可选参数的默认值
                    os.Add(parameters[i].DefaultValue!);
                }
            }
        }

        // 对于静态方法，实例参数为 null；对于实例方法，实例参数为 baseLangValue
        object? invokeInstance = m?.IsStatic == false ? baseLangValue : null;

        try
        {
            var r = m?.Invoke(invokeInstance, [.. os]);
            if (r is LangValueType v) return v;
            return ObjToValue(r!);
        }
        finally
        {
            // 清理执行上下文
            ValueFunctions.ExecutionContext.ClearCurrentManager();
        }
    }

    public override string ToString()
    {
        return $"{Id}({string.Join(", ", Ids)})";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 首先尝试通过全局函数注册器生成 IL 代码
        if (TryGenerateGlobalFunctionIl(ilGenerator, local))
        {
            return;
        }

        // 查找匹配的方法
        MethodInfo? matchingMethod = null;
        List<LangId>? funcParams = null;

        // 首先，尝试通过实际参数类型构建键进行精确匹配
        var actualParamTypes = Ids.Select(id => id.OutputType(local)).ToArray();
        var actualParamTypeNames = string.Join("_", actualParamTypes.Select(t => t?.Name ?? "object"));
        var exactDelegateKey = $"{Id.IdName}${actualParamTypeNames}";

        if (local.DelegateVar.TryGetValue(exactDelegateKey, out var exactResult))
        {
            matchingMethod = exactResult;
            local.FuncParameters.TryGetValue(exactDelegateKey, out funcParams);
        }
        else
        {
            // 如果精确匹配失败，尝试查找参数数量匹配的方法（支持默认参数）
            // 对于带默认参数的情况，从实际参数数量开始，逐步增加参数数量尝试匹配
            for (int paramCount = Ids.Count; matchingMethod == null && paramCount <= Ids.Count + 10; paramCount++)
            {
                // 遍历所有委托变量，查找参数数量匹配的方法
                foreach (var (key, result) in local.DelegateVar)
                {
                    if (!key.StartsWith($"{Id.IdName}$"))
                        continue;

                    // 获取方法参数信息
                    var methodParams = result.GetParameters();

                    // 检查参数数量是否匹配
                    if (methodParams.Length == paramCount)
                    {
                        // 获取函数的参数列表信息
                        local.FuncParameters.TryGetValue(key, out funcParams);

                        // 检查是否有默认参数可以补充
                        if (funcParams != null && Ids.Count <= methodParams.Length)
                        {
                            // 计算必需参数的数量（没有默认值的参数）
                            int requiredParamsCount = funcParams.Count(t => t.DefaultValue == null);

                            // 如果实际参数数量大于等于必需参数数量，则可以匹配
                            if (Ids.Count >= requiredParamsCount)
                            {
                                matchingMethod = result;
                                break;
                            }
                        }
                        else if (methodParams.Length == Ids.Count)
                        {
                            // 完全匹配
                            matchingMethod = result;
                            break;
                        }
                    }
                }
            }
        }

        if (matchingMethod == null)
        {
            var classType = local.ClassVar.GetValueOrDefault(Id.IdName);
            if (classType == null)
            {
                // 如果找不到类类型，可能是因为类还在编译中
                // 检查是否有对应的TypeBuilder
                if (local.InClassEnv?.Name == Id.IdName && local.InClassEnv is TypeBuilder builder)
                {
                    classType = builder;
                }
                else
                {
                    // 在当前编译上下文中查找未完成的类型
                    foreach (var kv in local.ClassVar)
                    {
                        if (kv.Key == Id.IdName && kv.Value is TypeBuilder)
                        {
                            classType = kv.Value;
                            break;
                        }
                    }

                    if (classType == null)
                    {
                        // 创建一个临时的object类型引用，允许编译继续进行
                        // 这是一个临时解决方案，编译器模式下类前向引用的处理需要改进
                        classType = typeof(object);

                        // 生成临时的null对象，实际运行时将通过解释器处理
                        ilGenerator.Emit(OpCodes.Ldnull);
                        return;
                    }
                }
            }

            // 获取默认构造函数
            var constructorInfo = classType.GetConstructor(Type.EmptyTypes);
            if (constructorInfo != null)
            {
                ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
            }

            var localA = ilGenerator.DeclareLocal(classType);
            ilGenerator.Emit(OpCodes.Stloc, localA.LocalIndex);

            // 使用BindingFlags.DeclaredOnly来只查找当前类声明的方法，避免与继承的方法冲突
            var initFunc = classType.GetMethod("init",
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
            if (initFunc != null)
            {
                // 加载 this 指针
                ilGenerator.Emit(OpCodes.Ldloc, localA.LocalIndex);

                // 加载参数
                var a = initFunc.GetParameters();
                for (var i = 0; i < Ids.Count; i++)
                {
                    var id = Ids[i];
                    id.LoadIlValue(ilGenerator, local);
                    var idType = id.OutputType(local);
                    if (a[i].ParameterType == typeof(object) && idType!.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }
                }

                // 调用 init 方法（实例方法使用 Callvirt）
                ilGenerator.Emit(OpCodes.Callvirt, initFunc);
            }

            // 加载对象实例作为返回值
            ilGenerator.Emit(OpCodes.Ldloc, localA.LocalIndex);

            return;
        }

        // 处理所有类型的方法调用，包括DynamicMethod和MethodBuilder
        var matchingParams = matchingMethod.GetParameters();

        // 检查是否有 params 参数
        int paramsIndex = -1;
        if (funcParams != null)
        {
            for (int i = 0; i < funcParams.Count; i++)
            {
                if (funcParams[i].IsParams)
                {
                    paramsIndex = i;
                    break;
                }
            }
        }

        // 如果有 params 参数，需要特殊处理
        if (paramsIndex >= 0)
        {
            // 加载 params 之前的普通参数
            for (var i = 0; i < paramsIndex; i++)
            {
                if (i < Ids.Count)
                {
                    var id = Ids[i];
                    id.LoadIlValue(ilGenerator, local);

                    // 参数类型匹配和转换
                    if (i < matchingParams.Length)
                    {
                        var paramType = matchingParams[i].ParameterType;
                        var idType = id.OutputType(local);
                        LoadParameterWithConversion(ilGenerator, local, id, idType, paramType);
                    }
                }
            }

            // 处理 params 参数：创建数组
            var paramsParam = matchingParams[paramsIndex];
            var arrayElementType = paramsParam.ParameterType.GetElementType()!;
            var paramsArgsCount = Math.Max(0, Ids.Count - paramsIndex);

            // 创建数组：ldc.i4 <count>; newarr <elementType>
            ilGenerator.Emit(OpCodes.Ldc_I4, paramsArgsCount);
            ilGenerator.Emit(OpCodes.Newarr, arrayElementType);

            // 填充数组元素
            for (var i = 0; i < paramsArgsCount; i++)
            {
                var argIndex = paramsIndex + i;
                var id = Ids[argIndex];

                // dup 数组引用
                ilGenerator.Emit(OpCodes.Dup);
                // 加载索引
                ilGenerator.Emit(OpCodes.Ldc_I4, i);
                // 加载参数值
                id.LoadIlValue(ilGenerator, local);

                // 类型转换
                var idType = id.OutputType(local);
                if (idType != null && arrayElementType != idType)
                {
                    if (arrayElementType == typeof(object) && idType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }
                    else if (arrayElementType == typeof(int) && idType == typeof(double))
                    {
                        ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
                    }
                    else if (arrayElementType == typeof(double) && idType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }
                }

                // 存储到数组：stelem <elementType>
                if (arrayElementType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }
                else if (arrayElementType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Stelem_I4);
                }
                else if (arrayElementType == typeof(double))
                {
                    ilGenerator.Emit(OpCodes.Stelem_R8);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Stelem, arrayElementType);
                }
            }
            // 此时栈顶是填充好的数组，作为 params 参数传递
        }
        else
        {
            // 没有 params 参数，使用原有逻辑
            // 加载实际传递的参数
            for (var i = 0; i < Ids.Count; i++)
            {
                var id = Ids[i];
                id.LoadIlValue(ilGenerator, local);

                // 确保参数类型匹配
                if (i < matchingParams.Length)
                {
                    var paramType = matchingParams[i].ParameterType;
                    var idType = id.OutputType(local);
                    LoadParameterWithConversion(ilGenerator, local, id, idType, paramType);
                }
            }
        }

        // 如果有默认参数需要补充
        if (funcParams != null && Ids.Count < matchingParams.Length)
        {
            // 补充默认参数值
            for (var i = Ids.Count; i < funcParams.Count; i++)
            {
                var param = funcParams[i];
                if (param.DefaultValue != null)
                {
                    // 加载默认参数值
                    param.DefaultValue.LoadIlValue(ilGenerator, local);

                    // 确保类型匹配
                    var paramType = matchingParams[i].ParameterType;
                    var defaultType = param.DefaultValue.OutputType(local);

                    // 使用LocalManager的ValidateType方法验证默认参数类型
                    if (defaultType != null)
                    {
                        local.ValidateType(paramType, defaultType, param.Position);
                    }

                    // 处理必要的类型转换
                    if (defaultType != null && paramType != defaultType)
                    {
                        if (paramType == typeof(object) && defaultType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, defaultType);
                        }
                        else if (paramType == typeof(int) && defaultType == typeof(double))
                        {
                            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
                        }
                        else if (paramType == typeof(double) && defaultType == typeof(int))
                        {
                            ilGenerator.Emit(OpCodes.Conv_R8);
                        }
                    }
                }
            }
        }

        // 调用方法：根据方法类型采用不同的调用方式
        if (matchingMethod is DynamicMethod dynamicMethod)
        {
            // DynamicMethod不能直接通过Call指令调用，需要通过委托调用
            // 对于DynamicMethod，我们需要确保栈上有正确数量的参数
            // 注意：参数已经在前面加载过了，包括补充的默认参数

            // 直接调用DynamicMethod的Invoke方法会导致栈不平衡
            // 因此，我们需要使用一种不同的方式来调用DynamicMethod

            // 简化实现：直接使用Call指令调用DynamicMethod
            // 这在某些情况下可能会失败，但在大多数情况下应该可以工作
            ilGenerator.Emit(OpCodes.Call, dynamicMethod);
        }
        else
        {
            // 对于普通方法，使用Call指令直接调用
            ilGenerator.Emit(OpCodes.Call, matchingMethod);
        }
    }

    public override Type OutputType(LocalManager local)
    {
        // 首先尝试通过全局函数注册器获取返回类型
        var globalFuncReturnType = TryGetGlobalFunctionReturnType(local);
        if (globalFuncReturnType != null)
        {
            return globalFuncReturnType;
        }

        // 尝试使用函数名+参数类型查找（支持重载）
        // 先尝试精确匹配参数类型
        var actualParamTypes = Ids.Select(id => id.OutputType(local)).ToArray();
        var actualParamTypeNames = string.Join("_", actualParamTypes.Select(t => t?.Name ?? "object"));
        var exactDelegateKey = $"{Id.IdName}${actualParamTypeNames}";
        var result = local.DelegateVar.GetValueOrDefault(exactDelegateKey);

        // 如果找不到，可能是带默认参数的函数，尝试查找参数数量匹配的方法
        if (result == null)
        {
            // 遍历所有委托变量，查找参数数量匹配的方法
            foreach (var (key, method) in local.DelegateVar)
            {
                if (!key.StartsWith($"{Id.IdName}$"))
                    continue;

                // 获取方法参数信息
                var methodParams = method.GetParameters();

                // 检查参数数量是否匹配
                if (methodParams.Length >= Ids.Count)
                {
                    // 获取函数的参数列表信息
                    if (local.FuncParameters.TryGetValue(key, out var funcParams))
                    {
                        // 计算必需参数的数量（没有默认值的参数）
                        int requiredParamsCount = funcParams.Count(t => t.DefaultValue == null);

                        // 如果实际参数数量大于等于必需参数数量，则可以匹配
                        if (Ids.Count >= requiredParamsCount)
                        {
                            result = method;
                            break;
                        }
                    }
                    else if (methodParams.Length == Ids.Count)
                    {
                        // 完全匹配
                        result = method;
                        break;
                    }
                }
            }
        }

        if (result != null) return result.ReturnType;

        var classType = local.ClassVar.GetValueOrDefault(Id.IdName);
        if (classType == null)
        {
            // 如果找不到类类型，尝试从ClassVar中查找TypeBuilder
            foreach (var kv in local.ClassVar)
            {
                if (kv.Key == Id.IdName)
                {
                    classType = kv.Value;
                    break;
                }
            }
        }

        return classType ?? typeof(object);
    }

    /// <summary>
    /// 加载参数并进行必要的类型转换
    /// </summary>
    private void LoadParameterWithConversion(ILGenerator ilGenerator, LocalManager local, LangExpression id, Type? idType, Type paramType)
    {
        // 使用LocalManager的ValidateType方法验证参数类型
        if (idType != null)
        {
            // 执行类型验证，确保参数类型与方法期望的类型兼容
            local.ValidateType(paramType, idType, id.Position);
        }

        // 处理必要的类型转换
        if (idType != null && paramType != idType)
        {
            if (paramType == typeof(object) && idType.IsValueType)
            {
                // 从值类型转换为object，需要装箱
                ilGenerator.Emit(OpCodes.Box, idType);
            }
            else if (paramType == typeof(int) && idType == typeof(object))
            {
                // 从object转换为int
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            }
            else if (paramType == typeof(double) && idType == typeof(object))
            {
                // 从object转换为double
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(double));
            }
            else if (paramType == typeof(int) && idType == typeof(double))
            {
                // 从double转换为int
                ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
            }
            else if (paramType == typeof(double) && idType == typeof(int))
            {
                // 从int转换为double
                ilGenerator.Emit(OpCodes.Conv_R8);
            }
            else if (paramType == typeof(string) && idType != typeof(string))
            {
                // 从其他类型转换为string
                ilGenerator.Emit(OpCodes.Call, idType.GetMethod("ToString", Type.EmptyTypes)!);
            }
        }
    }
}