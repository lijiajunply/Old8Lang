using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Statement 节点的 Visit 方法实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 BreakStatement 节点
    /// </summary>
    public object? VisitBreakStatement(BreakStatement node)
    {
        // 迁移自 BreakStatement.GenerateIl()
        if (_local.BreakLabel.HasValue)
        {
            _ilGenerator.Emit(OpCodes.Br, _local.BreakLabel.Value);
        }
        else
        {
            throw new InvalidOperationError(node.Position, "Break statement outside of loop",
                "break语句只能在循环内部使用");
        }
        return null;
    }

    /// <summary>
    /// 访问 ContinueStatement 节点
    /// </summary>
    public object? VisitContinueStatement(ContinueStatement node)
    {
        // 迁移自 ContinueStatement.GenerateIl()
        if (_local.ContinueLabel.HasValue)
        {
            _ilGenerator.Emit(OpCodes.Br, _local.ContinueLabel.Value);
        }
        else
        {
            throw new InvalidOperationError(node.Position, "Continue statement outside of loop",
                "continue语句只能在循环内部使用");
        }
        return null;
    }

    /// <summary>
    /// 访问 IfStatement 节点
    /// </summary>
    public object? VisitIfStatement(IfStatement node)
    {
        // 迁移自 IfStatement.GenerateIl()
        var labelElse = _ilGenerator.DefineLabel();
        var labelEnd = _ilGenerator.DefineLabel();

        // 处理 if 块
        var ifChild = node[0] as IfChild;
        if (ifChild != null)
        {
            ifChild.GenerateConditionIl(_ilGenerator, _local);
            _ilGenerator.Emit(OpCodes.Brfalse, labelElse);

            // if 部分
            ifChild.GenerateIl(_ilGenerator, _local);
            _ilGenerator.Emit(OpCodes.Br, labelEnd);
        }

        // 处理 elif 块
        _ilGenerator.MarkLabel(labelElse);
        for (int i = 1; i < node.Count; i++)
        {
            var elifChild = node[i] as IfChild;
            if (elifChild != null)
            {
                var nextElif = _ilGenerator.DefineLabel();
                elifChild.GenerateConditionIl(_ilGenerator, _local);
                _ilGenerator.Emit(OpCodes.Brfalse, nextElif);

                // elif 部分
                elifChild.GenerateIl(_ilGenerator, _local);
                _ilGenerator.Emit(OpCodes.Br, labelEnd);

                _ilGenerator.MarkLabel(nextElif);
            }
            else if (node[i] is BlockStatement elseBlock)
            {
                // 处理 else 块
                elseBlock.Accept(this);
            }
        }

        // 结束标签
        _ilGenerator.MarkLabel(labelEnd);
        return null;
    }

    /// <summary>
    /// 访问 BlockStatement 节点
    /// </summary>
    public object? VisitBlockStatement(BlockStatement node)
    {
        // 迁移自 BlockStatement.GenerateIl()
        // 先生成导入语句的IL
        node.GenerateImportIl(_ilGenerator, _local);

        // 生成其他语句的IL
        for (int i = 0; i < node.Count; i++)
        {
            var statement = node[i];
            if (statement != null)
            {
                statement.Accept(this);
            }
        }

        return null;
    }

    /// <summary>
    /// 访问 WhileStatement 节点（编译器）
    /// </summary>
    public object? VisitWhileStatement(WhileStatement node)
    {
        // 迁移自 WhileStatement.GenerateIl()
        // 由于 WhileStatement 使用主构造函数参数，无法通过索引访问子节点
        // 暂时调用原方法
        node.GenerateIl(_ilGenerator, _local);
        return null;
    }

    /// <summary>
    /// 访问 ForStatement 节点（编译器）
    /// </summary>
    public object? VisitForStatement(ForStatement node)
    {
        // 迁移自 ForStatement.GenerateIl()
        // 由于 ForStatement 使用主构造函数参数，无法通过索引访问子节点
        // 暂时调用原方法
        node.GenerateIl(_ilGenerator, _local);
        return null;
    }

    /// <summary>
    /// 访问 ForInStatement 节点（编译器）
    /// </summary>
    public object? VisitForInStatement(ForInStatement node)
    {
        // 迁移自 ForInStatement.GenerateIl()
        // ForInStatement 逻辑非常复杂（包含字典特殊处理等）
        // 暂时调用原方法
        node.GenerateIl(_ilGenerator, _local);
        return null;
    }

    /// <summary>
    /// 访问 SetStatement 节点（编译器）
    /// </summary>
    public object? VisitSetStatement(SetStatement node)
    {
        // 迁移自 SetStatement.GenerateIl()
        // SetStatement 的逻辑已经封装在其 GenerateIl 方法中，直接调用
        node.GenerateIl(_ilGenerator, _local);
        return null;
    }

    /// <summary>
    /// 访问 ReturnStatement 节点（编译器）
    /// </summary>
    public object? VisitReturnStatement(ReturnStatement node)
    {
        // 迁移自 ReturnStatement.GenerateIl()
        // ReturnStatement 的逻辑已经封装在其 GenerateIl 方法中，直接调用
        node.GenerateIl(_ilGenerator, _local);
        return null;
    }

    /// <summary>
    /// 访问 ThrowStatement 节点（编译器）
    /// </summary>
    public object? VisitThrowStatement(ThrowStatement node)
    {
        // 迁移自 ThrowStatement.GenerateIl()
        // ThrowStatement 的逻辑已经封装在其 GenerateIl 方法中，直接调用
        node.GenerateIl(_ilGenerator, _local);
        return null;
    }
}
