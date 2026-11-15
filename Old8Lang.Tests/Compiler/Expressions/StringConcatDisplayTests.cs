using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// IL 模式字符串拼接的显示规则。
/// </summary>
/// <remarks>
/// 拼接分支曾经直接调用 <c>string.Concat(object, object)</c>，走的是 CLR <c>ToString</c>：
/// bool 显示成 <c>True</c>、原生集合显示成
/// <c>System.Collections.Generic.List`1[System.Object]</c>，与解释器的
/// <c>true</c> / <c>[1, 2]</c> 不一致。现在操作数先经
/// <c>VMValueFormatter.ToDisplayString</c> 转成语言显示文本再拼接。
/// </remarks>
[Collection("Sequential")]
public class StringConcatDisplayTests
{
    [Fact]
    public void BoolOperand_UsesLanguageLowerCaseDisplay()
    {
        const string code = @"
            Assert.Equal(""xtrue"", ""x"" + true)
            Assert.Equal(""xfalse"", ""x"" + false)
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void ListOperand_UsesLanguageCollectionDisplay()
    {
        const string code = @"
            Assert.Equal(""x[1, 2]"", ""x"" + {1, 2})
            Assert.Equal(""x[1, \""a\""]"", ""x"" + {1, ""a""})
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void DictionaryOperand_UsesLanguageDictionaryDisplay()
    {
        const string code = @"
            Assert.Equal(""x{\""a\"": 1}"", ""x"" + {""a"": 1})
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void ScalarAndStringOperands_KeepExistingBehavior()
    {
        const string code = @"
            Assert.Equal(""x1"", ""x"" + 1)
            Assert.Equal(""x1.5"", ""x"" + 1.5)
            Assert.Equal(""xy"", ""x"" + ""y"")
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
