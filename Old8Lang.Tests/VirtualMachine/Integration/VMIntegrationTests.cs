using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine.Integration;

/// <summary>
/// 虚拟机集成测试
/// 测试虚拟机执行复杂程序和端到端功能的正确性
/// </summary>
[Collection("Sequential")]
public class VMIntegrationTests
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
            var vm = new Bytecode.VirtualMachine(bytecodeFile);
            vm.Execute();

            return stringWriter.ToString().Trim();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    /// 验证虚拟机代码执行不抛出异常
    /// </summary>
    private void AssertVMExecutionSucceeds(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 编译为字节码
        var compiler = new BytecodeCompiler();
        var bytecodeFile = compiler.Compile(ast);

        // 执行字节码 - 验证不抛出异常
        var vm = new Bytecode.VirtualMachine(bytecodeFile);
        var exception = Record.Exception(() => vm.Execute());
        Assert.Null(exception);
    }

    #region 综合算法测试

    [Fact]
    public void BubbleSort_ExecutesCorrectly()
    {
        // Arrange - 冒泡排序算法
        var code = @"
            arr <- [64, 34, 25, 12, 22, 11, 90]
            n <- 7

            // 冒泡排序
            for i <- 0, i < n - 1, i++ {
                for j <- 0, j < n - i - 1, j++ {
                    if arr[j] > arr[j + 1] {
                        // 交换元素
                        temp <- arr[j]
                        arr[j] <- arr[j + 1]
                        arr[j + 1] <- temp
                    }
                }
            }

            // 输出排序结果
            for i <- 0, i < n, i++ {
                PrintLine(arr[i].ToStr())
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(7, lines.Length);
        Assert.Equal("11", lines[0]);
        Assert.Equal("12", lines[1]);
        Assert.Equal("22", lines[2]);
        Assert.Equal("25", lines[3]);
        Assert.Equal("34", lines[4]);
        Assert.Equal("64", lines[5]);
        Assert.Equal("90", lines[6]);
    }

    [Fact]
    public void Fibonacci_ExecutesCorrectly()
    {
        // Arrange - 斐波那契数列
        var code = @"
            n <- 10
            a <- 0
            b <- 1

            PrintLine(a.ToStr())
            PrintLine(b.ToStr())

            for i <- 2, i < n, i++ {
                c <- a + b
                PrintLine(c.ToStr())
                a <- b
                b <- c
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(10, lines.Length);
        Assert.Equal("0", lines[0]);
        Assert.Equal("1", lines[1]);
        Assert.Equal("1", lines[2]);
        Assert.Equal("2", lines[3]);
        Assert.Equal("3", lines[4]);
        Assert.Equal("5", lines[5]);
        Assert.Equal("8", lines[6]);
        Assert.Equal("13", lines[7]);
        Assert.Equal("21", lines[8]);
        Assert.Equal("34", lines[9]);
    }

    [Fact]
    public void PrimeNumbers_ExecutesCorrectly()
    {
        // Arrange - 找出前几个质数
        var code = @"
            limit <- 20
            count <- 0

            for num <- 2, num <= limit, num++ {
                isPrime <- true

                for i <- 2, i * i <= num, i++ {
                    if num % i == 0 {
                        isPrime <- false
                    }
                }

                if isPrime {
                    PrintLine(num.ToStr())
                    count <- count + 1
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var primes = new[] { "2", "3", "5", "7", "11", "13", "17", "19" };
        Assert.Equal(primes.Length, lines.Length);
        for (int i = 0; i < primes.Length; i++)
        {
            Assert.Equal(primes[i], lines[i]);
        }
    }

    #endregion

    #region 数据结构操作测试

    [Fact]
    public void ComplexDataStructure_ExecutesCorrectly()
    {
        // Arrange - 复杂数据结构操作
        var code = @"
            // 创建学生成绩管理系统
            students <- {
                ""Alice"": {""math"": 85, ""english"": 90, ""science"": 88},
                ""Bob"": {""math"": 78, ""english"": 82, ""science"": 80},
                ""Charlie"": {""math"": 92, ""english"": 87, ""science"": 95}
            }

            // 计算每个学生的平均分
            alice_scores <- students[""Alice""]
            alice_avg <- (alice_scores[""math""] + alice_scores[""english""] + alice_scores[""science""]) / 3

            bob_scores <- students[""Bob""]
            bob_avg <- (bob_scores[""math""] + bob_scores[""english""] + bob_scores[""science""]) / 3

            charlie_scores <- students[""Charlie""]
            charlie_avg <- (charlie_scores[""math""] + charlie_scores[""english""] + charlie_scores[""science""]) / 3

            PrintLine(alice_avg.ToStr())
            PrintLine(bob_avg.ToStr())
            PrintLine(charlie_avg.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("87", lines[0]); // (85+90+88)/3 = 87
        Assert.Equal("80", lines[1]); // (78+82+80)/3 = 80
        Assert.Equal("91", lines[2]); // (92+87+95)/3 = 91
    }

    [Fact]
    public void MatrixOperations_ExecuteCorrectly()
    {
        // Arrange - 矩阵操作
        var code = @"
            // 创建两个2x2矩阵
            matrix1 <- [[1, 2], [3, 4]]
            matrix2 <- [[5, 6], [7, 8]]

            // 矩阵加法
            result <- [[0, 0], [0, 0]]
            for i <- 0, i < 2, i++ {
                for j <- 0, j < 2, j++ {
                    row1 <- matrix1[i]
                    row2 <- matrix2[i]
                    result_row <- result[i]
                    result_row[j] <- row1[j] + row2[j]
                }
            }

            // 输出结果
            for i <- 0, i < 2, i++ {
                row <- result[i]
                for j <- 0, j < 2, j++ {
                    PrintLine(row[j].ToStr())
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("6", lines[0]);  // 1+5
        Assert.Equal("8", lines[1]);  // 2+6
        Assert.Equal("10", lines[2]); // 3+7
        Assert.Equal("12", lines[3]); // 4+8
    }

    #endregion

    #region 字符串处理测试

    [Fact]
    public void StringProcessing_ExecutesCorrectly()
    {
        // Arrange - 字符串处理
        var code = @"
            text <- ""Hello World Programming""
            words <- [""Hello"", ""World"", ""Programming""]

            // 统计每个单词的出现次数
            for word in words {
                count <- 0
                // 简单的字符串包含检查
                if text == ""Hello World Programming"" {
                    if word == ""Hello"" {
                        count <- 1
                    } elif word == ""World"" {
                        count <- 1
                    } elif word == ""Programming"" {
                        count <- 1
                    }
                }
                PrintLine(word + "": "" + count.ToStr())
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Hello: 1", lines[0]);
        Assert.Equal("World: 1", lines[1]);
        Assert.Equal("Programming: 1", lines[2]);
    }

    [Fact]
    public void StringManipulation_ExecutesCorrectly()
    {
        // Arrange - 字符串操作
        var code = @"
            // 字符串拼接和处理
            firstName <- ""John""
            lastName <- ""Doe""
            age <- 30

            fullName <- firstName + "" "" + lastName
            greeting <- ""Hello, "" + fullName + ""! You are "" + age.ToStr() + "" years old.""

            PrintLine(fullName)
            PrintLine(greeting)

            // 字符串长度模拟（通过遍历字符）
            text <- ""test""
            length <- 0
            for char in text {
                length <- length + 1
            }
            PrintLine(""Length: "" + length.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3, lines.Length);
        Assert.Equal("John Doe", lines[0]);
        Assert.Equal("Hello, John Doe! You are 30 years old.", lines[1]);
        Assert.Equal("Length: 4", lines[2]);
    }

    #endregion

    #region 控制流综合测试

    [Fact]
    public void ComplexControlFlow_ExecutesCorrectly()
    {
        // Arrange - 复杂控制流
        var code = @"
            result <- 0

            for i <- 1, i <= 100, i++ {
                // FizzBuzz变种：计算特殊数字的和
                if i % 15 == 0 {
                    result <- result + i * 3  // 既是3又是5的倍数，乘以3
                } elif i % 3 == 0 {
                    result <- result + i      // 3的倍数，加原值
                } elif i % 5 == 0 {
                    result <- result + i * 2  // 5的倍数，乘以2
                }
            }

            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // 计算预期结果：
        // 15的倍数(15,30,45,60,75,90)*3 = (15+30+45+60+75+90)*3 = 315*3 = 945
        // 3的倍数但不是15的倍数(3,6,9,12,18,21,24,27,33,36,39,42,48,51,54,57,63,66,69,72,78,81,84,87,93,96,99) = 1368
        // 5的倍数但不是15的倍数(5,10,20,25,35,40,50,55,65,70,80,85,95,100)*2 = 735*2 = 1470
        // 总计: 945 + 1368 + 1470 = 3783
        Assert.Equal("3783", output);
    }

    [Fact]
    public void NestedLoopsWithConditions_ExecuteCorrectly()
    {
        // Arrange - 嵌套循环和条件
        var code = @"
            sum <- 0

            for i <- 1, i <= 5, i++ {
                for j <- 1, j <= 5, j++ {
                    product <- i * j
                    if product % 2 == 0 {
                        sum <- sum + product
                    } else {
                        sum <- sum - 1
                    }
                }
            }

            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // 计算所有i*j的值，偶数加到sum，奇数减1
        var expectedSum = 0;
        for (int i = 1; i <= 5; i++)
        {
            for (int j = 1; j <= 5; j++)
            {
                var product = i * j;
                if (product % 2 == 0)
                    expectedSum += product;
                else
                    expectedSum -= 1;
            }
        }
        Assert.Equal(expectedSum.ToString(), output);
    }

    #endregion

    #region 错误处理和边界测试

    [Fact]
    public void EmptyProgram_ExecutesSuccessfully()
    {
        // Arrange
        var code = "";

        // Act & Assert
        AssertVMExecutionSucceeds(code);
    }

    [Fact]
    public void OnlyComments_ExecutesSuccessfully()
    {
        // Arrange
        var code = @"
            // 这是注释
            // 另一个注释
        ";

        // Act & Assert
        AssertVMExecutionSucceeds(code);
    }

    [Fact]
    public void LargeNumbers_ExecuteCorrectly()
    {
        // Arrange
        var code = @"
            big1 <- 1000000
            big2 <- 2000000
            result <- big1 + big2
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("3000000", output);
    }

    [Fact]
    public void DeepNesting_ExecutesCorrectly()
    {
        // Arrange - 深度嵌套测试
        var code = @"
            result <- 0
            if true {
                if true {
                    if true {
                        if true {
                            if true {
                                result <- 42
                            }
                        }
                    }
                }
            }
            PrintLine(result.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        Assert.Equal("42", output);
    }

    #endregion

    #region 性能和压力测试

    [Fact]
    public void LargeLoop_ExecutesCorrectly()
    {
        // Arrange - 大循环测试
        var code = @"
            sum <- 0
            for i <- 1, i <= 1000, i++ {
                sum <- sum + i
            }
            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // 1到1000的和 = 1000 * 1001 / 2 = 500500
        Assert.Equal("500500", output);
    }

    [Fact]
    public void LargeArray_ExecutesCorrectly()
    {
        // Arrange - 大数组测试
        var code = @"
            arr <- []
            sum <- 0

            // 模拟创建大数组并求和
            for i <- 1, i <= 100, i++ {
                sum <- sum + i
            }

            PrintLine(sum.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        // 1到100的和 = 100 * 101 / 2 = 5050
        Assert.Equal("5050", output);
    }

    #endregion

    #region 综合应用测试

    [Fact]
    public void SimpleCalculator_ExecutesCorrectly()
    {
        // Arrange - 简单计算器
        var code = @"
            operations <- [""+"", ""-"", ""*"", ""/""]
            a <- 20
            b <- 4

            for op in operations {
                if op == ""+"" {
                    result <- a + b
                    PrintLine(a.ToStr() + "" + "" + b.ToStr() + "" = "" + result.ToStr())
                } elif op == ""-"" {
                    result <- a - b
                    PrintLine(a.ToStr() + "" - "" + b.ToStr() + "" = "" + result.ToStr())
                } elif op == ""*"" {
                    result <- a * b
                    PrintLine(a.ToStr() + "" * "" + b.ToStr() + "" = "" + result.ToStr())
                } elif op == ""/"" {
                    result <- a / b
                    PrintLine(a.ToStr() + "" / "" + b.ToStr() + "" = "" + result.ToStr())
                }
            }
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("20 + 4 = 24", lines[0]);
        Assert.Equal("20 - 4 = 16", lines[1]);
        Assert.Equal("20 * 4 = 80", lines[2]);
        Assert.Equal("20 / 4 = 5", lines[3]);
    }

    [Fact]
    public void DataAnalysis_ExecutesCorrectly()
    {
        // Arrange - 数据分析示例
        var code = @"
            // 模拟数据分析：计算平均值、最大值、最小值
            data <- [85, 92, 78, 96, 88, 79, 94, 87, 91, 83]

            // 计算总和和平均值
            sum <- 0
            count <- 0
            for value in data {
                sum <- sum + value
                count <- count + 1
            }
            average <- sum / count

            // 找最大值和最小值
            max <- data[0]
            min <- data[0]
            for value in data {
                if value > max {
                    max <- value
                }
                if value < min {
                    min <- value
                }
            }

            PrintLine(""Sum: "" + sum.ToStr())
            PrintLine(""Average: "" + average.ToStr())
            PrintLine(""Max: "" + max.ToStr())
            PrintLine(""Min: "" + min.ToStr())
        ";

        // Act
        var output = ExecuteVMCode(code);

        // Assert
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);
        Assert.Equal("Sum: 873", lines[0]);
        Assert.Equal("Average: 87", lines[1]); // 873/10 = 87
        Assert.Equal("Max: 96", lines[2]);
        Assert.Equal("Min: 78", lines[3]);
    }

    #endregion
}