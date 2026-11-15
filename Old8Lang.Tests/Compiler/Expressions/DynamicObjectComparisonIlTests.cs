using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Expressions;

/// <summary>
/// IL 模式：两个静态类型为 object 的值做大小比较。
/// </summary>
/// <remarks>
/// 数组/字典元素赋给变量后静态类型是 object，<c>item &lt; min</c> 两侧都是 object；
/// IL 之前落到数值/引用比较分支恒为 false，for-in 找最小值/最大值结果错误。
/// 现在按运行期语言值语义分派（IlDynamicComparison）。
/// </remarks>
[Collection("Sequential")]
public class DynamicObjectComparisonIlTests
{
    [Fact]
    public void ForInMinMax_OnObjectElements_Works()
    {
        const string code = @"
            arr <- [5, 3, 8, 1, 4]
            min <- arr[0]
            max <- arr[0]
            for item in arr {
                if item < min { min <- item }
                if item > max { max <- item }
            }
            Assert.Equal(1, min)
            Assert.Equal(8, max)
        ";
        CompileAndRun(code);
    }

    [Fact]
    public void ObjectVsObject_FromDictionary_ComparesNumerically()
    {
        const string code = @"
            d <- {""a"": 2, ""b"": 7}
            x <- d[""a""]
            y <- d[""b""]
            Assert.True(y > x)
            Assert.True(x < y)
            Assert.False(x >= y)
            Assert.True(x <= y)
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
