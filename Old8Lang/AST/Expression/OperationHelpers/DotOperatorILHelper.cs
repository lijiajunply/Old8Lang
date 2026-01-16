using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler;
using Old8Lang.Error;
using System.Linq;
using System.Collections;

namespace Old8Lang.AST.Expression.OperationHelpers;

/// <summary>
/// 点操作符（.）IL 代码生成助手类
/// </summary>
/// <remarks>
/// 该类负责生成点操作符（.）的IL代码，处理成员访问、方法调用和索引访问等操作。
///
/// 支持的操作：
/// - this.member 访问（类环境内）
/// - 静态类方法调用（通过 StaticClassCompiler）
/// - Assert 静态方法调用
/// - Task 静态方法调用（Delay, FromResult, Run, WhenAll, WhenAny）
/// - 实例方法调用
/// - 枚举成员访问
/// - 字段和属性访问
/// - 索引访问（数组、列表、字典、字符串）
/// - 特殊方法映射（ToStr → ToString, Count → Count/Length属性）
/// </remarks>
public static class DotOperatorILHelper
{
    /// <summary>
    /// 生成点操作符的IL代码
    /// </summary>
    /// <param name="left">左操作数表达式</param>
    /// <param name="right">右操作数表达式</param>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="leftType">左操作数的类型</param>
    /// <param name="rightType">右操作数的类型</param>
    /// <param name="operation">操作表达式（用于错误报告）</param>
    /// <returns>操作结果的类型</returns>
    public static Type GenerateDotOperator(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type? rightType,
        Operation operation)
    {
        // 1. 处理 this.member 访问（类环境内）
        if (local.InClassEnv is not null && left is LangId { IdName: "this" })
        {
            return GenerateThisMemberAccess(right, ilGenerator, local, operation);
        }

        // 2. 处理实例方法调用
        if (right is Instance instance)
        {
            return GenerateInstanceMethodCall(left, instance, ilGenerator, local, leftType, operation);
        }

        // 3. 处理字段和属性访问
        if (right is LangId id)
        {
            return GenerateFieldOrPropertyAccess(left, id, ilGenerator, local, leftType, operation);
        }

        // 4. 处理索引访问
        if (rightType is not null)
        {
            return GenerateIndexAccess(left, right, ilGenerator, local, leftType, rightType, operation);
        }

        return typeof(void);
    }

    /// <summary>
    /// 生成 this.member 访问的IL代码
    /// </summary>
    private static Type GenerateThisMemberAccess(
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Operation operation)
    {
        ilGenerator.Emit(OpCodes.Ldarg_0);
        if (right is not LangId rightId) return local.InClassEnv!;

        // 优先从 FieldVar 中查找字段（支持 TypeBuilder）
        FieldInfo? fieldInfo = null;
        if (local.FieldVar.TryGetValue(rightId.IdName, out fieldInfo))
        {
            // 找到了字段
        }
        // 如果 FieldVar 中没有，尝试从当前类型或父类中获取
        else if (local.InClassEnv is TypeBuilder classTypeBuilder)
        {
            // 对于 TypeBuilder，尝试从基类中查找字段
            var baseType = classTypeBuilder.BaseType;
            while (baseType is not null && baseType != typeof(object))
            {
                fieldInfo = baseType.GetField(rightId.IdName, BindingFlags.Public | BindingFlags.Instance);
                if (fieldInfo is not null) break;
                baseType = baseType.BaseType;
            }
        }
        else
        {
            // 对于已创建的类型，直接获取字段
            fieldInfo = local.InClassEnv!.GetField(rightId.IdName,
                BindingFlags.Public | BindingFlags.Instance);
        }

        if (fieldInfo is not null)
        {
            ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            return fieldInfo.FieldType;
        }

        // 尝试查找属性（仅适用于已创建的类型）
        if (local.InClassEnv is not TypeBuilder)
        {
            var p = local.InClassEnv!.GetProperty(rightId.IdName);
            if (p is not null && p.GetGetMethod() is not null)
            {
                ilGenerator.Emit(OpCodes.Call, p.GetGetMethod()!);
                return p.PropertyType;
            }
        }

        // 如果没有找到字段或属性，返回typeof(object)
        return typeof(object);
    }

    /// <summary>
    /// 生成实例方法调用的IL代码
    /// </summary>
    private static Type GenerateInstanceMethodCall(
        LangExpression? left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Operation operation)
    {
        // 特殊处理Assert静态方法调用
        if (left is LangId { IdName: "Assert" })
        {
            return GenerateAssertMethodCall(instance, ilGenerator, local, operation);
        }

        // 特殊处理Task静态方法调用
        if (left is LangId { IdName: "Task" })
        {
            return GenerateTaskStaticMethodCall(instance, ilGenerator, local, operation);
        }

        // 尝试使用StaticClassCompiler处理全局静态类方法调用
        if (left is LangId leftId && StaticClassCompiler.IsSupportedStaticClass(leftId.IdName))
        {
            var methodName = instance.Id.IdName;
            if (StaticClassCompiler.TryCompileStaticMethodCall(
                    leftId.IdName, methodName, instance, ilGenerator, local, out var returnType))
            {
                return returnType!;
            }

            // 如果StaticClassCompiler无法处理这个方法，抛出更有用的错误
            throw new InvalidOperationError(operation, $"方法 '{methodName}' 不支持",
                $"静态类 '{leftId.IdName}' 不支持方法 '{methodName}'。请检查方法名是否正确。");
        }

        // 普通实例方法调用
        return GenerateRegularInstanceMethodCall(left, instance, ilGenerator, local, leftType, operation);
    }

    /// <summary>
    /// 生成 Assert 静态方法调用的IL代码
    /// </summary>
    private static Type GenerateAssertMethodCall(
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Operation operation)
    {
        // Assert静态方法调用，如Assert.True(condition)
        var methodName = instance.Id.IdName;

        // 映射方法名：支持 "True" 和 "AssertTrue" 两种形式
        var actualMethodName = methodName switch
        {
            "True" => "AssertTrue",
            "False" => "AssertFalse",
            "Equal" => "AssertEqual",
            "NotEqual" => "AssertNotEqual",
            "Null" => "AssertNull",
            "NotNull" => "AssertNotNull",
            "Greater" => "AssertGreater",
            "GreaterOrEqual" => "AssertGreaterOrEqual",
            "Less" => "AssertLess",
            "LessOrEqual" => "AssertLessOrEqual",
            "Contains" => "AssertContains",
            "NotContains" => "AssertNotContains",
            "StartsWith" => "AssertStartsWith",
            "EndsWith" => "AssertEndsWith",
            _ => methodName // 如果已经是 AssertXxx 形式，直接使用
        };

        // 收集参数类型
        var paramTypes = instance.Ids.Select(id => id.OutputType(local) ?? typeof(object)).ToList();

        // 从 AssertHelper 类中查找匹配的方法
        var assertMethod = typeof(AssertHelper).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name == actualMethodName)
            .FirstOrDefault(m =>
            {
                var parameters = m.GetParameters();
                // 跳过可选参数
                var requiredParamCount = parameters.Count(p => !p.IsOptional);
                if (paramTypes.Count < requiredParamCount || paramTypes.Count > parameters.Length)
                    return false;

                // 检查参数类型是否兼容
                for (int i = 0; i < paramTypes.Count; i++)
                {
                    var expectedType = parameters[i].ParameterType;
                    var actualType = paramTypes[i];

                    // 如果期望的是 object 类型,任何类型都可以
                    if (expectedType == typeof(object))
                        continue;

                    // 如果类型完全匹配或者可以赋值
                if (expectedType.IsAssignableFrom(actualType))
                    continue;

                // 如果实际类型是 object，且期望是值类型，允许（我们将尝试拆箱）
                if (actualType == typeof(object) && expectedType.IsValueType)
                    continue;

                return false;
            }

                return true;
            });

        if (assertMethod is not null)
        {
            // 加载参数,根据方法签名决定是否装箱
            var parameters = assertMethod.GetParameters();
            for (int i = 0; i < instance.Ids.Count; i++)
            {
                instance.Ids[i].LoadIlValue(ilGenerator, local);
                var idType = instance.Ids[i].OutputType(local);
                var paramType = parameters[i].ParameterType;

                // 如果参数类型是 object,且值是值类型,需要装箱
                if (paramType == typeof(object) && idType is not null && idType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, idType);
                }

                // 如果参数类型是值类型,且栈上是 object,需要拆箱
                if (paramType.IsValueType && idType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, paramType);
                }
            }

            ilGenerator.Emit(OpCodes.Call, assertMethod);

            // 处理 void 返回类型
            // void 方法不会在栈上留下任何值，直接返回 void 类型
            // IL 验证器会正确处理这种情况
            return assertMethod.ReturnType;
        }

        throw new InvalidOperationError(operation, $"Assert方法 '{methodName}' 未找到",
            $"无法在 AssertHelper 类中找到方法 '{actualMethodName}'，参数类型: {string.Join(", ", paramTypes.Select(t => t.Name))}");
    }

    /// <summary>
    /// 生成 Task 静态方法调用的IL代码
    /// </summary>
    private static Type GenerateTaskStaticMethodCall(
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Operation operation)
    {
        // Task静态方法调用，如Task.Delay(100)
        var methodName = instance.Id.IdName;
        var paramTypes = new List<Type>();

        // 加载参数
        foreach (var instanceId in instance.Ids)
        {
            instanceId.LoadIlValue(ilGenerator, local);
            var idType = instanceId.OutputType(local);
            paramTypes.Add(idType!);
        }

        // 根据方法名调用对应的Task静态方法
        switch (methodName)
        {
            case "Delay":
                return GenerateTaskDelay(paramTypes, ilGenerator);
            case "FromResult":
                return GenerateTaskFromResult(paramTypes, ilGenerator);
            case "Run":
                return GenerateTaskRun(ilGenerator);
            case "WhenAll":
                return GenerateTaskWhenAll(instance, ilGenerator, local);
            case "WhenAny":
                return GenerateTaskWhenAny(instance, ilGenerator, local);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.Delay 的IL代码
    /// </summary>
    private static Type GenerateTaskDelay(List<Type> paramTypes, ILGenerator ilGenerator)
    {
        MethodInfo methodInfo;
        if (paramTypes.Count == 1 && paramTypes[0] == typeof(int))
        {
            // 调用 Task.Delay(int)
            methodInfo = typeof(Task).GetMethod("Delay", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Call, methodInfo);
            return typeof(Task);
        }
        else if (paramTypes.Count == 2)
        {
            // 调用 Task.Delay(int, CancellationToken)
            methodInfo = typeof(Task).GetMethod("Delay",
                [typeof(int), typeof(System.Threading.CancellationToken)])!;
            ilGenerator.Emit(OpCodes.Call, methodInfo);
            return typeof(Task);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.FromResult 的IL代码
    /// </summary>
    private static Type GenerateTaskFromResult(List<Type> paramTypes, ILGenerator ilGenerator)
    {
        if (paramTypes.Count == 1)
        {
            // 对于任何参数类型，直接返回 Task<object>
            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
            fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
            // 参数已经在栈上，直接调用
            ilGenerator.Emit(OpCodes.Call, fromResultMethod);
            return typeof(Task<object>);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.Run 的IL代码
    /// </summary>
    private static Type GenerateTaskRun(ILGenerator ilGenerator)
    {
        // 这里简化处理，直接返回一个已完成的 Task<object>
        var fromResultMethod = typeof(Task)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
        fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Call, fromResultMethod);
        return typeof(Task<object>);
    }

    /// <summary>
    /// 运行时辅助方法：Task.WhenAll
    /// </summary>
    public static Task<object[]> RuntimeWhenAll(object tasks)
    {
        var enumerable = (IEnumerable)tasks;
        var casted = enumerable.Cast<Task<object>>();
        return Task.WhenAll(casted);
    }

    /// <summary>
    /// 运行时辅助方法：Task.WhenAny
    /// </summary>
    public static Task<Task<object>> RuntimeWhenAny(object tasks)
    {
        var enumerable = (IEnumerable)tasks;
        var casted = enumerable.Cast<Task<object>>();
        return Task.WhenAny(casted);
    }

    /// <summary>
    /// 生成 Task.WhenAll 的IL代码
    /// </summary>
    private static Type GenerateTaskWhenAll(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count == 1)
        {
            // 加载列表/数组参数
            var listExpr = instance.Ids[0];
            listExpr.LoadIlValue(ilGenerator, local);

            // 获取列表/数组类型
            var listType = listExpr.OutputType(local);

            if (listType != null && typeof(IEnumerable).IsAssignableFrom(listType))
            {
                // 调用 RuntimeWhenAll
                var runtimeMethod = typeof(DotOperatorILHelper).GetMethod(nameof(RuntimeWhenAll))!;
                ilGenerator.Emit(OpCodes.Call, runtimeMethod);
                return typeof(Task<object[]>);
            }

            // 其他类型，简化处理，返回 Task.FromResult<object>(null)
            ilGenerator.Emit(OpCodes.Ldnull);
            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
            fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
            ilGenerator.Emit(OpCodes.Call, fromResultMethod);
            return typeof(Task<object>);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.WhenAny 的IL代码
    /// </summary>
    private static Type GenerateTaskWhenAny(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count == 1)
        {
            // 加载列表/数组参数
            var listExpr = instance.Ids[0];
            listExpr.LoadIlValue(ilGenerator, local);

            // 获取列表/数组类型
            var listType = listExpr.OutputType(local);

            if (listType != null && typeof(IEnumerable).IsAssignableFrom(listType))
            {
                // 调用 RuntimeWhenAny
                var runtimeMethod = typeof(DotOperatorILHelper).GetMethod(nameof(RuntimeWhenAny))!;
                ilGenerator.Emit(OpCodes.Call, runtimeMethod);
                return typeof(Task<Task<object>>);
            }

            // 其他类型，简化处理，返回 Task.FromResult<object>(null)
            ilGenerator.Emit(OpCodes.Ldnull);
            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
            fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
            ilGenerator.Emit(OpCodes.Call, fromResultMethod);
            return typeof(Task<object>);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成普通实例方法调用的IL代码
    /// </summary>
    private static Type GenerateRegularInstanceMethodCall(
        LangExpression? left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Operation operation)
    {
        left!.LoadIlValue(ilGenerator, local);
        var types = new List<Type>();
        foreach (var instanceId in instance.Ids)
        {
            instanceId.LoadIlValue(ilGenerator, local);
            var idType = instanceId.OutputType(local);
            // 不装箱，保持原始类型
            types.Add(idType!);
        }

        // 特殊处理Old8Lang的ToStr()方法
        // ToStr()是Old8Lang的扩展方法，在编译模式下将其映射到.NET的ToString()
        if (instance.Id.IdName == "ToStr" && instance.Ids.Count == 0)
        {
            // 调用ToString()方法
            var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;

            // 如果左侧是值类型，需要先装箱
            if (leftType!.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, leftType);
            }

            ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
            return typeof(string);
        }

        // 特殊处理Old8Lang的Count()方法
        // Count()在Old8Lang中是方法，但在.NET的List<T>和T[]中是属性
        if (instance.Id.IdName == "Count" && instance.Ids.Count == 0)
        {
            // 如果是泛型集合类型，使用Count属性
            if (leftType!.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // 获取Count属性
                var countProperty = leftType.GetProperty("Count")!;
                ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
                return typeof(int);
            }

            // 如果是数组类型，使用Length属性
            if (leftType.IsArray)
            {
                // 获取Length属性
                var lengthProperty = leftType.GetProperty("Length")!;
                ilGenerator.Emit(OpCodes.Callvirt, lengthProperty.GetGetMethod()!);
                return typeof(int);
            }
        }

        // 特殊处理Old8Lang的Length()方法
        // Length()在Old8Lang中是方法，但在.NET的数组中是属性
        if (instance.Id.IdName == "Length" && instance.Ids.Count == 0)
        {
            // 如果是数组类型，使用Length属性
            if (leftType!.IsArray)
            {
                // 获取Length属性
                var lengthProperty = leftType.GetProperty("Length")!;
                ilGenerator.Emit(OpCodes.Callvirt, lengthProperty.GetGetMethod()!);
                return typeof(int);
            }

            // 如果是泛型集合类型，使用Count属性
            if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // 获取Count属性
                var countProperty = leftType.GetProperty("Count")!;
                ilGenerator.Emit(OpCodes.Callvirt, countProperty.GetGetMethod()!);
                return typeof(int);
            }
        }

        // 尝试查找精确匹配的方法
        var m = leftType!.GetMethod(instance.Id.IdName, [.. types]);

        // 如果没有找到精确匹配，尝试查找参数数量匹配的方法
        if (m is null)
        {
            m = leftType.GetMethods()
                .FirstOrDefault(method =>
                    method.Name == instance.Id.IdName &&
                    method.GetParameters().Length == instance.Ids.Count);
        }

        // 如果找到了方法，检查参数类型是否需要拆箱
        if (m != null)
        {
            var parameters = m.GetParameters();
            // 如果参数类型不匹配（例如栈上是 object，但方法期望 int），需要进行类型转换
            for (int i = 0; i < parameters.Length && i < types.Count; i++)
            {
                var paramType = parameters[i].ParameterType;
                var stackType = types[i];

                // 如果栈上是 object，但方法期望值类型，需要拆箱
                if (stackType == typeof(object) && paramType.IsValueType)
                {
                    // 需要重新生成参数加载代码，这次不装箱
                    // 但这很复杂，因为参数已经在栈上了
                    // 暂时跳过这个优化
                }
            }
        }

        if (m is null)
        {
            // 尝试查找扩展方法
            Type? extensionType = null;

            // 根据左操作数类型确定对应的扩展方法类
            if (leftType == typeof(string))
            {
                extensionType = typeof(StringExtensions);
            }
            else if (leftType == typeof(object[]))
            {
                extensionType = typeof(ArrayExtensions);
            }
            else if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
            {
                extensionType = typeof(ListExtensions);
            }
            else if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                extensionType = typeof(DictionaryExtensions);
            }
            else if (leftType == typeof(int) || leftType == typeof(double) || 
                     leftType == typeof(bool) || leftType == typeof(char))
            {
                extensionType = typeof(PrimitiveExtensions);
            }
            // Old8Lang 类型的扩展方法
            else if (leftType == typeof(StringLangValue))
            {
                extensionType = typeof(StringValueFuncStatic);
            }
            else if (leftType == typeof(ArrayLangValue))
            {
                extensionType = typeof(ArrayValueFuncStatic);
            }
            else if (leftType == typeof(ListLangValue))
            {
                extensionType = typeof(ListValueFuncStatic);
            }
            else if (leftType == typeof(DictionaryLangValue))
            {
                extensionType = typeof(DictionaryValueFuncStatic);
            }
            else if (leftType.FullName?.StartsWith("System.ValueTuple") == true)
            {
                extensionType = typeof(ValueFunctions.TupleExtensions);
            }
            else if (leftType == typeof(TupleLangValue))
            {
                extensionType = typeof(TupleValueFuncStatic);
            }

            if (extensionType != null)
            {
                // 扩展方法的第一个参数是 this 参数（实例本身）
                // 对于泛型类型，需要使用扩展方法期望的类型
                Type firstParamType = leftType;

                // 如果是 List<T>，转换为 List<object?>
                if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    firstParamType = typeof(List<object?>);
                }
                // 如果是 Dictionary<TKey, TValue>，转换为 Dictionary<object, object?>
                else if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    firstParamType = typeof(Dictionary<object, object?>);
                }
                // 如果是 ValueTuple，使用 ITuple
                else if (leftType.FullName?.StartsWith("System.ValueTuple") == true)
                {
                    firstParamType = typeof(System.Runtime.CompilerServices.ITuple);
                }

                var extensionTypes = new Type[types.Count + 1];
                extensionTypes[0] = firstParamType;
                types.CopyTo(extensionTypes, 1);

                // 查找精确匹配的扩展方法
                m = extensionType.GetMethod(instance.Id.IdName, extensionTypes);

                // 如果没有找到精确匹配，尝试查找参数数量匹配的扩展方法
                if (m is null)
                {
                    m = extensionType.GetMethods()
                        .FirstOrDefault(method =>
                            method.Name == instance.Id.IdName &&
                            method.GetParameters().Length == instance.Ids.Count + 1); // +1 因为扩展方法有 this 参数
                }

                // 如果找到了扩展方法，且左操作数是值类型但扩展方法期望引用类型（如接口），需要装箱
                if (m != null && leftType.IsValueType && !m.GetParameters()[0].ParameterType.IsValueType)
                {
                    // 我们需要装箱 'left'，它在栈的深处，参数之下。
                    // 保存参数
                    var argLocals = new LocalBuilder[types.Count];
                    for (int i = types.Count - 1; i >= 0; i--)
                    {
                        argLocals[i] = ilGenerator.DeclareLocal(types[i]);
                        ilGenerator.Emit(OpCodes.Stloc, argLocals[i]);
                    }

                    // 装箱 left
                    ilGenerator.Emit(OpCodes.Box, leftType);

                    // 恢复参数
                    for (int i = 0; i < types.Count; i++)
                    {
                        ilGenerator.Emit(OpCodes.Ldloc, argLocals[i]);
                    }
                }
            }

            if (m is null)
            {
                // 方法未找到，抛出异常
                throw new InvalidOperationError(operation, $"方法 '{instance.Id.IdName}' 未找到",
                    $"无法在类型 '{leftType.Name}' 中找到方法 '{instance.Id.IdName}'，参数类型为: {string.Join(", ", types.Select(t => t.Name))}");
            }
        }

        // 对于实例方法使用 Callvirt，对于静态方法（包括扩展方法）使用 Call
        ilGenerator.Emit(m.IsStatic ? OpCodes.Call : OpCodes.Callvirt, m);

        return m.ReturnType;
    }

    /// <summary>
    /// 生成字段和属性访问的IL代码
    /// </summary>
    private static Type GenerateFieldOrPropertyAccess(
        LangExpression? left,
        LangId id,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Operation operation)
    {
        // 检查是否是枚举类型的静态成员访问
        if (leftType is not null && leftType.IsEnum)
        {
            // 枚举成员访问：直接加载枚举值（整数）
            var field = leftType.GetField(id.IdName);
            if (field is null)
            {
                throw new InvalidOperationError(operation, $"枚举 {leftType.Name} 没有成员 {id.IdName}");
            }

            // 获取枚举成员的值（整数）
            var enumValue = field.GetRawConstantValue();
            if (enumValue is int intValue)
            {
                // 直接加载整数常量
                ilGenerator.Emit(OpCodes.Ldc_I4, intValue);
            }
            else
            {
                throw new InvalidOperationError(operation, $"无法获取枚举成员 {id.IdName} 的值");
            }

            return leftType; // 返回枚举类型
        }

        // 普通实例成员访问
        left!.LoadIlValue(ilGenerator, local);

        // 特殊处理 ValueTuple 的 Length
        if (leftType!.FullName?.StartsWith("System.ValueTuple") == true && id.IdName == "Length")
        {
            // 弹出栈顶的 Tuple 实例 (因为我们不需要它来获取长度，如果是常量长度)
            ilGenerator.Emit(OpCodes.Pop);

            // 计算长度
            int length = GetValueTupleLength(leftType);
            ilGenerator.Emit(OpCodes.Ldc_I4, length);
            return typeof(int);
        }

        var instanceField = leftType!.GetField(id.IdName);
        if (instanceField is null)
        {
            var p = leftType.GetProperty(id.IdName);
            if (p is null)
            {
                throw new InvalidOperationError(operation, $"类型 {leftType.Name} 没有属性 {id.IdName}");
            }

            var getMethod = p.GetGetMethod();
            if (getMethod is null)
            {
                throw new InvalidOperationError(operation, $"属性 {id.IdName} 没有公开的 getter 方法");
            }

            ilGenerator.Emit(OpCodes.Call, getMethod);
            return p.PropertyType;
        }

        ilGenerator.Emit(OpCodes.Ldfld, instanceField);
        return instanceField.FieldType;
    }

    /// <summary>
    /// 生成索引访问的IL代码
    /// </summary>
    private static Type GenerateIndexAccess(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type rightType,
        Operation operation)
    {
        left!.LoadIlValue(ilGenerator, local);
        right!.LoadIlValue(ilGenerator, local);

        // 处理不同类型的索引访问
        if (leftType == typeof(object[]))
        {
            // 数组索引访问
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
            return typeof(object);
        }
        else if (leftType == typeof(List<object>))
        {
            // List<T>索引访问，调用索引器的getter方法
            var indexer = typeof(List<object>).GetProperty("Item")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
            return typeof(object);
        }
        else if (leftType == typeof(Dictionary<object, object>))
        {
            // Dictionary<TKey, TValue>索引访问，调用索引器的getter方法
            var indexer = typeof(Dictionary<object, object>).GetProperty("Item")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
            return typeof(object);
        }
        else if (leftType == typeof(string))
        {
            // 字符串索引访问
            var indexer = typeof(string).GetProperty("Chars")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
            return typeof(char);
        }
        else if (leftType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问
            if (right is IntLangValue intVal)
            {
                // 如果是常量索引，优化为字段访问
                return GenerateValueTupleItemAccess(ilGenerator, leftType, intVal.Value);
            }

            // 变量索引，装箱为 ITuple 并使用索引器
            ilGenerator.Emit(OpCodes.Box, leftType);
            var indexer = typeof(System.Runtime.CompilerServices.ITuple).GetProperty("Item")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
            return typeof(object);
        }
        else if (leftType == typeof(object))
        {
            // Object类型，可能是字典、列表或数组
            // 策略：先存储到局部变量，然后依次尝试类型转换
            return GenerateDynamicIndexAccess(ilGenerator, rightType);
        }

        // 默认情况，尝试装箱并调用索引器
        if (rightType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, rightType);
        }

        var defaultIndexer = leftType!.GetProperty("Item");
        if (defaultIndexer is null)
            throw new InvalidOperationError(operation, $"类型 '{leftType.Name}' 不支持索引访问");
        ilGenerator.Emit(OpCodes.Callvirt, defaultIndexer.GetGetMethod()!);
        return typeof(object);
    }

    /// <summary>
    /// 生成动态索引访问的IL代码（用于 object 类型）
    /// </summary>
    public static Type GenerateDynamicIndexAccess(ILGenerator ilGenerator, Type rightType)
    {
        // 栈上已经有: leftValue, rightValue
        // 先保存rightValue
        var rightLocal = ilGenerator.DeclareLocal(typeof(object));
        if (rightType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, rightType);
        }

        ilGenerator.Emit(OpCodes.Stloc, rightLocal);

        // leftValue仍在栈上，保存它
        var leftLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, leftLocal);

        var endLabel = ilGenerator.DefineLabel();
        var notDictLabel = ilGenerator.DefineLabel();
        var notListLabel = ilGenerator.DefineLabel();
        var notArrayLabel = ilGenerator.DefineLabel();
        var notTupleLabel = ilGenerator.DefineLabel();

        // 尝试Dictionary<object, object>
        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Brfalse, notDictLabel);

        // 是Dictionary
        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
        var dictIndexer = typeof(Dictionary<object, object>).GetProperty("Item")!;
        ilGenerator.Emit(OpCodes.Callvirt, dictIndexer.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 不是Dictionary，尝试List<object>
        ilGenerator.MarkLabel(notDictLabel);
        ilGenerator.Emit(OpCodes.Pop); // 弹出null
        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(List<object>));
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Brfalse, notListLabel);

        // 是List
        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
        var listIndexer = typeof(List<object>).GetProperty("Item")!;
        ilGenerator.Emit(OpCodes.Callvirt, listIndexer.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 不是List，尝试object[]
        ilGenerator.MarkLabel(notListLabel);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(object[]));
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Brfalse, notArrayLabel);

        // 是Array
        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
        ilGenerator.Emit(OpCodes.Ldelem_Ref);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 不是Array，尝试ITuple
        ilGenerator.MarkLabel(notArrayLabel);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
        ilGenerator.Emit(OpCodes.Isinst, typeof(System.Runtime.CompilerServices.ITuple));
        ilGenerator.Emit(OpCodes.Dup);
        ilGenerator.Emit(OpCodes.Brfalse, notTupleLabel);

        // 是Tuple
        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
        var tupleIndexer = typeof(System.Runtime.CompilerServices.ITuple).GetProperty("Item")!;
        ilGenerator.Emit(OpCodes.Callvirt, tupleIndexer.GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 都不匹配
        ilGenerator.MarkLabel(notTupleLabel);
        ilGenerator.Emit(OpCodes.Pop);
        ilGenerator.Emit(OpCodes.Ldnull); // 或者抛出异常

        ilGenerator.MarkLabel(endLabel);
        return typeof(object);
    }

    public static int GetValueTupleLength(Type type)
    {
        if (type.FullName?.StartsWith("System.ValueTuple") != true) return 0;
        var fields = type.GetFields();
        int count = fields.Length;
        // 如果最后一个字段是 Rest (ITuple)，则递归计算
        if (count == 8 && fields[7].Name == "Rest")
        {
            return 7 + GetValueTupleLength(fields[7].FieldType);
        }

        return count;
    }

    public static Type GenerateValueTupleItemAccess(ILGenerator ilGenerator, Type tupleType, int index)
    {
        // 索引越界检查应该在编译期还是运行期？这里假设调用者已经验证或由运行时异常处理
        // ValueTuple 的字段是 Item1, Item2...
        if (index < 7)
        {
            var field = tupleType.GetField($"Item{index + 1}");
            if (field == null)
            {
                // 可能是短元组，索引越界
                throw new InvalidOperationException($"无法访问 ValueTuple 的索引 {index}: 字段 Item{index + 1} 不存在");
            }

            ilGenerator.Emit(OpCodes.Ldfld, field);
            return field.FieldType;
        }
        else
        {
            var restField = tupleType.GetField("Rest");
            if (restField == null)
            {
                throw new InvalidOperationException($"无法访问 ValueTuple 的索引 {index}: Rest 字段不存在");
            }

            ilGenerator.Emit(OpCodes.Ldfld, restField);
            return GenerateValueTupleItemAccess(ilGenerator, restField.FieldType, index - 7);
        }
    }
}
