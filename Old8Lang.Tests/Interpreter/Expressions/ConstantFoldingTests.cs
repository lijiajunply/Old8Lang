using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Expressions;

/// <summary>
/// 常量表达式与 ConstantLangValue 解释模式测试
/// 说明：经源码核实，解释器无独立 AST 常量折叠优化遍；ConstantLangValue 用于模块通配符导出常量包装。
/// 本测试覆盖常量声明不可变性、常量表达式求值与传播，以及 ConstantLangValue 的实际运行时行为。
/// </summary>
[Collection("Sequential")]
public class ConstantFoldingTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void ConstantExpressions_ArithmeticBooleanAndStringLiterals_EvaluateAccurately()
    {
        var code = @"
a <- 2 + 3 * 4
b <- (10 - 2) * (3 + 5)
c <- 100 / 4 + 7 % 3
d <- 2 ^ 10
flag <- (true && !false) || (1 > 2)
text <- ""Old8"" + ""-"" + ""Lang""
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(14, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("a"))).Value);
        Assert.Equal(64, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("b"))).Value);
        Assert.Equal(26, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("c"))).Value);
        Assert.Equal(1024, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("d"))).Value);
        Assert.True(Assert.IsType<BoolLangValue>(interpreter.Manager.GetValue(new LangId("flag"))).Value);
        Assert.Equal("Old8-Lang", Assert.IsType<StringLangValue>(interpreter.Manager.GetValue(new LangId("text"))).Value);
    }

    [Fact]
    public void ConstantDeclaration_ConstPropagationThroughExpressionsAndFunctions_ProducesConsistentValues()
    {
        var code = @"
PI:const <- 3.14159
RADIUS:const <- 10
DIAMETER:const <- RADIUS * 2
AREA:const <- PI * RADIUS * RADIUS

func scaled(factor:int) -> int {
    return DIAMETER * factor
}

scaledResult <- scaled(3)
loopSum <- 0
for i <- 0, i < 3, i++ {
    loopSum <- loopSum + RADIUS
}
";
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);

        Assert.Equal(20, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("DIAMETER"))).Value);
        Assert.Equal(314.159, Assert.IsType<DoubleLangValue>(interpreter.Manager.GetValue(new LangId("AREA"))).Value, 0.0001);
        Assert.Equal(60, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("scaledResult"))).Value);
        Assert.Equal(30, Assert.IsType<IntLangValue>(interpreter.Manager.GetValue(new LangId("loopSum"))).Value);
    }

    [Fact]
    public void ConstantDeclaration_ReassignmentToConstVariable_ThrowsError()
    {
        var code = @"
MAX_RETRY:const <- 3
MAX_RETRY <- 5
";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void ConstantLangValue_WrapperMetadataAndWildcardModuleExport_WorkCorrectly()
    {
        // 直接验证 ConstantLangValue 包装节点属性
        var wrapped = new ConstantLangValue("ANSWER", new IntLangValue(42));
        Assert.Equal("ANSWER", wrapped.Name);
        Assert.Equal(42, Assert.IsType<IntLangValue>(wrapped.Value).Value);
        Assert.Equal("<constant ANSWER = 42>", wrapped.ToString());

        // 验证模块通配符导入时通过 ConstantLangValue 包装并解包常量
        var constModule = @"
APP_NAME:const <- ""Old8App""
BUILD_NUMBER:const <- 1024
";
        var mainCode = @"
import * from ""const_export_mod""
name <- APP_NAME
build <- BUILD_NUMBER
";
        CreateTempModuleFile("const_export_mod.old8", constModule);
        CreateTempModuleFile("const_export_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("const_export_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "name", "Old8App");
        AssertVariableValue(interpreter, "build", 1024);
    }
}
