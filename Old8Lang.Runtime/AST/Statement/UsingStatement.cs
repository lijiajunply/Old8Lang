using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Concurrency;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Using语句，用于资源自动管理（类似于C#的using语句）
/// </summary>
/// <param name="variableName">可选的变量名，用于存储资源</param>
/// <param name="resourceExpression">返回资源ID的表达式</param>
/// <param name="blockStatement">using块中的语句</param>
/// <param name="position">位置信息</param>
public partial class UsingStatement(
    string? variableName,
    LangExpression resourceExpression,
    BlockStatement blockStatement,
    SourcePosition position = default) : OldStatement(position)
{

    /// <summary>
    /// 获取变量名（可能为null）
    /// </summary>
    public string? VariableName => variableName;

    /// <summary>
    /// 获取资源表达式
    /// </summary>
    public LangExpression ResourceExpression => resourceExpression;

    /// <summary>
    /// 获取using块
    /// </summary>
    public BlockStatement BlockStatement => blockStatement;



    /// <summary>
    /// 释放资源
    /// </summary>
    internal void DisposeResource(LangValueType resource)
    {
        // 1. 如果是整数值（资源ID），尝试通过ResourceManager释放
        if (resource is IntLangValue intVal)
        {
            ResourceManager.TryDispose(intVal.Value);
        }
        // 2. 如果是 AnyLangValue（用户自定义类实例），尝试调用 dispose 方法
        else if (resource is AnyLangValue anyValue)
        {
            anyValue.TryDispose();
        }
        // 3. 如果实现了IDisposable接口，直接调用Dispose
        else if (resource is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public override OldStatement this[int index]
    {
        get
        {
            // Using 语句只包含一个块语句
            if (index == 0)
            {
                return blockStatement;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<OldStatement>());
        }
    }

    public override int Count => 1; // 只有 using 块
}