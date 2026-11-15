using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// IL 模式：元组实例方法（此前只有解释器可用）。
/// </summary>
/// <remarks>
/// IL 的元组字面量是扁平 <c>ValueTuple</c>，实例方法注册在 <c>TupleLangValue</c> 上，
/// 解析时找不到方法；现在走 <c>IlNativeCollectionBridge</c>，结果与解释器一致。
/// </remarks>
[Collection("Sequential")]
public class TupleMethodIlTests
{
    [Fact]
    public void TupleMethods_OnValueTupleLiteral_Work()
    {
        const string code = @"
            tuple <- (1, 2, 3)
            Assert.Equal(2, tuple.Get(1))
            Assert.Equal(""(1, 2)"", tuple.Slice(0, 2).ToStr())
            Assert.Equal(""[1, 2, 3]"", tuple.ToList().ToStr())
            Assert.Equal(""[3, 2, 1]"", tuple.Reverse().ToStr())
            Assert.True(tuple.Contains(2))
            Assert.True(tuple.All((x) -> x > 0))
            Assert.True(tuple.Any((x) -> x > 2))
            Assert.Equal(""[2, 3]"", tuple.Filter((x) -> x > 1).ToStr())
            Assert.Equal(""[2, 4, 6]"", tuple.Map((x) -> x * 2).ToStr())
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
