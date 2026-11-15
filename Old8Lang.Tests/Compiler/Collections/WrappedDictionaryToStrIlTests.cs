using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// IL 模式：原生字典包装成 <c>DictionaryLangValue</c> 后的显示与读取。
/// </summary>
/// <remarks>
/// <c>IlValueBridge.Wrap(IDictionary)</c> 只填 Tuples，Value 为空；
/// 语言层 ToStr / 索引读的是 Value，于是 <c>d.ToStr()</c> 输出 <c>{(), ()}</c>，
/// 已有键也会被当成缺失。现在包装后立即按解释器求值一次填充 Value。
/// </remarks>
[Collection("Sequential")]
public class WrappedDictionaryToStrIlTests
{
    [Fact]
    public void DictionaryToStr_And_FieldDictionary_ShowEntries()
    {
        const string code = @"
d <- {""a"": 1, ""b"": 2}
Assert.Equal(""{\""a\"": 1, \""b\"": 2}"", d.ToStr())
Assert.Equal(1, d[""a""])
Assert.Equal(2, d.Keys().Count())

class Holder {
    public lookup <- {""x"": 10}
    public func text() {
        return this.lookup.ToStr()
    }
}
h <- Holder()
Assert.Equal(""{\""x\"": 10}"", h.text())
Assert.Equal(10, h.lookup[""x""])
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
