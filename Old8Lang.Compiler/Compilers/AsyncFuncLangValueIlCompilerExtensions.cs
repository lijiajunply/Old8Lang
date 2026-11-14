using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Verification;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// <see cref="AsyncFuncLangValue"/> 的 IL 后端辅助：异步状态机启动代码。
/// </summary>
/// <remarks>
/// 该方法原先是 Runtime 中的 <c>AsyncFuncLangValue.GenerateMethodBody</c>；它只被 IL 后端调用，
/// 因此改为 <c>Old8Lang.Compiler</c> 中的扩展方法。Runtime 只保留共享的编译开关
/// <see cref="IlCompilerOptions"/>。
/// </remarks>
internal static class AsyncFuncLangValueIlCompilerExtensions
{
    /// <summary>
    /// 生成异步方法体（状态机启动代码）。
    /// </summary>
    internal static void GenerateMethodBody(this AsyncFuncLangValue func, ILGenerator ilGenerator, LocalManager local)
    {
        if (!IlCompilerOptions.EnableAsyncStateMachineAwait)
        {
            local.AsyncStateMachineGenerator = null;

            var returnValueLocal = ilGenerator.DeclareLocal(typeof(object));
            var returnLabel = ilGenerator.DefineLabel();
            local.ReturnValueLocal = returnValueLocal;
            local.ReturnLabel = returnLabel;

            ilGenerator.BeginExceptionBlock();
            func.BlockStatement.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

            if (func.BlockStatement.Count == 0 || func.BlockStatement[^1] is not ReturnStatement)
            {
                ilGenerator.Emit(OpCodes.Ldnull);
                ilGenerator.Emit(OpCodes.Stloc, returnValueLocal);
            }

            ilGenerator.Emit(OpCodes.Leave, returnLabel);
            ilGenerator.BeginFinallyBlock();
            ilGenerator.EndExceptionBlock();

            ilGenerator.MarkLabel(returnLabel);
            ilGenerator.Emit(OpCodes.Ldloc, returnValueLocal);

            var fromResultMethod = typeof(Task)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m is { Name: "FromResult", IsGenericMethodDefinition: true });
            ilGenerator.Emit(OpCodes.Call, fromResultMethod.MakeGenericMethod(typeof(object)));
            ilGenerator.Emit(OpCodes.Ret);
            return;
        }

        // 创建动态程序集和类型来生成状态机
        var assemblyName = new AssemblyName($"Old8LangAsync_{func.Id?.IdName ?? "Anonymous"}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);
        var typeBuilder = moduleBuilder.DefineType(
            $"AsyncStateMachine_{func.Id?.IdName ?? "Anonymous"}",
            TypeAttributes.Public |
            TypeAttributes.Sealed |
            TypeAttributes.AutoLayout | // 使用 AutoLayout 而不是 SequentialLayout
            TypeAttributes.AnsiClass |
            TypeAttributes.BeforeFieldInit,
            typeof(ValueType));

        var stateMachineGenerator =
            new AsyncStateMachineGenerator(ilGenerator, local, func.BlockStatement)
            {
                TypeBuilder = typeBuilder
            };

        local.AsyncStateMachineGenerator = stateMachineGenerator;

        // 提升参数到状态机字段
        if (func.Ids != null)
        {
            foreach (var id in func.Ids)
            {
                var argName = id.IdName;
                var argLocal = local.GetLocalVar(argName);
                if (argLocal != null)
                {
                    stateMachineGenerator.DefineVariable(argName, argLocal.LocalType);
                }
            }
        }

        stateMachineGenerator.GenerateStateMachine(typeBuilder);

        var stateMachineType = typeBuilder.CreateType()!;
        var stateField = stateMachineType.GetField(stateMachineGenerator.StateField!.Name)!;
        var builderField = stateMachineType.GetField(stateMachineGenerator.BuilderField!.Name)!;

        if (IlCompilerOptions.DebugOutputEnabled)
        {
            var moveNext = stateMachineType.GetMethod("MoveNext", BindingFlags.Public | BindingFlags.Instance);
            if (moveNext != null) Console.WriteLine(IlDisassembler.Disassemble(moveNext));
        }

        var smLocal = ilGenerator.DeclareLocal(stateMachineType);

        // 1. sm = default;
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Initobj, stateMachineType);

        // 初始化提升的参数字段
        if (func.Ids != null)
        {
            foreach (var argName in func.Ids.Select(id => id.IdName))
            {
                if (!stateMachineGenerator.VariableFields.TryGetValue(argName, out var fieldBuilder)) continue;
                var field = stateMachineType.GetField(fieldBuilder.Name)!;
                var argLocal = local.GetLocalVar(argName)!;
                ilGenerator.Emit(OpCodes.Ldloca, smLocal);
                ilGenerator.Emit(OpCodes.Ldloc, argLocal);
                ilGenerator.Emit(OpCodes.Stfld, field);
            }
        }

        // 2. sm.builder = AsyncTaskMethodBuilder<object>.Create();
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetMethod("Create")!);
        ilGenerator.Emit(OpCodes.Stfld, builderField);

        // 3. sm.state = -1;
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4_M1);
        ilGenerator.Emit(OpCodes.Stfld, stateField);

        // 4. sm.builder.Start(ref sm);
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Ldflda, builderField);
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetMethod("Start")!
                .MakeGenericMethod(stateMachineType));

        // 5. return sm.builder.Task;
        ilGenerator.Emit(OpCodes.Ldloca, smLocal);
        ilGenerator.Emit(OpCodes.Ldflda, builderField);
        ilGenerator.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetProperty("Task")!
                .GetGetMethod()!);
        ilGenerator.Emit(OpCodes.Ret);
    }
}
