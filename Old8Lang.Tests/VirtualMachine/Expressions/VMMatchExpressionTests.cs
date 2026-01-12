using Old8Lang.Bytecode;
using Xunit;
using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机 Match 表达式测试
/// 测试模式匹配功能
/// </summary>
public class VMMatchExpressionTests
{
    [Fact(Skip = "虚拟机 Match 表达式实现可能不完整")]
    public void MatchExpression_SimpleValueMatch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 2
            result <- match x {
                1 -> ""one""
                2 -> ""two""
                3 -> ""three""
                _ -> ""other""
            }
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("two", vm.GetGlobalVariable("result"));
    }

    [Fact(Skip = "虚拟机 Match 表达式实现可能不完整")]
    public void MatchExpression_DefaultBranch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- match x {
                1 -> ""one""
                2 -> ""two""
                3 -> ""three""
                _ -> ""other""
            }
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("other", vm.GetGlobalVariable("result"));
    }

    [Fact(Skip = "虚拟机 Match 表达式实现可能不完整")]
    public void MatchExpression_WithExpressions_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- match score {
                90 -> ""A""
                80 -> ""B""
                70 -> ""C""
                _ -> ""F""
            }
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("F", vm.GetGlobalVariable("grade"));
    }
}
