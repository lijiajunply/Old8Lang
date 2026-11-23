using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
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

    /// <summary>
    /// 生成异步生成器函数体（返回 <see cref="IAsyncEnumerable{T}"/>）。
    /// </summary>
    /// <remarks>
    /// 使用与普通异步函数相同的 <see cref="AsyncStateMachineGenerator"/>（启用 <c>isAsyncGenerator</c>），
    /// 通过动态生成的启动器类保存实参并构造 <see cref="IlAsyncGeneratorStream"/>，实现延迟启动与按序流式产出。
    /// </remarks>
    internal static void GenerateAsyncGeneratorMethodBody(
        this AsyncFuncLangValue func,
        ILGenerator ilGenerator,
        LocalManager local)
    {
        var funcName = func.Id?.IdName ?? "Anonymous";
        var assemblyName = new AssemblyName($"Old8LangAsyncGen_{funcName}_{Guid.NewGuid():N}");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

        // 1. 生成异步状态机值类型（带 <>y__emitter 字段）
        var smTypeBuilder = moduleBuilder.DefineType(
            $"AsyncGenStateMachine_{funcName}",
            TypeAttributes.Public |
            TypeAttributes.Sealed |
            TypeAttributes.AutoLayout |
            TypeAttributes.AnsiClass |
            TypeAttributes.BeforeFieldInit,
            typeof(ValueType));

        var stateMachineGenerator = new AsyncStateMachineGenerator(
            ilGenerator, local, func.BlockStatement, isAsyncGenerator: true)
        {
            TypeBuilder = smTypeBuilder
        };

        local.AsyncStateMachineGenerator = stateMachineGenerator;

        var paramNames = new List<string>();
        if (func.Ids is not null)
        {
            foreach (var id in func.Ids)
            {
                var argName = id.IdName;
                var argLocal = local.GetLocalVar(argName);
                if (argLocal is not null)
                {
                    stateMachineGenerator.DefineVariable(argName, argLocal.LocalType);
                    paramNames.Add(argName);
                }
            }
        }

        stateMachineGenerator.GenerateStateMachine(smTypeBuilder);
        var stateMachineType = smTypeBuilder.CreateType()!;

        var smStateField = stateMachineType.GetField(stateMachineGenerator.StateField!.Name)!;
        var smBuilderField = stateMachineType.GetField(stateMachineGenerator.BuilderField!.Name)!;
        var smEmitterField = stateMachineType.GetField(stateMachineGenerator.YieldEmitterField!.Name)!;

        // 2. 生成启动器引用类型，捕获实参并提供 Start(IlAsyncYieldEmitter) -> Task<object>
        var starterTypeBuilder = moduleBuilder.DefineType(
            $"AsyncGenStarter_{funcName}",
            TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            typeof(object));

        var starterParamFields = new Dictionary<string, FieldBuilder>();
        foreach (var argName in paramNames)
        {
            var smField = stateMachineType.GetField(stateMachineGenerator.VariableFields[argName].Name)!;
            starterParamFields[argName] = starterTypeBuilder.DefineField(
                $"<>p__{argName}", smField.FieldType, FieldAttributes.Public);
        }

        var starterCtor = starterTypeBuilder.DefineDefaultConstructor(MethodAttributes.Public);

        var startMethod = starterTypeBuilder.DefineMethod(
            "Start",
            MethodAttributes.Public,
            typeof(Task<object>),
            [typeof(IlAsyncYieldEmitter)]);

        var startIl = startMethod.GetILGenerator();
        var smLocal = startIl.DeclareLocal(stateMachineType);

        // sm = default;
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Initobj, stateMachineType);

        // 拷贝实参字段
        foreach (var argName in paramNames)
        {
            var smField = stateMachineType.GetField(stateMachineGenerator.VariableFields[argName].Name)!;
            startIl.Emit(OpCodes.Ldloca, smLocal);
            startIl.Emit(OpCodes.Ldarg_0);
            startIl.Emit(OpCodes.Ldfld, starterParamFields[argName]);
            startIl.Emit(OpCodes.Stfld, smField);
        }

        // sm.<>y__emitter = emitter;
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Ldarg_1);
        startIl.Emit(OpCodes.Stfld, smEmitterField);

        // sm.<>t__builder = AsyncTaskMethodBuilder<object>.Create();
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetMethod("Create")!);
        startIl.Emit(OpCodes.Stfld, smBuilderField);

        // sm.<>1__state = -1;
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Ldc_I4_M1);
        startIl.Emit(OpCodes.Stfld, smStateField);

        // sm.<>t__builder.Start(ref sm);
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Ldflda, smBuilderField);
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetMethod("Start")!
                .MakeGenericMethod(stateMachineType));

        // return sm.<>t__builder.Task;
        startIl.Emit(OpCodes.Ldloca, smLocal);
        startIl.Emit(OpCodes.Ldflda, smBuilderField);
        startIl.Emit(OpCodes.Call,
            typeof(System.Runtime.CompilerServices.AsyncTaskMethodBuilder<object>).GetProperty("Task")!
                .GetGetMethod()!);
        startIl.Emit(OpCodes.Ret);

        var starterType = starterTypeBuilder.CreateType()!;
        var runtimeStarterCtor = starterType.GetConstructor(Type.EmptyTypes)!;
        var runtimeStartMethod = starterType.GetMethod("Start", [typeof(IlAsyncYieldEmitter)])!;

        // 3. 动态方法体：实例化启动器、保存实参并返回 IlAsyncGeneratorStream
        var starterLocal = ilGenerator.DeclareLocal(starterType);
        ilGenerator.Emit(OpCodes.Newobj, runtimeStarterCtor);
        ilGenerator.Emit(OpCodes.Stloc, starterLocal);

        foreach (var argName in paramNames)
        {
            var argLocal = local.GetLocalVar(argName)!;
            var runtimeField = starterType.GetField(starterParamFields[argName].Name)!;
            ilGenerator.Emit(OpCodes.Ldloc, starterLocal);
            ilGenerator.Emit(OpCodes.Ldloc, argLocal);
            ilGenerator.Emit(OpCodes.Stfld, runtimeField);
        }

        ilGenerator.Emit(OpCodes.Ldloc, starterLocal);
        ilGenerator.Emit(OpCodes.Ldftn, runtimeStartMethod);
        ilGenerator.Emit(OpCodes.Newobj,
            typeof(Func<IlAsyncYieldEmitter, Task<object?>>).GetConstructor([typeof(object), typeof(IntPtr)])!);
        ilGenerator.Emit(OpCodes.Newobj,
            typeof(IlAsyncGeneratorStream).GetConstructor([typeof(Func<IlAsyncYieldEmitter, Task<object?>>)])!);
        ilGenerator.Emit(OpCodes.Ret);
    }
}
