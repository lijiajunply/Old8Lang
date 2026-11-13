using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Interpreter;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 异步流表达式
/// 表示 async { block } 语法，创建一个异步流（异步生成器）
/// </summary>
public partial class AsyncStreamExpression : LangExpression
{
    internal readonly BlockStatement _block;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="block">异步流体</param>
    /// <param name="position">源代码位置</param>
    public AsyncStreamExpression(BlockStatement block, SourcePosition position = default)
        : base(position)
    {
        _block = block;
    }

    public override string ToString()
    {
        return $"async {{ {_block} }}";
    }
}