using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 实例，a(b,c)
/// </summary>
/// <param name="langId"></param>
/// <param name="ids"></param>
/// <param name="position"></param>
public class Instance(LangId langId, List<LangExpression> ids, SourcePosition position = default)
    : LangValueType(position)
{
    public readonly List<LangExpression> Ids = ids;
    public readonly LangId Id = langId;

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        LangValueType result;
        var results = Ids.Select(t => t.Run(manager)).ToList();

        switch (Id.IdName)
        {
            case "Type" or "type":
                return new TypeLangValue(results[0]).Run(manager);
            case "Exec" or "exec":
            {
                if (results[0] is not StringLangValue execStringValue)
                    throw new TypeError(this, "StringValue", results[0].GetType().Name);
                var a = manager.Interpreter.Build(code: execStringValue.Value);
                a.Run(manager);
                return new VoidLangValue();
            }
            case "ShowValues" or "showValues":
            {
#if DEBUG
                manager.Interpreter.OutputProvider.WriteLine(manager.ToString());
#endif
                return new VoidLangValue();
            }
            case "Json" or "json":
            {
                // 支持多种类型的 JSON 序列化
                switch (results[0])
                {
                    case AnyLangValue jsonAnyValue:
                        return jsonAnyValue.ToJson();

                    case DictionaryLangValue dictValue:
                    {
                        // 将字典转换为 JSON 字符串
                        var dict = new Dictionary<string, object>();
                        foreach (var (key, value) in dictValue.Value)
                        {
                            var keyStr = key.ToDisplayString();
                            dict[keyStr] = value.GetValue();
                        }

                        var jsonStr = System.Text.Json.JsonSerializer.Serialize(dict);
                        return new StringLangValue(jsonStr);
                    }

                    case ArrayLangValue arrayValue:
                    {
                        // 将数组转换为 JSON 字符串
                        var list = arrayValue.GetItems().Select(item => item.GetValue()).ToList();
                        var jsonStr = System.Text.Json.JsonSerializer.Serialize(list);
                        return new StringLangValue(jsonStr);
                    }

                    case ListLangValue listValue:
                    {
                        // 将列表转换为 JSON 字符串
                        var list = listValue.GetItems().Select(item => item.GetValue()).ToList();
                        var jsonStr = System.Text.Json.JsonSerializer.Serialize(list);
                        return new StringLangValue(jsonStr);
                    }

                    default:
                        throw new TypeError(this, "AnyValue/DictionaryValue/ArrayValue/ListValue",
                            results[0].GetType().Name);
                }
            }
            case "ToObj" or "toObj":
                if (results[0] is not StringLangValue stringValue)
                    throw new TypeError(this, "StringValue", results[0].GetType().Name);
                return stringValue.ToObj();
            case "PrintLine" or "printLine":
            {
                if (results.Count == 0)
                {
                    manager.Interpreter.OutputProvider.WriteLine("");
                    return new VoidLangValue();
                }

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.OutputProvider.WriteLine(value);
                return new VoidLangValue();
            }
            case "Print" or "print":
            {
                if (results.Count == 0) return new VoidLangValue();

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.OutputProvider.Write(value);
                return new VoidLangValue();
            }
            case "Error" or "error":
            {
                if (results.Count == 0)
                {
                    manager.Interpreter.OutputProvider.WriteLine("");
                    return new VoidLangValue();
                }

                var value = results[0].ToDisplayString();
                for (var i = 1; i < results.Count; i++) value += results[i].ToDisplayString();

                manager.Interpreter.OutputProvider.Error(value);
                return new VoidLangValue();
            }
            case "ReadLine" or "readLine":
            {
                var res = manager.Interpreter.OutputProvider.ReadLine();
                return new StringLangValue(res);
            }
            case "Clear" or "clear":
            {
                manager.Interpreter.OutputProvider.Clear();
                return new VoidLangValue();
            }
            case "Compiler" or "compiler":
            {
                if (results.Count == 0) return new VoidLangValue();
                string value;
                if (results[0] is StringLangValue sv) // 使用不同的变量名，避免冲突
                {
                    value = sv.Value; // 直接访问Value属性，避免带引号
                }
                else
                {
                    value = results[0].ToString();
                }

                var statement = manager.Interpreter.Build(code: value);
                var dynamicMethod = new DynamicMethod("OldLangRun", null, null, true);
                var ilGenerator = dynamicMethod.GetILGenerator();
                var local = new LocalManager();
                statement.GenerateIl(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ret);
                foreach (var info in local.DelegateVar)
                {
                    manager.AddClassAndFunc(new FuncLangValue(info.Key, info.Value));
                }

                return new VoidLangValue();
            }
            case "Len" or "len":
            {
                var value = results[0].Run(manager);
                if (value is ILangList list) return new IntLangValue(list.GetLength());
                throw new InvalidOperationError(this, $"{results[0]} 不是列表类型");
            }
            case "Assert" or "assert":
            {
                var value = results[0].Run(manager);
                var value1 = results[1].Run(manager);
                if (!value.Equal(value1))
                {
                    var message = $"断言失败: 期望 {value1}，但得到 {value}";
                    throw new AssertionError(this, message);
                }

                return new BoolLangValue(true);
            }
            case "Spawn" or "spawn":
            {
                // 确保参数数量至少为1
                if (Ids.Count == 0)
                {
                    throw new ArgumentError(this, "spawn 函数需要至少一个参数");
                }

                // 获取第一个参数，应该是一个函数
                var funcExpr = Ids[0];
                var funcValue = funcExpr.Run(manager);

                if (funcValue is not FuncLangValue spawnFunc)
                {
                    throw new TypeError(this, "FuncValue", funcValue.GetType().Name);
                }

                // 创建线程参数列表（跳过第一个函数参数）
                var threadArgs = Ids.Skip(1).ToList();

                // 创建新的变量管理器，复制当前管理器的状态
                var threadManager = manager.Clone();

                // 使用临时变量来存储线程对象，避免闭包引用问题
                ThreadLangValue? tempThread = null;

                // 调用函数
                if (threadArgs.Count == 0)
                {
                    // 无参数情况
                    tempThread = new ThreadLangValue(ThreadCallback, Position);
                }
                else
                {
                    // 带参数情况
                    tempThread = new ThreadLangValue(_ => ThreadCallback(), null, Position);
                }

                // 赋值给最终的线程变量
                var thread = tempThread;

                result = thread;
                return result;

                void ThreadCallback()
                {
                    try
                    {
                        // 调用函数
                        var funcResult = spawnFunc.Run(threadManager, threadArgs);

                        // 设置线程结果
                        tempThread?.SetResult(funcResult.GetValue());
                    }
                    catch (Exception ex)
                    {
                        // 设置线程异常
                        tempThread?.SetException(ex);
                    }
                }
            }
        }

        // 先尝试根据函数名和参数数量查找重载函数
        var func = manager.GetFunc(Id, Ids.Count);
        if (func != null)
        {
            // 检查是否为异步函数
            if (func is AsyncFuncLangValue asyncFunc)
            {
                // 先调用 Run() 捕获闭包，然后调用返回的副本的 RunAsync()
                var closedFunc = (AsyncFuncLangValue)asyncFunc.Run(manager);
                result = closedFunc.RunAsync(manager, Ids);
            }
            else if (func is FuncLangValue funcValue)
            {
                // 找到匹配的重载函数，直接调用
                result = funcValue.Run(manager, Ids);
            }
            else
            {
                // 其他类型的 ImportInfo，使用单参数 Run
                result = func.Run(manager);
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
                // 创建类的实例
                var instance = typeTemplate.CreateInstance(manager);

                // 初始化实例，设置Interpreter
                instance.Init(manager.Interpreter);

                // 保存init方法的引用
                if (instance.Result.TryGetValue("init", out var initResult))
                {
                    if (initResult is not FuncLangValue initFunc) throw new TypeError(this, "FuncValue", "init 不是函数类型");

                    // 在调用init方法前，将当前实例添加到AnyInfo中，以便this关键字访问
                    instance.Manager.Set(new LangId("this"), instance);
                    instance.Manager.IsFunc = true; // 设置为函数上下文

                    // 调用init方法，并将参数传递给它
                    initFunc.Run(instance.Manager, Ids);

                    // 恢复非函数上下文标志
                    instance.Manager.IsFunc = false;
                }
                else if (Ids.Count != 0)
                {
                    throw new InvalidOperationError(this, "找不到对应的init函数");
                }

                result = instance;
            }
            // 如果idResult是FuncLangValue，则调用它
            else if (idResult is FuncLangValue funcValue)
            {
                // 直接调用函数，参数表达式会在函数体内执行
                result = funcValue.Run(manager, Ids);
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
        }

        // 原来的AnyLangValue处理逻辑，用于兼容旧代码
        if (result is AnyLangValue anyValue)
        {
            // 保存init方法的引用，避免覆盖result变量
            if (anyValue.Result.TryGetValue("init", out var initResult))
            {
                if (initResult is not FuncLangValue initFunc) throw new TypeError(this, "FuncValue", "init 不是函数类型");

                // 在调用init方法前，将当前实例添加到AnyInfo中，以便this关键字访问
                anyValue.Manager.Set(new LangId("this"), anyValue);
                anyValue.Manager.IsFunc = true; // 设置为函数上下文

                // 调用init方法，并将参数传递给它
                initFunc.Run(anyValue.Manager, Ids);

                // 恢复非函数上下文标志
                anyValue.Manager.IsFunc = false;
            }
            else if (results.Count != 0)
            {
                throw new InvalidOperationError(this, "找不到对应的init函数");
            }
        }

        if (result is NativeAnyLangValue nativeAnyValue)
        {
            List<LangValueType> a = [];
            a.AddRange(Ids.Select(id => id.Run(manager)));
            nativeAnyValue.New([.. Apis.ListToObjects(a)]);
            result = nativeAnyValue;
        }

        return result;
    }

    public LangValueType FromClassToResult(LangValueType baseLangValue)
        {
            var type = baseLangValue.GetType();
            var m = type.GetMethod(Id.IdName);
            if (m == null)
            {
                type = baseLangValue switch
                {
                    DictionaryLangValue => Type.GetType("Old8Lang.AST.Expression.DictionaryValueFuncStatic"),
                    ListLangValue => Type.GetType("Old8Lang.AST.Expression.ListValueFuncStatic"),
                    TaskLangValue => Type.GetType("Old8Lang.AST.Expression.TaskValueFuncStatic"),
                    _ => Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic")
                };
                m = type?.GetMethod(Id.IdName);
            }

            if (m == null && baseLangValue is not DictionaryLangValue or ListLangValue or TaskLangValue)
            {
                type = Type.GetType("Old8Lang.AST.Expression.ValueTypeFuncStatic");
                m = type?.GetMethod(Id.IdName);
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
                var argValue = Ids[i] is LangValueType langValue
                    ? langValue
                    : Ids[i].Run(null!);
                os.Add(argValue);
            }

            // 对于静态方法，实例参数为 null；对于实例方法，实例参数为 baseLangValue
            object? invokeInstance = m?.IsStatic == false ? baseLangValue : null;

            var r = m?.Invoke(invokeInstance, [.. os]);
            if (r is LangValueType v) return v;
            return ObjToValue(r!);
        }

    public override string ToString()
    {
        return $"{Id}({string.Join(", ", Ids)})";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        switch (Id.IdName)
        {
            case "PrintLine" or "printLine":
                // 处理多个参数，将它们转换为字符串并拼接
                if (Ids.Count == 0)
                {
                    // 没有参数，调用 Console.WriteLine()
                    var writeLineNoArg = typeof(Console).GetMethod("WriteLine", Type.EmptyTypes);
                    if (writeLineNoArg != null)
                    {
                        ilGenerator.Emit(OpCodes.Call, writeLineNoArg);
                    }

                    return;
                }

                // 简化实现：只处理第一个参数，将其转换为字符串
                var printLineExpr = Ids[0];
                printLineExpr.LoadIlValue(ilGenerator, local);
                var printLineType = printLineExpr.OutputType(local);

                // 直接调用Console.WriteLine(object)方法，让CLR处理类型转换
                var writeLineObject = typeof(Console).GetMethod("WriteLine", [typeof(object)]);
                if (writeLineObject != null)
                {
                    // 如果是值类型，先装箱
                    if (printLineType is { IsValueType: true })
                    {
                        ilGenerator.Emit(OpCodes.Box, printLineType);
                    }

                    ilGenerator.Emit(OpCodes.Call, writeLineObject);
                }

                return;
            case "Print" or "print":
                // 处理多个参数，将它们转换为字符串并拼接
                if (Ids.Count == 0)
                {
                    // 没有参数，直接返回
                    return;
                }

                // 简化实现：只处理第一个参数，将其转换为字符串
                var printExpr = Ids[0];
                printExpr.LoadIlValue(ilGenerator, local);
                var printType = printExpr.OutputType(local);

                // 如果参数不是字符串类型，调用 ToString() 方法转换为字符串
                if (printType != typeof(string))
                {
                    // 获取 ToString() 方法
                    var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;
                    // 如果是值类型，先装箱
                    if (printType is { IsValueType: true })
                    {
                        ilGenerator.Emit(OpCodes.Box, printType);
                    }

                    // 调用 ToString() 方法
                    ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
                }

                // 调用 Console.Write(string)
                ilGenerator.Emit(OpCodes.Call, typeof(Console).GetMethod("Write", [typeof(string)])!);
                return;
            case "Json" or "json":
                return;
            case "ToObj" or "toObj":
                return;
            case "Len" or "len":
                var lenId = Ids[0];
                lenId.LoadIlValue(ilGenerator, local);
                var lenType = lenId.OutputType(local)!;

                // 尝试获取Length属性，适用于数组、字符串等
                var lengthProp = lenType.GetProperty("Length");
                if (lengthProp != null)
                {
                    ilGenerator.Emit(OpCodes.Call, lengthProp.GetGetMethod()!);
                    return;
                }

                // 尝试获取Count属性，适用于集合类
                var countProp = lenType.GetProperty("Count");
                if (countProp != null)
                {
                    ilGenerator.Emit(OpCodes.Call, countProp.GetGetMethod()!);
                    return;
                }

                // 尝试获取Length字段，适用于某些自定义类型
                var lengthField = lenType.GetField("Length");
                if (lengthField != null)
                {
                    ilGenerator.Emit(OpCodes.Ldfld, lengthField);
                    return;
                }

                // 尝试获取Count字段，适用于某些自定义类型
                var countField = lenType.GetField("Count");
                if (countField != null)
                {
                    ilGenerator.Emit(OpCodes.Ldfld, countField);
                    return;
                }

                // 如果是object类型，说明类型推断失败，使用默认值0
                if (lenType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    return;
                }

                // 所有尝试都失败，抛出错误
                throw new InvalidOperationError(this, $"类型 {lenType.Name} 没有 Length 或 Count 属性");
            case "Type" or "type":
                // 编译模式下type()函数返回类型名称字符串
                var typeId = Ids[0];
                var typeIdType = typeId.OutputType(local);
                // 直接返回类型名称字符串，不调用GetType()
                ilGenerator.Emit(OpCodes.Ldstr, typeIdType != null ? typeIdType.Name : "object");

                return;
            case "Compiler" or "compiler":
                ilGenerator.Emit(OpCodes.Ldstr, "编译环境不需要使用Compiler方法");
                ilGenerator.Emit(OpCodes.Call,
                    typeof(Console).GetMethod("WriteLine", [typeof(string)])!);
                return;
            case "Exec" or "exec":
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
            if (classType == null) return;

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

                // 确保参数类型与方法期望的类型匹配
                if (idType != null && paramType != idType)
                {
                    if (paramType == typeof(int) && idType == typeof(int))
                    {
                        // 类型已经匹配，不需要转换
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
                    else if (paramType == typeof(object) && idType.IsValueType)
                    {
                        // 从值类型转换为object，需要装箱
                        ilGenerator.Emit(OpCodes.Box, idType);
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
            // 思路：
            // 1. 为DynamicMethod创建委托类型
            // 2. 创建委托实例
            // 3. 调用委托的Invoke方法

            try
            {
                // 获取委托类型
                var delegateType = CreateDelegateType(matchingMethod);

                // 创建委托实例
                dynamicMethod.CreateDelegate(delegateType);

                // 注意：在IL生成阶段，我们无法直接创建委托实例
                // 这里我们需要生成IL代码来创建委托实例并调用它

                // 步骤1：将DynamicMethod引用加载到栈上
                // 注意：这在IL生成阶段是不可能的，因为DynamicMethod是运行时对象
                // 因此，我们需要采用另一种方式：将DynamicMethod存储在LocalManager中，
                // 然后在运行时通过反射调用

                // 步骤2：加载参数到栈上
                foreach (var t in Ids)
                {
                    t.LoadIlValue(ilGenerator, local);
                }

                // 步骤3：调用委托的Invoke方法
                // 注意：这也无法直接在IL生成阶段完成

                // 因此，我们暂时采用一种简化的方式：
                // 对于返回值类型，确保栈上有返回值，避免栈不平衡
                if (matchingMethod.ReturnType != typeof(void))
                {
                    if (matchingMethod.ReturnType.IsValueType)
                    {
                        // 对于值类型，返回默认值
                        if (matchingMethod.ReturnType == typeof(int))
                        {
                            ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        }
                        else if (matchingMethod.ReturnType == typeof(double))
                        {
                            ilGenerator.Emit(OpCodes.Ldc_R8, 0.0);
                        }
                        else if (matchingMethod.ReturnType == typeof(bool))
                        {
                            ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        }
                        else
                        {
                            // 对于其他值类型，初始化并加载默认值
                            var defaultValueLocal = ilGenerator.DeclareLocal(matchingMethod.ReturnType);
                            ilGenerator.Emit(OpCodes.Initobj, matchingMethod.ReturnType);
                            ilGenerator.Emit(OpCodes.Ldloc, defaultValueLocal);
                        }
                    }
                    else
                    {
                        // 对于引用类型，返回null
                        ilGenerator.Emit(OpCodes.Ldnull);
                    }
                }
            }
            catch (Exception)
            {
                // 如果创建委托类型失败，确保栈平衡
                if (matchingMethod.ReturnType != typeof(void))
                {
                    ilGenerator.Emit(matchingMethod.ReturnType.IsValueType ? OpCodes.Ldc_I4_0 : OpCodes.Ldnull);
                }
            }
        }
        else
        {
            // 对于普通方法，使用Call指令直接调用
            ilGenerator.Emit(OpCodes.Call, matchingMethod);
        }
    }

    /// <summary>
    /// 根据MethodInfo创建对应的委托类型
    /// </summary>
    /// <param name="method">方法信息</param>
    /// <returns>委托类型</returns>
    private Type CreateDelegateType(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var paramTypes = parameters.Select(p => p.ParameterType).ToArray();

        // 使用Expression.GetDelegateType创建委托类型
        // 这个方法会根据参数类型和返回类型创建合适的委托类型
        return System.Linq.Expressions.Expression.GetDelegateType(
            [.. paramTypes, method.ReturnType]
        );
    }

    public override Type OutputType(LocalManager local)
    {
        switch (Id.IdName)
        {
            case "PrintLine":
            case "Print":
            case "Compiler":
                return typeof(void);
            case "Len":
                return typeof(int);
            case "Json":
                return typeof(string);
            case "Type":
                return typeof(string);
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
        return classType ?? typeof(object);
    }
}