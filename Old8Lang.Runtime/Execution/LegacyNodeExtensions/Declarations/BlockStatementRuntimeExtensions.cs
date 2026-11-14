using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// BlockStatement 的执行/编译/类成员辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
public static class BlockStatementRuntimeExtensions
{
    /// <summary>
    /// 只执行声明类语句（函数、类、异步函数、extern、枚举、扩展方法、动态导入），
    /// 跳过赋值、打印、循环等有副作用的顶层语句。
    /// 用于 IL 模式编译前的解释器预执行：既要让装饰器 / 扩展方法在编译期可见，
    /// 又不能把顶层代码执行两遍。
    /// </summary>
    /// <param name="block">块语句</param>
    /// <param name="manager">变量管理器</param>
    public static void ExecuteDeclarations(this BlockStatement block, VariateManager manager)
    {
        // ImportStatements 里只包含声明与导入；导入语句由调用方单独处理
        foreach (var statement in block.ImportStatements)
        {
            if (statement is not ImportStatement && statement is not NativeStatement)
            {
                statement.Accept(RuntimeVisitors.CreateInterpreter(manager));
            }
        }

        // OtherStatements 里绝大多数是有副作用的普通语句，只挑出声明类节点执行
        foreach (var statement in block.OtherStatements)
        {
            switch (statement)
            {
                case EnumInit:
                case ExtensionDeclaration:
                case ImportStatement { IsDynamicImport: true }:
                    statement.Accept(RuntimeVisitors.CreateInterpreter(manager));
                    break;
            }
        }
    }

    /// <summary>
    /// 执行模块的非导入语句，包括函数定义、类定义和变量赋值
    /// 但跳过导入语句，避免递归导入
    /// </summary>
    /// <param name="block">块语句</param>
    /// <param name="manager">变量管理器</param>
    /// <param name="skipFunctionClassInit">是否跳过函数定义和类定义，只执行变量赋值语句</param>
    public static void ExecuteModule(this BlockStatement block, VariateManager manager, bool skipFunctionClassInit = false)
    {
        // 执行ImportStatements中的非导入语句（函数定义、类定义）
        foreach (var statement in block.ImportStatements)
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
                        statement.Accept(RuntimeVisitors.CreateInterpreter(manager));
                    }
                }
                else
                {
                    // 执行所有非导入语句，包括 FuncInit 和 ClassInit
                    statement.Accept(RuntimeVisitors.CreateInterpreter(manager));
                }
            }
        }

        // 执行所有OtherStatements（变量赋值等）
        foreach (var statement in block.OtherStatements)
        {
            statement.Accept(RuntimeVisitors.CreateInterpreter(manager));

            if (manager.IsReturn)
            {
                return;
            }
        }
    }

    /// <summary>
    /// 生成模块导入的 IL。
    /// </summary>
    /// <param name="block">块语句</param>
    /// <param name="ilGenerator">IL 生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <param name="includeBody">
    /// 导入模块时传 true：模块顶层的变量赋值等语句也要内联，否则模块别名访问不到模块变量。
    /// 主程序块传 false（其顶层语句由 VisitBlockStatement 正常编译）。
    /// </param>
    public static void GenerateImportIl(this BlockStatement block, ILGenerator ilGenerator, LocalManager local,
        bool includeBody = false)
    {
        foreach (var statement in block.ImportStatements)
        {
            statement.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        }

        if (!includeBody) return;

        foreach (var statement in block.OtherStatements)
        {
            // 导入语句已由上面的循环处理，避免重复
            if (statement is ImportStatement) continue;
            statement.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
        }
    }

    /// <summary>
    /// 获取实例成员字典
    /// </summary>
    public static Dictionary<ClassMemberId, LangExpression> ToAnyData(this BlockStatement block)
    {
        var c = new Dictionary<ClassMemberId, LangExpression>();

        // 处理所有语句，筛选出非静态成员
        foreach (var x in block.OtherStatements.Concat(block.ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id is null || expr is null) continue;
            // 只添加非静态成员
            if (!id.HasModifier(AccessModifierType.Static))
            {
                AddMember(c, id, expr);
            }
        }

        return c;
    }

    /// <summary>
    /// 获取静态成员字典
    /// </summary>
    public static Dictionary<ClassMemberId, LangExpression> ToStaticData(this BlockStatement block)
    {
        var c = new Dictionary<ClassMemberId, LangExpression>();

        // 处理所有语句，筛选出静态成员
        foreach (var x in block.OtherStatements.Concat(block.ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id is null || expr is null) continue;
            // 只添加静态成员
            if (id.HasModifier(AccessModifierType.Static))
            {
                AddMember(c, id, expr);
            }
        }

        return c;
    }

    private static void AddMember(Dictionary<ClassMemberId, LangExpression> c, ClassMemberId id, LangExpression expr)
    {
        // 如果是函数重载，需要特殊处理
        if (IsFunction(expr) && c.ContainsKey(id))
        {
            // 已有同名函数，创建函数重载
            var existingExpr = c[id];
            if (existingExpr is FuncLangValue existingFunc && expr is FuncLangValue currentFunc)
            {
                // 创建函数重载列表
                var overloadList = new MethodOverloadList([existingFunc, currentFunc]);
                c[id] = overloadList;
            }
            else if (existingExpr is MethodOverloadList overloadList)
            {
                // 已有重载列表，添加新重载
                if (expr is FuncLangValue funcToAdd)
                {
                    overloadList.AddOverload(funcToAdd);
                }
            }
        }
        else
        {
            // 普通成员或首次定义的函数
            c[id] = expr;
        }
    }

    private static (ClassMemberId? id, LangExpression? Expr) GetTuple(IOldLangTree a)
    {
        switch (a)
        {
            case SetStatement statement:
                if (statement.Id is null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId1 = statement.Id as ClassMemberId ?? new ClassMemberId(statement.Id);
                return (id: memberId1, Expr: statement.Value);
            case ClassFieldSetStatement classFieldSet:
                // 直接使用 ClassFieldSetStatement 中的 ClassMemberId
                return (id: classFieldSet.Id, Expr: classFieldSet.Value);
            case FuncInit init:
                if (init.GetFuncValue().Id is null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId2 = init.GetFuncValue().Id as ClassMemberId ?? new ClassMemberId(init.GetFuncValue().Id);
                return (memberId2, Expr: init.GetFuncValue());
            case ClassFuncInitStatement classFuncInit:
                // 直接使用 ClassFuncInitStatement 中的 ClassMemberId
                return (id: classFuncInit.Id, Expr: classFuncInit.GetFuncValue());
            case ClassInit classInit:
                // 对于嵌套类，创建一个特殊的ClassMemberId来标识它
                var nestedClassId = new ClassMemberId(classInit.GetTypeTemplate().ClassName, "", [], classInit.Position);
                // 将嵌套类作为TypeTemplate存储
                return (id: nestedClassId, Expr: classInit.GetTypeTemplate());
            default:
                return (null, null);
        }
    }

    /// <summary>
    /// 检查表达式是否是函数
    /// </summary>
    private static bool IsFunction(LangExpression expr) => expr is FuncLangValue or AsyncFuncLangValue;
}
