using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

using Old8Lang.AST.Visitor;

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
    /// AST 子节点访问（无子节点）
    /// </summary>
    public override OldStatement? this[int index] => null;

    /// <summary>
    /// AST 子节点数量
    /// </summary>
    public override int Count => 0;
}
