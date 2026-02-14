using System.Globalization;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Error;

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
    private object? InvokeTypeMethod(object obj, string methodName, object?[] args)
    {
        if (obj == null)
        {
            throw new NullReferenceError(new SourcePosition(), methodName);
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

        // 首先尝试使用实例方法系统（支持类型等价）
        var registry = InstanceMethods.Core.InstanceMethodRegistry.Instance;

        // 尝试查找实例方法
        var instanceMethod = registry.TryGetMethod(obj.GetType(), methodName);

        // 如果没找到，尝试查找等价类型的方法
        if (instanceMethod == null)
        {
            var equivalentType = VMTypeMapper.GetEquivalentLangType(obj.GetType());
            instanceMethod = registry.TryGetMethod(equivalentType, methodName);
        }

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

        // 如果找不到方法，抛出异常
        if (method == null)
        {
            throw new MethodNotFoundError(new SourcePosition(), methodName, obj.GetType().Name);
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
        return method.Invoke(invokeInstance, invokeArgs.ToArray());
    }
}
