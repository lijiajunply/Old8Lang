using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Value 节点的 Visit 方法实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 IntLangValue 节点
    /// </summary>
    public object? VisitIntLangValue(IntLangValue node)
    {
        // 迁移自 IntLangValue.LoadIlValue()
        ilGenerator.Emit(OpCodes.Ldc_I4, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 DoubleLangValue 节点
    /// </summary>
    public object? VisitDoubleLangValue(DoubleLangValue node)
    {
        // 迁移自 DoubleLangValue.LoadIlValue()
        ilGenerator.Emit(OpCodes.Ldc_R8, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 StringLangValue 节点
    /// </summary>
    public object? VisitStringLangValue(StringLangValue node)
    {
        // 迁移自 StringLangValue.LoadIlValue()
        ilGenerator.Emit(OpCodes.Ldstr, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 BoolLangValue 节点
    /// </summary>
    public object? VisitBoolLangValue(BoolLangValue node)
    {
        // 迁移自 BoolLangValue.LoadIlValue()
        ilGenerator.Emit(node.Value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
        return null;
    }

    /// <summary>
    /// 访问 CharLangValue 节点
    /// </summary>
    public object? VisitCharLangValue(CharLangValue node)
    {
        // 迁移自 CharLangValue.LoadIlValue()
        ilGenerator.Emit(OpCodes.Ldc_I4, (int)node.Value);
        return null;
    }

    /// <summary>
    /// 访问 NullLangValue 节点
    /// </summary>
    public object? VisitNullLangValue(NullLangValue node)
    {
        // 迁移自 NullLangValue.LoadIlValue()
        ilGenerator.Emit(OpCodes.Ldnull);
        return null;
    }

    /// <summary>
    /// 访问 VoidLangValue 节点
    /// </summary>
    public object? VisitVoidLangValue(VoidLangValue node)
    {
        // Void值在 IL 模式下不需要加载任何值到栈上
        return null;
    }

    /// <summary>
    /// 访问 EnumLangValue 节点
    /// </summary>
    public object? VisitEnumLangValue(EnumLangValue node)
    {
        // 迁移自 EnumLangValue.LoadIlValue()
        // 枚举值在 IL 模式下加载其整数值
        ilGenerator.Emit(OpCodes.Ldc_I4, node.Value);
        return null;
    }

    /// <summary>
    /// 访问 FuncLangValue 节点
    /// </summary>
    public object? VisitFuncLangValue(FuncLangValue node)
    {

        if (node.Method is not null)
        {
            throw new InvalidOperationError(node.Position,
                "IL 模式暂不支持把原生方法作为函数值传递",
                $"方法 '{node.Method.Name}' 不能直接作为函数值参数，请先用 Old8Lang 函数包装它");
        }

        if (!node.IsLambdaExpression)
        {
            throw new InvalidOperationError(node.Position,
                "IL 模式暂不支持把具名函数作为函数值传递",
                $"函数 '{node.Id?.IdName}' 不能直接作为函数值参数，请改写为 lambda 表达式");
        }

        node.EmitLambdaClosure(ilGenerator, local, node.Id?.IdName ?? "lambda");

        return null;
    
    }

    public object? VisitAsyncFuncLangValue(AsyncFuncLangValue node)
    {
        var capturedValues = FuncLangValueIlCompilerExtensions.GetCapturedLocalValues(local);
        var hasCaptures = capturedValues.Count > 0;

        // 1. 创建新的 LocalManager 实例，继承全局与捕获信息
        var funcLocal = new LocalManager()
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            Globals = local.Globals,
            InClassEnv = local.InClassEnv
        };

        foreach (var (key, value) in local.DelegateVar)
        {
            funcLocal.DelegateVar[key] = value;
        }

        foreach (var (key, value) in local.ClassVar)
        {
            funcLocal.ClassVar[key] = value;
        }

        foreach (var (key, value) in local.GlobalStaticClasses)
        {
            funcLocal.GlobalStaticClasses[key] = value;
        }

        foreach (var (key, value) in local.FuncParameters)
        {
            funcLocal.FuncParameters[key] = value;
        }

        foreach (var (key, value) in local.GenericFunctions)
        {
            funcLocal.GenericFunctions[key] = value;
        }

        foreach (var (key, value) in local.FieldVar)
        {
            funcLocal.FieldVar[key] = value;
        }

        foreach (var (key, value) in local.LocalDelegateReturnTypes)
        {
            funcLocal.LocalDelegateReturnTypes[key] = value;
        }

        // 2. 确定参数类型和返回类型（统一采用 object[] 闭包模板，避免对 DynamicMethod 发射非法的 ldftn）
        var parameterTypes = node.Ids!.Select(item => item.Accept(new TypeInferenceVisitor(funcLocal))).ToArray();
        var returnType = typeof(Task<object>);
        var methodParameterTypes = new Type[1 + parameterTypes.Length];
        methodParameterTypes[0] = typeof(object[]);
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            methodParameterTypes[i + 1] = parameterTypes[i]!;
        }

        // 3. 创建 DynamicMethod
        var dynamicMethod = new DynamicMethod(
            node.Id?.IdName ?? "AnonymousAsync",
            returnType,
            methodParameterTypes,
            true
        );

        // 4. 生成方法体
        var methodIl = dynamicMethod.GetILGenerator();

        // 有捕获时先在入口把闭包 object[] 解包为普通局部槽，再交给异步状态机一并提升为状态机字段
        // （状态机 MoveNext 里的 arg 0 是状态机 struct，不能直接用 Ldarg_0 读 object[]）
        var paramNameSet = new HashSet<string>(node.Ids!.Select(id => id.IdName), StringComparer.Ordinal);
        if (hasCaptures)
        {
            for (var i = 0; i < capturedValues.Count; i++)
            {
                var captured = capturedValues[i];
                if (paramNameSet.Contains(captured.Name))
                {
                    continue;
                }

                var capturedLocal = methodIl.DeclareLocal(captured.Type);
                methodIl.Emit(OpCodes.Ldarg_0);
                methodIl.Emit(OpCodes.Ldc_I4, i);
                methodIl.Emit(OpCodes.Ldelem_Ref);
                if (captured.Type.IsValueType)
                {
                    methodIl.Emit(OpCodes.Unbox_Any, captured.Type);
                }
                else if (captured.Type != typeof(object))
                {
                    methodIl.Emit(OpCodes.Castclass, captured.Type);
                }

                methodIl.Emit(OpCodes.Stloc, capturedLocal);
                funcLocal.AddLocalVar(captured.Name, capturedLocal);
                funcLocal.LocalVarTypes[captured.Name] = captured.Type;
            }
        }

        // 处理参数（arg 0 恒为闭包 object[]，实参从 arg 1 开始）
        for (var i = 0; i < node.Ids!.Count; i++)
        {
            var id = node.Ids![i];
            var paramType = parameterTypes[i]!;
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            funcLocal.LocalVarTypes[id.IdName] = paramType;

            methodIl.Emit(OpCodes.Ldarg, i + 1);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        node.GenerateMethodBody(methodIl, funcLocal);

        // 5. 创建委托并加载到栈上
        var templateId = CompiledDelegateRegistry.RegisterClosureTemplate(dynamicMethod);
        FuncLangValueIlCompilerExtensions.EmitClosureCreation(ilGenerator, local, templateId, capturedValues);

        return null;
    }

    public object? VisitNativeAnyLangValue(NativeAnyLangValue node)
    {
        return null;
    }

    public object? VisitVMThreadLangValue(VMThreadLangValue node)
    {
        // VM 线程句柄没有 IL 表示
        return null;
    }
}
