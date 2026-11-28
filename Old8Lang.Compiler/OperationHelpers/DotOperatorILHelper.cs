using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using System.Collections;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.InstanceMethods.Core;

using Old8Lang.AST.Visitor;

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
        if (left is LangId { IdName: "Thread" } && right is LangId { IdName: "CurrentThread" })
        {
            ilGenerator.Emit(OpCodes.Call, typeof(Thread).GetProperty(nameof(Thread.CurrentThread))!.GetMethod!);
            return typeof(Thread);
        }

        // 1. 处理 this.member 访问（类环境内）
        // 支持 LangId { IdName: "this" } 和 ThisExpression 两种形式
        if (local.InClassEnv is not null && right is LangId &&
            (left is LangId { IdName: "this" } || left is ThisExpression))
        {
            return GenerateThisMemberAccess(right, ilGenerator, local);
        }

        // 2. 模块别名：import "m" as tm 后，tm.member 展开为模块内联后的同名符号
        if (left is LangId aliasId && local.ModuleAliases.Contains(aliasId.IdName))
        {
            return GenerateModuleAliasMember(aliasId, right, ilGenerator, local);
        }

        // 3. 处理实例方法调用
        if (right is Instance instance)
        {
            return GenerateInstanceMethodCall(left, instance, ilGenerator, local, leftType, operation);
        }

        // 4. 处理字段和属性访问
        if (right is LangId id)
        {
            return GenerateFieldOrPropertyAccess(left, id, ilGenerator, local, leftType, operation);
        }

        // 5. 处理索引访问
        if (rightType is not null)
        {
            return GenerateIndexAccess(left, right, ilGenerator, local, leftType, rightType, operation);
        }

        return typeof(void);
    }

    /// <summary>
    /// 生成模块别名成员访问的 IL（IL 模式）：import "m" as tm 后，tm.member 等价于 member。
    /// 模块体已被 GenerateImportIl 内联到当前方法，因此直接把成员名当作普通符号编译。
    /// </summary>
    internal static Type GenerateModuleAliasMember(
        LangId aliasId,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        if (right is Instance instance)
        {
            var memberName = instance.Id.IdName;
            LangExpression call = instance.NamedArgs.Count > 0
                ? new FunctionCallExpression(new LangId(memberName), instance.Ids, instance.NamedArgs, aliasId.Position)
                : new FunctionCallExpression(new LangId(memberName), instance.Ids, aliasId.Position);
            call.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            return call.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        }

        if (right is LangId memberId)
        {
            var value = new LangId(memberId.IdName, position: memberId.Position);
            value.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            return value.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        }

        throw new InvalidOperationError(aliasId, $"不支持访问模块别名 '{aliasId.IdName}' 的成员");
    }

    /// <summary>
    /// 生成 this.member 访问的IL代码
    /// </summary>
    internal static Type GenerateThisMemberAccess(
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        new ThisExpression().Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        if (right is not LangId rightId) return local.InClassEnv!;

        // 优先从 FieldVar 中查找字段（支持 TypeBuilder）
        if (local.FieldVar.TryGetValue(rightId.IdName, out var fieldInfo))
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
            // DynamicMethod 中不能直接引用尚未完成 TypeBuilder 的 FieldBuilder。
            // 捕获 this 的 lambda 在类创建后运行，改由运行期反射读取该字段。
            if (fieldInfo is FieldBuilder && local.TryGetCapturedLocal("this", out _))
            {
                ilGenerator.Emit(OpCodes.Ldstr, rightId.IdName);
                ilGenerator.Emit(OpCodes.Call,
                    typeof(DotOperatorILHelper).GetMethod(nameof(GetDynamicObjectProperty))!);
                if (fieldInfo.FieldType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                }
                else if (fieldInfo.FieldType != typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Castclass, fieldInfo.FieldType);
                }

                return fieldInfo.FieldType;
            }

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
    internal static Type GenerateInstanceMethodCall(
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
            return GenerateTaskStaticMethodCall(instance, ilGenerator, local);
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

        // super.method(...) 必须以非虚调用直接绑定到父类实现。若按普通实例方法
        // 进行运行期分派，接收者仍是子类实例，重写的 init/method 会再次进入自身，
        // 例如 Child.init() 中的 super.init() 会无限递归。
        if (left is SuperExpression)
        {
            return GenerateSuperInstanceMethodCall(instance, ilGenerator, local, operation);
        }

        // Lock(value) 在 IL 中保留为 CLR 锁对象；其读取和写入必须经由
        // 原子桥接方法，不能走解释器实例方法注册表。
        if (leftType == typeof(LockedVariableLangValue))
        {
            return GenerateLockedVariableMethodCall(left!, instance, ilGenerator, local, operation);
        }

        // 在类定义尚未 CreateType 时，TypeBuilder 无法可靠地枚举当前类正在
        // 构造的方法。对 this 的调用先按签名从父类解析；使用 Callvirt 保留
        // 同签名重写的动态分派，也支持 this.init(...) 和受保护父类方法。
        if (left is ThisExpression or LangId { IdName: "this" } &&
            local.InClassEnv is TypeBuilder buildingType &&
            buildingType.BaseType is { } baseType && baseType != typeof(object))
        {
            var argumentTypes = instance.Ids
                .Select(argument => argument.Accept(new TypeInferenceVisitor(local)) ?? typeof(object))
                .ToArray();
            var baseMethod = baseType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(candidate => candidate.Name == instance.Id.IdName)
                .FirstOrDefault(candidate => ParametersMatch(candidate.GetParameters(), argumentTypes));
            if (baseMethod is not null)
            {
                new ThisExpression().Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
                foreach (var argument in instance.Ids)
                {
                    argument.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
                }

                ilGenerator.Emit(OpCodes.Callvirt, baseMethod);
                return baseMethod.ReturnType;
            }
        }

        // 用户定义类的静态方法需要直接从类型调用，不能把类名当作局部变量加载。
        if ((left is LangId || left is GenericInstanceExpression { IsFunctionCall: false }) && leftType is not null)
        {
            var argumentTypes = instance.Ids
                .Select(argument => argument.Accept(new TypeInferenceVisitor(local)) ?? typeof(object))
                .ToArray();
            var staticMethod = leftType
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(candidate => candidate.Name == instance.Id.IdName)
                .FirstOrDefault(candidate => ParametersMatch(candidate.GetParameters(), argumentTypes));
            if (staticMethod is not null)
            {
                foreach (var argument in instance.Ids)
                {
                    argument.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
                }

                ilGenerator.Emit(OpCodes.Call, staticMethod);
                return staticMethod.ReturnType;
            }
        }

        // 普通实例方法调用
        return GenerateRegularInstanceMethodCall(left, instance, ilGenerator, local, leftType, operation);
    }

    /// <summary>
    /// 生成直接调用父类实例方法的 IL，绕开 CLR 虚分派以实现语言层 <c>super</c> 语义。
    /// </summary>
    internal static Type GenerateSuperInstanceMethodCall(
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Operation operation)
    {
        var parentType = local.InClassEnv?.BaseType;
        if (parentType is null || parentType == typeof(object))
        {
            throw new InvalidOperationError(operation, "super 只能在具有父类的实例方法中使用");
        }

        var argumentTypes = instance.Ids
            .Select(argument => argument.Accept(new TypeInferenceVisitor(local)) ?? typeof(object))
            .ToArray();
        var method = parentType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(candidate => candidate.Name == instance.Id.IdName)
            .FirstOrDefault(candidate => ParametersMatch(candidate.GetParameters(), argumentTypes));
        if (method is null)
        {
            throw new InvalidOperationError(operation,
                $"父类 '{parentType.Name}' 中未找到方法 '{instance.Id.IdName}'");
        }

        ilGenerator.Emit(OpCodes.Ldarg_0);
        foreach (var argument in instance.Ids)
        {
            argument.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        }

        ilGenerator.Emit(OpCodes.Call, method);
        return method.ReturnType;
    }

    internal static bool ParametersMatch(ParameterInfo[] parameters, Type[] argumentTypes)
    {
        if (parameters.Length != argumentTypes.Length)
        {
            return false;
        }

        return parameters.Zip(argumentTypes).All(pair =>
            pair.First.ParameterType == pair.Second ||
            pair.First.ParameterType.IsAssignableFrom(pair.Second) ||
            pair.Second == typeof(object));
    }

    /// <summary>
    /// 生成 Assert 静态方法调用的IL代码
    /// </summary>
    internal static Type GenerateAssertMethodCall(
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
        var paramTypes = instance.Ids.Select(id => id.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToList();

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

                    // string 形参接受任意实参：运行期按语言的显示规则转换，
                    // 例如 Assert.Contains("Error:", 捕获到的异常)。
                    if (expectedType == typeof(string))
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
                instance.Ids[i].Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
                var idType = instance.Ids[i].Accept(new TypeInferenceVisitor(local));
                var paramType = parameters[i].ParameterType;

                // 如果参数类型是 object,且值是值类型,需要装箱
                if (paramType == typeof(object) && idType is not null && idType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, idType);
                }

                // 静态类型为 object 的实参可能是运行期语言值（如 Thread.Join 的结果）：
                // 先取出底层原生值，Assert.Equal("x", joinResult) 才能按字符串/数值比较。
                if (paramType == typeof(object) &&
                    (idType == typeof(object) || idType == typeof(LangValueType)))
                {
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.UnwrapDynamic))!);
                }

                // 如果参数类型是值类型,且栈上是 object,需要拆箱
                if (paramType.IsValueType && idType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, paramType);
                }

                // string 形参但实参不是 string：先装箱再按语言的显示规则转成字符串
                if (paramType == typeof(string) && idType != typeof(string))
                {
                    if (idType is { IsValueType: true })
                    {
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }

                    ilGenerator.Emit(OpCodes.Call,
                        typeof(Old8Lang.Runtime.VMValueFormatter).GetMethod(
                            nameof(Old8Lang.Runtime.VMValueFormatter.ToDisplayString))!);
                }
            }

            // 未显式传入的可选参数必须补上默认值：
            // C# 的可选参数在 IL 层面仍是完整形参（如 AssertHelper.AssertEqual 的 message），
            // 只压入实际实参会让 call 的栈深度不足，生成无效 IL。
            for (var i = instance.Ids.Count; i < parameters.Length; i++)
            {
                var optionalParamType = parameters[i].ParameterType;
                if (optionalParamType.IsValueType)
                {
                    var defaultLocal = ilGenerator.DeclareLocal(optionalParamType);
                    ilGenerator.Emit(OpCodes.Ldloca, defaultLocal);
                    ilGenerator.Emit(OpCodes.Initobj, optionalParamType);
                    ilGenerator.Emit(OpCodes.Ldloc, defaultLocal);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldnull);
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
    /// <remarks>
    /// 实参由各分支自己求值：不同静态方法的形参个数与类型不同，
    /// 在分派前统一压栈会让「不需要实参的分支」留下未消费的栈值
    /// （<c>Task.Run</c>/<c>Task.StartNew</c> 会多压一个值，
    /// <c>WhenAll</c>/<c>WhenAny</c> 则会把同一个实参压两遍），生成无效 IL。
    /// </remarks>
    internal static Type GenerateTaskStaticMethodCall(
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        // Task静态方法调用，如Task.Delay(100)
        var methodName = instance.Id.IdName;

        return methodName switch
        {
            "Delay" => GenerateTaskDelay(instance, ilGenerator, local),
            "FromResult" => GenerateTaskFromResult(instance, ilGenerator, local),
            "FromException" => GenerateTaskFromException(instance, ilGenerator, local),
            "Run" => GenerateTaskRun(instance, ilGenerator, local),
            "StartNew" => GenerateTaskStartNew(instance, ilGenerator, local),
            "WhenAll" => GenerateTaskWhenAll(instance, ilGenerator, local),
            "WhenAny" => GenerateTaskWhenAny(instance, ilGenerator, local),
            "WaitAll" => GenerateTaskWaitAll(instance, ilGenerator, local),
            "WaitAny" => GenerateTaskWaitAny(instance, ilGenerator, local),
            "Completed" => GenerateTaskCompleted(instance, ilGenerator, local),
            // 未知的 Task 静态方法不能「什么都不发射却声明返回 Task<object>」：
            // 那样调用方会按有一个返回值来用栈，生成无效 IL。
            _ => throw new InvalidOperationError(instance,
                $"Task 静态方法 '{methodName}' 未在 IL 模式实现",
                "可用方法：Delay、FromResult、FromException、Run、StartNew、WhenAll、WhenAny、WaitAll、WaitAny、Completed")
        };
    }

    /// <summary>
    /// 按顺序求值实参，返回每个实参的静态类型。
    /// </summary>
    private static List<Type> EmitTaskArguments(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        var paramTypes = new List<Type>();
        foreach (var instanceId in instance.Ids)
        {
            instanceId.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            paramTypes.Add(instanceId.Accept(new TypeInferenceVisitor(local)) ?? typeof(object));
        }

        return paramTypes;
    }

    /// <summary>
    /// 把栈顶实参转换成形参期望的类型；无法静态确定时保持原样。
    /// </summary>
    private static void EmitTaskArgumentConversion(ILGenerator ilGenerator, Type actualType, Type expectedType)
    {
        if (actualType == expectedType) return;

        if (expectedType == typeof(object))
        {
            if (actualType.IsValueType) ilGenerator.Emit(OpCodes.Box, actualType);
            return;
        }

        if (actualType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, expectedType);
            return;
        }

        if (expectedType == typeof(int) &&
            actualType is { IsValueType: true } &&
            actualType != typeof(bool) && actualType != typeof(char))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
            return;
        }

        if (expectedType == typeof(double) && actualType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Conv_R8);
        }
    }

    /// <summary>
    /// 生成 Task.Delay 的IL代码
    /// </summary>
    internal static Type GenerateTaskDelay(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        var paramTypes = EmitTaskArguments(instance, ilGenerator, local);

        if (paramTypes.Count == 1)
        {
            // 调用 Task.Delay(int)
            EmitTaskArgumentConversion(ilGenerator, paramTypes[0], typeof(int));
            ilGenerator.Emit(OpCodes.Call, typeof(Task).GetMethod("Delay", [typeof(int)])!);
            return typeof(Task);
        }

        if (paramTypes.Count == 2)
        {
            // 调用 Task.Delay(int, CancellationToken)
            EmitTaskArgumentConversion(ilGenerator, paramTypes[1], typeof(CancellationToken));
            EmitTaskArgumentConversion(ilGenerator, paramTypes[0], typeof(int));
            ilGenerator.Emit(OpCodes.Call, typeof(Task).GetMethod("Delay",
                [typeof(int), typeof(CancellationToken)])!);
            return typeof(Task);
        }

        // 参数个数不匹配：实参已经求值，先平衡栈再退化成已完成的 Task。
        for (var i = 0; i < paramTypes.Count; i++) ilGenerator.Emit(OpCodes.Pop);
        EmitCompletedTaskFromNull(ilGenerator);
        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.FromResult 的IL代码
    /// </summary>
    internal static Type GenerateTaskFromResult(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        var paramTypes = EmitTaskArguments(instance, ilGenerator, local);

        if (paramTypes.Count == 1)
        {
            // 统一实例化为 Task.FromResult<object>(object)：
            // 形参是 object，值类型实参必须先装箱。漏掉 box 会让栈上的 int
            // 直接当 object 传入，生成无效 IL。
            EmitTaskArgumentConversion(ilGenerator, paramTypes[0], typeof(object));

            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
            fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
            ilGenerator.Emit(OpCodes.Call, fromResultMethod);
        }
        else
        {
            for (var i = 0; i < paramTypes.Count; i++) ilGenerator.Emit(OpCodes.Pop);
            EmitCompletedTaskFromNull(ilGenerator);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.FromException 的IL代码
    /// </summary>
    internal static Type GenerateTaskFromException(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        var paramTypes = EmitTaskArguments(instance, ilGenerator, local);

        if (paramTypes.Count == 1)
        {
            // 先按解释器语义转成 Exception（允许传字符串），再调用 FromException<object>
            EmitTaskArgumentConversion(ilGenerator, paramTypes[0], typeof(object));

            ilGenerator.Emit(OpCodes.Call, typeof(TaskHelper).GetMethod(nameof(TaskHelper.ToException))!);

            var fromExceptionMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromException", IsGenericMethodDefinition: true });
            fromExceptionMethod = fromExceptionMethod.MakeGenericMethod(typeof(object));
            ilGenerator.Emit(OpCodes.Call, fromExceptionMethod);
        }
        else
        {
            for (var i = 0; i < paramTypes.Count; i++) ilGenerator.Emit(OpCodes.Pop);
            EmitCompletedTaskFromNull(ilGenerator);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 <c>Task.FromResult&lt;object&gt;(null)</c> 作为已完成任务。
    /// </summary>
    private static void EmitCompletedTaskFromNull(ILGenerator ilGenerator)
    {
        var fromResultMethod = typeof(Task)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
        fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
        ilGenerator.Emit(OpCodes.Ldnull);
        ilGenerator.Emit(OpCodes.Call, fromResultMethod);
    }

    /// <summary>
    /// 生成 Task.Run 的IL代码
    /// </summary>
    internal static Type GenerateTaskRun(Instance instance, ILGenerator ilGenerator, LocalManager local)
        => EmitTaskStartFunction(instance, ilGenerator, local);

    /// <summary>
    /// 生成 Task.StartNew 的IL代码
    /// </summary>
    internal static Type GenerateTaskStartNew(Instance instance, ILGenerator ilGenerator, LocalManager local)
        => EmitTaskStartFunction(instance, ilGenerator, local);

    /// <summary>
    /// Task.Run / Task.StartNew：在线程池上调用语言函数值，等待其返回的 Task 并取回结果。
    /// </summary>
    private static Type EmitTaskStartFunction(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count == 0)
        {
            EmitCompletedTaskFromNull(ilGenerator);
            return typeof(Task<object>);
        }

        // 第一个实参是函数值，其余实参收集成 object[] 交给运行期调用。
        instance.Ids[0].Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        ilGenerator.Emit(OpCodes.Ldc_I4, instance.Ids.Count - 1);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));

        for (var i = 1; i < instance.Ids.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i - 1);
            instance.Ids[i].Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var argumentType = instance.Ids[i].Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (argumentType.IsValueType) ilGenerator.Emit(OpCodes.Box, argumentType);
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        ilGenerator.Emit(OpCodes.Call, typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.RunAsync))!);
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

    private static Task[] ExtractRuntimeTasks(object tasks)
    {
        if (tasks is not IEnumerable enumerable)
        {
            throw new InvalidOperationError(new SourcePosition(), $"Task.WaitAll/WaitAny 期望任务集合，实际类型: {tasks?.GetType().Name ?? "null"}");
        }

        var taskList = new List<Task>();
        foreach (var item in enumerable)
        {
            switch (item)
            {
                case Task clrTask:
                    taskList.Add(clrTask);
                    break;
                case TaskLangValue langTask:
                    taskList.Add(langTask.Task);
                    break;
                default:
                    throw new InvalidOperationError(new SourcePosition(), $"Task.WaitAll/WaitAny 集合元素必须是 Task，实际类型: {item?.GetType().Name ?? "null"}");
            }
        }

        return taskList.ToArray();
    }

    /// <summary>
    /// 运行时辅助方法：Task.WaitAll
    /// </summary>
    public static void RuntimeWaitAll(object tasks)
    {
        var taskArray = ExtractRuntimeTasks(tasks);
        if (taskArray.Length == 0) return;

        try
        {
            Task.WaitAll(taskArray);
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerExceptions[0]).Throw();
        }
    }

    /// <summary>
    /// 运行时辅助方法：Task.WaitAny
    /// </summary>
    public static int RuntimeWaitAny(object tasks)
    {
        var taskArray = ExtractRuntimeTasks(tasks);
        if (taskArray.Length == 0) return -1;
        return Task.WaitAny(taskArray);
    }

    /// <summary>
    /// 生成 Task.Completed 的 IL 代码
    /// </summary>
    internal static Type GenerateTaskCompleted(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count != 0)
        {
            throw new ArgumentError(instance.Position, $"Completed 期望 0 个参数,但提供了 {instance.Ids.Count} 个");
        }

        var completedTaskGetter = typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Call, completedTaskGetter);
        return typeof(Task);
    }

    /// <summary>
    /// 生成 Task.WaitAll 的 IL 代码
    /// </summary>
    internal static Type GenerateTaskWaitAll(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count != 1)
        {
            throw new ArgumentError(instance.Position, $"WaitAll 期望 1 个参数(任务数组),但提供了 {instance.Ids.Count} 个");
        }

        var listExpr = instance.Ids[0];
        listExpr.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var listType = listExpr.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (listType.IsValueType) ilGenerator.Emit(OpCodes.Box, listType);

        var runtimeMethod = typeof(DotOperatorILHelper).GetMethod(nameof(RuntimeWaitAll))!;
        ilGenerator.Emit(OpCodes.Call, runtimeMethod);
        return typeof(void);
    }

    /// <summary>
    /// 生成 Task.WaitAny 的 IL 代码
    /// </summary>
    internal static Type GenerateTaskWaitAny(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count != 1)
        {
            throw new ArgumentError(instance.Position, $"WaitAny 期望 1 个参数(任务数组),但提供了 {instance.Ids.Count} 个");
        }

        var listExpr = instance.Ids[0];
        listExpr.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var listType = listExpr.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (listType.IsValueType) ilGenerator.Emit(OpCodes.Box, listType);

        var runtimeMethod = typeof(DotOperatorILHelper).GetMethod(nameof(RuntimeWaitAny))!;
        ilGenerator.Emit(OpCodes.Call, runtimeMethod);
        return typeof(int);
    }

    /// <summary>
    /// 生成 Task.WhenAll 的IL代码
    /// </summary>
    internal static Type GenerateTaskWhenAll(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count == 1)
        {
            // 加载列表/数组参数
            var listExpr = instance.Ids[0];
            listExpr.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

            // 获取列表/数组类型
            var listType = listExpr.Accept(new TypeInferenceVisitor(local));

            if (listType != null && (listType == typeof(object) || typeof(IEnumerable).IsAssignableFrom(listType)))
            {
                // 调用 RuntimeWhenAll（运行期按实际对象转成 IEnumerable）
                var runtimeMethod = typeof(DotOperatorILHelper).GetMethod(nameof(RuntimeWhenAll))!;
                ilGenerator.Emit(OpCodes.Call, runtimeMethod);
                return typeof(Task<object[]>);
            }

            // 其他类型，简化处理：先弹掉已求值的实参，再返回已完成任务
            ilGenerator.Emit(OpCodes.Pop);
            EmitCompletedTaskFromNull(ilGenerator);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成 Task.WhenAny 的IL代码
    /// </summary>
    internal static Type GenerateTaskWhenAny(Instance instance, ILGenerator ilGenerator, LocalManager local)
    {
        if (instance.Ids.Count == 1)
        {
            // 加载列表/数组参数
            var listExpr = instance.Ids[0];
            listExpr.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

            // 获取列表/数组类型
            var listType = listExpr.Accept(new TypeInferenceVisitor(local));

            if (listType != null && (listType == typeof(object) || typeof(IEnumerable).IsAssignableFrom(listType)))
            {
                // 调用 RuntimeWhenAny（运行期按实际对象转成 IEnumerable）
                var runtimeMethod = typeof(DotOperatorILHelper).GetMethod(nameof(RuntimeWhenAny))!;
                ilGenerator.Emit(OpCodes.Call, runtimeMethod);
                return typeof(Task<Task<object>>);
            }

            // 其他类型，简化处理：先弹掉已求值的实参，再返回已完成任务
            ilGenerator.Emit(OpCodes.Pop);
            EmitCompletedTaskFromNull(ilGenerator);
        }

        return typeof(Task<object>);
    }

    /// <summary>
    /// 生成普通实例方法调用的IL代码
    /// </summary>
    internal static Type GenerateRegularInstanceMethodCall(
        LangExpression? left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Operation operation)
    {
        // 历史语言用法允许 number.Sqrt()；CLR 只提供 Math.Sqrt(double)。
        if (instance.Id.IdName == "Sqrt" && instance.Ids.Count == 0 &&
            leftType is not null && IsNumericType(leftType))
        {
            left!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            if (leftType != typeof(double))
            {
                ilGenerator.Emit(OpCodes.Conv_R8);
            }

            // 与 Math.Sqrt(...) 同一语义：负数抛「平方根函数的参数必须是非负数」，
            // 不能直接 Math.Sqrt 返回 NaN。
            ilGenerator.Emit(OpCodes.Call,
                typeof(IlNumericHelper).GetMethod(nameof(IlNumericHelper.Sqrt))!);
            return typeof(double);
        }

        // 集合索引和未标注的类字段在 IL 中可能只能得到 object。
        // 这时不能在编译期用 object 查找成员；保留接收者和实参，在运行期按实际 CLR 类型分派。
        if (leftType == typeof(object))
        {
            return GenerateDynamicObjectMethodCall(left!, instance, ilGenerator, local);
        }

        // 类体内部对自身方法的调用（this.method()）：
        // 优先从 LocalManager 预登记的 MethodBuilder（及已完成的父类方法）直接发射 callvirt/call，
        // 避免反射分派开销；若位于 DynamicMethod 闭包/异步状态机或目标为函数值字段，则安全回退动态分派。
        if (leftType is TypeBuilder buildingType)
        {
            if (TryEmitBuildingClassMethodCall(left!, instance, ilGenerator, local, buildingType, operation,
                    out var directReturnType))
            {
                return directReturnType;
            }

            return GenerateDynamicObjectMethodCall(left!, instance, ilGenerator, local);
        }

        // 首先尝试从实例方法注册表中查找
        // 注意：IL 代码生成不经过解释器的实例方法分派，某些程序（例如只用到 Thread 的
        // 原生 Dot 分派、从没触发过解释器实例方法查找的程序）不会初始化注册表，
        // 这里必须显式确保初始化，否则会查不到任何实例方法。
        var instanceMethod = InstanceMethodTypeResolver.ResolveInstanceMethod(leftType!, instance, local, out var mappedType);

        if (instanceMethod != null)
        {
            // 使用实例方法系统生成 IL
            instanceMethod.GenerateIl(left!, instance.Ids, ilGenerator, local, operation.Position);
            var returnReceiverType = instanceMethod is ExtensionMethodWrapper ? leftType! : mappedType;
            return instanceMethod.GetReturnType(returnReceiverType, instance.Ids, local);
        }

        // 注册表里有对应语言方法、但该方法只有解释器实现（未实现 IIlNativeValueInstanceMethod）时，
        // 在运行期桥接回解释器实现，避免原生 List<T> / T[] / Dictionary<K,V> 接收者报「方法未找到」。
        if (leftType is not null &&
            TryEmitIlBridgeInstanceMethodCall(left!, instance, ilGenerator, local, leftType, out var bridgeReturnType))
        {
            return bridgeReturnType;
        }

        // 带命名实参的实例方法调用：按 CLR 方法形参名重排实参后再发射调用。
        if (leftType is not null && instance.NamedArgs is { Count: > 0 })
        {
            return GenerateNamedArgumentInstanceMethodCall(left!, instance, ilGenerator, local, leftType, operation);
        }

        // 如果实例方法注册表中没有找到，继续使用原有的逻辑
        left!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var types = new List<Type>();
        foreach (var instanceId in instance.Ids)
        {
            instanceId.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var idType = instanceId.Accept(new TypeInferenceVisitor(local));
            // 不装箱，保持原始类型
            types.Add(idType!);
        }

        // 特殊处理Old8Lang的ToStr()方法
        // ToStr()是Old8Lang的扩展方法，在 IL 模式下将其映射到.NET的ToString()
        if (instance.Id.IdName == "ToStr" && instance.Ids.Count == 0)
        {
            // 调用ToString()方法
            var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;

            // 如果左侧是值类型，需要先装箱
            if (leftType!.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, leftType);
            }

            // 空引用的 ToStr() 与解释器一致返回 "null"，不再抛 NRE。
            var nonNullLabel = ilGenerator.DefineLabel();
            var doneLabel = ilGenerator.DefineLabel();
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Brtrue, nonNullLabel);
            ilGenerator.Emit(OpCodes.Pop);
            ilGenerator.Emit(OpCodes.Ldstr, "null");
            ilGenerator.Emit(OpCodes.Br, doneLabel);
            ilGenerator.MarkLabel(nonNullLabel);
            ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
            ilGenerator.MarkLabel(doneLabel);
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

            // 其它实现 ICollection 的集合（字典等）同样用 Count 属性
            if (typeof(ICollection).IsAssignableFrom(leftType) && !leftType.IsValueType)
            {
                var collectionCount = typeof(ICollection).GetProperty(nameof(ICollection.Count))!.GetGetMethod()!;
                ilGenerator.Emit(OpCodes.Callvirt, collectionCount);
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
        // 如果没有找到精确匹配，尝试查找参数数量匹配的方法
        var m = leftType!.GetMethod(instance.Id.IdName, [.. types]) ?? leftType.GetMethods()
            .FirstOrDefault(method =>
                method.Name == instance.Id.IdName &&
                method.GetParameters().Length == instance.Ids.Count);

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
                // 如果没有找到精确匹配，尝试查找参数数量匹配的扩展方法
                m = extensionType.GetMethod(instance.Id.IdName, extensionTypes) ?? extensionType.GetMethods()
                    .FirstOrDefault(method =>
                        method.Name == instance.Id.IdName &&
                        method.GetParameters().Length == instance.Ids.Count + 1); // +1 因为扩展方法有 this 参数

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
                // 函数值字段当方法调用（如 `this.validator(x)` 或 `obj.callback(x)`，常见于泛型类特化或 object/function 字段）
                var callableField = leftType.GetField(
                    instance.Id.IdName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (callableField is not null &&
                    (callableField.FieldType == typeof(object) ||
                     typeof(Delegate).IsAssignableFrom(callableField.FieldType) ||
                     typeof(FuncLangValue).IsAssignableFrom(callableField.FieldType) ||
                     typeof(AsyncFuncLangValue).IsAssignableFrom(callableField.FieldType)))
                {
                    // 当前栈：[left, arg0, arg1, ..., argN-1]
                    var argLocals = new LocalBuilder[types.Count];
                    for (int i = types.Count - 1; i >= 0; i--)
                    {
                        argLocals[i] = ilGenerator.DeclareLocal(types[i]);
                        ilGenerator.Emit(OpCodes.Stloc, argLocals[i]);
                    }

                    ilGenerator.Emit(OpCodes.Ldfld, callableField);
                    if (callableField.FieldType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, callableField.FieldType);
                    }

                    ilGenerator.Emit(OpCodes.Ldc_I4, types.Count);
                    ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                    for (int i = 0; i < types.Count; i++)
                    {
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Ldc_I4, i);
                        ilGenerator.Emit(OpCodes.Ldloc, argLocals[i]);
                        if (types[i].IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, types[i]);
                        }
                        ilGenerator.Emit(OpCodes.Stelem_Ref);
                    }

                    ilGenerator.Emit(OpCodes.Call,
                        typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.InvokeFunctionValue))!);
                    return typeof(object);
                }

                // 方法未找到，抛出异常
                throw new InvalidOperationError(operation, $"方法 '{instance.Id.IdName}' 未找到",
                    $"无法在类型 '{leftType.Name}' 中找到方法 '{instance.Id.IdName}'，参数类型为: {string.Join(", ", types.Select(t => t.Name))}");
            }
        }

        // 实参在查找方法前已按「原始类型」压栈，但形参可能是 object（未标注类型的参数）
        // 或更窄的数值类型。直接 callvirt 会生成无效 IL（把未装箱的 int 当 object 传），
        // 因此需要时把实参落到局部变量，再按形参类型重新加载。
        EmitInstanceArgumentConversions(ilGenerator, m, types);

        // 对于实例方法使用 Callvirt，对于静态方法（包括扩展方法）使用 Call
        ilGenerator.Emit(m.IsStatic ? OpCodes.Call : OpCodes.Callvirt, m);

        return m.ReturnType;
    }

    /// <summary>
    /// 在当前正在构建的类（<see cref="TypeBuilder"/>）内尝试直接发射 <c>callvirt</c> / <c>call</c>
    /// 调用预登记的 <see cref="MethodBuilder"/> 或已完成基类的 <see cref="MethodInfo"/>。
    /// </summary>
    private static bool TryEmitBuildingClassMethodCall(
        LangExpression left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        TypeBuilder buildingType,
        Operation operation,
        out Type returnType)
    {
        returnType = typeof(object);

        // DynamicMethod（闭包）与异步状态机（独立动态程序集）无法直接引用未完成的 MethodBuilder。
        if (local.IsInDynamicMethod || local.AsyncStateMachineGenerator is not null || local.InClassEnv is not TypeBuilder)
        {
            return false;
        }

        var namedArgs = instance.NamedArgs;
        var hasNamed = namedArgs is { Count: > 0 };
        var namedNames = hasNamed ? namedArgs!.Select(na => na.Name).ToArray() : null;
        var positionalTypes = hasNamed
            ? null
            : instance.Ids.Select(id => id.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToArray();

        if (!local.TryResolveBuildingMethod(
                buildingType,
                instance.Id.IdName,
                instance.Ids.Count,
                namedNames,
                positionalTypes,
                out var pendingMethod,
                out var completedBaseMethod))
        {
            return false;
        }

        MethodInfo targetMethod;
        Type[] paramTypes;
        string[] paramNames;
        bool isStatic;

        if (pendingMethod is not null)
        {
            targetMethod = pendingMethod.Method;
            paramTypes = pendingMethod.ParameterTypes;
            paramNames = pendingMethod.ParameterNames;
            returnType = pendingMethod.ReturnType;
            isStatic = pendingMethod.IsStatic;
        }
        else if (completedBaseMethod is not null)
        {
            var ps = completedBaseMethod.GetParameters();
            targetMethod = completedBaseMethod;
            paramTypes = ps.Select(p => p.ParameterType).ToArray();
            paramNames = ps.Select(p => p.Name ?? string.Empty).ToArray();
            returnType = completedBaseMethod.ReturnType;
            isStatic = completedBaseMethod.IsStatic;
        }
        else
        {
            return false;
        }

        // 若静态方法通过非无副作用表达式调用，退回动态路径以免丢失左侧求值。
        if (isStatic && left is not (ThisExpression or LangId))
        {
            return false;
        }

        LangExpression[] orderedArgs;
        if (hasNamed)
        {
            var reordered = new LangExpression?[paramTypes.Length];
            for (var i = 0; i < instance.Ids.Count; i++)
            {
                reordered[i] = instance.Ids[i];
            }

            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var namedArg in namedArgs!)
            {
                if (!seenNames.Add(namedArg.Name))
                {
                    throw new ArgumentError(namedArg.Position,
                        $"方法 '{instance.Id.IdName}' 的命名参数 '{namedArg.Name}' 重复");
                }

                var paramIndex = Array.FindIndex(paramNames, name => name == namedArg.Name);
                if (paramIndex < 0)
                {
                    throw new ArgumentError(namedArg.Position,
                        $"方法 '{instance.Id.IdName}' 没有名为 '{namedArg.Name}' 的参数");
                }

                if (reordered[paramIndex] is not null)
                {
                    throw new ArgumentError(namedArg.Position,
                        $"方法 '{instance.Id.IdName}' 的参数 '{namedArg.Name}' 已由位置参数提供");
                }

                reordered[paramIndex] = namedArg.Value;
            }

            for (var i = 0; i < paramTypes.Length; i++)
            {
                if (reordered[i] is null)
                {
                    throw new ArgumentError(operation.Position,
                        $"方法 '{instance.Id.IdName}' 缺少必需参数 '{(string.IsNullOrEmpty(paramNames[i]) ? $"arg{i}" : paramNames[i])}'");
                }
            }

            orderedArgs = reordered.Select(expr => expr!).ToArray();
        }
        else
        {
            orderedArgs = instance.Ids.ToArray();
        }

        if (!isStatic)
        {
            left.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        }

        for (var i = 0; i < orderedArgs.Length; i++)
        {
            orderedArgs[i].Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var actualType = orderedArgs[i].Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (NeedConversion(actualType, paramTypes[i]))
            {
                EmitArgumentConversion(ilGenerator, actualType, paramTypes[i]);
            }
        }

        ilGenerator.Emit(isStatic ? OpCodes.Call : OpCodes.Callvirt, targetMethod);
        return true;
    }

    /// <summary>
    /// 处理带命名实参的普通实例方法调用（如 <c>calc.compute(op: "add", y: 15, x: 25)</c>）。
    /// </summary>
    private static Type GenerateNamedArgumentInstanceMethodCall(
        LangExpression left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Type leftType,
        Operation operation)
    {
        var namedArgs = instance.NamedArgs!;
        var totalArgs = instance.Ids.Count + namedArgs.Count;

        // 先按名字与参数总数找候选方法；若有重载，优先所有命名参数名都匹配的那个。
        var candidates = leftType.GetMethods()
            .Where(method => method.Name == instance.Id.IdName)
            .ToList();

        var m = candidates.FirstOrDefault(method =>
                {
                    var ps = method.GetParameters();
                    return ps.Length == totalArgs &&
                           namedArgs.All(na => ps.Any(p => p.Name == na.Name));
                })
                ?? candidates.FirstOrDefault(method => method.GetParameters().Length == totalArgs);

        if (m is null)
        {
            throw new InvalidOperationError(operation, $"方法 '{instance.Id.IdName}' 未找到",
                $"无法在类型 '{leftType.Name}' 中找到接受 {totalArgs} 个参数的方法 '{instance.Id.IdName}'");
        }

        var parameters = m.GetParameters();
        var reordered = new LangExpression?[parameters.Length];
        for (var i = 0; i < instance.Ids.Count; i++)
        {
            reordered[i] = instance.Ids[i];
        }

        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var namedArg in namedArgs)
        {
            if (!seenNames.Add(namedArg.Name))
            {
                throw new ArgumentError(namedArg.Position,
                    $"方法 '{instance.Id.IdName}' 的命名参数 '{namedArg.Name}' 重复");
            }

            var paramIndex = Array.FindIndex(parameters, p => p.Name == namedArg.Name);
            if (paramIndex < 0)
            {
                throw new ArgumentError(namedArg.Position,
                    $"方法 '{instance.Id.IdName}' 没有名为 '{namedArg.Name}' 的参数");
            }

            if (reordered[paramIndex] is not null)
            {
                throw new ArgumentError(namedArg.Position,
                    $"方法 '{instance.Id.IdName}' 的参数 '{namedArg.Name}' 已由位置参数提供");
            }

            reordered[paramIndex] = namedArg.Value;
        }

        for (var i = 0; i < parameters.Length; i++)
        {
            if (reordered[i] is null)
            {
                throw new ArgumentError(operation.Position,
                    $"方法 '{instance.Id.IdName}' 缺少必需参数 '{parameters[i].Name ?? $"arg{i}"}'");
            }
        }

        left.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var argTypes = new List<Type>(parameters.Length);
        foreach (var expr in reordered)
        {
            expr!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var argType = expr.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            argTypes.Add(argType);
        }

        EmitInstanceArgumentConversions(ilGenerator, m, argTypes);
        ilGenerator.Emit(m.IsStatic ? OpCodes.Call : OpCodes.Callvirt, m);
        return m.ReturnType;
    }

    /// <summary>
    /// 按目标方法形参类型转换已压栈的实参。
    /// </summary>
    /// <remarks>
    /// 实参先在调用点按原始类型求值（栈上已经有接收者），这里把它们按逆序落到局部变量，
    /// 再按形参类型逐个装箱/拆箱/数值转换后重新压栈；不需要转换时保持原样。
    /// </remarks>
    private static void EmitInstanceArgumentConversions(
        ILGenerator ilGenerator,
        MethodInfo method,
        List<Type> argumentTypes)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != argumentTypes.Count) return;

        var conversions = new (Type From, Type To)[parameters.Length];
        var anyNeeded = false;
        for (var i = 0; i < parameters.Length; i++)
        {
            var from = argumentTypes[i];
            var to = parameters[i].ParameterType;
            conversions[i] = (from, to);
            if (NeedConversion(from, to)) anyNeeded = true;
        }

        if (!anyNeeded) return;

        var locals = new LocalBuilder[argumentTypes.Count];
        for (var i = argumentTypes.Count - 1; i >= 0; i--)
        {
            locals[i] = ilGenerator.DeclareLocal(argumentTypes[i]);
            ilGenerator.Emit(OpCodes.Stloc, locals[i]);
        }

        for (var i = 0; i < argumentTypes.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Ldloc, locals[i]);
            EmitArgumentConversion(ilGenerator, conversions[i].From, conversions[i].To);
        }
    }

    private static bool NeedConversion(Type from, Type to)
    {
        if (from == to) return false;
        if (to == typeof(object)) return from.IsValueType;
        if (to.IsValueType) return from == typeof(object) || IsNumericType(from) || IsNumericType(to);

        // 引用类型之间不做 castclass：`{1, 2, 3}` 的静态类型是 List<object>，
        // 而形参可能写成 list<int>；强行 castclass 会在运行期抛 InvalidCastException，
        // 而这类「集合字面量 vs 具体泛型」的宽松匹配是既有行为，保持原样。
        return false;
    }

    private static void EmitArgumentConversion(ILGenerator ilGenerator, Type from, Type to)
    {
        if (from == to) return;

        if (to == typeof(object))
        {
            if (from.IsValueType) ilGenerator.Emit(OpCodes.Box, from);
            return;
        }

        if (to.IsValueType)
        {
            if (from == typeof(object))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, to);
                return;
            }

            if (IsNumericType(from) && IsNumericType(to))
            {
                if (to == typeof(double)) ilGenerator.Emit(OpCodes.Conv_R8);
                else if (to == typeof(float)) ilGenerator.Emit(OpCodes.Conv_R4);
                else if (to == typeof(long)) ilGenerator.Emit(OpCodes.Conv_I8);
                else if (to == typeof(short)) ilGenerator.Emit(OpCodes.Conv_I2);
                else if (to == typeof(byte)) ilGenerator.Emit(OpCodes.Conv_U1);
                else if (to == typeof(sbyte)) ilGenerator.Emit(OpCodes.Conv_I1);
                else if (to == typeof(ushort)) ilGenerator.Emit(OpCodes.Conv_U2);
                else if (to == typeof(uint)) ilGenerator.Emit(OpCodes.Conv_U4);
                else if (to == typeof(ulong)) ilGenerator.Emit(OpCodes.Conv_U8);
                else ilGenerator.Emit(OpCodes.Conv_I4);
                return;
            }

            return;
        }

    }

    /// <summary>
    /// 为「原生集合接收者 + 只有解释器实现的实例方法」发射运行期桥接调用。
    /// </summary>
    /// <remarks>
    /// 方法解析、实参包装与结果还原都在
    /// <see cref="IlNativeCollectionBridge"/> 中完成；这里只负责按解析出的
    /// 声明返回类型决定 <c>pop</c> / <c>unbox.any</c> / <c>castclass</c>，
    /// 并与 <see cref="InstanceMethodTypeResolver.InferIlBridgeReturnType"/>
    /// （类型推断）保持同一映射。
    /// </remarks>
    internal static bool TryEmitIlBridgeInstanceMethodCall(
        LangExpression left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Type leftType,
        out Type returnType)
    {
        returnType = typeof(object);
        var method = InstanceMethodTypeResolver.ResolveIlBridgeMethod(leftType, instance, local, out var mappedType);
        if (method is null)
        {
            return false;
        }

        Type declaredType;
        try
        {
            declaredType = method.GetReturnType(mappedType, instance.Ids, local);
        }
        catch
        {
            declaredType = typeof(object);
        }

        returnType = InstanceMethodTypeResolver.MapIlBridgeReturnType(declaredType);

        left.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        if (leftType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, leftType);
        }

        ilGenerator.Emit(OpCodes.Ldstr, instance.Id.IdName);
        ilGenerator.Emit(OpCodes.Ldc_I4, instance.Ids.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));
        for (var index = 0; index < instance.Ids.Count; index++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, index);
            IlValueBridge.EmitLoadWrapped(instance.Ids[index], ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        // 把编译期解析出的具体方法类型传给运行期：同一方法名可能有多个重载
        // （例如 First() / First(predicate)），仅凭参数个数在运行期重新解析会选错。
        ilGenerator.Emit(OpCodes.Ldtoken, method.GetType());
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);
        ilGenerator.Emit(OpCodes.Call,
            typeof(IlNativeCollectionBridge).GetMethod(nameof(IlNativeCollectionBridge.Invoke))!);

        if (returnType == typeof(void))
        {
            ilGenerator.Emit(OpCodes.Pop);
        }
        else if (returnType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, returnType);
        }
        else if (returnType != typeof(object))
        {
            ilGenerator.Emit(OpCodes.Castclass, returnType);
        }

        return true;
    }

    internal static Type GenerateLockedVariableMethodCall(
        LangExpression left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local,
        Operation operation)
    {
        var methodName = instance.Id.IdName;
        var method = methodName switch
        {
            "Set" when instance.Ids.Count == 1 => typeof(LockedVariableLangValue)
                .GetMethod(nameof(LockedVariableLangValue.SetIlValue)),
            "Get" when instance.Ids.Count == 0 => typeof(LockedVariableLangValue)
                .GetMethod(nameof(LockedVariableLangValue.GetIlValue)),
            "Add" when instance.Ids.Count == 1 => typeof(LockedVariableLangValue)
                .GetMethod(nameof(LockedVariableLangValue.AddIlValue)),
            _ => null
        };

        if (method is null)
        {
            throw new InvalidOperationError(operation,
                $"锁定变量不支持方法 '{methodName}'");
        }

        left.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        foreach (var argument in instance.Ids)
        {
            argument.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var argumentType = argument.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (argumentType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, argumentType);
            }
        }

        ilGenerator.Emit(OpCodes.Callvirt, method);
        return method.ReturnType;
    }

    internal static bool IsNumericType(Type type) => type == typeof(int) || type == typeof(double) ||
                                                     type == typeof(float) || type == typeof(long) ||
                                                     type == typeof(short) || type == typeof(byte);

    /// <summary>
    /// 为静态类型未知的接收者生成运行期实例方法调用。
    /// </summary>
    internal static Type GenerateDynamicObjectMethodCall(
        LangExpression left,
        Instance instance,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        left.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        var receiverType = left.Accept(new TypeInferenceVisitor(local));
        if (receiverType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, receiverType);
        }

        ilGenerator.Emit(OpCodes.Ldstr, instance.Id.IdName);
        ilGenerator.Emit(OpCodes.Ldc_I4, instance.Ids.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));

        for (var index = 0; index < instance.Ids.Count; index++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, index);
            instance.Ids[index].Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            var argumentType = instance.Ids[index].Accept(new TypeInferenceVisitor(local));
            if (argumentType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, argumentType);
            }

            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        ilGenerator.Emit(OpCodes.Call,
            typeof(DotOperatorILHelper).GetMethod(nameof(InvokeDynamicObjectMethod))!);
        return typeof(object);
    }

    /// <summary>
    /// 使用运行期 CLR 类型调用对象方法，供静态类型为 <see cref="object"/> 的链式访问使用。
    /// </summary>
    public static object? InvokeDynamicObjectMethod(object? receiver, string methodName, object?[] arguments)
    {
        if (receiver is null)
        {
            // 与解释器/VM 一致：空引用的 ToStr() 返回 "null"。
            if (methodName == "ToStr" && arguments.Length == 0)
            {
                return "null";
            }

            throw new NullReferenceException($"无法在 null 上调用方法 '{methodName}'");
        }

        if (methodName == "ToStr" && arguments.Length == 0)
        {
            return Old8Lang.Runtime.VMValueFormatter.ToDisplayString(receiver);
        }

        if (receiver is IDictionary dictionary &&
            methodName.Equals("GetOrElse", StringComparison.OrdinalIgnoreCase) && arguments.Length == 2)
        {
            return dictionary.Contains(arguments[0]!) ? dictionary[arguments[0]!] : arguments[1];
        }

        var type = receiver.GetType();
        if (arguments.Length == 0 && methodName is "Count" or "Length")
        {
            var property = type.GetProperty(methodName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanRead == true)
            {
                return property.GetValue(receiver);
            }

            // Old8Lang 的集合同时允许 Count()/Length()；CLR List<T> 仅公开 Count。
            if (receiver is ICollection collection)
            {
                return collection.Count;
            }
        }

        // 字典索引后的集合静态类型为 object；数组本身没有 Old8Lang 的 Reduce CLR 方法。
        // 在运行期重新桥接为语言值，复用已验证的 Reduce helper。兼容历史的
        // Reduce(initialValue, reducer) 调用形式，同时保留 API 文档的 reducer-first 形式。
        if (methodName.Equals("Reduce", StringComparison.OrdinalIgnoreCase) && arguments.Length == 2 &&
            receiver is IEnumerable)
        {
            var collection = IlValueBridge.Wrap(receiver);
            var first = IlValueBridge.Wrap(arguments[0]);
            var second = IlValueBridge.Wrap(arguments[1]);
            if (first is FuncLangValue)
            {
                return Old8Lang.InstanceMethods.Implementations.Generic.LangListReduceMethod
                    .ReduceWithInitialHelper(collection, first, second);
            }

            if (second is FuncLangValue)
            {
                return Old8Lang.InstanceMethods.Implementations.Generic.LangListReduceMethod
                    .ReduceWithInitialHelper(collection, second, first);
            }
        }

        // 静态类型为 object 的运行期值是语言值（如 Thread.Join 返回的结果）：
        // 用解释器的实例方法表分派，保证 list.Count() / list.Add() 等与解释器一致。
        if (receiver is LangValueType languageReceiver)
        {
            var wrappedArguments = arguments
                .Select(argument => (Old8Lang.AST.LangExpression)IlValueBridge.Wrap(argument))
                .ToList();
            var instanceMethod = Old8Lang.InstanceMethods.Core.InstanceMethodRegistry.Instance
                .ResolveMethod(languageReceiver.GetType(), methodName, wrappedArguments, local: null);

            if (instanceMethod is not null)
            {
                var manager = Old8Lang.Compiler.RuntimeGlobalWriter.CurrentManager ??
                              new Old8Lang.Interpreter.VariateManager();
                var result = instanceMethod.Execute(languageReceiver, wrappedArguments, manager, new SourcePosition());
                return IlValueBridge.Unwrap(result);
            }
        }

        // 字典索引或动态变量得到的集合静态类型是 object；运行期再判定它是否为原生集合，
        // 是则复用语言实例方法桥接，保证与解释器一致的方法集合（Chunk / GroupBy / Permutations 等）。
        if ((receiver is Array || receiver is IDictionary || receiver is IList) &&
            IlNativeCollectionBridge.TryInvoke(receiver, methodName, arguments, methodType: null,
                out var languageResult))
        {
            return languageResult;
        }

        var method = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(candidate => candidate.Name == methodName && ParametersMatch(candidate.GetParameters(), arguments))
            .OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();

        // 类体内的 this.method() 走这里分派：语言层的 private 方法在 CLR 上是私有方法，
        // 只在公有方法里找不到时再退到非公有成员，避免误绑到基类的内部方法。
        method ??= type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(candidate => candidate.Name == methodName && ParametersMatch(candidate.GetParameters(), arguments))
            .OrderBy(candidate => candidate.GetParameters().Length)
            .FirstOrDefault();

        if (method is null)
        {
            // 类体内（含泛型类特化）通过 `this.field(args)` 调用函数值字段
            var field = type.GetField(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field is not null)
            {
                var fieldValue = field.GetValue(receiver);
                if (fieldValue is Delegate or FuncLangValue or AsyncFuncLangValue)
                {
                    return IlValueBridge.InvokeFunctionValue(fieldValue, arguments);
                }
            }

            var property = type.GetProperty(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property?.CanRead == true)
            {
                var propValue = property.GetValue(receiver);
                if (propValue is Delegate or FuncLangValue or AsyncFuncLangValue)
                {
                    return IlValueBridge.InvokeFunctionValue(propValue, arguments);
                }
            }

            throw new MissingMethodException(type.FullName, methodName);
        }

        try
        {
            return method.Invoke(receiver, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    internal static bool ParametersMatch(ParameterInfo[] parameters, object?[] arguments)
    {
        if (parameters.Length != arguments.Length)
        {
            return false;
        }

        for (var index = 0; index < parameters.Length; index++)
        {
            var argument = arguments[index];
            var parameterType = parameters[index].ParameterType;
            if (argument is null)
            {
                if (parameterType.IsValueType && Nullable.GetUnderlyingType(parameterType) is null)
                {
                    return false;
                }

                continue;
            }

            if (!parameterType.IsInstanceOfType(argument))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 生成字段和属性访问的IL代码
    /// </summary>
    internal static Type GenerateFieldOrPropertyAccess(
        LangExpression? left,
        LangId id,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Operation operation)
    {
        if (leftType == typeof(LockedVariableLangValue) && id.IdName == "Value")
        {
            left!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
            ilGenerator.Emit(OpCodes.Callvirt,
                typeof(LockedVariableLangValue).GetMethod(nameof(LockedVariableLangValue.GetIlValue))!);
            return typeof(object);
        }

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

            // 成员在 IL 里是 int32 常量；返回枚举类型会让调用方按 Color 装箱，
            // 显示成 "Red"（CLR enum ToString），match/ToStr/算术也随之错位。
            return typeof(int);
        }

        // 用户定义类的静态字段/属性访问。这里不能先加载 left：类名仅用于
        // 类型解析，在运行期没有对应的局部变量。
        if (left is GenericInstanceExpression { IsFunctionCall: false } &&
            local.FieldVar.TryGetValue(id.IdName, out var genericStaticField) && genericStaticField.IsStatic)
        {
            ilGenerator.Emit(OpCodes.Ldsfld, genericStaticField);
            return genericStaticField.FieldType;
        }

        if ((left is LangId || left is GenericInstanceExpression { IsFunctionCall: false }) && leftType is not null)
        {
            // TypeBuilder 在 CreateType 前不支持 GetField；当前类的字段已经由
            // ClassInit 放进 FieldVar，可直接复用 FieldBuilder。
            FieldInfo? staticField = leftType is TypeBuilder && leftType == local.InClassEnv
                ? local.FieldVar.GetValueOrDefault(id.IdName)
                : leftType.GetField(id.IdName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (staticField is not null && !staticField.IsStatic)
            {
                staticField = null;
            }
            if (staticField is not null)
            {
                ilGenerator.Emit(OpCodes.Ldsfld, staticField);
                return staticField.FieldType;
            }

            // 未完成的 TypeBuilder 不支持反射查询属性；该路径随后会按实例字段
            // 的运行期反射回退处理。
            if (leftType is not TypeBuilder)
            {
                var staticProperty = leftType.GetProperty(id.IdName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                var staticGetter = staticProperty?.GetGetMethod();
                if (staticGetter is not null && staticGetter.IsStatic)
                {
                    ilGenerator.Emit(OpCodes.Call, staticGetter);
                    return staticProperty!.PropertyType;
                }
            }
        }

        // 普通实例成员访问
        left!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        // 泛型类特化尚未完成时，参数类型可能是当前的 TypeBuilder（例如
        // ComparableBox<T>.compare(other: ComparableBox<T>)）。DynamicMethod
        // 不能可靠地把该未完成类型的 FieldBuilder 直接作为 ldfld 操作数；
        // 运行期通过反射读取，并按字段的已知类型还原栈值，避免生成无效 IL。
        if (leftType is TypeBuilder && local.FieldVar.TryGetValue(id.IdName, out var pendingField))
        {
            ilGenerator.Emit(OpCodes.Ldstr, id.IdName);
            ilGenerator.Emit(OpCodes.Call,
                typeof(DotOperatorILHelper).GetMethod(nameof(GetDynamicObjectProperty))!);
            if (pendingField.FieldType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, pendingField.FieldType);
            }
            else if (pendingField.FieldType != typeof(object))
            {
                ilGenerator.Emit(OpCodes.Castclass, pendingField.FieldType);
            }

            return pendingField.FieldType;
        }

        // 字典或列表索引的静态类型是 object 时，在运行期解析成员；否则嵌套
        // 集合（例如 person["scores"].Length）无法进入下方的 CLR 属性分支。
        if (leftType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Ldstr, id.IdName);
            ilGenerator.Emit(OpCodes.Call,
                typeof(DotOperatorILHelper).GetMethod(nameof(GetDynamicObjectProperty))!);
            return typeof(object);
        }

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

        // Old8Lang 中 Length 是集合长度的通用属性；原生 List<T>、Dictionary<K,V>
        // 等 CLR ICollection 只提供 Count 属性。保持该语言层别名，避免链式调用
        // 结果在 IL 模式下因静态类型是 List<object?> 而无法访问 Length。
        if (id.IdName == "Length" && typeof(ICollection).IsAssignableFrom(leftType) && !leftType.IsValueType)
        {
            var collectionCount = typeof(ICollection).GetProperty(nameof(ICollection.Count))!.GetGetMethod()!;
            ilGenerator.Emit(OpCodes.Callvirt, collectionCount);
            return typeof(int);
        }

        var instanceField = leftType.GetField(id.IdName);
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
    /// 获取静态类型为 <see cref="object"/> 的接收者成员。
    /// 语言层把集合 <c>Length</c> 视为通用长度属性，CLR 集合则通常只公开 <c>Count</c>。
    /// </summary>
    public static object? GetDynamicObjectProperty(object? receiver, string propertyName)
    {
        if (receiver is null)
        {
            throw new NullReferenceException($"无法在 null 上访问属性 '{propertyName}'");
        }

        if (propertyName == "Length" && receiver is ICollection collection)
        {
            return collection.Count;
        }

        var receiverType = receiver.GetType();
        var property = receiverType.GetProperty(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property?.CanRead == true)
        {
            return property.GetValue(receiver);
        }

        var field = receiverType.GetField(propertyName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is not null)
        {
            return field.GetValue(receiver);
        }

        throw new MissingMemberException(receiverType.FullName, propertyName);
    }

    /// <summary>
    /// 为捕获类实例的动态 lambda 写入字段或属性。
    /// </summary>
    public static void SetDynamicObjectProperty(object? receiver, string propertyName, object? value)
    {
        if (receiver is null)
        {
            throw new NullReferenceException($"无法在 null 上设置成员 '{propertyName}'");
        }

        var type = receiver.GetType();
        var field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (field is not null)
        {
            field.SetValue(receiver, value);
            return;
        }

        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (property?.CanWrite == true)
        {
            property.SetValue(receiver, value);
            return;
        }

        throw new MissingMemberException(type.FullName, propertyName);
    }

    /// <summary>
    /// 生成索引访问的IL代码
    /// </summary>
    internal static Type GenerateIndexAccess(
        LangExpression? left,
        LangExpression? right,
        ILGenerator ilGenerator,
        LocalManager local,
        Type? leftType,
        Type rightType,
        Operation operation)
    {
        left!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        right!.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

        // 处理不同类型的索引访问
        if (leftType == typeof(object[]))
        {
            // 数组索引访问（负索引 -1 表示最后一个元素）
            EmitIntIndexConversion(ilGenerator, rightType);
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetArrayItem))!);
            return typeof(object);
        }

        if (leftType != null && typeof(System.Collections.IList).IsAssignableFrom(leftType) &&
            !leftType.IsArray && leftType != typeof(string))
        {
            // 列表索引访问：走非泛型 IList 接口。
            // 列表字面量可能是 List<int> / List<string> 等具体泛型，
            // 而 list 参数在 IL 里静态类型是 List<object>；直接 callvirt List<object>.get_Item
            // 会在运行期把 List<int> 当 List<object> 使用，导致内存访问越界。
            // 负索引由 IlIndexHelper 归一化。
            EmitIntIndexConversion(ilGenerator, rightType);
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetListItem))!);
            return typeof(object);
        }

        if (leftType == typeof(Dictionary<object, object>))
        {
            // Dictionary<TKey, TValue>索引访问，调用索引器的getter方法
            var indexer = typeof(Dictionary<object, object>).GetProperty("Item")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
            return typeof(object);
        }

        if (leftType == typeof(string))
        {
            // 字符串索引访问（负索引 -1 表示最后一个字符）
            EmitIntIndexConversion(ilGenerator, rightType);
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetStringChar))!);
            return typeof(char);
        }

        if (leftType?.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问
            if (right is IntLangValue { Value: >= 0 } intVal)
            {
                // 如果是非负常量索引，优化为字段访问
                return GenerateValueTupleItemAccess(ilGenerator, leftType, intVal.Value);
            }

            // 变量索引或负常量索引：装箱为 ITuple 并经 IlIndexHelper 归一化下标
            ilGenerator.Emit(OpCodes.Box, leftType);
            EmitIntIndexConversion(ilGenerator, rightType);
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetTupleItem))!);
            return typeof(object);
        }

        if (leftType == typeof(object))
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
    /// 保证栈上的索引值是 <see cref="int"/>（IlIndexHelper 的参数类型）。
    /// </summary>
    internal static void EmitIntIndexConversion(ILGenerator ilGenerator, Type rightType)
    {
        if (rightType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
        }
        else if (rightType == typeof(long))
        {
            ilGenerator.Emit(OpCodes.Conv_I4);
        }
    }

    /// <summary>
    /// 生成动态索引访问的IL代码（用于 object 类型）
    /// </summary>
    public static Type GenerateDynamicIndexAccess(ILGenerator ilGenerator, Type rightType)
    {
        // 栈上已经有: leftValue, rightValue。字典键不必是 int，因此统一把两个
        // 值装箱后交给运行期辅助器分派。
        if (rightType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, rightType);
        }
        ilGenerator.Emit(OpCodes.Call,
            typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetDynamicObjectItem))!);
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

        var restField = tupleType.GetField("Rest");
        if (restField == null)
        {
            throw new InvalidOperationException($"无法访问 ValueTuple 的索引 {index}: Rest 字段不存在");
        }

        ilGenerator.Emit(OpCodes.Ldfld, restField);
        return GenerateValueTupleItemAccess(ilGenerator, restField.FieldType, index - 7);
    }

}
