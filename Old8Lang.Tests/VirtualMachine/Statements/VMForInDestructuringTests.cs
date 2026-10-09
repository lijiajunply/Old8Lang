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
    public void ForIn_DictionaryWithSingleIdentifier_BindsKeyValuePair()
    {
        // 单标识符遍历字典绑定 (键, 值) 元组，与解释器/IL 模式一致。
        // 此前虚拟机绑定的是「键」，且遍历顺序为插入顺序的逆序，两者都已修正。
        // pair[0] 用作键回查（绑定成元组才能命中），pair[1] 应等于同一个值：
        // 因此 total = 2 * (1 + 2)。
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2}
            total <- 0
            for pair in dict {
                total <- total + dict[pair[0]] + pair[1]
            }
            print(total)
            """);

        Assert.Equal("6", output.Trim());
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
    // 单标识符遍历字典：绑定 (键, 值) 元组，且按插入顺序。
    // 分开打印键与值而非整个元组，避免依赖元组的字符串格式。
    [InlineData("""
        dict <- {"a": 1, "b": 2, "c": 3}
        for pair in dict {
            print(pair[0].ToStr() + ":" + pair[1].ToStr())
        }
        """)]
    // 迭代元素的第二个分量本身是元组时不得被展平
    [InlineData("""
        rows <- [(1, (2, 3)), (4, (5, 6))]
        for a, b in rows {
            print(a.ToStr() + "-" + b[0].ToStr() + b[1].ToStr())
        }
        """)]
    // 字典的值是元组
    [InlineData("""
        dict <- {"a": (1, 2)}
        for key, value in dict {
            print(key + value[0].ToStr() + value[1].ToStr())
        }
        """)]
    public void ForIn_DestructuringMatchesInterpreter(string code)
    {
        Assert.Equal(ExecuteInterpreter(code).Trim(), ExecuteVm(code).Trim());
    }
}
