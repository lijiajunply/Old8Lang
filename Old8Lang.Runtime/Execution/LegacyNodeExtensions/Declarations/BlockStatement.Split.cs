using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// BlockStatement 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class BlockStatement
{
    /// <summary>
    /// 只执行声明类语句（函数、类、异步函数、extern、枚举、扩展方法、动态导入），
    /// 跳过赋值、打印、循环等有副作用的顶层语句。
    /// 用于 IL 模式编译前的解释器预执行：既要让装饰器 / 扩展方法在编译期可见，
    /// 又不能把顶层代码执行两遍。
    /// </summary>
    /// <param name="manager">变量管理器</param>
    public void ExecuteDeclarations(VariateManager manager)
    {
        // ImportStatements 里只包含声明与导入；导入语句由调用方单独处理
        foreach (var statement in ImportStatements)
        {
            if (statement is not ImportStatement && statement is not NativeStatement)
            {
                statement.Accept(new InterpreterVisitor(manager));
            }
        }

        // OtherStatements 里绝大多数是有副作用的普通语句，只挑出声明类节点执行
        foreach (var statement in OtherStatements)
        {
            switch (statement)
            {
                case EnumInit:
                case ExtensionDeclaration:
                case ImportStatement { IsDynamicImport: true }:
                    statement.Accept(new InterpreterVisitor(manager));
                    break;
            }
        }
    }
    /// <summary>
    /// 执行模块的非导入语句，包括函数定义、类定义和变量赋值
    /// 但跳过导入语句，避免递归导入
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <param name="skipFunctionClassInit">是否跳过函数定义和类定义，只执行变量赋值语句</param>
    public void ExecuteModule(VariateManager manager, bool skipFunctionClassInit = false)
    {
        // 执行ImportStatements中的非导入语句（函数定义、类定义）
        foreach (var statement in ImportStatements)
        {
            // 只跳过 ImportStatement 和 NativeStatement
            if (statement is not ImportStatement && statement is not NativeStatement)
            {
                // 如果跳过函数和类定义，则只执行其他类型的语句
                if (skipFunctionClassInit)
                {
                    // 跳过 FuncInit 和 ClassInit，只执行其他类型的语句
                    if (statement is not FuncInit && statement is not ClassInit)
                    {
                        statement.Accept(new InterpreterVisitor(manager));
                    }
                }
                else
                {
                    // 执行所有非导入语句，包括 FuncInit 和 ClassInit
                    statement.Accept(new InterpreterVisitor(manager));
                }
            }
        }

        // 执行所有OtherStatements（变量赋值等）
        foreach (var statement in OtherStatements)
        {
            statement.Accept(new InterpreterVisitor(manager));

            if (manager.IsReturn)
            {
                return;
            }
        }
    }
    /// <summary>
    /// 生成模块导入的 IL。
    /// </summary>
    /// <param name="includeBody">
    /// 导入模块时传 true：模块顶层的变量赋值等语句也要内联，否则模块别名访问不到模块变量。
    /// 主程序块传 false（其顶层语句由 VisitBlockStatement 正常编译）。
    /// </param>
    public void GenerateImportIl(ILGenerator ilGenerator, LocalManager local, bool includeBody = false)
    {
        foreach (var statement in ImportStatements)
        {
            statement.Accept(new CompilerVisitor(ilGenerator, local));
        }

        if (!includeBody) return;

        foreach (var statement in OtherStatements)
        {
            // 导入语句已由上面的循环处理，避免重复
            if (statement is ImportStatement) continue;
            statement.Accept(new CompilerVisitor(ilGenerator, local));
        }
    }
}
