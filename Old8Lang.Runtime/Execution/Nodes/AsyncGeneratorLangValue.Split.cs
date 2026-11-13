using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Generators;

/// <summary>
/// AsyncGeneratorLangValue 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class AsyncGeneratorLangValue
{
    /// <summary>
    /// 生成异步生成器方法体
    /// </summary>
    internal void GenerateGeneratorMethodBody(ILGenerator ilGenerator, LocalManager local)
    {
        // 对于异步生成器，我们需要生成一个返回IAsyncEnumerable<object>的方法
        // 创建一个简单的空异步枚举器实现
        
        // 1. 创建类型构建器
        var assemblyName = new AssemblyName("TempAsyncEnumerableAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("TempModule");
        var typeBuilder = moduleBuilder.DefineType(
            "EmptyAsyncEnumerable",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
        
        // 实现IAsyncEnumerable<object>接口
        typeBuilder.AddInterfaceImplementation(typeof(IAsyncEnumerable<object>));
        
        // 2. 实现GetAsyncEnumerator方法
        var methodBuilder = typeBuilder.DefineMethod(
            "GetAsyncEnumerator",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(IAsyncEnumerator<object>),
            [typeof(CancellationToken)]);
        
        typeBuilder.DefineMethodOverride(methodBuilder, typeof(IAsyncEnumerable<object>).GetMethod("GetAsyncEnumerator")!);
        
        var methodIl = methodBuilder.GetILGenerator();
        
        // 创建一个空的异步枚举器
        // 我们需要创建一个实现了IAsyncEnumerator<object>的类
        var enumeratorTypeBuilder = typeBuilder.DefineNestedType(
            "EmptyAsyncEnumerator",
            TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit);
        
        enumeratorTypeBuilder.AddInterfaceImplementation(typeof(IAsyncEnumerator<object>));
        
        // 定义Current属性
        var currentProperty = enumeratorTypeBuilder.DefineProperty(
            "Current",
            PropertyAttributes.None,
            typeof(object),
            Type.EmptyTypes);
        
        var currentGetMethod = enumeratorTypeBuilder.DefineMethod(
            "get_Current",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(object),
            Type.EmptyTypes);
        
        var currentGetIl = currentGetMethod.GetILGenerator();
        currentGetIl.Emit(OpCodes.Ldnull);
        currentGetIl.Emit(OpCodes.Ret);
        
        currentProperty.SetGetMethod(currentGetMethod);
        
        // 定义MoveNextAsync方法
        var moveNextMethod = enumeratorTypeBuilder.DefineMethod(
            "MoveNextAsync",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(ValueTask<bool>),
            Type.EmptyTypes);
        
        var moveNextIl = moveNextMethod.GetILGenerator();
        moveNextIl.Emit(OpCodes.Ldc_I4_0); // false
        moveNextIl.Emit(OpCodes.Newobj, typeof(ValueTask<bool>).GetConstructor([typeof(bool)])!);
        moveNextIl.Emit(OpCodes.Ret);
        
        // 定义DisposeAsync方法
        var disposeMethod = enumeratorTypeBuilder.DefineMethod(
            "DisposeAsync",
            MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig,
            typeof(ValueTask),
            Type.EmptyTypes);
        
        var disposeIl = disposeMethod.GetILGenerator();
        disposeIl.Emit(OpCodes.Newobj, typeof(ValueTask).GetConstructor(Type.EmptyTypes)!);
        disposeIl.Emit(OpCodes.Ret);
        
        // 完成枚举器类型
        var enumeratorType = enumeratorTypeBuilder.CreateType();
        
        // 在GetAsyncEnumerator方法中创建枚举器实例
        var enumeratorConstructor = enumeratorType.GetConstructor(Type.EmptyTypes)!;
        methodIl.Emit(OpCodes.Newobj, enumeratorConstructor);
        methodIl.Emit(OpCodes.Ret);
        
        // 完成类型
        var type = typeBuilder.CreateType()!;
        
        // 在原始方法中创建实例
        ilGenerator.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes)!);
        ilGenerator.Emit(OpCodes.Ret);
    }
    /// <summary>
    /// 作为可调用对象执行，返回下一个值
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="args">参数列表（生成器调用不需要参数）</param>
    /// <param name="obj">对象实例（生成器调用不需要）</param>
    /// <returns>生成器的下一个值</returns>
    public LangValueType Run(VariateManager manager, List<LangExpression> args, object? obj = null)
    {
        // 异步生成器调用不需要参数，忽略args
        return Accept(new InterpreterVisitor(manager));
    }
    /// <summary>
    /// 异步运行生成器，返回下一个值的 Task
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>包含下一个值的 TaskLangValue</returns>
    public TaskLangValue RunAsync(VariateManager manager)
    {
        // 如果状态机还未初始化，则创建它
        if (StateMachine is null)
        {
            // 优先使用AsyncFunc捕获的闭包，如果没有则使用传入的manager
            var baseManager = AsyncFunc.CapturedScope ?? manager;

            // 为生成器创建独立的变量环境
            var generatorManager = baseManager.CloneForGenerator();

            // 设置参数值到生成器环境中
            foreach (var (paramName, paramValue) in ParameterValues)
            {
                generatorManager.Set(new LangId(paramName), paramValue);
            }

            // 创建异步状态机
            StateMachine = new AsyncGeneratorStateMachine(AsyncFunc, generatorManager,
                CancellationTokenSource?.Token ?? CancellationToken.None);
        }

        // 异步获取下一个值
        var task = Task.Run(async () =>
        {
            if (await StateMachine.MoveNextAsync())
            {
                // 还有更多值
                State = AsyncGeneratorState.Suspended;
                NextValue = StateMachine.Current;
                return NextValue ?? new VoidLangValue();
            }

            // 生成器完成
            State = AsyncGeneratorState.Completed;
            return new VoidLangValue();
        }, CancellationTokenSource?.Token ?? CancellationToken.None);

        return new TaskLangValue(task, CancellationTokenSource?.Token ?? CancellationToken.None, Position);
    }
}
