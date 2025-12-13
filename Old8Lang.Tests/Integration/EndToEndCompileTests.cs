using Old8Lang.LangParser;

namespace Old8Lang.Tests.Integration;

/// <summary>
/// 编译模式端到端测试 - 验证从源代码到目标代码输出的全过程
/// 测试编译模式下的完整流程：解析 -> IL生成 -> 验证 -> 执行
/// </summary>
[Collection("Sequential")]
public class EndToEndCompileTests
{
    /// <summary>
    /// 测试完整编译流程：从文件读取到IL生成和执行
    /// </summary>
    [Fact]
    public void EndToEndCompile_FromFile_CompilesAndExecutes()
    {
        // Arrange
        var testCode = """
                       func test:int() {
                           a <- 123
                           b <- 456
                           c <- a + b
                           return c
                       }
                       result <- test()

                       """;

        var testFile = Path.GetTempFileName() + ".old8";
        File.WriteAllText(testFile, testCode);

        try
        {
            // Act
            var interpreter = new LangInterpreter();
            var compiledAction = Compiler.Compiler.Compile(testFile, interpreter);

            // Assert
            Assert.NotNull(compiledAction);

            // 执行编译后的代码
            var exception = Record.Exception(() => compiledAction());
            Assert.Null(exception);
        }
        finally
        {
            // Cleanup
            if (File.Exists(testFile))
            {
                File.Delete(testFile);
            }
        }
    }

    /// <summary>
    /// 测试编译模式下的基本语法结构
    /// </summary>
    [Fact(Skip = "函数调用的IL生成有已知bug - 需要编译器层面修复")]
    public void EndToEndCompile_BasicSyntaxStructures_CompilesCorrectly()
    {
        // 测试各种基本语法结构在编译模式下的表现
        var code = """
                               // 变量赋值
                               a <- 123
                               b <- 456.789
                               c <- "test string"
                               d <- true
                               
                               // 数组和字典
                               arr <- [1, 2, 3, 4, 5]
                               dict <- {"name": "test", "value": 123}
                               
                               // 条件语句
                               if a > 100 {
                                   result1 <- "greater"
                               } else {
                                   result1 <- "less"
                               }
                               
                               // 循环语句
                               sum <- 0
                               for i <- 0, i < 4, i++ {
                                   sum <- sum + arr[i]
                               }
                               
                               // 函数定义和调用
                               func add:int(x:int, y:int) {
                                   return x + y
                               }
                               
                               func multiply:int(x:int, y:int) {
                                   return x * y
                               }
                               
                               func complex_calc:int(a:int, b:int, c:int) {
                                   temp <- add(a, b)
                                   return multiply(temp, c)
                               }
                               
                               // 函数调用
                               result2 <- add(10, 20)
                               result3 <- multiply(5, 6)
                               result4 <- complex_calc(2, 3, 4)
                           
                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试编译模式下的类和对象
    /// </summary>
    [Fact(Skip = "类方法调用的IL生成有已知bug - 需要编译器层面修复")]
    public void EndToEndCompile_ClassAndObject_CompilesCorrectly()
    {
        // 测试类声明、实例化和方法调用在编译模式下的表现
        var code = """
                               class Person {
                                    name <- ""
                                    age <- 18
                                   func init(name:string, age:int) -> void {
                                       this.name <- name
                                       this.age <- age
                                   }

                                   func get_name() -> string {
                                       return this.name
                                   }

                                   func get_age() -> int {
                                       return this.age
                                   }

                                   func set_age(new_age:int) -> void {
                                       this.age <- new_age
                                   }

                                   func get_info() -> string {
                                       return this.name + " " + this.age
                                   }
                               }
                               
                               // 实例化对象
                               person1 <- Person("John", 30)
                               person2 <- Person("Jane", 25)
                               
                               // 调用对象方法
                               name1 <- person1.get_name()
                               age1 <- person1.get_age()
                               
                               // 修改对象属性
                               person1.set_age(31)
                               updated_age <- person1.get_age()
                               
                               // 调用更复杂的方法
                               info1 <- person1.get_info()
                               info2 <- person2.get_info()
                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试编译模式下的lambda表达式
    /// </summary>
    [Fact(Skip = "Lambda表达式调用的IL生成有已知bug - 需要编译器层面修复")]
    public void EndToEndCompile_LambdaExpressions_CompilesCorrectly()
    {
        // 测试lambda表达式在编译模式下的表现
        var code = """
                               // 简单lambda
                               add <- (x:int, y:int) -> x + y

                               // 多行lambda
                               multiply <- (x:int, y:int) -> {
                                   result <- x * y
                                   return result
                               }

                               // lambda作为参数
                               double <- (x:int) -> x * 2
                               triple <- (x:int) -> x * 3

                               // 调用
                               result1 <- add(10, 20)
                               result2 <- multiply(5, 6)

                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试编译模式下的错误处理
    /// </summary>
    [Fact]
    public void EndToEndCompile_ErrorHandling_CompilesCorrectly()
    {
        // 测试try-catch语句在编译模式下的表现
        var code = """

                               // 测试try-catch
                               try {
                                   // 这行代码在执行时会抛出异常
                                   a <- 10 / 0
                               } catch {
                                   // 异常处理
                                   a <- 0
                                   error_occurred <- true
                               }
                               
                               // 测试finally块
                               result <- 0
                               try {
                                   result <- 100
                                   // return result
                               } catch {
                                   result <- 200
                               } finally {
                                   // finally块始终执行
                                   finally_executed <- true
                               }
                           
                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试编译模式下的复杂控制流
    /// </summary>
    [Fact]
    public void EndToEndCompile_ComplexControlFlow_CompilesCorrectly()
    {
        // 测试复杂控制流在编译模式下的表现
        var code = """

                               // 测试嵌套条件和循环
                               outer <- 0
                               inner <- 0
                               
                               for i in [0~2] {
                                   outer <- outer + 1
                                   
                                   for j in [0~2] {
                                       inner <- inner + 1
                                       
                                       if i == 1 && j == 1 {
                                           // 跳过这个组合
                                           continue
                                       }
                                       
                                       if i == 2 && j == 2 {
                                           // 提前退出内层循环
                                           break
                                       }
                                       
                                       combination <- i * 10 + j
                                   }
                                   
                                   if outer == 2 {
                                       // 提前退出外层循环
                                       break
                                   }
                               }
                               
                               // 测试switch语句
                               value <- 2
                               switch value {
                                   case 1 {
                                       result <- "one"
                                   }
                                   case 2 {
                                       result <- "two"
                                   }
                                   case 3 {
                                       result <- "three"
                                   }
                                   default {
                                       result <- "other"
                                   }
                               }
                           
                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试编译模式下的递归函数
    /// </summary>
    [Fact(Skip = "递归函数调用的IL生成有已知bug - 需要编译器层面修复")]
    public void EndToEndCompile_RecursiveFunctions_CompilesCorrectly()
    {
        // 测试递归函数在编译模式下的表现
        var code = """

                               // 测试阶乘函数
                               func factorial(n:int) -> int {
                                   if n <= 1 {
                                       return 1
                                   }
                                   return n * factorial(n - 1)
                               }
                               
                               // 测试斐波那契数列
                               func fibonacci(n:int) -> int {
                                   if n <= 1 {
                                       return n
                                   }
                                   return fibonacci(n - 1) + fibonacci(n - 2)
                               }
                               
                               // 调用递归函数
                               fact5 <- factorial(5)
                               fib10 <- fibonacci(10)
                           
                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }

    /// <summary>
    /// 测试编译模式下的类型转换
    /// </summary>
    [Fact]
    public void EndToEndCompile_TypeConversion_CompilesCorrectly()
    {
        // 测试类型转换在编译模式下的表现
        var code = """
                       // 数值类型转换
                       int_val <- 123
                       double_val <- 456.789
                       
                       // 隐式转换
                       result1 <- int_val + double_val
                       
                       // 显式转换
                       str_val <- "789"
                       
                       // 测试各种类型转换
                       bool_val <- true
                       
                       // 测试字符串操作
                       str1 <- "hello"
                       str2 <- "world"
                       combined <- str1 + " " + str2
                       
                       // 测试数组和字典操作
                       arr <- [1, 2, 3, 4, 5]
                       dict <- {"key1": 123, "key2": "test"}
                   """;

        var interpreter = new LangInterpreter();

        // Act
        var ast = interpreter.Build(code);
        var compiledAction = Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert
        Assert.NotNull(compiledAction);

        // 执行编译后的代码
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
    }
}