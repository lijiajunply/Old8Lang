using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Collections;

/// <summary>
/// 虚拟机集合访问测试
/// 测试虚拟机执行索引访问、切片访问等集合操作的正确性
/// </summary>
[Collection("Sequential")]
public class VMCollectionAccessTests
{
    /// <summary>
    /// 执行虚拟机代码并捕获控制台输出
    /// </summary>
    private string ExecuteVMCode(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 捕获控制台输出
        var originalOut = Console.Out;
        using var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        try
        {
            // 执行字节码
            var vm = new Old8Lang.Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    #region Array Index Access Tests

    [Fact]
    public void ArrayIndexAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
        arr <- [10, 20, 30, 40, 50]
        value <- arr[2]
        PrintLine(value.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("30", output);
    }

    [Fact]
    public void NestedArrayIndexAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
        matrix <- [[1, 2, 3], [4, 5, 6], [7, 8, 9]]
        value <- matrix[1][2]
        PrintLine(value.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("6", output);
    }

    #endregion

    #region List Index Access Tests

    [Fact]
    public void ListIndexAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
        list <- {10, 20, 30, 40, 50}
        value <- list[3]
        PrintLine(value.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("40", output);
    }

    #endregion

    #region String Index Access Tests

    [Fact]
    public void StringIndexAccess_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
        str <- ""Hello""
        char <- str[1]
        PrintLine(char.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("e", output);
    }

    #endregion
}
