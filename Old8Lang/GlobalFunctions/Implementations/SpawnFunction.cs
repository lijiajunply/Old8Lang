using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.GlobalFunctions.Implementations;

/// <summary>
/// Spawn 函数 - 创建新线程执行函数
/// </summary>
public sealed class SpawnFunction : BaseGlobalFunction
{
    public override string[] Names => ["Spawn", "spawn"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => -1;

    protected override LangValueType ExecuteInternal(List<LangExpression> parameters, VariateManager manager, SourcePosition position)
    {
        // 确保参数数量至少为1
        if (parameters.Count == 0)
        {
            throw new ArgumentError(position, "spawn 函数需要至少一个参数");
        }

        // 获取第一个参数，应该是一个函数
        var funcExpr = parameters[0];

        // 检查是否是成员方法访问（obj.method）
        AnyLangValue? instanceContext = null;
        if (funcExpr is Operation { Opera: LangTokenType.Dot } dotOp)
        {
            // 获取实例（左操作数）
            var instanceValue = dotOp.Left?.Run(manager);
            if (instanceValue is AnyLangValue anyInstance)
            {
                instanceContext = anyInstance;
            }
        }

        var funcValue = funcExpr.Run(manager);

        // 检查函数类型
        FuncLangValue? spawnFunc = null;
        AsyncFuncLangValue? asyncSpawnFunc = null;

        if (funcValue is FuncLangValue func)
        {
            spawnFunc = func;
        }
        else if (funcValue is AsyncFuncLangValue asyncFunc)
        {
            asyncSpawnFunc = asyncFunc;
        }
        else
        {
            throw new TypeError(parameters[0], "FuncValue or AsyncFuncValue", funcValue.GetType().Name);
        }

        // 创建线程参数列表（跳过第一个函数参数）
        var threadArgs = parameters.Skip(1).ToList();

        // 创建新的变量管理器，复制当前管理器的状态
        var threadManager = manager.Clone();

        // 如果有实例上下文，需要在线程管理器中设置this
        if (instanceContext != null)
        {
            threadManager.Set(new LangId("this"), instanceContext);
        }

        // 使用临时变量来存储线程对象，避免闭包引用问题
        ThreadLangValue? tempThread = null;

        // 创建取消令牌源
        var cts = new CancellationTokenSource();

        // 统一创建 ThreadLangValue，内部根据函数类型调用不同的执行方法
        // 无参数情况
        tempThread = threadArgs.Count == 0
            ? new ThreadLangValue(ThreadCallback, parameters[0].Position, cts.Token)
            : new ThreadLangValue(_ => ThreadCallback(), null, parameters[0].Position, cts.Token); // 带参数情况

        // 赋值给最终的线程变量
        var thread = tempThread;

        // 设置外部管理器
        thread.ExternalManager = manager;

        var result = thread;
        return result;

        void ThreadCallback()
        {
            try
            {
                LangValueType funcResult;

                if (asyncSpawnFunc != null)
                {
                    // 异步函数：调用 RunAsync 并等待完成
                    var taskValue = asyncSpawnFunc.RunAsync(threadManager, threadArgs, cts.Token);
                    // 等待任务完成并获取结果
                    funcResult = taskValue.Await();
                }
                else if (spawnFunc != null)
                {
                    // 普通函数：直接调用
                    funcResult = spawnFunc.Run(threadManager, threadArgs, instanceContext);
                }
                else
                {
                    throw new InvalidOperationException("未知的函数类型");
                }

                // 设置线程结果
                // 直接保存 funcResult 对象，不进行转换
                tempThread?.SetResult(funcResult);
            }
            catch (Exception ex)
            {
                // 设置线程异常
                tempThread?.SetException(ex);
            }
        }
    }

    protected override void GenerateIlInternal(List<LangExpression> parameters, ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 编译模式暂不支持线程创建
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ThreadLangValue);
    }
}