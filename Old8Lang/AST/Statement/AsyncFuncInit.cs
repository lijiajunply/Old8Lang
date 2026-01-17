using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步函数声明语句
/// 表示 async func 定义
/// </summary>
public partial class AsyncFuncInit : OldStatement
{
    public readonly AsyncFuncLangValue AsyncFuncValue;

    /// <summary>
    /// 判断是否为 Lambda 表达式
    /// </summary>
    public bool IsLambda => AsyncFuncValue.Id is null;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncFuncInit(AsyncFuncLangValue funcValue, SourcePosition position = default)
        : base(position)
    {
        AsyncFuncValue = funcValue;
    }

    /// <summary>
    /// 解释执行：注册异步函数到变量管理器
    /// </summary>
    public override void Run(VariateManager manager)
    {
        // 检查函数重复声明
        if (AsyncFuncValue.Id is not null)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is AsyncFuncLangValue func &&
                func.Id?.IdName == AsyncFuncValue.Id.IdName &&
                func.Ids?.Count == AsyncFuncValue.Ids?.Count);

            if (existingFunc is not null)
            {
                throw new DuplicateNameError(
                    this,
                    AsyncFuncValue.Id.IdName,
                    "异步函数"
                );
            }
        }

        // 应用装饰器（如果有）
        var finalFunc = ApplyDecorators(AsyncFuncValue, manager);

        // 添加到导入信息列表
        manager.AddClassAndFunc(finalFunc);
    }

    /// <summary>
    /// 应用装饰器到异步函数
    /// </summary>
    private AsyncFuncLangValue ApplyDecorators(AsyncFuncLangValue originalFunc, VariateManager manager)
    {
        if (originalFunc.Decorators is null || originalFunc.Decorators.Count == 0)
        {
            return originalFunc;
        }

        // 对于异步函数，装饰器的处理稍微复杂一些
        // 因为装饰器可能返回普通函数或异步函数
        // 这里我们简化处理：装饰器必须返回异步函数

        LangValueType currentFunc = originalFunc;

        // 从下到上应用装饰器（最接近函数的装饰器最先应用）
        for (int i = originalFunc.Decorators.Count - 1; i >= 0; i--)
        {
            var decorator = originalFunc.Decorators[i];
            currentFunc = ApplySingleDecorator(decorator, currentFunc, manager);
        }

        // 确保最终结果是异步函数
        if (currentFunc is not AsyncFuncLangValue asyncFunc)
        {
            throw new InvalidOperationError(originalFunc.Position, "异步函数的装饰器必须返回异步函数");
        }

        return asyncFunc;
    }

    /// <summary>
    /// 应用单个装饰器
    /// </summary>
    private Old8Lang.AST.Expression.LangValueType ApplySingleDecorator(FunctionDecorator decorator, Old8Lang.AST.Expression.LangValueType targetFunc, VariateManager manager)
    {
        // 准备装饰器调用参数
        var args = new List<LangExpression>();

        // 将目标函数临时注册
        var tempVarName = $"__decorator_target_{Guid.NewGuid():N}";
        if (targetFunc is AsyncFuncLangValue asyncTarget)
        {
            var tempAsyncFunc = new AsyncFuncLangValue(
                new LangId(tempVarName, position: decorator.Position),
                asyncTarget.Ids,
                asyncTarget.BlockStatement,
                asyncTarget.Position
            );
            manager.AddClassAndFunc(tempAsyncFunc);
        }
        else if (targetFunc is FuncLangValue funcTarget)
        {
            var tempFunc = new FuncLangValue(
                new LangId(tempVarName, position: decorator.Position),
                funcTarget.Ids ?? [],
                funcTarget.BlockStatement,
                funcTarget.GenericParameters,
                funcTarget.Position,
                isLambda: false
            );
            manager.AddClassAndFunc(tempFunc);
        }
        args.Add(new LangId(tempVarName, position: decorator.Position));

        // 添加装饰器参数（如果有）
        if (decorator.Arguments is not null)
        {
            args.AddRange(decorator.Arguments);
        }

        // 创建函数调用表达式来调用装饰器
        var callExpr = new FunctionCallExpression(
            new LangId(decorator.Name, position: decorator.Position),
            args,
            decorator.Position
        );

        // 执行装饰器调用
        var result = callExpr.Run(manager);

        // 注意：我们不清理临时变量，因为它的名称是唯一的，不会冲突

        return result;
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// </summary>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取方法的名称
        var methodName = AsyncFuncValue.Id!.IdName;

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = AsyncFuncValue.Ids!.Select(item => item.OutputType(local)).ToArray();

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        // 但需要保留DelegateVar、ClassVar等全局信息，以便函数内部可以调用其他函数
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

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < AsyncFuncValue.Ids!.Count; i++)
        {
            var id = AsyncFuncValue.Ids[i];
            var paramType = parameterTypes[i];
            // 存储参数类型，用于后续推断返回类型
            funcLocal.LocalVarTypes[id.IdName] = paramType;
        }

        // 异步函数返回Task<object>
        var returnType = typeof(Task<object>);

        // 定义新的动态方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes,
            true
        );

        // 获取动态方法的IL生成器
        var methodIl = dynamicMethod.GetILGenerator();

        // 清空funcLocal，重新添加参数（这次使用真正的LocalBuilder）
        funcLocal.LocalVar.Clear();

        // 处理参数
        for (var i = 0; i < AsyncFuncValue.Ids!.Count; i++)
        {
            var id = AsyncFuncValue.Ids[i];
            // 使用实际的参数类型声明局部变量
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            // 加载参数并存储到局部变量
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        // 生成异步方法体
        GenerateAsyncMethodBody(methodIl, funcLocal);

        // 将方法添加到本地变量管理器
        // 使用函数名+参数类型作为键，支持更准确的函数重载
        var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
        var delegateKey = $"{methodName}${paramTypeNames}";
        local.DelegateVar.TryAdd(delegateKey, dynamicMethod);

        funcLocal.DelegateVar.TryAdd(delegateKey, dynamicMethod);
        if (Old8Lang.Compiler.Compiler.EnableAsyncStateMachineAwait)
        {
            CompiledDelegateRegistry.Register(delegateKey, dynamicMethod);
        }

        // 同时存储函数的参数列表信息
        if (AsyncFuncValue.Ids is not null)
        {
            local.FuncParameters.TryAdd(delegateKey, AsyncFuncValue.Ids);
            funcLocal.FuncParameters.TryAdd(delegateKey, AsyncFuncValue.Ids);
        }
    }

    /// <summary>
    /// 生成异步方法体的IL代码
    /// </summary>
    private void GenerateAsyncMethodBody(ILGenerator ilGenerator, LocalManager local)
    {
        AsyncFuncValue.GenerateMethodBody(ilGenerator, local);
    }

    /// <summary>
    /// 实现MoveNext方法
    /// </summary>
    private void ImplementMoveNext(
        ILGenerator ilGenerator,
        Type stateMachineType,
        FieldInfo stateField,
        FieldInfo builderField,
        FieldInfo awaiterField,
        LocalManager local
    )
    {
        // 简化实现：这个方法暂时不会被调用
        ilGenerator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// AST 子节点访问（无子节点）
    /// </summary>
    public override OldStatement? this[int index] => null;

    /// <summary>
    /// AST 子节点数量
    /// </summary>
    public override int Count => 0;
}
