using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Types;

/// <summary>
/// IL 模式：枚举成员的值是整数（与解释器/虚拟机一致）。
/// </summary>
/// <remarks>
/// 此前 `EmitStaticMemberAccess` 对枚举成员发射 `ldc.i4` 却返回枚举类型，
/// 调用方按 `Color` 装箱，`PrintLine(Color.Red)` 显示 `Red`、`match` 匹配不到
/// `case 0`、`ToStr()` 也不是 `0`。现在返回 `typeof(int)`；
/// 类型推断同步把 `Color.Member` 推断为 int。
/// </remarks>
[Collection("Sequential")]
public class EnumValueIlTests
{
    [Fact]
    public void EnumMember_IsInteger_AndMatchesNumericCases()
    {
        const string code = @"
            enum Color {
                Red <- 0,
                Green <- 1,
                Blue,
                Purple
            }

            Assert.Equal(0, Color.Red)
            Assert.Equal(1, Color.Green)
            Assert.Equal(2, Color.Blue)
            Assert.Equal(3, Color.Purple)
            Assert.Equal(""0"", Color.Red.ToStr())
            Assert.Equal(1, Color.Green + 0)

            result <- match Color.Green {
                case 0 -> ""red""
                case 1 -> ""green""
                case _ -> ""other""
            }
            Assert.Equal(""green"", result)
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
