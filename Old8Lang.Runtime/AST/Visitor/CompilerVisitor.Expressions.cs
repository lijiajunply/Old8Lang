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
            staticClassInstance.LoadIlValue(ilGenerator, local);
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
        if (LangId.TryGetFunctionMethod(node.IdName, local, out var functionMethod))
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

        // 定义标签
        var endLabel = ilGenerator.DefineLabel();
        var caseLabels = node.Cases.Select(_ => ilGenerator.DefineLabel()).ToList();
        var resultLocal = ilGenerator.DeclareLocal(typeof(object)); // 用于存储结果

        // 保存 match 表达式值到局部变量
        node.MatchValue.LoadIlValue(ilGenerator, local);
        var matchValueType = node.MatchValue.OutputType(local) ?? typeof(object);
        var matchValueLocal = ilGenerator.DeclareLocal(matchValueType);
        ilGenerator.Emit(OpCodes.Stloc, matchValueLocal);

        // 生成每个 case 的匹配判断
        for (int i = 0; i < node.Cases.Count; i++)
        {
            var matchCase = node.Cases[i];
            var caseLabel = caseLabels[i];

            // 检查是否是通配符 (匹配所有)
            if (matchCase.IsWildcard)
            {
                // 通配符直接跳转到对应的 case
                ilGenerator.Emit(OpCodes.Br, caseLabel);
                continue;
            }

            // 检查是否是变量绑定 (匹配所有并绑定变量)
            if (matchCase.IsVariableBinding)
            {
                // 变量绑定也直接跳转到对应的 case
                ilGenerator.Emit(OpCodes.Br, caseLabel);
                continue;
            }

            // 普通值匹配：比较 match 值和 case 值
            ilGenerator.Emit(OpCodes.Ldloc, matchValueLocal);
            matchCase.Pattern!.LoadIlValue(ilGenerator, local);

            // 根据类型选择合适的比较指令
            if (matchValueType == typeof(int) || matchValueType == typeof(bool) || matchValueType == typeof(char))
            {
                ilGenerator.Emit(OpCodes.Ceq);
            }
            else if (matchValueType == typeof(string))
            {
                var equalsMethod = typeof(string).GetMethod("Equals",
                    [typeof(string), typeof(string)])!;
                ilGenerator.Emit(OpCodes.Call, equalsMethod);
            }
            else
            {
                // 其他类型：装箱后调用 Equals
                if (matchValueType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, matchValueType);

                var patternType = matchCase.Pattern.OutputType(local);
                if (patternType is not null && patternType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, patternType);

                var equalsMethod = typeof(object).GetMethod("Equals", [typeof(object), typeof(object)])!;
                ilGenerator.Emit(OpCodes.Call, equalsMethod);
            }

            // 若匹配，跳转到对应的 case
            ilGenerator.Emit(OpCodes.Brtrue, caseLabel);
        }

        // 所有 case 都不匹配，抛出异常
        var exceptionType = typeof(InvalidOperationError);
        var exceptionCtor = exceptionType.GetConstructor([typeof(IOldLangTree), typeof(string)])!;
        ilGenerator.Emit(OpCodes.Ldnull); // IOldLangTree 参数
        ilGenerator.Emit(OpCodes.Ldstr, "Match 表达式没有匹配的分支");
        ilGenerator.Emit(OpCodes.Newobj, exceptionCtor);
        ilGenerator.Emit(OpCodes.Throw);

        // 生成各个 case 的 IL 代码
        for (var i = 0; i < node.Cases.Count; i++)
        {
            var matchCase = node.Cases[i];
            var caseLabel = caseLabels[i];

            ilGenerator.MarkLabel(caseLabel);

            // 如果有变量绑定，将 match 值赋给变量
            if (matchCase.BindingVariable is not null)
            {
                ilGenerator.Emit(OpCodes.Ldloc, matchValueLocal);
                if (matchValueType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, matchValueType);

                var localVar = ilGenerator.DeclareLocal(typeof(object));
                local.AddLocalVar(matchCase.BindingVariable, localVar);
                ilGenerator.Emit(OpCodes.Stloc, localVar);
            }

            // 计算并存储结果
            matchCase.ResultExpression.LoadIlValue(ilGenerator, local);
            var resultType = matchCase.ResultExpression.OutputType(local);
            if (resultType is not null && resultType.IsValueType)
                ilGenerator.Emit(OpCodes.Box, resultType);

            ilGenerator.Emit(OpCodes.Stloc, resultLocal);

            // 跳转到结束
            ilGenerator.Emit(OpCodes.Br, endLabel);
        }

        // 结束标签
        ilGenerator.MarkLabel(endLabel);

        // 加载结果
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal);

        return null;
    
    }
}
