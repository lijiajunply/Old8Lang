using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Integration;

/// <summary>
/// 端到端测试 - 验证完整的真实场景
/// 模拟实际使用中的复杂业务逻辑和操作
/// </summary>
[Collection("Sequential")]
public class EndToEndTests(ITestOutputHelper testOutputHelper)
{
    #region 数学计算场景 (3 个)

    [Fact]
    public void EndToEnd_Calculator_WorksCorrectly()
    {
        // 测试计算器功能（加减乘除）
        var code = @"
            // 定义计算器函数
            func add(x, y) { return x + y }
            func subtract(x, y) { return x - y }
            func multiply(x, y) { return x * y }
            func divide(x, y) { return x / y }

            // 执行计算
            result1 <- add(100, 50)
            result2 <- subtract(100, 30)
            result3 <- multiply(12, 5)
            result4 <- divide(100, 4)

            // 复合计算
            final <- add(multiply(10, 5), divide(100, 2))
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(150, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result1"))!).Value);
        Assert.Equal(70, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result2"))!).Value);
        Assert.Equal(60, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result3"))!).Value);
        Assert.Equal(25, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result4"))!).Value);
        Assert.Equal(100, ((IntLangValue)interpreter.Manager.GetValue(new LangId("final"))!).Value); // 10*5 + 100/2 = 50 + 50 = 100
    }

    [Fact]
    public void EndToEnd_FibonacciSequence_GeneratesCorrectValues()
    {
        // 测试斐波那契数列生成
        var code = @"
            func fibonacci(n) {
                if n <= 1 {
                    return n
                }
                return fibonacci(n - 1) + fibonacci(n - 2)
            }

            fib0 <- fibonacci(0)
            fib1 <- fibonacci(1)
            fib5 <- fibonacci(5)
            fib7 <- fibonacci(7)
            fib10 <- fibonacci(10)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(0, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fib0"))!).Value);
        Assert.Equal(1, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fib1"))!).Value);
        Assert.Equal(5, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fib5"))!).Value);
        Assert.Equal(13, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fib7"))!).Value);
        Assert.Equal(55, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fib10"))!).Value);
    }

    [Fact]
    public void EndToEnd_FactorialCalculation_WorksCorrectly()
    {
        // 测试阶乘计算
        var code = @"
            func factorial(n) {
                if n <= 1 {
                    return 1
                }
                return n * factorial(n - 1)
            }

            fact0 <- factorial(0)
            fact1 <- factorial(1)
            fact5 <- factorial(5)
            fact7 <- factorial(7)
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(1, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fact0"))!).Value);
        Assert.Equal(1, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fact1"))!).Value);
        Assert.Equal(120, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fact5"))!).Value); // 5! = 120
        Assert.Equal(5040, ((IntLangValue)interpreter.Manager.GetValue(new LangId("fact7"))!).Value); // 7! = 5040
    }

    #endregion

    #region 集合操作场景 (2 个)

    [Fact]
    public void EndToEnd_ArraySorting_WorksCorrectly()
    {
        // 测试数组排序（冒泡排序）
        var code = @"
            arr <- [64, 34, 25, 12, 22, 11, 90]
            n <- 7

            // 冒泡排序
            for i <- 0, i < n - 1, i++ {
                for j <- 0, j < n - i - 1, j++ {
                    if arr[j] > arr[j + 1] {
                        // 交换
                        temp <- arr[j]
                        arr[j] <- arr[j + 1]
                        arr[j + 1] <- temp
                    }
                }
            }

            first <- arr[0]
            second <- arr[1]
            last <- arr[6]
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // 验证排序结果
        Assert.Equal(11, ((IntLangValue)interpreter.Manager.GetValue(new LangId("first"))!).Value);
        Assert.Equal(12, ((IntLangValue)interpreter.Manager.GetValue(new LangId("second"))!).Value);
        Assert.Equal(90, ((IntLangValue)interpreter.Manager.GetValue(new LangId("last"))!).Value);
    }

    [Fact]
    public void EndToEnd_DictionaryManipulation_WorksCorrectly()
    {
        // 测试字典操作
        var code = @"
            // 创建学生信息字典
            student <- {""name"": ""Alice"", ""age"": 20, ""grade"": ""A""}

            // 读取信息
            name <- student[""name""]
            age <- student[""age""]
            grade <- student[""grade""]

            // 创建课程成绩字典
            scores <- {""math"": 95, ""english"": 88, ""physics"": 92}
            mathScore <- scores[""math""]
            englishScore <- scores[""english""]
            physicsScore <- scores[""physics""]

            // 计算平均分
            avgScore <- (mathScore + englishScore + physicsScore) / 3
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal("Alice", ((StringLangValue)interpreter.Manager.GetValue(new LangId("name"))!).Value);
        Assert.Equal(20, ((IntLangValue)interpreter.Manager.GetValue(new LangId("age"))!).Value);
        Assert.Equal("A", ((StringLangValue)interpreter.Manager.GetValue(new LangId("grade"))!).Value);
        Assert.Equal(95, ((IntLangValue)interpreter.Manager.GetValue(new LangId("mathScore"))!).Value);
        Assert.Equal(88, ((IntLangValue)interpreter.Manager.GetValue(new LangId("englishScore"))!).Value);
        Assert.Equal(92, ((IntLangValue)interpreter.Manager.GetValue(new LangId("physicsScore"))!).Value);
        Assert.Equal(91, ((IntLangValue)interpreter.Manager.GetValue(new LangId("avgScore"))!).Value); // (95+88+92)/3 = 91
    }

    #endregion

    #region 字符串和循环场景 (2 个)

    [Fact]
    public void EndToEnd_StringManipulation_WorksCorrectly()
    {
        // 测试字符串操作
        var code = @"
            // 字符串拼接
            firstName <- ""John""
            lastName <- ""Doe""
            fullName <- firstName + "" "" + lastName

            // 字符串重复拼接
            repeated <- """"
            for i <- 0, i < 3, i++ {
                repeated <- repeated + ""Hello""
                if i < 2 {
                    repeated <- repeated + "" ""
                }
            }

            // 构建格式化字符串
            age <- 25
            message <- ""Name: "" + fullName + "", Age: ""
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal("John Doe", ((StringLangValue)interpreter.Manager.GetValue(new LangId("fullName"))!).Value);
        Assert.Equal("Hello Hello Hello", ((StringLangValue)interpreter.Manager.GetValue(new LangId("repeated"))!).Value);
        Assert.Equal("Name: John Doe, Age: ", ((StringLangValue)interpreter.Manager.GetValue(new LangId("message"))!).Value);
    }

    [Fact]
    public void EndToEnd_NestedLoops_WorksCorrectly()
    {
        // 测试嵌套循环 - 生成乘法表
        var code = @"
            // 计算 1-5 的乘法表特定位置的值
            result1 <- 0
            result2 <- 0
            result3 <- 0

            for i <- 1, i <= 5, i++ {
                for j <- 1, j <= 5, j++ {
                    product <- i * j
                    if i == 2 && j == 3 {
                        result1 <- product
                    }
                    if i == 4 && j == 5 {
                        result2 <- product
                    }
                    if i == 5 && j == 5 {
                        result3 <- product
                    }
                }
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        Assert.Equal(6, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result1"))!).Value); // 2 * 3 = 6
        Assert.Equal(20, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result2"))!).Value); // 4 * 5 = 20
        Assert.Equal(25, ((IntLangValue)interpreter.Manager.GetValue(new LangId("result3"))!).Value); // 5 * 5 = 25
    }

    #endregion

    #region 复杂业务逻辑场景 (1 个)

    [Fact]
    public void EndToEnd_ComplexBusinessLogic_WorksCorrectly()
    {
        // 测试复杂业务逻辑 - 订单处理系统
        var code = @"
            // 定义价格计算函数
            func calculateTotal(price, quantity, discount) {
                subtotal <- price * quantity
                discountAmount <- subtotal * discount / 100
                return subtotal - discountAmount
            }

            // 定义评级函数
            func getGrade(score) {
                if score >= 90 {
                    return ""A""
                } elif score >= 80 {
                    return ""B""
                } elif score >= 70 {
                    return ""C""
                } else {
                    return ""D""
                }
            }

            // 订单 1: 价格 100, 数量 3, 折扣 10%
            order1 <- calculateTotal(100, 3, 10)

            // 订单 2: 价格 50, 数量 5, 折扣 20%
            order2 <- calculateTotal(50, 5, 20)

            // 计算总收入
            totalRevenue <- order1 + order2

            // 评分系统
            score1 <- 95
            score2 <- 82
            score3 <- 68

            grade1 <- getGrade(score1)
            grade2 <- getGrade(score2)
            grade3 <- getGrade(score3)

            // 统计及格数量（分数 >= 70）
            passCount <- 0

            // 在if语句前验证变量值
            debugScore1 <- score1
            debugScore2 <- score2
            debugScore3 <- score3

            if score1 >= 70 {
                passCount <- passCount + 1
            }

            if score2 >= 70 {
                passCount <- passCount + 1
            }

            if score3 >= 70 {
                passCount <- passCount + 1
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);
        ast.Run(interpreter.Manager);

        // 验证订单计算
        Assert.Equal(270, ((IntLangValue)interpreter.Manager.GetValue(new LangId("order1"))!).Value); // 100*3 - 30 = 270
        Assert.Equal(200, ((IntLangValue)interpreter.Manager.GetValue(new LangId("order2"))!).Value); // 50*5 - 50 = 200
        Assert.Equal(470, ((IntLangValue)interpreter.Manager.GetValue(new LangId("totalRevenue"))!).Value); // 270 + 200

        // 验证评分系统
        Assert.Equal("A", ((StringLangValue)interpreter.Manager.GetValue(new LangId("grade1"))!).Value);
        Assert.Equal("B", ((StringLangValue)interpreter.Manager.GetValue(new LangId("grade2"))!).Value);
        Assert.Equal("D", ((StringLangValue)interpreter.Manager.GetValue(new LangId("grade3"))!).Value);

        // 调试AST结构
        testOutputHelper.WriteLine($"AST structure: {ast.GetType().Name} with {ast.Count} statements");
        for (int i = 0; i < ast.Count; i++)
        {
            testOutputHelper.WriteLine($"  Statement {i}: {ast[i].GetType().Name} - {ast[i]}");
        }

        // 检查所有相关变量
        testOutputHelper.WriteLine("=== All variables check ===");

        var allVariables = new[] {
            "order1", "order2", "totalRevenue",
            "score1", "score2", "score3",
            "grade1", "grade2", "grade3",
            "passCount", "debugScore1", "debugScore2", "debugScore3"
        };

        foreach (var varName in allVariables)
        {
            var value = interpreter.Manager.GetValue(new LangId(varName));
            testOutputHelper.WriteLine($"{varName}: {value} ({value?.GetType().Name})");

            // 检查是否包含"Task"字符串
            if (value?.GetType().Name.Contains("Task") == true)
            {
                testOutputHelper.WriteLine($"  ⚠️  WARNING: {varName} appears to be a Task type!");
            }
        }

        // 验证关键变量的值
        var score1Val = interpreter.Manager.GetValue(new LangId("score1"));
        var score2Val = interpreter.Manager.GetValue(new LangId("score2"));
        var score3Val = interpreter.Manager.GetValue(new LangId("score3"));
        var passCountVal = interpreter.Manager.GetValue(new LangId("passCount"));

        if (score1Val is IntLangValue s1 && score2Val is IntLangValue s2 && score3Val is IntLangValue s3 && passCountVal is IntLangValue pc)
        {
            testOutputHelper.WriteLine($"Score values: score1={s1.Value}, score2={s2.Value}, score3={s3.Value}");
            testOutputHelper.WriteLine($"Expected passCount: 2, Actual: {pc.Value}");

            // 手动验证比较操作
            testOutputHelper.WriteLine("Manual comparison checks:");
            testOutputHelper.WriteLine($"  score1 >= 70: {s1.Value >= 70}");
            testOutputHelper.WriteLine($"  score2 >= 70: {s2.Value >= 70}");
            testOutputHelper.WriteLine($"  score3 >= 70: {s3.Value >= 70}");

            Assert.Equal(2, pc.Value);
        }
        else
        {
            var types = $"score1={score1Val?.GetType().Name}, score2={score2Val?.GetType().Name}, score3={score3Val?.GetType().Name}, passCount={passCountVal?.GetType().Name}";
            Assert.Fail($"Variables are not expected IntLangValue types: {types}");
        }
    }

    #endregion
}
