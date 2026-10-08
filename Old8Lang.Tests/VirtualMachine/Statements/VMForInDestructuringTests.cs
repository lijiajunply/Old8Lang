using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Statements;

/// <summary>
/// 虚拟机 for-in 多标识符绑定（解构遍历）测试
/// </summary>
/// <remarks>
/// 文档 §5.4.4 给出的 "for key, value in dict" 写法此前在虚拟机模式下报
/// 名称 'v' 未定义 —— 字节码访问者只绑定了首个标识符，忽略了附加标识符。
/// </remarks>
[Collection("Sequential")]
public class VMForInDestructuringTests
{
    private static string ExecuteVm(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var bytecodeFile = new Old8Lang.Bytecode.BytecodeCompiler().Compile(ast);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            new VM(bytecodeFile).Execute();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string ExecuteInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            ast.Run(interpreter.Manager);
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void ForIn_DictionaryWithTwoIdentifiers_BindsKeyAndValue()
    {
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2}
            total <- 0
            for key, value in dict {
                total <- total + value
            }
            print(total)
            """);

        Assert.Equal("3", output.Trim());
    }

    [Fact]
    public void ForIn_DictionaryWithSingleIdentifier_StillBindsKeys()
    {
        // 单标识符遍历字典仍然是「键」，这是既有行为，不应被解构支持改变
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2}
            total <- 0
            for key in dict {
                total <- total + dict[key]
            }
            print(total)
            """);

        Assert.Equal("3", output.Trim());
    }

    [Fact]
    public void ForIn_ListOfTuplesWithTwoIdentifiers_DestructuresEachPair()
    {
        var output = ExecuteVm("""
            pairs <- [("a", 1), ("b", 2)]
            for key, value in pairs {
                print(key + value.ToStr())
            }
            """);

        Assert.Equal("a1b2", output.Trim());
    }

    [Fact]
    public void ForIn_ThreeIdentifiers_DestructuresTriple()
    {
        var output = ExecuteVm("""
            rows <- [(1, 2, 3), (4, 5, 6)]
            total <- 0
            for a, b, c in rows {
                total <- total + a + b + c
            }
            print(total)
            """);

        Assert.Equal("21", output.Trim());
    }

    [Fact]
    public void ForIn_TupleDestructuringAssignment_ExtractsElements()
    {
        // 同一处 LoadConst 下标误用也影响元组解构赋值，这里一并覆盖
        var output = ExecuteVm("""
            (a, b) <- (1, 2)
            print(a)
            print(b)
            """);

        Assert.Equal("12", output.Trim());
    }

    [Fact]
    public void ForIn_TwoIdentifiersOverNonPairCollection_ReportsClearError()
    {
        var exception = Assert.Throws<Old8Lang.Error.TypeError>(() =>
            ExecuteVm("""
                for a, b in [1, 2, 3] {
                    print(a)
                }
                """));

        // 报错应指向无法解构，而不是栈失衡或名称未定义
        Assert.Contains("解构", exception.Message);
    }

    [Theory]
    [InlineData("""
        dict <- {"a": 1, "b": 2}
        total <- 0
        for key, value in dict {
            total <- total + value
        }
        print(total)
        """)]
    [InlineData("""
        pairs <- [("a", 1), ("b", 2)]
        for key, value in pairs {
            print(key + value.ToStr())
        }
        """)]
    public void ForIn_DestructuringMatchesInterpreter(string code)
    {
        Assert.Equal(ExecuteInterpreter(code).Trim(), ExecuteVm(code).Trim());
    }
}
