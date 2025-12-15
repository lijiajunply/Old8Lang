using Old8Lang.AST;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 异步函数值类型
/// 表示一个异步函数，可以被调用并返回 TaskLangValue
/// </summary>
public class AsyncFuncLangValue : ImportInfo
{
    public readonly LangId? Id;
    public readonly List<LangId>? Ids;
    public readonly BlockStatement BlockStatement;

    // 闭包环境：捕获的作用域
    private VariateManager? CapturedScope { get; init; }

    // 默认参数值缓存
    private Dictionary<int, LangValueType>? CachedDefaultValues { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncFuncLangValue(
        LangId? id,
        List<LangId>? ids,
        BlockStatement blockStatement,
        SourcePosition position = default)
        : base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
    }

    /// <summary>
    /// Run 方法：返回捕获了闭包的异步函数副本
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        // 创建新的异步函数副本，捕获当前作用域
        var closureFunc = new AsyncFuncLangValue(Id, Ids, BlockStatement, Position)
        {
            CapturedScope = manager.CaptureForClosure()
        };
        return closureFunc;
    }

    /// <summary>
    /// 调用异步函数，返回 Task
    /// </summary>
    /// <param name="variateManagerFunc">调用时的变量管理器</param>
    /// <param name="ids">参数表达式列表</param>
    /// <returns>包含异步操作的 TaskLangValue</returns>
    public TaskLangValue RunAsync(VariateManager variateManagerFunc, List<LangExpression> ids)
    {
        // 创建 .NET Task
        var task = Task.Run(() =>
        {
            try
            {
                // 参数数量检查
                if (Ids != null && ids.Count > Ids.Count)
                {
                    throw new ArgumentError(
                        Position,
                        $"异步函数 '{Id?.IdName ?? "anonymous"}' 期望最多 {Ids.Count} 个参数，但实际提供了 {ids.Count} 个参数"
                    );
                }

                // 为异步执行创建独立的 VariateManager 实例，确保状态隔离
                var baseManager = CapturedScope ?? variateManagerFunc;
                var executionManager = baseManager.NewManger();
                
                // 重置返回状态，确保异步函数体能够正常执行
                executionManager.IsReturn = false;

                // 为异步执行创建独立的调用栈上下文
                var asyncCallStack = new List<CallStackFrame>(Old8Exception.CurrentCallStack);

                // 临时替换全局调用栈
                var originalCallStack = Old8Exception.CurrentCallStack;
                Old8Exception.CurrentCallStack = asyncCallStack;

                try
                {
                    // 增加递归深度
                    executionManager.RecursionDepth++;

                    // 入栈
                    Old8Exception.PushCallStack(Id?.IdName ?? "anonymous async", Position);

                    // 添加新作用域
                    executionManager.AddChildren();
                    executionManager.IsFunc = true;

                    try
                    {
                        // 处理参数
                        if (Ids != null && Ids.Count != 0)
                        {
                            // 初始化默认参数值缓存（仅在首次调用时）
                            if (CachedDefaultValues == null && Ids.Any(id => id.DefaultValue != null))
                            {
                                InitializeDefaultValueCache(executionManager);
                            }

                            // 评估参数
                            var paramValues = ids.Select(t => t.Run(variateManagerFunc)).ToList();

                            // 补全默认参数
                            for (var i = paramValues.Count; i < Ids.Count; i++)
                            {
                                var id = Ids[i];
                                if (id.DefaultValue != null)
                                {
                                    // 优先使用缓存值（常量表达式）
                                    if (CachedDefaultValues?.TryGetValue(i, out var cachedValue) == true)
                                    {
                                        paramValues.Add(cachedValue);
                                    }
                                    else
                                    {
                                        paramValues.Add(id.DefaultValue.Run(executionManager));
                                    }
                                }
                                else
                                {
                                    throw new ArgumentError(
                                        Position,
                                        $"异步函数 '{Id?.IdName ?? "anonymous"}' 的参数 '{id.IdName}' 缺少实参且没有默认值"
                                    );
                                }
                            }

                            // 设置参数到作用域
                        for (var i = 0; i < Ids.Count; i++)
                        {
                            executionManager.Set(Ids[i], paramValues[i]);
                        }
                    }

                            // 执行函数体，保持 IsFunc = true
                        BlockStatement.Run(executionManager);

                        // 保存返回值（在清理之前）
                        var result = executionManager.Result;

                        return result;
                    }
                    finally
                    {
                        // 清理资源
                        executionManager.IsReturn = false;
                        executionManager.IsFunc = false;
                        executionManager.RemoveChildren();
                    }
                }
                finally
                {
                    executionManager.RecursionDepth--;
                    Old8Exception.PopCallStack();

                    // 恢复原始调用栈
                    Old8Exception.CurrentCallStack = originalCallStack;
                }
            }
            catch (Exception ex)
            {
                // 异常会被 Task 捕获并在 await 时重新抛出
                throw;
            }
        });

        return new TaskLangValue(task, Position);
    }

    /// <summary>
    /// 初始化默认参数值缓存
    /// </summary>
    private void InitializeDefaultValueCache(VariateManager manager)
    {
        CachedDefaultValues = new Dictionary<int, LangValueType>();

        for (int i = 0; i < Ids!.Count; i++)
        {
            var param = Ids[i];
            if (param.DefaultValue != null && IsConstantExpression(param.DefaultValue))
            {
                var defaultValue = param.DefaultValue.Run(manager);
                CachedDefaultValues[i] = defaultValue;
            }
        }
    }

    /// <summary>
    /// 判断是否为常量表达式
    /// </summary>
    private static bool IsConstantExpression(LangExpression? expr)
    {
        return expr switch
        {
            IntLangValue or DoubleLangValue or StringLangValue or BoolLangValue or CharLangValue => true,
            Operation op => IsConstantExpression(op.Left) && IsConstantExpression(op.Right),
            _ => false
        };
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(Position, "编译模式暂不支持异步函数");
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        return typeof(Task<object>);
    }
}
