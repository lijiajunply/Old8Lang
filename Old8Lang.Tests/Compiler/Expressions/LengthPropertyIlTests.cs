using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// IL 模式：数组 / 列表 / 字符串 / 字典的 <c>.Length</c> 属性。
/// </summary>
[Collection("Sequential")]
public class LengthPropertyIlTests
{
    [Fact]
    public void LengthProperty_OnCollectionsAndString_EqualsLen()
    {
        const string code = @"
array <- [1, 2, 3]
list <- {1, 2}
text <- ""abc""
dict <- {""a"": 1, ""b"": 2}
Assert.Equal(3, array.Length)
Assert.Equal(2, list.Length)
Assert.Equal(3, text.Length)
Assert.Equal(2, dict.Length)
Assert.Equal(3, len(array))
Assert.Equal(2, len(list))
Assert.Equal(3, len(text))
Assert.Equal(2, len(dict))
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
