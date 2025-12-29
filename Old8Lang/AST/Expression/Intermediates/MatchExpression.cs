using System.Reflection.Emit;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// Match 表达式节点
/// 语法: match value { case pattern -> expression ... }
/// </summary>
public partial class MatchExpression(
    LangExpression matchExpression,
    List<MatchCase> matchCases,
    SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>
    /// 被匹配的表达式
    /// </summary>
    public LangExpression MatchValue { get; } = matchExpression;

    /// <summary>
    /// case 分支列表
    /// </summary>
    public List<MatchCase> Cases { get; } = matchCases;

    /// <summary>
    /// 执行 match 表达式 (解释器模式)
    /// </summary>
    public override LangValueType Run(VariateManager manager)
    {
        // 计算被匹配的值
        var matchValue = MatchValue.Run(manager);

        // 依次检查每个 case
        foreach (var matchCase in Cases)
        {
            // 检查是否匹配
            if (matchCase.IsMatch(matchValue, manager, out var boundValues))
            {
                // 如果有变量绑定，将值绑定到新的作用域
                if (boundValues != null && boundValues.Count > 0)
                {
                    // 添加新的子作用域
                    manager.AddChildren();

                    // 直接在当前作用域中创建绑定变量，避免修改父作用域的同名变量
                    var currentScope = manager.Scopes[^1];
                    foreach (var (varName, varValue) in boundValues)
                    {
                        currentScope[varName] = varValue;
                    }
                }

                try
                {
                    // 执行对应的表达式并返回结果
                    return matchCase.ResultExpression.Run(manager);
                }
                finally
                {
                    // 清理变量绑定的作用域
                    if (boundValues != null && boundValues.Count > 0)
                    {
                        manager.RemoveChildren();
                    }
                }
            }
        }

        // 如果没有任何 case 匹配，抛出错误
        throw new InvalidOperationError(this, $"Match 表达式没有匹配的分支，值为: {matchValue}");
    }

    /// <summary>
    /// 生成 IL 代码 (编译器模式)
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 定义标签
        var endLabel = ilGenerator.DefineLabel();
        var caseLabels = Cases.Select(_ => ilGenerator.DefineLabel()).ToList();
        var resultLocal = ilGenerator.DeclareLocal(typeof(object)); // 用于存储结果

        // 保存 match 表达式值到局部变量
        MatchValue.LoadIlValue(ilGenerator, local);
        var matchValueType = MatchValue.OutputType(local) ?? typeof(object);
        var matchValueLocal = ilGenerator.DeclareLocal(matchValueType);
        ilGenerator.Emit(OpCodes.Stloc, matchValueLocal.LocalIndex);

        // 生成每个 case 的匹配判断
        for (int i = 0; i < Cases.Count; i++)
        {
            var matchCase = Cases[i];
            var caseLabel = caseLabels[i];

            // 检查是否是通配符 (匹配所有)
            if (matchCase.IsWildcard)
            {
                // 通配符直接跳转到对应的 case
                ilGenerator.Emit(OpCodes.Br, caseLabel);
                continue;
            }

            // 检查是否是变量绑定 (匹配所有并绑定变量)
            if (matchCase.IsVariableBinding)
            {
                // 变量绑定也直接跳转到对应的 case
                ilGenerator.Emit(OpCodes.Br, caseLabel);
                continue;
            }

            // 普通值匹配：比较 match 值和 case 值
            ilGenerator.Emit(OpCodes.Ldloc, matchValueLocal.LocalIndex);
            matchCase.Pattern!.LoadIlValue(ilGenerator, local);

            // 根据类型选择合适的比较指令
            if (matchValueType == typeof(int) || matchValueType == typeof(bool) || matchValueType == typeof(char))
            {
                ilGenerator.Emit(OpCodes.Ceq);
            }
            else if (matchValueType == typeof(string))
            {
                var equalsMethod = typeof(string).GetMethod("Equals",
                    [typeof(string), typeof(string)])!;
                ilGenerator.Emit(OpCodes.Call, equalsMethod);
            }
            else
            {
                // 其他类型：装箱后调用 Equals
                if (matchValueType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, matchValueType);

                var patternType = matchCase.Pattern.OutputType(local);
                if (patternType != null && patternType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, patternType);

                var equalsMethod = typeof(object).GetMethod("Equals", [typeof(object), typeof(object)])!;
                ilGenerator.Emit(OpCodes.Call, equalsMethod);
            }

            // 若匹配，跳转到对应的 case
            ilGenerator.Emit(OpCodes.Brtrue, caseLabel);
        }

        // 所有 case 都不匹配，抛出异常
        var exceptionType = typeof(InvalidOperationError);
        var exceptionCtor = exceptionType.GetConstructor([typeof(IOldLangTree), typeof(string)])!;
        ilGenerator.Emit(OpCodes.Ldnull); // IOldLangTree 参数
        ilGenerator.Emit(OpCodes.Ldstr, "Match 表达式没有匹配的分支");
        ilGenerator.Emit(OpCodes.Newobj, exceptionCtor);
        ilGenerator.Emit(OpCodes.Throw);

        // 生成各个 case 的 IL 代码
        for (var i = 0; i < Cases.Count; i++)
        {
            var matchCase = Cases[i];
            var caseLabel = caseLabels[i];

            ilGenerator.MarkLabel(caseLabel);

            // 如果有变量绑定，将 match 值赋给变量
            if (matchCase.BindingVariable != null)
            {
                ilGenerator.Emit(OpCodes.Ldloc, matchValueLocal.LocalIndex);
                if (matchValueType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, matchValueType);

                var localVar = ilGenerator.DeclareLocal(typeof(object));
                local.AddLocalVar(matchCase.BindingVariable, localVar);
                ilGenerator.Emit(OpCodes.Stloc, localVar.LocalIndex);
            }

            // 计算并存储结果
            matchCase.ResultExpression.LoadIlValue(ilGenerator, local);
            var resultType = matchCase.ResultExpression.OutputType(local);
            if (resultType != null && resultType.IsValueType)
                ilGenerator.Emit(OpCodes.Box, resultType);

            ilGenerator.Emit(OpCodes.Stloc, resultLocal.LocalIndex);

            // 跳转到结束
            ilGenerator.Emit(OpCodes.Br, endLabel);
        }

        // 结束标签
        ilGenerator.MarkLabel(endLabel);

        // 加载结果
        ilGenerator.Emit(OpCodes.Ldloc, resultLocal.LocalIndex);
    }

    /// <summary>
    /// 获取输出类型
    /// </summary>
    public override Type? OutputType(LocalManager local)
    {
        // 返回第一个 case 的结果类型（假设所有 case 返回相同类型）
        if (Cases.Count > 0)
        {
            return Cases[0].ResultExpression.OutputType(local) ?? typeof(object);
        }

        return typeof(object);
    }

    /// <inheritdoc />
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotImplementedException("MatchExpression visitor not implemented");
    }
}
