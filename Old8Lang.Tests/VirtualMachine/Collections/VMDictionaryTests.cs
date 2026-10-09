using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Collections;

/// <summary>
/// 虚拟机字典测试
/// </summary>
[Collection("Sequential")]
public class VMDictionaryTests
{
    [Fact]
    public void Dictionary_Creation_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""name"": ""Alice"", ""age"": 25}
            result <- dict
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        var result = vm.GetGlobalVariable("result");
        Assert.NotNull(result);
        Assert.IsAssignableFrom<System.Collections.IDictionary>(result);
    }

    [Fact]
    public void Dictionary_Access_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""name"": ""Bob"", ""age"": 30}
            result1 <- dict[""name""]
            result2 <- dict[""age""]
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("Bob", vm.GetGlobalVariable("result1"));
        Assert.Equal(30, vm.GetGlobalVariable("result2"));
    }

    [Fact]
    public void Dictionary_Keys_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            dict <- {""x"": 10, ""y"": 20, ""z"": 30}
            keys <- dict.Keys
            result <- keys.Count
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal(3, vm.GetGlobalVariable("result"));
    }

    #region for-in 遍历

    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
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
            return writer.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 执行同一段代码于解释器，用于模式一致性对照
    /// </summary>
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
            return writer.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    [Fact]
    public void Dictionary_ForInWithTwoIdentifiers_BindsKeyAndValue()
    {
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2, "c": 3}
            total <- 0
            for key, value in dict {
                total <- total + value
            }
            print(total)
            """);

        Assert.Equal("6", output);
    }

    [Fact]
    public void Dictionary_ForInWithSingleIdentifier_BindsKeyValuePair()
    {
        // 单标识符绑定 (键, 值) 元组：pair[0] 能当键回查，pair[1] 是同一个值
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2, "c": 3}
            total <- 0
            for pair in dict {
                total <- total + dict[pair[0]] + pair[1]
            }
            print(total)
            """);

        Assert.Equal("12", output);
    }

    [Fact]
    public void Dictionary_ForIn_PreservesInsertionOrder()
    {
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2, "c": 3, "d": 4, "e": 5}
            order <- ""
            for key, value in dict {
                order <- order + key
            }
            print(order)
            """);

        Assert.Equal("abcde", output);
    }

    [Fact]
    public void Dictionary_ForIn_SingleIdentifier_PreservesInsertionOrder()
    {
        var output = ExecuteVm("""
            dict <- {"a": 1, "b": 2, "c": 3}
            order <- ""
            for pair in dict {
                order <- order + pair[0]
            }
            print(order)
            """);

        Assert.Equal("abc", output);
    }

    [Fact]
    public void Dictionary_ForIn_WithTupleValue_DoesNotFlattenValue()
    {
        var output = ExecuteVm("""
            dict <- {"a": (1, 2)}
            for key, value in dict {
                print(key + value[0].ToStr() + value[1].ToStr())
            }
            """);

        Assert.Equal("a12", output);
    }

    [Fact]
    public void Dictionary_ForIn_EmptyDictionary_DoesNotRunBody()
    {
        var output = ExecuteVm("""
            dict <- {}
            count <- 0
            for key, value in dict {
                count <- count + 1
            }
            print(count)
            """);

        Assert.Equal("0", output);
    }

    [Fact]
    public void Dictionary_ForIn_AccumulatesIntoOuterVariable()
    {
        var output = ExecuteVm("""
            dict <- {"a": 10, "b": 20}
            keys <- ""
            total <- 0
            for key, value in dict {
                keys <- keys + key
                total <- total + value
            }
            print(keys + "/" + total.ToStr())
            """);

        Assert.Equal("ab/30", output);
    }

    [Fact]
    public void Dictionary_ForIn_Nested_IteratesAllPairs()
    {
        var output = ExecuteVm("""
            outer <- {"x": 1, "y": 2}
            inner <- {"p": 10, "q": 20}
            total <- 0
            for ok, ov in outer {
                for ik, iv in inner {
                    total <- total + ov * iv
                }
            }
            print(total)
            """);

        // (1 + 2) * (10 + 20)
        Assert.Equal("90", output);
    }

    [Theory]
    [InlineData("""
        dict <- {"a": 1, "b": 2, "c": 3}
        for pair in dict {
            print(pair[0].ToStr() + ":" + pair[1].ToStr())
        }
        """)]
    [InlineData("""
        dict <- {"a": 1, "b": 2, "c": 3}
        for key, value in dict {
            print(key + "=" + value.ToStr())
        }
        """)]
    [InlineData("""
        dict <- {}
        for key, value in dict {
            print(key)
        }
        print("done")
        """)]
    public void Dictionary_ForIn_MatchesInterpreter(string code)
    {
        // 两模式的绑定语义与遍历顺序必须一致
        Assert.Equal(ExecuteInterpreter(code), ExecuteVm(code));
    }

    #endregion
}
