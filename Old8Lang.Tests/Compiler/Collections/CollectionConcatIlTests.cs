using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// IL 模式：数组 / 列表的 <c>+</c> 按顺序拼接。
/// </summary>
/// <remarks>
/// 此前 IL 把集合 <c>+</c> 当数值加法推断或直接报错；现在数组走
/// <c>LangCollectionOperations.ConcatArrays</c>，列表走 <c>ConcatLists</c>，
/// 与解释器、虚拟机结果一致。
/// </remarks>
[Collection("Sequential")]
public class CollectionConcatIlTests
{
    [Fact]
    public void ArrayPlusArray_ConcatenatesInOrder()
    {
        const string code = @"
left <- [1, 2]
right <- [3, 4]
joined <- left + right
Assert.Equal(4, Len(joined))
Assert.Equal(""[1, 2, 3, 4]"", joined.ToStr())
Assert.Equal(2, Len(left))
";
        CompileAndRun(code);
    }

    [Fact]
    public void ListPlusList_ConcatenatesInOrder()
    {
        const string code = @"
left <- {1, 2}
right <- {3, 4}
joined <- left + right
Assert.Equal(4, joined.Count())
Assert.Equal(""[1, 2, 3, 4]"", joined.ToStr())
Assert.Equal(2, left.Count())
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
