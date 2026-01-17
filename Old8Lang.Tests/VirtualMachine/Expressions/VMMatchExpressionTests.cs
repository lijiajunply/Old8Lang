using VM = Old8Lang.Bytecode.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Expressions;

/// <summary>
/// 虚拟机 Match 表达式测试
/// 测试模式匹配功能
/// </summary>
public class VMMatchExpressionTests
{
    [Fact]
    public void MatchExpression_SimpleValueMatch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 2
            result <- match x {
                case 1 -> ""one""
                case 2 -> ""two""
                case 3 -> ""three""
                case _ -> ""other""
            }
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("two", vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void MatchExpression_DefaultBranch_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            x <- 10
            result <- match x {
                case 1 -> ""one""
                case 2 -> ""two""
                case 3 -> ""three""
                case _ -> ""other""
            }
        ";

        // Act
        var bytecodeFile = CompileHelper.CompileToBytecode(code);
        var vm = new VM(bytecodeFile);
        vm.Execute();

        // Assert
        Assert.Equal("other", vm.GetGlobalVariable("result"));
    }

    [Fact]
    public void MatchExpression_WithExpressions_ExecutesCorrectly()
    {
        // Arrange
        var code = @"
            score <- 85
            grade <- match score {
                case 90 -> ""A""
                case 80 -> ""B""
                case 70 -> ""C""
                case _ -> ""F""
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
