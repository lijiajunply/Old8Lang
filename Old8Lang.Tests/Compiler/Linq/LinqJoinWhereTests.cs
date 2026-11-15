using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Linq;

/// <summary>
/// IL 模式下 `where` 子句与非流式子句（join / 非末尾 orderby）组合的执行。
/// </summary>
/// <remarks>
/// 非流式查询走旧的手写 IL 生成路径，这里曾经有两处会生成无效 IL：
/// 1. `where` 条件比较语言值（range variable 是 <c>LangValueType</c>）时直接发 `cgt`；
/// 2. 条件值没有按推断类型装箱就传给真值判定助手。
/// 两者都会让 JIT 判为 <c>InvalidProgramException</c>。
/// </remarks>
[Collection("Sequential")]
public class LinqJoinWhereTests
{
    [Fact]
    public void JoinThenWhere_FiltersByOuterVariable()
    {
        const string code = @"
            left <- {1, 2, 3, 4, 5}
            right <- {2, 3, 4}
            result <- from x in left join y in right on x == y where x > 2 select x
            Assert.Equal(""[3, 4]"", result.ToStr())
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void WhereThenJoin_FiltersOuterSourceBeforeJoin()
    {
        const string code = @"
            left <- {1, 2, 3, 4, 5}
            right <- {2, 3, 4}
            result <- from x in left where x > 1 join y in right on x == y select x
            Assert.Equal(""[2, 3, 4]"", result.ToStr())
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void JoinThenWhere_LiteralConditionCompiles()
    {
        const string code = @"
            left <- {1, 2, 3}
            right <- {2, 3}
            result <- from x in left join y in right on x == y where true select x
            Assert.Equal(""[2, 3]"", result.ToStr())
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void OrderByThenWhere_AppliesFilter()
    {
        // orderby 不在末尾时走非流式路径，where 的过滤结果必须和解释器一致。
        const string code = @"
            numbers <- {3, 1, 2}
            result <- from x in numbers orderby x where x > 1 select x
            Assert.Equal(""[2, 3]"", result.ToStr())
        ";
        CompileAndRun(code);
    }

    private static void CompileAndRun(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter)();
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
