using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Compiler.Verification;
using Old8Lang.Concurrency;
using Old8Lang.Error;
using Old8Lang.ExternProviders;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.ProjectManagement;
using Old8Lang.Runtime;
using Old8Lang.TypeSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

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

        // 1. 确定参数类型和返回类型
        var parameterTypes = node.Ids!.Select(item => item.Accept(new TypeInferenceVisitor(local))).ToArray();
        var returnType = typeof(Task<object>);

        // 2. 创建 DynamicMethod
        var dynamicMethod = new DynamicMethod(
            node.Id?.IdName ?? "AnonymousAsync",
            returnType,
            parameterTypes,
            true
        );

        // 3. 生成方法体
        var methodIl = dynamicMethod.GetILGenerator();

        // 创建新的LocalManager实例，专门用于函数体的IL生成
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

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

        // 处理参数
        for (var i = 0; i < node.Ids!.Count; i++)
        {
            var id = node.Ids![i];
            var paramType = parameterTypes[i]!;
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            funcLocal.LocalVarTypes[id.IdName] = paramType;

            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        node.GenerateMethodBody(methodIl, funcLocal);

        // 4. 创建委托并加载到栈上
        var delegateType = System.Linq.Expressions.Expression.GetDelegateType(
            parameterTypes.Concat([returnType]).ToArray());

        ilGenerator.Emit(OpCodes.Ldnull); // target (null for static method)
        ilGenerator.Emit(OpCodes.Ldftn, dynamicMethod);
        ilGenerator.Emit(OpCodes.Newobj, delegateType.GetConstructors()[0]);

        return null;
    
    }
}
