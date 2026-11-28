using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.InstanceMethods.Core;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 反射和任务
/// </summary>
public partial class VirtualMachine
{
    private int RegisterTask(TaskLangValue task)
    {
        int taskId = _nextTaskId++;
        _tasks[taskId] = task;
        return taskId;
    }

    /// <summary>
    /// 获取 Task
    /// </summary>
    private TaskLangValue GetTask(int taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new StateError(new SourcePosition(), $"Task ID {taskId} 不存在");
        }

        return task;
    }

    /// <summary>
    /// 辅助方法：调用实例方法
    /// </summary>
    private static InstanceMethods.Core.IInstanceMethod? ResolveVmInstanceMethod(
        InstanceMethods.Core.InstanceMethodRegistry registry,
        Type objectType,
        string methodName,
        int argCount)
    {
        var group = registry.GetOverloadGroup(objectType, methodName);
        if (group == null)
        {
            var equivalentType = VMTypeMapper.GetEquivalentLangType(objectType);
            group = registry.GetOverloadGroup(equivalentType, methodName);
        }

        if (group == null)
        {
            return null;
        }

        var overloads = group.GetAllOverloads();
        return overloads.FirstOrDefault(m =>
                   argCount >= m.MinParameterCount &&
                   (m.MaxParameterCount == -1 || argCount <= m.MaxParameterCount))
               ?? overloads.FirstOrDefault();
    }

    private object? InvokeTypeMethod(object obj, string methodName, object?[] args,
        string[]? namedArgNames = null, object?[]? namedArgValues = null)
    {
        if (obj == null)
        {
            // 与解释器/IL 一致：空引用的 ToStr() 返回 "null"。
            if (methodName == "ToStr" && args.Length == 0)
            {
                return "null";
            }

            throw new NullReferenceError(new SourcePosition(), methodName);
        }

        // 历史语言用法 number.Sqrt()：与 Math.Sqrt 相同，负数要报错而不是 NaN。
        if (methodName == "Sqrt" && args.Length == 0 &&
            obj is double or int or long or float or decimal)
        {
            return IlNumericHelper.Sqrt(Convert.ToDouble(obj, System.Globalization.CultureInfo.InvariantCulture));
        }

        // VM 的元组字面量是右嵌套原生 Tuple，实例方法注册在 TupleLangValue 上。
        // 先按 VM 的元组编码展平并转成语言值，再按普通实例方法分派：这样
        // Get/ToList 拿到 TupleLangValue，All/Filter/Map 的 lambda 也会走
        // ExecuteInVMInternal（vm.CallFunctionObject），而不是解释器路径。
        if (obj is System.Runtime.CompilerServices.ITuple nativeTuple && obj is not TupleLangValue)
        {
            var tupleItems = FlattenVmTuple(nativeTuple)
                .Where(item => item is not null)
                .Select(item => (LangExpression)IlValueBridge.Wrap(item))
                .ToList();
            var tupleValue = new TupleLangValue(tupleItems);
            tupleValue.Accept(Old8Lang.AST.Visitor.RuntimeVisitors.CreateInterpreter(
                new Old8Lang.Interpreter.VariateManager()));
            return InvokeTypeMethod(tupleValue, methodName, args);
        }

        if (obj is Old8Lang.AST.Expression.ModuleObjects.UnifiedModule module)
        {
            var manager = new Old8Lang.Interpreter.VariateManager();
            var arguments = args.Select(value => (Old8Lang.AST.LangExpression)
                (value as Old8Lang.AST.Expression.LangValueType ?? Old8Lang.AST.Expression.LangValueType.ObjToValue(value))).ToList();
            var named = new List<Old8Lang.AST.Expression.NamedArgument>();
            for (var i = 0; namedArgNames is not null && namedArgValues is not null && i < namedArgNames.Length; i++)
            {
                var namedValue = namedArgValues[i];
                named.Add(new Old8Lang.AST.Expression.NamedArgument(namedArgNames[i],
                    (Old8Lang.AST.LangExpression)(namedValue as Old8Lang.AST.Expression.LangValueType ??
                                                  Old8Lang.AST.Expression.LangValueType.ObjToValue(namedValue))));
            }

            var call = new Old8Lang.AST.Expression.Value.Instance(
                new Old8Lang.AST.Expression.LangId(methodName), arguments, named);
            return UnwrapToVmValue(module.Dot(call, manager));
        }

        // 如果没有参数，先尝试作为属性访问
        if (args.Length == 0)
        {
            var objType = obj.GetType();
            var property = objType.GetProperty(methodName);
            if (property != null && property.CanRead)
            {
                return property.GetValue(obj);
            }
        }

        // 优先匹配当前 VM 自身注册的扩展方法，避免与同进程其他模式残留的全局扩展包装器冲突
        var vmExtensionMethod = TryGetVmExtensionMethod(obj.GetType(), methodName);
        if (vmExtensionMethod != null)
        {
            return vmExtensionMethod.ExecuteInVM(obj, args);
        }

        // 首先尝试使用实例方法系统（支持类型等价和按参数个数匹配重载）
        var registry = InstanceMethods.Core.InstanceMethodRegistry.Instance;

        var instanceMethod = ResolveVmInstanceMethod(registry, obj.GetType(), methodName, args.Length);

        if (instanceMethod != null)
        {
            try
            {
                // 使用实例方法系统执行
                var result = instanceMethod.ExecuteInVM(obj, args);
                return result;
            }
            catch (NotSupportedException)
            {
                // 如果实例方法不支持 VM 模式，继续使用旧的扩展方法系统
            }
        }

        Type? extensionType = null;
        System.Reflection.MethodInfo? method = null;
        // Old8Lang 类型已经通过 InstanceMethods 系统处理，不需要在这里处理

        // 如果找到扩展类型，尝试查找扩展方法
        if (extensionType != null)
        {
            var allMethods = extensionType.GetMethods().Where(x => x.Name == methodName).ToArray();
            if (allMethods.Length > 0)
            {
                // 预期参数数量 = 传入参数数量 + 1 (扩展方法的第一个参数是对象本身)
                var expectedParamCount = args.Length + 1;

                // 查找参数数量和类型都匹配的方法
                method = allMethods.FirstOrDefault(x =>
                {
                    var parameters = x.GetParameters();
                    if (parameters.Length != expectedParamCount) return false;

                    // 检查第一个参数（扩展方法的 'this' 参数）类型兼容性
                    if (obj != null && !parameters[0].ParameterType.IsInstanceOfType(obj))
                    {
                        return false;
                    }

                    return true;
                });

                // 如果没找到，查找有可选参数的方法
                if (method == null)
                {
                    method = allMethods.FirstOrDefault(x =>
                    {
                        var parameters = x.GetParameters();
                        if (parameters.Length < expectedParamCount) return false;

                        // 检查除了第一个参数（对象本身）之外，剩余的参数是否都是可选的
                        for (int i = expectedParamCount; i < parameters.Length; i++)
                        {
                            if (!parameters[i].IsOptional && !parameters[i].HasDefaultValue)
                                return false;
                        }

                        return true;
                    });
                }

                // 如果还是没找到，使用第一个方法
                method ??= allMethods[0];
            }
        }

        // 如果没有找到扩展方法，尝试在类型本身上查找实例方法
        if (method == null)
        {
            var objType = obj.GetType();

            // 特殊处理：将 ToStr 映射到 ToString
            var actualMethodName = methodName == "ToStr" ? "ToString" : methodName;

            var allInstanceMethods = objType.GetMethods().Where(x => x.Name == actualMethodName).ToArray();
            if (allInstanceMethods.Length > 0)
            {
                // 对于实例方法，预期参数数量 = 传入参数数量
                var expectedParamCount = args.Length;
                method = allInstanceMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount)
                         ?? allInstanceMethods[0];
            }
        }

        // 如果找不到方法，抛出异常（类型名用语言口径，避免泄露 Int32 / List`1 这类 .NET 名）
        if (method == null)
        {
            throw new MethodNotFoundError(new SourcePosition(), methodName, GetValueTypeName(obj));
        }

        // 准备方法调用参数
        var parameters = method.GetParameters();
        var invokeArgs = new List<object?>();

        // 对于静态方法（扩展方法），第一个参数是对象本身
        if (method.IsStatic && parameters.Length > 0)
        {
            invokeArgs.Add(obj);
        }

        // 添加传入的参数，并进行类型转换
        int startIndex = invokeArgs.Count; // 记录参数起始位置
        invokeArgs.AddRange(args);

        // 类型转换：将 C# 原始类型转换为 Old8Lang 类型（如果需要）
        for (int i = startIndex; i < invokeArgs.Count && i < parameters.Length; i++)
        {
            var arg = invokeArgs[i];
            var paramType = parameters[i].ParameterType;

            // 如果参数期望 LangValueType，但传入的是 C# 原始类型，则进行转换
            if (paramType == typeof(LangValueType) || paramType.IsSubclassOf(typeof(LangValueType)))
            {
                if (arg is not LangValueType)
                {
                    invokeArgs[i] = ConvertToLangValueType(arg);
                }
            }
        }

        // 补充缺失的可选参数
        if (invokeArgs.Count < parameters.Length)
        {
            for (int i = invokeArgs.Count; i < parameters.Length; i++)
            {
                if (parameters[i].IsOptional || parameters[i].HasDefaultValue)
                {
                    invokeArgs.Add(parameters[i].DefaultValue);
                }
            }
        }

        // 调用方法
        object? invokeInstance = method.IsStatic ? null : obj;
        try
        {
            return method.Invoke(invokeInstance, invokeArgs.ToArray());
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // 反射调用会把被调用方法抛出的异常包装成 TargetInvocationException，
            // 其 Message 只有 "Exception has been thrown by the target of an invocation."，
            // 真实原因在 InnerException 中。直接抛出内部异常（保留原始调用栈），
            // 否则用户看到的报错信息完全不指向真实原因。
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
