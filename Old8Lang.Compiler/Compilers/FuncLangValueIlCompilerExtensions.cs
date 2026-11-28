using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// <see cref="FuncLangValue"/> 的 IL 后端辅助：lambda 方法体生成与闭包捕获槽位发射。
/// </summary>
/// <remarks>
/// 这些成员原先是 Runtime 中的 <c>FuncLangValue</c> partial；它们只被 IL 后端使用，
/// 因此改为 <c>Old8Lang.Compiler</c> 中的扩展方法，Runtime 不再包含 IL 发射代码。
/// </remarks>
internal static class FuncLangValueIlCompilerExtensions
{
    /// <summary>
    /// 把匿名函数 / lambda 编译成 <see cref="DynamicMethod"/>（不注册到 DelegateVar）。
    /// </summary>
    internal static DynamicMethod BuildLambdaDynamicMethod(
        this FuncLangValue func,
        LocalManager local,
        string methodName,
        IReadOnlyList<CapturedLocalValue> capturedValues)
    {
        // 创建一个新的 LocalManager 实例，专门用于函数体的 IL 生成
        var funcLocal = new LocalManager
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            Globals = local.Globals,
            InClassEnv = local.InClassEnv,
            BuildingClassMethods = local.BuildingClassMethods,
            IsInDynamicMethod = true
        };

        foreach (var (key, value) in local.GlobalStaticClasses)
            funcLocal.GlobalStaticClasses[key] = value;

        // 继承外层的函数委托表：让 lambda 体内可以按名字调用已定义的函数
        // （例如 `spawn(f).Then((r:int) -> spawn(g))` 里的 g）。
        foreach (var (key, value) in local.DelegateVar)
        {
            funcLocal.DelegateVar.TryAdd(key, value);
        }

        foreach (var (key, value) in local.FuncParameters)
        {
            funcLocal.FuncParameters.TryAdd(key, value);
        }

        foreach (var (key, value) in local.FieldVar)
        {
            funcLocal.FieldVar.TryAdd(key, value);
        }

        for (var i = 0; i < capturedValues.Count; i++)
        {
            var captured = capturedValues[i];
            funcLocal.CapturedLocals[captured.Name] = new CapturedLocal(i, captured.Type);
            funcLocal.LocalVarTypes[captured.Name] = captured.Type;
        }

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = func.Ids!.Select(item => item.Accept(new TypeInferenceVisitor(funcLocal))).ToArray();

        // 先处理参数，将它们添加到 funcLocal 中，这样 GetItemType 才能正确推断返回类型
        for (var i = 0; i < func.Ids!.Count; i++)
        {
            funcLocal.LocalVarTypes[func.Ids[i].IdName] = parameterTypes[i]!;
        }

        // 获取返回类型
        var returnType = FuncLangValue.GetItemType(func.BlockStatement, funcLocal);

        // 定义新的方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            [typeof(object[]), .. parameterTypes!],
            true);
        var methodIl = dynamicMethod.GetILGenerator();

        // 处理参数
        for (var i = 0; i < func.Ids!.Count; i++)
        {
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType!);
            funcLocal.AddLocalVar(func.Ids[i].IdName, localVar);
            methodIl.Emit(OpCodes.Ldarg, i + 1);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        // 生成方法体的 IL 代码
        func.BlockStatement.Accept(RuntimeVisitors.CreateCompiler(methodIl, funcLocal));

        // 检查函数体的最后一个语句是否是 ReturnStatement
        var lastStatement = func.BlockStatement.Count > 0 ? func.BlockStatement[^1] : null;
        if (lastStatement is not ReturnStatement)
        {
            EmitDefaultReturn(methodIl, returnType);
        }

        return dynamicMethod;
    }

    /// <summary>
    /// 发射创建 lambda 闭包的 IL，并把捕获槽位打包成 object[]。
    /// </summary>
    internal static DynamicMethod EmitLambdaClosure(
        this FuncLangValue func,
        ILGenerator ilGenerator,
        LocalManager local,
        string methodName)
    {
        var capturedValues = GetCapturedLocalValues(local);
        var dynamicMethod = func.BuildLambdaDynamicMethod(local, methodName, capturedValues);
        var templateId = CompiledDelegateRegistry.RegisterClosureTemplate(dynamicMethod);
        EmitClosureCreation(ilGenerator, local, templateId, capturedValues);
        return dynamicMethod;
    }

    /// <summary>
    /// 发射打包捕获槽位数组并调用 <see cref="CompiledDelegateRegistry.CreateClosure"/> 的 IL 指令（同步/异步 lambda 共用）。
    /// </summary>
    internal static void EmitClosureCreation(
        ILGenerator ilGenerator,
        LocalManager local,
        int templateId,
        IReadOnlyList<CapturedLocalValue> capturedValues)
    {
        ilGenerator.Emit(OpCodes.Ldc_I4, templateId);
        ilGenerator.Emit(OpCodes.Ldc_I4, capturedValues.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < capturedValues.Count; i++)
        {
            var captured = capturedValues[i];
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i);
            if (captured.Name == "this" && captured.Local is null)
            {
                // 当前类实例方法的 this 位于参数 0；若由嵌套 lambda 创建，
                // 则先从外层闭包槽位取得已捕获的 this。
                if (local.TryGetCapturedLocal("this", out var outerThis))
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    ilGenerator.Emit(OpCodes.Ldc_I4, outerThis.Index);
                    ilGenerator.Emit(OpCodes.Ldelem_Ref);
                    if (outerThis.Type.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, outerThis.Type);
                    }
                    else if (outerThis.Type != typeof(object) && outerThis.Type is not TypeBuilder)
                    {
                        ilGenerator.Emit(OpCodes.Castclass, outerThis.Type);
                    }
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                }
            }
            else if (captured.Local is not null)
            {
                ilGenerator.Emit(OpCodes.Ldloc, captured.Local);
            }
            else if (local.ArgumentIndices.TryGetValue(captured.Name, out var capturedArgIndex))
            {
                // 形参没有 LocalBuilder，直接从参数槽位取值做成闭包快照
                ilGenerator.Emit(OpCodes.Ldarg, capturedArgIndex);
            }
            else if (local.TryGetCapturedLocal(captured.Name, out var outerCaptured))
            {
                // 外层闭包已经捕获过：从当前闭包环境数组里取出，再打包进新闭包。
                // 环境数组是 object[]，值类型必须拆箱（castclass 对 int/bool 这类会产出无效 IL）。
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldc_I4, outerCaptured.Index);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
                if (outerCaptured.Type.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, outerCaptured.Type);
                }
                else if (outerCaptured.Type != typeof(object) && outerCaptured.Type is not TypeBuilder)
                {
                    ilGenerator.Emit(OpCodes.Castclass, outerCaptured.Type);
                }
            }
            else
            {
                throw new InvalidOperationException($"无法捕获变量 '{captured.Name}'");
            }

            if (captured.Type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, captured.Type);
            }

            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        ilGenerator.Emit(OpCodes.Call,
            typeof(CompiledDelegateRegistry).GetMethod(nameof(CompiledDelegateRegistry.CreateClosure))!);
    }

    /// <summary>
    /// 收集当前 IL 作用域需要打包进闭包的局部变量。
    /// </summary>
    internal static List<CapturedLocalValue> GetCapturedLocalValues(LocalManager local)
    {
        var captured = local.LocalVar
            .Select(pair => new CapturedLocalValue(pair.Key, pair.Value, pair.Value.LocalType))
            .ToList();

        // 形参不在 LocalVar 里（走 ArgumentIndices），但闭包同样要能读取它们：
        // 例如 `func makeAdder(n:int) -> function { return (x:int) -> x + n }`。
        // 这里登记成「无 LocalBuilder 的捕获槽位」，由 EmitLambdaClosure 用 ldarg 取值。
        foreach (var (argumentName, _) in local.ArgumentIndices)
        {
            if (captured.Any(value => value.Name == argumentName))
            {
                continue;
            }

            var argumentType = local.LocalVarTypes.TryGetValue(argumentName, out var registered)
                ? registered
                : typeof(object);
            captured.Add(new CapturedLocalValue(argumentName, null, argumentType ?? typeof(object)));
        }

        // 外层闭包已经捕获的变量，嵌套 lambda 也要能继续引用（再打包进新闭包）
        foreach (var (capturedName, capturedLocal) in local.CapturedLocals)
        {
            if (capturedName == "this" || captured.Any(value => value.Name == capturedName))
            {
                continue;
            }

            captured.Add(new CapturedLocalValue(capturedName, null, capturedLocal.Type));
        }

        if (local.InClassEnv is not null && captured.All(value => value.Name != "this"))
        {
            // DynamicMethod 不能将尚未完成的 TypeBuilder 用作 castclass 目标；
            // 字段解析仍由 InClassEnv / FieldVar 保留，捕获槽位只需保存 object。
            var thisType = local.InClassEnv is TypeBuilder ? typeof(object) : local.InClassEnv;
            captured.Add(new CapturedLocalValue("this", null, thisType));
        }

        return captured;
    }

    /// <summary>
    /// 一个捕获槽位：局部变量名、对应的 <see cref="LocalBuilder"/> 与类型。
    /// </summary>
    internal sealed record CapturedLocalValue(string Name, LocalBuilder? Local, Type Type);

    /// <summary>
    /// 为没有显式 return 的函数体补一段默认返回。
    /// </summary>
    internal static void EmitDefaultReturn(ILGenerator methodIl, Type returnType)
    {
        if (returnType == typeof(void))
        {
            methodIl.Emit(OpCodes.Ret);
            return;
        }

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
                methodIl.Emit(OpCodes.Ldloca, defaultValueLocal);
                methodIl.Emit(OpCodes.Initobj, returnType);
                methodIl.Emit(OpCodes.Ldloc, defaultValueLocal);
            }
        }
        else
        {
            // 引用类型返回 null
            methodIl.Emit(OpCodes.Ldnull);
        }

        methodIl.Emit(OpCodes.Ret);
    }
}
