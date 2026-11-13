using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;

// ReSharper disable CheckNamespace

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 编译器支持
/// </summary>
public partial class FuncLangValue
{

    internal static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.Accept(new TypeInferenceVisitor(local));
                if (varType is not null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.Accept(new TypeInferenceVisitor(local))!;
            }

            if (item is null || item.Count == 0)
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
        if (Method is not null)
        {
            return $"{Method}";
        }

        var paramList = Ids is not null ? string.Join(", ", Ids) : string.Empty;
        return $"func {Id}({paramList}) \n {{ {BlockStatement} }}";
    }

    /// <summary>
    /// 把匿名函数 / lambda 编译成 <see cref="DynamicMethod"/>（不注册到 DelegateVar）。
    /// </summary>
    internal DynamicMethod BuildLambdaDynamicMethod(
        LocalManager local,
        string methodName,
        IReadOnlyList<CapturedLocalValue> capturedValues)
    {
        // 创建一个新的 LocalManager 实例，专门用于函数体的 IL 生成
        var funcLocal = new LocalManager()
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            Globals = local.Globals,
            InClassEnv = local.InClassEnv
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
        var parameterTypes = Ids!.Select(item => item.Accept(new TypeInferenceVisitor(funcLocal))).ToArray();

        // 先处理参数，将它们添加到 funcLocal 中，这样 GetItemType 才能正确推断返回类型
        for (var i = 0; i < Ids!.Count; i++)
        {
            funcLocal.LocalVarTypes[Ids[i].IdName] = parameterTypes[i]!;
        }

        // 获取返回类型
        var returnType = GetItemType(BlockStatement, funcLocal);

        // 定义新的方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            [typeof(object[]), .. parameterTypes!],
            true);
        var methodIl = dynamicMethod.GetILGenerator();

        // 处理参数
        for (var i = 0; i < Ids!.Count; i++)
        {
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType!);
            funcLocal.AddLocalVar(Ids[i].IdName, localVar);
            methodIl.Emit(OpCodes.Ldarg, i + 1);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        // 生成方法体的 IL 代码
        BlockStatement.Accept(new CompilerVisitor(methodIl, funcLocal));

        // 检查函数体的最后一个语句是否是 ReturnStatement
        var lastStatement = BlockStatement.Count > 0 ? BlockStatement[^1] : null;
        if (lastStatement is not ReturnStatement)
        {
            EmitDefaultReturn(methodIl, returnType);
        }

        return dynamicMethod;
    }

    internal DynamicMethod EmitLambdaClosure(ILGenerator ilGenerator, LocalManager local, string methodName)
    {
        var capturedValues = GetCapturedLocalValues(local);
        var dynamicMethod = BuildLambdaDynamicMethod(local, methodName, capturedValues);
        var templateId = CompiledDelegateRegistry.RegisterClosureTemplate(dynamicMethod);

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
                    if (outerThis.Type != typeof(object) && outerThis.Type is not TypeBuilder)
                    {
                        ilGenerator.Emit(OpCodes.Castclass, outerThis.Type);
                    }
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldloc, captured.Local!);
            }

            if (captured.Type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, captured.Type);
            }

            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        ilGenerator.Emit(OpCodes.Call,
            typeof(CompiledDelegateRegistry).GetMethod(nameof(CompiledDelegateRegistry.CreateClosure))!);
        return dynamicMethod;
    }

    internal static List<CapturedLocalValue> GetCapturedLocalValues(LocalManager local)
    {
        var captured = local.LocalVar
            .Select(pair => new CapturedLocalValue(pair.Key, pair.Value, pair.Value.LocalType))
            .ToList();

        if (local.InClassEnv is not null && captured.All(value => value.Name != "this"))
        {
            // DynamicMethod 不能将尚未完成的 TypeBuilder 用作 castclass 目标；
            // 字段解析仍由 InClassEnv / FieldVar 保留，捕获槽位只需保存 object。
            var thisType = local.InClassEnv is TypeBuilder ? typeof(object) : local.InClassEnv;
            captured.Add(new CapturedLocalValue("this", null, thisType));
        }

        return captured;
    }

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

    /// <summary>
    /// 检查表达式是否为常量表达式（可以安全缓存）
    /// </summary>

}
