using System.Reflection.Emit;
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
            "TaskScheduler" => true,
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
            // 其他静态类可以在这里添加
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
        return methodName switch
        {
            "AssertEqual" => CompileAssertEqual(instance, ilGenerator, local),
            "AssertNotEqual" => CompileAssertNotEqual(instance, ilGenerator, local),
            "AssertTrue" => CompileAssertTrue(instance, ilGenerator, local),
            "AssertFalse" => CompileAssertFalse(instance, ilGenerator, local),
            "AssertNull" => CompileAssertNull(instance, ilGenerator, local),
            "AssertNotNull" => CompileAssertNotNull(instance, ilGenerator, local),
            "AssertGreater" => CompileAssertGreater(instance, ilGenerator, local),
            "AssertGreaterOrEqual" => CompileAssertGreaterOrEqual(instance, ilGenerator, local),
            "AssertLess" => CompileAssertLess(instance, ilGenerator, local),
            "AssertLessOrEqual" => CompileAssertLessOrEqual(instance, ilGenerator, local),
            "AssertContains" => CompileAssertContains(instance, ilGenerator, local),
            "AssertNotContains" => CompileAssertNotContains(instance, ilGenerator, local),
            "AssertStartsWith" => CompileAssertStartsWith(instance, ilGenerator, local),
            "AssertEndsWith" => CompileAssertEndsWith(instance, ilGenerator, local),
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
