using System.Reflection.Emit;
using System.Reflection;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

namespace Old8Lang.Compiler;

/// <summary>
/// 全局静态类编译器，负责为全局静态类生成IL代码
/// </summary>
/// <remarks>
/// 该类提供了一个集中的位置来处理所有全局静态类的编译逻辑，
/// 避免在Operation类中堆积过多的特殊处理代码。
/// 每个静态类的方法调用都会被转换为相应的IL指令。
/// </remarks>
public static class StaticClassCompiler
{
    /// <summary>
    /// 检查给定的标识符是否是支持的全局静态类
    /// </summary>
    /// <param name="className">类名</param>
    /// <returns>如果是支持的静态类返回true，否则返回false</returns>
    public static bool IsSupportedStaticClass(string className)
    {
        return className switch
        {
            "Assert" => true,
            "Task" => true,
            "Thread" => true,
            _ => false
        };
    }

    /// <summary>
    /// 尝试为静态类方法调用生成IL代码
    /// </summary>
    /// <param name="className">静态类名</param>
    /// <param name="methodName">方法名</param>
    /// <param name="instance">方法调用实例，包含参数信息</param>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="returnType">输出：返回类型</param>
    /// <returns>如果成功生成IL代码返回true，否则返回false</returns>
    public static bool TryCompileStaticMethodCall(
        string className,
        string methodName,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        out Type? returnType)
    {
        returnType = null;

        return className switch
        {
            "Assert" => TryCompileAssertMethod(methodName, instance, ilGenerator, local, out returnType),
            "Task" => TryCompileTaskMethod(methodName, instance, ilGenerator, local, out returnType),
            "Thread" => TryCompileThreadMethod(methodName, instance, ilGenerator, local, out returnType),
            _ => false
        };
    }

    /// <summary>
    /// 尝试为Assert类的方法生成IL代码
    /// </summary>
    private static bool TryCompileAssertMethod(
        string methodName,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        out Type? returnType)
    {
        returnType = typeof(void);

        // 根据方法名分发到具体的编译方法
        // 支持简短名称（True, False等）和完整名称（AssertTrue, AssertFalse等）
        return methodName switch
        {
            "AssertEqual" or "Equal" => CompileAssertEqual(instance, ilGenerator, local),
            "AssertNotEqual" or "NotEqual" => CompileAssertNotEqual(instance, ilGenerator, local),
            "AssertTrue" or "True" => CompileAssertTrue(instance, ilGenerator, local),
            "AssertFalse" or "False" => CompileAssertFalse(instance, ilGenerator, local),
            "AssertNull" or "Null" => CompileAssertNull(instance, ilGenerator, local),
            "AssertNotNull" or "NotNull" => CompileAssertNotNull(instance, ilGenerator, local),
            "AssertGreater" or "Greater" => CompileAssertGreater(instance, ilGenerator, local),
            "AssertGreaterOrEqual" or "GreaterOrEqual" => CompileAssertGreaterOrEqual(instance, ilGenerator, local),
            "AssertLess" or "Less" => CompileAssertLess(instance, ilGenerator, local),
            "AssertLessOrEqual" or "LessOrEqual" => CompileAssertLessOrEqual(instance, ilGenerator, local),
            "AssertContains" or "Contains" => CompileAssertContains(instance, ilGenerator, local),
            "AssertNotContains" or "NotContains" => CompileAssertNotContains(instance, ilGenerator, local),
            "AssertStartsWith" or "StartsWith" => CompileAssertStartsWith(instance, ilGenerator, local),
            "AssertEndsWith" or "EndsWith" => CompileAssertEndsWith(instance, ilGenerator, local),
            _ => false
        };
    }

    #region Assert方法实现

    /// <summary>
    /// 编译 Assert.AssertEqual(expected, actual) 或 Assert.AssertEqual(expected, actual, message)
    /// </summary>
    private static bool CompileAssertEqual(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        // 参数数量检查（2-3个参数）
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        // 生成参数加载代码
        var paramTypes = LoadParameters(instance.Ids, ilGenerator, local);

        // 获取AssertEqual方法
        // 我们需要调用一个辅助方法来执行断言
        // 由于Assert方法较为复杂（需要比较不同类型），我们生成调用到辅助类的代码
        var assertHelperType = typeof(AssertHelper);
        var assertEqualMethod = assertHelperType.GetMethod("AssertEqual",
            instance.Ids.Count == 2
                ? [typeof(object), typeof(object)]
                : [typeof(object), typeof(object), typeof(string)]);

        if (assertEqualMethod == null)
        {
            return false;
        }

        ilGenerator.Emit(OpCodes.Call, assertEqualMethod);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertNotEqual(notExpected, actual) 或 Assert.AssertNotEqual(notExpected, actual, message)
    /// </summary>
    private static bool CompileAssertNotEqual(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertNotEqual",
            instance.Ids.Count == 2
                ? [typeof(object), typeof(object)]
                : [typeof(object), typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertTrue(condition) 或 Assert.AssertTrue(condition, message)
    /// </summary>
    private static bool CompileAssertTrue(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 1 or > 2)
        {
            return false;
        }

        // 加载第一个参数（bool类型，不装箱）
        instance.Ids[0].LoadIlValue(ilGenerator, local);

        // 如果有第二个参数（message），加载它
        if (instance.Ids.Count == 2)
        {
            instance.Ids[1].LoadIlValue(ilGenerator, local);
        }

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertTrue",
            instance.Ids.Count == 1
                ? [typeof(bool)]
                : [typeof(bool), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertFalse(condition) 或 Assert.AssertFalse(condition, message)
    /// </summary>
    private static bool CompileAssertFalse(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 1 or > 2)
        {
            return false;
        }

        // 加载第一个参数（bool类型，不装箱）
        instance.Ids[0].LoadIlValue(ilGenerator, local);

        // 如果有第二个参数（message），加载它
        if (instance.Ids.Count == 2)
        {
            instance.Ids[1].LoadIlValue(ilGenerator, local);
        }

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertFalse",
            instance.Ids.Count == 1
                ? [typeof(bool)]
                : [typeof(bool), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertNull(value) 或 Assert.AssertNull(value, message)
    /// </summary>
    private static bool CompileAssertNull(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 1 or > 2)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertNull",
            instance.Ids.Count == 1
                ? [typeof(object)]
                : [typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertNotNull(value) 或 Assert.AssertNotNull(value, message)
    /// </summary>
    private static bool CompileAssertNotNull(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 1 or > 2)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertNotNull",
            instance.Ids.Count == 1
                ? [typeof(object)]
                : [typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertGreater(value, other) 或 Assert.AssertGreater(value, other, message)
    /// </summary>
    private static bool CompileAssertGreater(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertGreater",
            instance.Ids.Count == 2
                ? [typeof(object), typeof(object)]
                : [typeof(object), typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertGreaterOrEqual(value, other) 或 Assert.AssertGreaterOrEqual(value, other, message)
    /// </summary>
    private static bool CompileAssertGreaterOrEqual(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertGreaterOrEqual",
            instance.Ids.Count == 2
                ? [typeof(object), typeof(object)]
                : [typeof(object), typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertLess(value, other) 或 Assert.AssertLess(value, other, message)
    /// </summary>
    private static bool CompileAssertLess(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertLess",
            instance.Ids.Count == 2
                ? [typeof(object), typeof(object)]
                : [typeof(object), typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertLessOrEqual(value, other) 或 Assert.AssertLessOrEqual(value, other, message)
    /// </summary>
    private static bool CompileAssertLessOrEqual(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        LoadParameters(instance.Ids, ilGenerator, local);

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertLessOrEqual",
            instance.Ids.Count == 2
                ? [typeof(object), typeof(object)]
                : [typeof(object), typeof(object), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertContains(text, substring) 或 Assert.AssertContains(text, substring, message)
    /// </summary>
    private static bool CompileAssertContains(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        // 加载字符串参数（不装箱）
        instance.Ids[0].LoadIlValue(ilGenerator, local);
        instance.Ids[1].LoadIlValue(ilGenerator, local);

        // 如果有第三个参数（message），加载它
        if (instance.Ids.Count == 3)
        {
            instance.Ids[2].LoadIlValue(ilGenerator, local);
        }

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertContains",
            instance.Ids.Count == 2
                ? [typeof(string), typeof(string)]
                : [typeof(string), typeof(string), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertNotContains(text, substring) 或 Assert.AssertNotContains(text, substring, message)
    /// </summary>
    private static bool CompileAssertNotContains(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        // 加载字符串参数（不装箱）
        instance.Ids[0].LoadIlValue(ilGenerator, local);
        instance.Ids[1].LoadIlValue(ilGenerator, local);

        // 如果有第三个参数（message），加载它
        if (instance.Ids.Count == 3)
        {
            instance.Ids[2].LoadIlValue(ilGenerator, local);
        }

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertNotContains",
            instance.Ids.Count == 2
                ? [typeof(string), typeof(string)]
                : [typeof(string), typeof(string), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertStartsWith(text, prefix) 或 Assert.AssertStartsWith(text, prefix, message)
    /// </summary>
    private static bool CompileAssertStartsWith(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        // 加载字符串参数（不装箱）
        instance.Ids[0].LoadIlValue(ilGenerator, local);
        instance.Ids[1].LoadIlValue(ilGenerator, local);

        // 如果有第三个参数（message），加载它
        if (instance.Ids.Count == 3)
        {
            instance.Ids[2].LoadIlValue(ilGenerator, local);
        }

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertStartsWith",
            instance.Ids.Count == 2
                ? [typeof(string), typeof(string)]
                : [typeof(string), typeof(string), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    /// <summary>
    /// 编译 Assert.AssertEndsWith(text, suffix) 或 Assert.AssertEndsWith(text, suffix, message)
    /// </summary>
    private static bool CompileAssertEndsWith(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count is < 2 or > 3)
        {
            return false;
        }

        // 加载字符串参数（不装箱）
        instance.Ids[0].LoadIlValue(ilGenerator, local);
        instance.Ids[1].LoadIlValue(ilGenerator, local);

        // 如果有第三个参数（message），加载它
        if (instance.Ids.Count == 3)
        {
            instance.Ids[2].LoadIlValue(ilGenerator, local);
        }

        var assertHelperType = typeof(AssertHelper);
        var method = assertHelperType.GetMethod("AssertEndsWith",
            instance.Ids.Count == 2
                ? [typeof(string), typeof(string)]
                : [typeof(string), typeof(string), typeof(string)]);

        if (method == null) return false;

        ilGenerator.Emit(OpCodes.Call, method);
        return true;
    }

    #endregion

    #region Task方法实现

    /// <summary>
    /// 尝试为Task类的方法生成IL代码
    /// </summary>
    private static bool TryCompileTaskMethod(
        string methodName,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        out Type? returnType)
    {
        returnType = typeof(Task<object>);

        // 根据方法名分发到具体的编译方法
        return methodName switch
        {
            "Delay" => CompileTaskDelay(instance, ilGenerator, local, out returnType),
            "FromResult" => CompileTaskFromResult(instance, ilGenerator, local, out returnType),
            "Run" => CompileTaskRun(instance, ilGenerator, local, out returnType),
            "WhenAll" => CompileTaskWhenAll(instance, ilGenerator, local, out returnType),
            "WhenAny" => CompileTaskWhenAny(instance, ilGenerator, local, out returnType),
            _ => false
        };
    }

    /// <summary>
    /// 编译 Task.Delay(milliseconds)
    /// </summary>
    private static bool CompileTaskDelay(Instance instance, ILGenerator ilGenerator, LocalManager local, out Type? returnType)
    {
        returnType = typeof(Task<object>);

        if (instance.Ids.Count != 1)
        {
            return false;
        }

        // 加载延迟时间参数
        instance.Ids[0].LoadIlValue(ilGenerator, local);

        // 调用 Task.Delay(int)
        var delayMethod = typeof(Task).GetMethod("Delay", [typeof(int)])!;
        ilGenerator.Emit(OpCodes.Call, delayMethod);

        // Task.Delay 返回 Task，我们需要将其转换为 Task<object>
        // 使用 ContinueWith 创建一个返回 object 的延续任务
        // 查找泛型版本的 ContinueWith<TResult>(Func<Task, TResult>)
        var continueWithMethod = typeof(Task)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "ContinueWith" &&
                m.IsGenericMethodDefinition &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

        if (continueWithMethod == null)
        {
            return false;
        }

        // 将泛型方法实例化为 ContinueWith<object>(Func<Task, object>)
        continueWithMethod = continueWithMethod.MakeGenericMethod(typeof(object));

        // 创建一个返回null的lambda委托
        // 使用静态辅助方法
        var helperMethod = typeof(TaskHelper).GetMethod("ReturnNull")!;
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Ldftn, helperMethod);
        var funcConstructor = typeof(Func<Task, object>).GetConstructor([typeof(object), typeof(IntPtr)])!;
        ilGenerator.Emit(OpCodes.Newobj, funcConstructor);

        ilGenerator.Emit(OpCodes.Callvirt, continueWithMethod);

        return true;
    }

    /// <summary>
    /// 编译 Task.FromResult(value)
    /// </summary>
    private static bool CompileTaskFromResult(Instance instance, ILGenerator ilGenerator, LocalManager local, out Type? returnType)
    {
        returnType = typeof(Task<object>);

        if (instance.Ids.Count != 1)
        {
            return false;
        }

        // 加载参数
        instance.Ids[0].LoadIlValue(ilGenerator, local);
        var paramType = instance.Ids[0].OutputType(local);

        // 如果是值类型，装箱为object
        if (paramType != null && paramType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, paramType);
        }

        // 调用 Task.FromResult<object>(object)
        var fromResultMethod = typeof(Task)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
        fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
        ilGenerator.Emit(OpCodes.Call, fromResultMethod);

        return true;
    }

    /// <summary>
    /// 编译 Task.Run(action)
    /// </summary>
    private static bool CompileTaskRun(Instance instance, ILGenerator ilGenerator, LocalManager local, out Type? returnType)
    {
        returnType = typeof(Task<object>);

        if (instance.Ids.Count != 1)
        {
            return false;
        }

        // 加载委托参数
        instance.Ids[0].LoadIlValue(ilGenerator, local);
        var paramType = instance.Ids[0].OutputType(local);

        // 检查参数类型，应该是 Func<object> 或 Action
        if (paramType == typeof(Func<object>))
        {
            // 调用 Task.Run<object>(Func<object>)
            var runMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Run" &&
                           m.IsGenericMethodDefinition &&
                           m.GetParameters().Length == 1 &&
                           m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Func<>));
            runMethod = runMethod.MakeGenericMethod(typeof(object));
            ilGenerator.Emit(OpCodes.Call, runMethod);
            return true;
        }
        else if (paramType == typeof(Action))
        {
            // 调用 Task.Run(Action)，然后转换为 Task<object>
            var runMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Run" &&
                           !m.IsGenericMethodDefinition &&
                           m.GetParameters().Length == 1 &&
                           m.GetParameters()[0].ParameterType == typeof(Action));
            ilGenerator.Emit(OpCodes.Call, runMethod);

            // 将 Task 转换为 Task<object>，使用 ContinueWith
            var continueWithMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(m =>
                    m.Name == "ContinueWith" &&
                    m.IsGenericMethodDefinition &&
                    m.GetParameters().Length == 1 &&
                    m.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

            if (continueWithMethod != null)
            {
                continueWithMethod = continueWithMethod.MakeGenericMethod(typeof(object));

                // 创建返回 null 的委托
                var helperMethod = typeof(TaskHelper).GetMethod("ReturnNull")!;
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Ldftn, helperMethod);
                var funcConstructor = typeof(Func<Task, object>).GetConstructor([typeof(object), typeof(IntPtr)])!;
                ilGenerator.Emit(OpCodes.Newobj, funcConstructor);

                ilGenerator.Emit(OpCodes.Callvirt, continueWithMethod);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 编译 Task.WhenAll(tasks)
    /// </summary>
    private static bool CompileTaskWhenAll(Instance instance, ILGenerator ilGenerator, LocalManager local, out Type? returnType)
    {
        returnType = typeof(Task<object[]>);

        if (instance.Ids.Count != 1)
        {
            return false;
        }

        // 加载任务列表/数组参数
        var listExpr = instance.Ids[0];
        listExpr.LoadIlValue(ilGenerator, local);

        // 获取列表/数组类型
        var listType = listExpr.OutputType(local);

        if (listType == typeof(List<object>))
        {
            // 对于 List<object>，转换为数组
            var toArrayMethod = typeof(List<object>).GetMethod("ToArray")!;
            ilGenerator.Emit(OpCodes.Callvirt, toArrayMethod);

            // 调用 Task.WhenAll，假设数组元素是 Task<object>
            // 实际运行时需要转换，这里简化处理
            var whenAllMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "WhenAll" &&
                                    m.GetParameters()[0].ParameterType == typeof(Task<object>[]));

            if (whenAllMethod != null)
            {
                ilGenerator.Emit(OpCodes.Call, whenAllMethod);
                return true;
            }
        }
        else if (listType == typeof(object[]))
        {
            // 对于 object[]，假设元素是 Task，调用 Task.WhenAll
            var whenAllMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "WhenAll" &&
                                    m.GetParameters().Length == 1 &&
                                    m.GetParameters()[0].ParameterType.IsArray);

            if (whenAllMethod != null)
            {
                ilGenerator.Emit(OpCodes.Call, whenAllMethod);
                returnType = typeof(Task);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 编译 Task.WhenAny(tasks)
    /// </summary>
    private static bool CompileTaskWhenAny(Instance instance, ILGenerator ilGenerator, LocalManager local, out Type? returnType)
    {
        returnType = typeof(Task<object>);

        if (instance.Ids.Count != 1)
        {
            return false;
        }

        // 加载任务列表/数组参数
        var listExpr = instance.Ids[0];
        listExpr.LoadIlValue(ilGenerator, local);

        // 获取列表/数组类型
        var listType = listExpr.OutputType(local);

        if (listType == typeof(List<object>))
        {
            // 对于 List<object>，转换为数组
            var toArrayMethod = typeof(List<object>).GetMethod("ToArray")!;
            ilGenerator.Emit(OpCodes.Callvirt, toArrayMethod);

            // 调用 Task.WhenAny
            var whenAnyMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "WhenAny" &&
                                    m.GetParameters()[0].ParameterType == typeof(Task<object>[]));

            if (whenAnyMethod != null)
            {
                ilGenerator.Emit(OpCodes.Call, whenAnyMethod);
                returnType = typeof(Task<Task<object>>);
                return true;
            }
        }
        else if (listType == typeof(object[]))
        {
            // 对于 object[]，假设元素是 Task
            var whenAnyMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "WhenAny" &&
                                    m.GetParameters().Length == 1 &&
                                    m.GetParameters()[0].ParameterType.IsArray);

            if (whenAnyMethod != null)
            {
                ilGenerator.Emit(OpCodes.Call, whenAnyMethod);
                returnType = typeof(Task<Task>);
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Thread方法实现

    /// <summary>
    /// 尝试为Thread类的方法生成IL代码
    /// </summary>
    private static bool TryCompileThreadMethod(
        string methodName,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        out Type? returnType)
    {
        returnType = typeof(void);

        // 根据方法名分发到具体的编译方法
        return methodName switch
        {
            "Sleep" => CompileThreadSleep(instance, ilGenerator, local, out returnType),
            _ => false
        };
    }

    /// <summary>
    /// 编译 Thread.Sleep(milliseconds)
    /// </summary>
    private static bool CompileThreadSleep(Instance instance, ILGenerator ilGenerator, LocalManager local, out Type? returnType)
    {
        returnType = typeof(void);

        if (instance.Ids.Count != 1)
        {
            return false;
        }

        // 加载延迟时间参数
        instance.Ids[0].LoadIlValue(ilGenerator, local);

        // 调用 System.Threading.Thread.Sleep(int)
        var sleepMethod = typeof(System.Threading.Thread).GetMethod("Sleep", [typeof(int)])!;
        ilGenerator.Emit(OpCodes.Call, sleepMethod);

        return true;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 加载参数到IL栈上，并返回参数类型列表
    /// </summary>
    private static List<Type> LoadParameters(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local)
    {
        var paramTypes = new List<Type>();

        foreach (var param in parameters)
        {
            param.LoadIlValue(ilGenerator, local);
            var paramType = param.OutputType(local);

            // 如果是值类型，需要装箱
            if (paramType != null && paramType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, paramType);
                paramTypes.Add(typeof(object));
            }
            else
            {
                paramTypes.Add(paramType ?? typeof(object));
            }
        }

        return paramTypes;
    }

    #endregion
}
