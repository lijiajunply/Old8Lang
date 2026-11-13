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
/// 块
/// </summary>
public partial class BlockStatement : OldStatement
{
    public readonly List<OldStatement> ImportStatements = [];
    public readonly List<OldStatement> OtherStatements = [];
    public override int Count => OtherStatements.Count;
    public int Total => ImportStatements.Count + OtherStatements.Count;

    public BlockStatement(IEnumerable<IOldLangTree> statements, SourcePosition position = default) : base(position)
    {
        // 遍历所有语句
        foreach (var statement in statements.OfType<OldStatement>())
        {
            // 根据语句类型添加到不同的列表中
            switch (statement)
            {
                case ImportStatement importStmt:
                    // 动态导入需要放在 OtherStatements 中，以便在正常语句流中执行
                    if (importStmt.IsDynamicImport)
                    {
                        OtherStatements.Add(statement);
                    }
                    else
                    {
                        ImportStatements.Add(statement);
                    }

                    break;
                case NativeStatement or FuncInit or ClassInit or AsyncFuncInit or ExternStatement:
                    ImportStatements.Add(statement);
                    break;
                case ReturnStatement:
                    OtherStatements.Add(statement);
                    return;
                default:
                    OtherStatements.Add(statement);
                    break;
            }
        }
    }




    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var statement in ImportStatements)
            sb.Append(statement + Environment.NewLine);
        foreach (var statement in OtherStatements)
            sb.Append(statement + Environment.NewLine);

        return sb.ToString();
    }

    public string ToCode()
    {
        var sb = new StringBuilder();
        // 输出所有导入和初始化语句
        foreach (var statement in ImportStatements)
            sb.AppendLine(statement.ToString());
        // 输出其他语句
        foreach (var statement in OtherStatements)
            sb.AppendLine(statement.ToString());
        return sb.ToString();
    }

    /// <summary>
    /// 获取实例成员字典
    /// </summary>
    /// <returns>实例成员字典</returns>
    public Dictionary<ClassMemberId, LangExpression> ToAnyData()
    {
        var c = new Dictionary<ClassMemberId, LangExpression>();

        // 处理所有语句，筛选出非静态成员
        foreach (var x in OtherStatements.Concat(ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id is null || expr is null) continue;
            // 只添加非静态成员
            if (!id.HasModifier(AccessModifierType.Static))
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
        }

        return c;
    }

    /// <summary>
    /// 获取静态成员字典
    /// </summary>
    /// <returns>静态成员字典</returns>
    public Dictionary<ClassMemberId, LangExpression> ToStaticData()
    {
        var c = new Dictionary<ClassMemberId, LangExpression>();

        // 处理所有语句，筛选出静态成员
        foreach (var x in OtherStatements.Concat(ImportStatements))
        {
            var (id, expr) = GetTuple(x);
            if (id is null || expr is null) continue;
            // 只添加静态成员
            if (id.HasModifier(AccessModifierType.Static))
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
        }

        return c;
    }

    internal static (ClassMemberId? id, LangExpression? Expr) GetTuple(IOldLangTree a)
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
                if (init.FuncValue.Id is null) return (null, null);
                // 如果是 ClassMemberId 直接使用，否则转换
                var memberId2 = init.FuncValue.Id as ClassMemberId ?? new ClassMemberId(init.FuncValue.Id);
                return (memberId2, Expr: init.FuncValue);
            case ClassFuncInitStatement classFuncInit:
                // 直接使用 ClassFuncInitStatement 中的 ClassMemberId
                return (id: classFuncInit.Id, Expr: classFuncInit.FuncValue);
            case ClassInit classInit:
                // 对于嵌套类，创建一个特殊的ClassMemberId来标识它
                var nestedClassId = new ClassMemberId(classInit.AnyValue.ClassName, "", [], classInit.Position);
                // 将嵌套类作为TypeTemplate存储
                return (id: nestedClassId, Expr: classInit.AnyValue);
            default:
                return (null, null);
        }
    }

    public override OldStatement this[int index] => OtherStatements[index];

    public IOldLangTree GetImportStatement(int index) => ImportStatements[index];

    /// <summary>
    /// 检查表达式是否是函数
    /// </summary>
    /// <param name="expr">要检查的表达式</param>
    /// <returns>如果是函数返回true，否则返回false</returns>
    internal static bool IsFunction(LangExpression expr)
    {
        return expr is FuncLangValue or AsyncFuncLangValue;
    }
}
