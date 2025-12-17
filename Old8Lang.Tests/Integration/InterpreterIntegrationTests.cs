using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Integration;

/// <summary>
/// 解释器集成测试 - 完整的 Tokenize → Parse → Interpret 流程测试
/// 测试从源代码到执行结果的完整管道
/// </summary>
[Collection("Sequential")]
public class InterpreterIntegrationTests
{
    #region 基础场景测试 (5 个)

    [Fact]
    public void FullPipeline_SimpleAssignment_ExecutesCorrectly()
    {
        // 测试简单赋值操作
        var code = "a <- 123";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("a"));
        Assert.NotNull(result);
        Assert.IsType<IntLangValue>(result);
        Assert.Equal(123, ((IntLangValue)result).Value);
    }

    [Fact]
    public void FullPipeline_MultipleAssignments_ExecutesCorrectly()
    {
        // 测试多个赋值操作
        var code = @"
            a <- 10
            b <- 20
            c <- 30
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var a = interpreter.Manager.GetValue(new LangId("a")) as IntLangValue;
        var b = interpreter.Manager.GetValue(new LangId("b")) as IntLangValue;
        var c = interpreter.Manager.GetValue(new LangId("c")) as IntLangValue;

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.NotNull(c);
        Assert.Equal(10, a.Value);
        Assert.Equal(20, b.Value);
        Assert.Equal(30, c.Value);
    }

    [Fact]
    public void FullPipeline_FunctionDeclarationAndCall_ExecutesCorrectly()
    {
        // 测试函数声明和调用
        var code = @"
            func add(x, y) {
                return x + y
            }
            result <- add(5, 3)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(8, result.Value);
    }

    [Fact]
    public void FullPipeline_ArithmeticOperations_ExecutesCorrectly()
    {
        // 测试算术运算
        var code = @"
            a <- 10 + 5
            b <- 20 - 8
            c <- 6 * 7
            d <- 100 / 4
            e <- 17 % 5
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(15, ((IntLangValue)interpreter.Manager.GetValue(new LangId("a"))!).Value);
        Assert.Equal(12, ((IntLangValue)interpreter.Manager.GetValue(new LangId("b"))!).Value);
        Assert.Equal(42, ((IntLangValue)interpreter.Manager.GetValue(new LangId("c"))!).Value);
        Assert.Equal(25, ((IntLangValue)interpreter.Manager.GetValue(new LangId("d"))!).Value);
        Assert.Equal(2, ((IntLangValue)interpreter.Manager.GetValue(new LangId("e"))!).Value);
    }

    [Fact]
    public void FullPipeline_StringConcatenation_ExecutesCorrectly()
    {
        // 测试字符串拼接
        var code = @"
            greeting <- ""Hello""
            name <- ""World""
            message <- greeting + "" "" + name
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var message = interpreter.Manager.GetValue(new LangId("message")) as StringLangValue;
        Assert.NotNull(message);
        Assert.Equal("Hello World", message.Value);
    }

    #endregion

    #region 控制流场景测试 (4 个)

    [Fact]
    public void FullPipeline_IfElseStatement_ExecutesCorrectBranch()
    {
        // 测试 if/else 分支执行 - 测试 if 分支
        var code1 = @"
            x <- 10
            result <- """"
            if x > 5 {
                result <- ""greater""
            } else {
                result <- ""smaller""
            }
        ";
        var interpreter1 = new LangInterpreter();
        var ast1 = interpreter1.Build(code1);
        ast1.Run(interpreter1.Manager);

        var result1 = interpreter1.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result1);
        Assert.Equal("greater", result1.Value);

        // 测试 else 分支
        var code2 = @"
            x <- 3
            result <- """"
            if x > 5 {
                result <- ""greater""
            } else {
                result <- ""smaller""
            }
        ";
        var interpreter2 = new LangInterpreter();
        var ast2 = interpreter2.Build(code2);
        ast2.Run(interpreter2.Manager);

        var result2 = interpreter2.Manager.GetValue(new LangId("result")) as StringLangValue;
        Assert.NotNull(result2);
        Assert.Equal("smaller", result2.Value);
    }

    [Fact]
    public void FullPipeline_ForLoop_IteratesCorrectly()
    {
        // 测试 for 循环
        var code = @"
            sum <- 0
            for i <- 1, i <= 5, i++ {
                sum <- sum + i
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var sum = interpreter.Manager.GetValue(new LangId("sum")) as IntLangValue;
        Assert.NotNull(sum);
        Assert.Equal(15, sum.Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void FullPipeline_WhileLoop_IteratesCorrectly()
    {
        // 测试 while 循环
        var code = @"
            count <- 0
            i <- 1
            while i <= 5 {
                count <- count + i
                i <- i + 1
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var count = interpreter.Manager.GetValue(new LangId("count")) as IntLangValue;
        Assert.NotNull(count);
        Assert.Equal(15, count.Value); // 1+2+3+4+5 = 15
    }

    [Fact]
    public void FullPipeline_ForInLoop_IteratesCorrectly()
    {
        // 测试 for-in 循环
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            for item in arr {
                sum <- sum + item
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var sum = interpreter.Manager.GetValue(new LangId("sum")) as IntLangValue;
        Assert.NotNull(sum);
        Assert.Equal(15, sum.Value);
    }

    #endregion

    #region 集合操作场景测试 (4 个)

    [Fact]
    public void FullPipeline_ArrayOperations_WorksCorrectly()
    {
        // 测试数组操作
        var code = @"
            arr <- [10, 20, 30, 40, 50]
            first <- arr[0]
            last <- arr[4]
            middle <- arr[2]
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var first = interpreter.Manager.GetValue(new LangId("first")) as IntLangValue;
        var last = interpreter.Manager.GetValue(new LangId("last")) as IntLangValue;
        var middle = interpreter.Manager.GetValue(new LangId("middle")) as IntLangValue;

        Assert.NotNull(first);
        Assert.NotNull(last);
        Assert.NotNull(middle);
        Assert.Equal(10, first.Value);
        Assert.Equal(50, last.Value);
        Assert.Equal(30, middle.Value);
    }

    [Fact]
    public void FullPipeline_DictionaryOperations_WorksCorrectly()
    {
        // 测试字典操作
        var code = @"
            dict <- {""name"": ""Alice"", ""age"": 25, ""city"": ""Beijing""}
            name <- dict[""name""]
            age <- dict[""age""]
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var name = interpreter.Manager.GetValue(new LangId("name")) as StringLangValue;
        var age = interpreter.Manager.GetValue(new LangId("age")) as IntLangValue;

        Assert.NotNull(name);
        Assert.NotNull(age);
        Assert.Equal("Alice", name.Value);
        Assert.Equal(25, age.Value);
    }

    [Fact]
    public void FullPipeline_ListOperations_WorksCorrectly()
    {
        // 测试列表操作
        var code = @"
            mylist <- {1, 2, 3, 4, 5}
            first <- mylist[0]
            third <- mylist[2]
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var first = interpreter.Manager.GetValue(new LangId("first")) as IntLangValue;
        var third = interpreter.Manager.GetValue(new LangId("third")) as IntLangValue;

        Assert.NotNull(first);
        Assert.NotNull(third);
        Assert.Equal(1, first.Value);
        Assert.Equal(3, third.Value);
    }

    [Fact]
    public void FullPipeline_TupleOperations_WorksCorrectly()
    {
        // 测试元组操作（创建和验证）
        var code = """tuple <- (100, "test")""";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var tuple = interpreter.Manager.GetValue(new LangId("tuple")) as TupleLangValue;
        Assert.NotNull(tuple);

        // 验证元组的值
        var v1 = tuple.V1.Run(interpreter.Manager) as IntLangValue;
        Assert.NotNull(v1);
        Assert.Equal(100, v1.Value);

        var v2 = tuple.V2.Run(interpreter.Manager) as StringLangValue;
        Assert.NotNull(v2);
        Assert.Equal("test", v2.Value);
    }

    #endregion

    #region 高级特性测试 (3 个)

    [Fact]
    public void FullPipeline_LambdaExecution_WorksCorrectly()
    {
        // 测试 Lambda 表达式执行
        var code = @"
            add <- (x, y) -> { return x + y }
            result <- add(10, 20)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(30, result.Value);
    }

    [Fact]
    public void FullPipeline_NestedFunctionCalls_WorksCorrectly()
    {
        // 测试嵌套函数调用
        var code = @"
            func double(x) {
                return x * 2
            }
            func addTen(x) {
                return x + 10
            }
            result <- addTen(double(5))
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(20, result.Value); // double(5) = 10, addTen(10) = 20
    }

    [Fact]
    public void FullPipeline_RecursiveFunction_WorksCorrectly()
    {
        // 测试递归函数 - 计算阶乘
        var code = """

                               func factorial(n) {
                                   if n <= 1 {
                                       return 1
                                   } else {
                                       return n * factorial(n - 1)
                                   }
                               }
                               result <- factorial(5)
                           
                   """;
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        var result = interpreter.Manager.GetValue(new LangId("result")) as IntLangValue;
        Assert.NotNull(result);
        Assert.Equal(120, result.Value); // 5! = 120
    }

    #endregion

    #region 错误处理场景测试 (2 个)

    [Fact]
    public void FullPipeline_SyntaxError_ThrowsAtParseStage()
    {
        // 测试语法错误在解析阶段抛出 - 未闭合的括号
        var code = "func test() { a <- 10"; // 未闭合的大括号
        var interpreter = new LangInterpreter();

        // 语法错误应该在 Build 阶段抛出
        Assert.ThrowsAny<Exception>(() => { interpreter.Build(code); });
    }

    [Fact]
    public void FullPipeline_RuntimeError_ThrowsAtExecutionStage()
    {
        // 测试运行时错误在执行阶段抛出 - 除以零
        var code = @"
            a <- 10
            b <- 0
            c <- a / b
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);

        // 运行时错误应该在 Run 阶段抛出
        Assert.Throws<ZeroDivisionError>(() => { ast.Run(interpreter.Manager); });
    }

    #endregion
}