using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Generators;

/// <summary>
/// 异步生成器对象，用于表示异步生成器函数的实例，实现ILangList接口以支持异步迭代
/// 类似于 C# 的 IAsyncEnumerable&lt;T&gt;
/// </summary>
public partial class AsyncGeneratorLangValue : LangValueType, ILangList
{
    /// <summary>
    /// 异步函数引用
    /// </summary>
    public AsyncFuncLangValue AsyncFunc { get; init; }

    /// <summary>
    /// 异步生成器状态机
    /// </summary>
    private AsyncGeneratorStateMachine? StateMachine { get; set; }

    /// <summary>
    /// 生成器当前状态
    /// </summary>
    public AsyncGeneratorState State { get; set; } = AsyncGeneratorState.Suspended;

    /// <summary>
    /// 生成器迭代器的下一个值
    /// </summary>
    public LangValueType? NextValue { get; set; }

    /// <summary>
    /// 生成器函数的参数值
    /// </summary>
    private Dictionary<string, LangValueType> ParameterValues { get; } = new();

    /// <summary>
    /// 取消令牌源，用于取消异步操作
    /// </summary>
    private CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// 异步生成器状态枚举
    /// </summary>
    public enum AsyncGeneratorState
    {
        Suspended, // 已暂停，等待下一个值
        Running, // 正在执行
        Completed // 已完成
    }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="asyncFunc">异步函数引用</param>
    /// <param name="position">源代码位置</param>
    public AsyncGeneratorLangValue(AsyncFuncLangValue asyncFunc, SourcePosition position = default) : base(position)
    {
        AsyncFunc = asyncFunc;
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 设置生成器函数的参数值
    /// </summary>
    /// <param name="paramName">参数名称</param>
    /// <param name="value">参数值</param>
    public void SetParameter(string paramName, LangValueType value)
    {
        ParameterValues[paramName] = value;
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

    /// <summary>
    /// 同步运行方法，用于向后兼容，实际会阻塞等待异步操作完成
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>生成器的下一个值</returns>
    public override LangValueType Run(VariateManager manager)
    {
        // 调用异步方法并阻塞等待
        var taskResult = RunAsync(manager);
        return taskResult.Await();
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
        return Run(manager);
    }

    /// <summary>
    /// 取消异步生成器的执行
    /// </summary>
    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
    }

    /// <summary>
    /// 重置生成器状态
    /// </summary>
    public void Reset()
    {
        State = AsyncGeneratorState.Suspended;
        NextValue = null;
        StateMachine?.Reset();

        // 创建新的取消令牌源
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// 获取生成器的输出类型
    /// </summary>
    /// <param name="local">局部变量管理器</param>
    /// <returns>生成器的输出类型</returns>
    public override Type OutputType(LocalManager local) => typeof(object);

    /// <summary>
    /// 生成IL代码（编译器模式）
    /// 生成异步生成器的委托，支持异步生成器的编译
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 异步生成器在编译器模式下返回一个可以迭代的对象
        // 我们创建一个委托，当调用时会返回一个IAsyncEnumerable<object>
        
        // 1. 确定参数类型和返回类型
        var parameterTypes = AsyncFunc.Ids?.Select(item => item.OutputType(local)).ToArray() ?? Array.Empty<Type>();
        var returnType = typeof(IAsyncEnumerable<object>);

        // 2. 创建 DynamicMethod
        var dynamicMethod = new DynamicMethod(
            $"AsyncGenerator_{AsyncFunc.Id?.IdName ?? "Anonymous"}",
            returnType,
            parameterTypes,
            true
        );

        // 3. 生成方法体
        var methodIl = dynamicMethod.GetILGenerator();

        // 创建新的LocalManager实例，专门用于生成器函数体的IL生成
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
        if (AsyncFunc.Ids != null)
        {
            for (var i = 0; i < AsyncFunc.Ids.Count; i++)
            {
                var id = AsyncFunc.Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType);
                funcLocal.AddLocalVar(id.IdName, localVar);
                funcLocal.LocalVarTypes[id.IdName] = paramType;

                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }
        }

        // 生成异步生成器的方法体
        GenerateGeneratorMethodBody(methodIl, funcLocal);

        // 4. 创建委托并加载到栈上
        var delegateType = System.Linq.Expressions.Expression.GetDelegateType(
            parameterTypes.Concat([returnType]).ToArray());

        ilGenerator.Emit(OpCodes.Ldnull); // target (null for static method)
        ilGenerator.Emit(OpCodes.Ldftn, dynamicMethod);
        ilGenerator.Emit(OpCodes.Newobj, delegateType.GetConstructors()[0]);
    }

    /// <summary>
    /// 生成异步生成器方法体
    /// </summary>
    private void GenerateGeneratorMethodBody(ILGenerator ilGenerator, LocalManager local)
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
    /// 设置值到IL代码
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="idName">标识符名称</param>
    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 异步生成器不支持赋值操作
        throw new NotSupportedException("异步生成器不支持赋值操作");
    }

    /// <summary>
    /// 转换为字符串
    /// </summary>
    /// <returns>异步生成器的字符串表示</returns>
    public override string ToString() => $"AsyncGenerator({AsyncFunc.Id?.IdName ?? "anonymous"})";

    /// <summary>
    /// 获取生成器的所有项
    /// </summary>
    /// <returns>生成器项的枚举</returns>
    public IEnumerable<LangValueType> GetItems()
    {
        // 异步生成器的迭代逻辑由AsyncForInStatement处理，这里只返回空枚举
        // 避免在迭代过程中影响生成器的状态
        yield break;
    }

    /// <summary>
    /// 获取生成器的长度
    /// </summary>
    /// <returns>生成器的长度，-1表示未知长度</returns>
    public int GetLength()
    {
        // 异步生成器的长度通常是未知的，返回-1表示未知长度
        return -1;
    }

    /// <summary>
    /// 对生成器进行切片（带步长）
    /// </summary>
    /// <param name="start">起始索引</param>
    /// <param name="end">结束索引</param>
    /// <param name="step">步长</param>
    /// <returns>切片后的生成器</returns>
    public LangValueType Slice(int start, int end, int step)
    {
        // 异步生成器不支持切片
        throw new NotSupportedException("异步生成器不支持切片操作");
    }

    /// <summary>
    /// 设置生成器中指定索引的值
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <exception cref="NotSupportedException">异步生成器不支持设置值</exception>
    public void Set(LangValueType index, LangValueType value)
    {
        // 异步生成器是只读的，不支持设置值
        throw new NotSupportedException("异步生成器不支持设置值");
    }

    /// <summary>
    /// 切片赋值操作
    /// </summary>
    /// <exception cref="NotSupportedException">异步生成器不支持切片赋值</exception>
    public void SetSlice(int start, int end, IEnumerable<LangValueType> values)
    {
        throw new NotSupportedException("异步生成器不支持切片赋值操作");
    }

    /// <summary>
    /// 检查值是否在生成器中
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <returns>如果值在生成器中则返回true，否则返回false</returns>
    public bool In(LangValueType value)
    {
        // 迭代生成器，检查是否包含指定值
        return GetItems().Any(item => item.ToString() == value.ToString());
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        CancellationTokenSource?.Dispose();
    }
}

/// <summary>
/// 空的异步可枚举实现，用于编译器模式下的异步生成器
/// </summary>
public class EmptyAsyncEnumerable : IAsyncEnumerable<object>
{
    /// <summary>
    /// 获取异步枚举器
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>异步枚举器</returns>
    public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new EmptyAsyncEnumerator();
    }

    /// <summary>
    /// 空的异步枚举器实现
    /// </summary>
    private class EmptyAsyncEnumerator : IAsyncEnumerator<object>
    {
        /// <summary>
        /// 当前值
        /// </summary>
        public object Current => null!;

        /// <summary>
        /// 移动到下一个元素
        /// </summary>
        /// <returns>总是返回false，表示没有更多元素</returns>
        public ValueTask<bool> MoveNextAsync()
        {
            return new ValueTask<bool>(false);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}