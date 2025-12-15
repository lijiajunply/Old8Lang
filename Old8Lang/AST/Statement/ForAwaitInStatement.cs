using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection.Emit;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步 for-in 语句
/// 表示异步迭代循环，支持遍历异步流
/// </summary>
public class ForAwaitInStatement : OldStatement
{
    public readonly LangId Id;
    public readonly LangExpression Expression;
    public readonly OldStatement Body;
    public readonly List<LangId>? AdditionalIds;

    // 获取所有标识符，包括主标识符和附加标识符
    private List<LangId> AllIds => [Id, .. (AdditionalIds ?? [])];

    /// <summary>
    /// 构造函数
    /// </summary>
    public ForAwaitInStatement(
        LangId id,
        LangExpression expression,
        OldStatement body,
        SourcePosition position = default,
        List<LangId>? additionalIds = null)
        : base(position)
    {
        Id = id;
        Expression = expression;
        Body = body;
        AdditionalIds = additionalIds;
    }

    /// <summary>
    /// 解释执行：异步遍历流并执行循环体
    /// </summary>
    public override void Run(VariateManager manager)
    {
        manager.AddChildren();
        // 压入新的控制流状态
        manager.ControlFlowManager.PushState();
        
        try
        {
            // 执行表达式，期望得到 AsyncStreamLangValue
            var value = Expression.Run(manager);
            if (value is not AsyncStreamLangValue asyncStream)
            {
                throw new TypeError(this, "AsyncStream", value.TypeToString());
            }

            // 同步执行异步迭代
            Task.Run(async () =>
            {
                await using var enumerator = asyncStream.GetAsyncEnumerator();
                while (await enumerator.MoveNextAsync())
                {
                    var idValue = enumerator.Current;
                    
                    // 在每次循环迭代开始时重置控制流标志
                    manager.ControlFlowManager.ResetCurrentState();
                    
                    if (AllIds.Count == 1)
                    {
                        // 单个标识符的情况
                        manager.Set(Id, idValue);
                    }
                    else
                    {
                        // 多个标识符的情况，处理键值对
                        if (idValue is TupleLangValue tupleValue)
                        {
                            // 运行元组，获取实际值
                            tupleValue.Run(manager);

                            // 字典键值对，赋值给多个标识符
                            var values = new List<LangValueType> { tupleValue.Value.Item1, tupleValue.Value.Item2 };

                            for (int i = 0; i < AllIds.Count && i < values.Count; i++)
                            {
                                manager.Set(AllIds[i], values[i]);
                            }
                        }
                        else
                        {
                            // 不是键值对，只赋值给第一个标识符
                            manager.Set(Id, idValue);
                        }
                    }

                    Body.Run(manager);
                    
                    // 处理break
                    if (manager.ControlFlowManager.BreakFlag)
                    {
                        break;
                    }
                }
            }).GetAwaiter().GetResult();
        }
        finally
        {
            // 弹出当前控制流状态
            manager.ControlFlowManager.PopState();
            manager.RemoveChildren();
        }
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式暂不支持）
    /// </summary>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedError(Position, "编译模式暂不支持异步 for-in 语句");
    }

    public override OldStatement this[int index] => Body[index]!;

    public override int Count => Body.Count;
}