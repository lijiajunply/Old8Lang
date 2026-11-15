using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
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
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Expression 节点的 Visit 方法实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 LangId 节点
    /// </summary>
    public object? VisitLangId(LangId node)
    {

        // 首先检查是否是全局静态类
        if (local.GlobalStaticClasses.TryGetValue(node.IdName, out var staticClassInstance))
        {
            // 将静态类实例加载到栈上
            staticClassInstance.Accept(this);
            return null;
        }

        // 如果在异步状态机中，使用变量提升机制
        if (local.AsyncStateMachineGenerator != null)
        {
            local.LoadVariable(ilGenerator, node.IdName, node.Position);
            return null;
        }

        if (local.TryGetCapturedLocal(node.IdName, out var captured))
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, captured.Index);
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
            if (captured.Type.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, captured.Type);
            }
            else if (captured.Type != typeof(object))
            {
                ilGenerator.Emit(OpCodes.Castclass, captured.Type);
            }

            return null;
        }

        // 参数和局部变量遮蔽同名字段；字段可通过 node.member 显式访问。
        var value = local.GetLocalVar(node.IdName);
        if (value is not null)
        {
            ilGenerator.Emit(OpCodes.Ldloc, value);
            return null;
        }

        if (local.ArgumentIndices.TryGetValue(node.IdName, out var argIndex))
        {
            if (argIndex == 0) ilGenerator.Emit(OpCodes.Ldarg_0);
            else if (argIndex == 1) ilGenerator.Emit(OpCodes.Ldarg_1);
            else if (argIndex == 2) ilGenerator.Emit(OpCodes.Ldarg_2);
            else if (argIndex == 3) ilGenerator.Emit(OpCodes.Ldarg_3);
            else if (argIndex <= 255) ilGenerator.Emit(OpCodes.Ldarg_S, (byte)argIndex);
            else ilGenerator.Emit(OpCodes.Ldarg, argIndex);
            return null;
        }

        // 检查是否为字段 (在类的方法中)
        if (local.FieldVar.TryGetValue(node.IdName, out var field))
        {
            // 确保我们在实例方法中（有 node 指针）
            // 加载 node
            ilGenerator.Emit(OpCodes.Ldarg_0);
            // 加载字段
            ilGenerator.Emit(OpCodes.Ldfld, field);
            return null;
        }

        // 具名函数在 IL 模式下也可以作为「函数值」传递（例如传入 List.FindAll 这类高阶方法）。
        // 这里从已编译的委托表里取出对应的 DynamicMethod，注册成一个委托常量后加载。
        if (LangIdCompilerHelper.TryGetFunctionMethod(node.IdName, local, out var functionMethod))
        {
            var functionValueId = CompiledDelegateRegistry.RegisterFunctionValue(functionMethod);
            ilGenerator.Emit(OpCodes.Ldc_I4, functionValueId);
            ilGenerator.Emit(OpCodes.Call,
                typeof(CompiledDelegateRegistry).GetMethod(nameof(CompiledDelegateRegistry.Get))!);
            return null;
        }

        // 被函数体访问的顶层变量存放在运行期全局槽位中：
        // 读取时按变量名从全局环境取回，而不是复用 OldLangRun 的 LocalBuilder。
        if (local.Globals is { } globals && globals.IsSlot(node.IdName))
        {
            RuntimeGlobals.EmitLoad(ilGenerator, node.IdName, globals.GetType(node.IdName));
            return null;
        }

        // 如果找不到，且没有其他匹配，抛出错误
        // 之前的实现盲目使用 Ldarg_0，这可能掩盖了错误
        local.ReportError($"变量 '{node.IdName}' 未声明", node.Position);
    
    
        return null;
    }

    /// <summary>
    /// 访问 MatchExpression 节点（编译器）
    /// </summary>
    public object? VisitMatchExpression(MatchExpression node)
    {
        // 各分支的模式匹配统一交给运行期（复用解释器的 IsMatch 语义），
        // IL 只负责「按 id 询问是否匹配 → 写回绑定变量 → 算结果」。
        var endLabel = ilGenerator.DefineLabel();
        var resultLocal = ilGenerator.DeclareLocal(typeof(object));

        // 保存 match 值（统一装箱成 object，模式匹配按语言值语义判定）
        node.MatchValue.Accept(this);
        var matchValueType = node.MatchValue.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (matchValueType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, matchValueType);
        }

        var matchValueLocal = ilGenerator.DeclareLocal(typeof(object));
        ilGenerator.Emit(OpCodes.Stloc, matchValueLocal);

        var boundValuesLocal = ilGenerator.DeclareLocal(typeof(object[]));

        foreach (var matchCase in node.Cases)
        {
            var nextCaseLabel = ilGenerator.DefineLabel();
            var bindingNames = CollectMatchBindingNames(matchCase);
            var caseId = IlMatchRegistry.Register(matchCase, bindingNames);

            ilGenerator.Emit(OpCodes.Ldc_I4, caseId);
            ilGenerator.Emit(OpCodes.Ldloc, matchValueLocal);
            ilGenerator.Emit(OpCodes.Ldloca, boundValuesLocal);
            ilGenerator.Emit(OpCodes.Call, typeof(IlMatchRegistry).GetMethod(nameof(IlMatchRegistry.TryMatch))!);
            ilGenerator.Emit(OpCodes.Brfalse, nextCaseLabel);

            // 写回绑定变量（object 槽位，后续按动态语义使用）
            for (var index = 0; index < bindingNames.Length; index++)
            {
                var bindingLocal = ilGenerator.DeclareLocal(typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, boundValuesLocal);
                ilGenerator.Emit(OpCodes.Ldc_I4, index);
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
                ilGenerator.Emit(OpCodes.Stloc, bindingLocal);
                local.AddLocalVar(bindingNames[index], bindingLocal);
            }

            // 计算并存储结果
            matchCase.ResultExpression.Accept(this);
            var resultType = matchCase.ResultExpression.Accept(new TypeInferenceVisitor(local));
            if (resultType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, resultType);
            }

            ilGenerator.Emit(OpCodes.Stloc, resultLocal);
            ilGenerator.Emit(OpCodes.Br, endLabel);

            ilGenerator.MarkLabel(nextCaseLabel);
            foreach (var bindingName in bindingNames)
            {
                local.RemoveLocalVar(bindingName);
            }
        }

        // 所有 case 都不匹配，抛出异常
        var exceptionCtor = typeof(InvalidOperationError).GetConstructor([typeof(IOldLangTree), typeof(string)])!;
        ilGenerator.Emit(OpCodes.Ldnull); // IOldLangTree 参数
        ilGenerator.Emit(OpCodes.Ldstr, "Match 表达式没有匹配的分支");
        ilGenerator.Emit(OpCodes.Newobj, exceptionCtor);
        ilGenerator.Emit(OpCodes.Throw);

        // 结束标签
        ilGenerator.MarkLabel(endLabel);

        // 加载结果并按推断类型还原（与各分支的装箱对应）
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);
        var matchResultType = node.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
        if (matchResultType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, matchResultType);
        }

        return null;
    }

    /// <summary>
    /// 收集一个 match 分支会绑定的变量名（顺序即运行期返回值顺序）。
    /// </summary>
    private static string[] CollectMatchBindingNames(MatchCase matchCase)
    {
        return matchCase.Type switch
        {
            PatternType.VariableBinding or PatternType.TypeMatch =>
                matchCase.BindingVariable is { Length: > 0 } name ? [name] : [],
            PatternType.Tuple =>
                matchCase.TuplePattern?.Elements
                    .Where(element => element.Variable is not null)
                    .Select(element => element.Variable!)
                    .ToArray() ?? [],
            _ => []
        };
    }
}
