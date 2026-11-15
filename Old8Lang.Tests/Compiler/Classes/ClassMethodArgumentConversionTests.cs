using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// IL 模式下类方法调用的实参必须按形参类型转换。
/// </summary>
/// <remarks>
/// 未标注类型的参数在 IL 里是 <c>object</c>，而调用点的实参按原始类型（int/double）求值。
/// 直接把未装箱的 int 传给 <c>object</c> 形参会生成无效 IL，JIT 报
/// <c>InvalidProgramException</c>；<c>func add(a, b) { return a + b }</c> 就是最小形态。
/// </remarks>
public class ClassMethodArgumentConversionTests
{
    [Fact]
    public void UntypedParameters_ReceiveValueTypeArguments()
    {
        var code = @"
            class Calculator {
                func add(a, b) {
                    return a + b
                }
            }

            calc <- Calculator()
            Assert.Equal(15, calc.add(10, 5))
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    // 未标注类型的参数收到 double/string 时，方法体里的 `a + b` 还会撞上
    // 「IL 算术对 object 操作数一律按 int 拆箱」的独立缺口（见 changes/Todo.md），
    // 这里只覆盖 int 实参这条已经可用的路径。

    [Fact]
    public void TypedParameters_StillMatchExactly()
    {
        var code = @"
            class Calculator {
                func add(a:int, b:int) -> int {
                    return a + b
                }
            }

            calc <- Calculator()
            Assert.Equal(15, calc.add(10, 5))
        ";

        Assert.Null(CompileAndInvoke(code));
    }

    private static Exception? CompileAndInvoke(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        try
        {
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
            return Record.Exception(() => compiledAction());
        }
        finally
        {
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
}
