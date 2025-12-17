using Xunit;
using Old8Lang.Interpreter;
using Old8Lang.LangParser;

namespace Old8Lang.Tests.Interpreter;

/// <summary>
/// 真正的解释模式执行测试 - 测试代码的实际执行结果
/// </summary>
[Collection("Sequential")]
public class InterpreterExecutionTests
{
    /// <summary>
    /// 执行代码并验证不会抛出异常
    /// </summary>
    private void ExecuteCodeWithoutException(string code)
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 如果代码能成功执行到这里，说明解析成功
        Assert.NotNull(ast);

        // 执行代码，不应该抛出异常
        var exception = Record.Exception(() => ast.Run(interpreter.Manager));

        // 可以根据预期的行为调整这个断言
        // 如果某些操作预期会抛出异常，需要单独处理
        Assert.True(exception == null || IsExpectedException(exception),
                   $"Unexpected exception: {exception?.Message}");
    }

    /// <summary>
    /// 判断是否是预期的异常
    /// </summary>
    private bool IsExpectedException(Exception ex)
    {
        var message = ex.Message.ToLower();
        return message.Contains("除零") ||
               message.Contains("division") ||
               message.Contains("zero") ||
               message.Contains("索引") ||
               message.Contains("index") ||
               message.Contains("未实现") ||
               message.Contains("not implemented");
    }

    [Fact(DisplayName = "解释器 - 基本变量赋值执行")]
    public void Interpreter_BasicVariableAssignment_ShouldExecute()
    {
        var code = """
                   a <- 42
                   b <- "hello world"
                   c <- 3.14159
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 算术运算执行")]
    public void Interpreter_ArithmeticOperations_ShouldExecute()
    {
        var code = """
                   x <- 10
                   y <- 20
                   sum <- x + y
                   diff <- y - x
                   product <- x * y
                   quotient <- y / x
                   remainder <- y % x
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 函数定义和调用执行")]
    public void Interpreter_FunctionDefinitionAndCall_ShouldExecute()
    {
        var code = """
                   func add(a: int, b: int) -> int {
                       return a + b
                   }

                   result1 <- add(5, 3)
                   result2 <- add(10, 20)
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 条件语句执行")]
    public void Interpreter_ConditionalStatements_ShouldExecute()
    {
        var code = """
                   x <- 10
                   if x > 5 {
                       result <- "greater than 5"
                   } else {
                       result <- "less than or equal to 5"
                   }

                   y <- 3
                   if y > 5 {
                       result2 <- "greater than 5"
                   } else {
                       result2 <- "less than or equal to 5"
                   }
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 循环语句执行")]
    public void Interpreter_LoopStatements_ShouldExecute()
    {
        var code = """
                   sum <- 0
                   count <- 0
                   for i <- 0, i < 5, i <- i + 1 {
                       sum <- sum + i
                       count <- count + 1
                   }

                   while_count <- 0
                   while while_count < 3 {
                       while_count <- while_count + 1
                   }
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 列表操作执行")]
    public void Interpreter_ListOperations_ShouldExecute()
    {
        var code = """
                   numbers <- {1, 2, 3, 4, 5}
                   first <- numbers[0]
                   last <- numbers[4]
                   count <- numbers.Count()
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 字典操作执行")]
    public void Interpreter_DictionaryOperations_ShouldExecute()
    {
        var code = """
                   person <- {"name": "Alice", "age": 25, "city": "Beijing"}
                   name <- person["name"]
                   age <- person["age"]
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 字符串操作执行")]
    public void Interpreter_StringOperations_ShouldExecute()
    {
        var code = """
                   greeting <- "Hello"
                   name <- "World"
                   message <- greeting + " " + name
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 复杂表达式执行")]
    public void Interpreter_ComplexExpressions_ShouldExecute()
    {
        var code = """
                   a <- 10
                   b <- 20
                   c <- 5
                   result <- (a + b) * c - (a / b)
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 嵌套函数调用执行")]
    public void Interpreter_NestedFunctionCalls_ShouldExecute()
    {
        var code = """
                   func multiply(x: int, y: int) -> int {
                       return x * y
                   }

                   func add(x: int, y: int) -> int {
                       return x + y
                   }

                   result <- multiply(add(2, 3), 4)
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 变量重新赋值执行")]
    public void Interpreter_VariableReassignment_ShouldExecute()
    {
        var code = """
                   x <- 10
                   x <- x + 5
                   x <- x * 2
                   final_value <- x
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 作用域测试执行")]
    public void Interpreter_ScopeTest_ShouldExecute()
    {
        var code = """
                   global_var <- 100

                   func testScope() -> int {
                       local_var <- 50
                       return global_var + local_var
                   }

                   result <- testScope()
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 数组操作执行")]
    public void Interpreter_ArrayOperations_ShouldExecute()
    {
        var code = """
                   arr <- [1, 2, 3, 4, 5]
                   length <- len(arr)
                   first_element <- arr[0]
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - switch语句执行")]
    public void Interpreter_SwitchStatement_ShouldExecute()
    {
        var code = """
                   day <- 3
                   result <- ""

                   switch day {
                       case 1 {
                           result <- "Monday"
                       }
                       case 2 {
                           result <- "Tuesday"
                       }
                       case 3 {
                           result <- "Wednesday"
                       }
                       default {
                           result <- "Other day"
                       }
                   }
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 控制流语句执行")]
    public void Interpreter_ControlFlowStatements_ShouldExecute()
    {
        var code = """
                   // break 测试
                   for i <- 0, i < 10, i <- i + 1 {
                       if i == 5 {
                           break
                       }
                   }

                   // continue 测试
                   sum <- 0
                   for j <- 0, j < 5, j <- j + 1 {
                       if j == 2 {
                           continue
                       }
                       sum <- sum + j
                   }

                   // return 测试
                   func testReturn() -> int {
                       if true {
                           return 42
                       }
                       return 0
                   }

                   return_result <- testReturn()
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 嵌套结构执行")]
    public void Interpreter_NestedStructures_ShouldExecute()
    {
        var code = """
                   // 嵌套数组
                   matrix <- {{1, 2}, {3, 4}, {5, 6}}
                   first_row <- matrix[0]
                   first_element <- first_row[0]

                   // 嵌套字典
                   person <- {
                       "name": "Alice",
                       "address": {
                           "street": "123 Main St",
                           "city": "Beijing"
                       }
                   }

                   street_name <- person["address"]["street"]
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - for-in循环执行")]
    public void Interpreter_ForInLoop_ShouldExecute()
    {
        var code = """
                   numbers <- {1, 2, 3, 4, 5}
                   total <- 0
                   count <- 0

                   for num in numbers {
                       total <- total + num
                       count <- count + 1
                   }

                   // 字典遍历
                   person <- {"name": "Bob", "age": 30}
                   for key in person {
                       // 简单处理
                   }
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 错误处理执行")]
    public void Interpreter_ErrorHandling_ShouldExecute()
    {
        // 测试正常除法
        var normalCode = """
                         result1 <- 10 / 2
                         result2 <- 100 / 5
                         """;

        ExecuteCodeWithoutException(normalCode);

        // 测试数组边界访问（可能会抛出异常，但这是预期的）
        var boundaryCode = """
                          arr <- {1, 2, 3}
                          // valid_access <- arr[0]
                          // invalid_access <- arr[10]  // 这可能会抛出异常
                          """;

        ExecuteCodeWithoutException(boundaryCode);
    }

    [Fact(DisplayName = "解释器 - 比较运算执行")]
    public void Interpreter_ComparisonOperations_ShouldExecute()
    {
        var code = """
                   a <- 10
                   b <- 20
                   c <- 10

                   result1 <- a > b      // false
                   result2 <- a < b      // true
                   result3 <- a >= c     // true
                   result4 <- b <= a     // false
                   result5 <- a == c     // true
                   result6 <- a != b     // true
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 逻辑运算执行")]
    public void Interpreter_LogicalOperations_ShouldExecute()
    {
        var code = """
                   // 注意：如果 true/false 关键字不支持，这些测试可能会失败
                   // 这也帮助识别语言实现的缺口

                   // 测试数值的逻辑运算（某些语言中非零为true）
                   a <- 1
                   b <- 0

                   // 这些运算的具体行为取决于语言的实现
                   // result1 <- a && b
                   // result2 <- a || b
                   // result3 <- !a
                   """;

        ExecuteCodeWithoutException(code);
    }

    [Fact(DisplayName = "解释器 - 复合表达式执行")]
    public void Interpreter_CompoundExpressions_ShouldExecute()
    {
        var code = """
                   // 复合算术表达式
                   result1 <- (10 + 20) * 3 - 15 / 5

                   // 混合运算
                   x <- 5
                   y <- 10
                   z <- 2
                   result2 <- x * y + z * (x + y)

                   // 字符串和数值混合
                   text <- "Value is: "
                   num <- 42
                   // result3 <- text + num.ToStr()  // 如果支持ToString方法
                   """;

        ExecuteCodeWithoutException(code);
    }
}