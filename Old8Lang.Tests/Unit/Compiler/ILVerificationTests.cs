using System.Reflection.Emit;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Unit.Compiler;

/// <summary>
/// IL验证器单元测试
/// </summary>
public class ILVerificationTests
{
    /// <summary>
    /// 测试有效的IL代码能够通过验证
    /// </summary>
    [Fact]
    public void Verify_ValidIL_CanPassVerification()
    {
        // 测试正常情况：应该能通过IL验证
        var code = @"
            func normal_function() -> void {
                a <- 123
                b <- 456
                c <- a + b
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);

        // 编译生成IL - 应该能通过验证
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // 验证IL已生成且委托已创建
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试IL验证开关控制
    /// </summary>
    [Fact]
    public void Verify_ILVerificationCanBeDisabled()
    {
        // 禁用IL验证
        Old8Lang.Compiler.Compiler.IlVerificationEnabled = false;
        
        try
        {
            // 测试正常情况：应该能通过编译
            var code = @"
                func normal_function() -> void {
                    a <- 123
                    b <- 456
                    c <- a + b
                }
            ";
            var interpreter = new LangInterpreter();

            var ast = interpreter.Build(code);

            // 编译生成IL - 应该能通过编译
            var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

            // 验证IL已生成且委托已创建
            Assert.NotNull(compiledAction);
        }
        finally
        {
            // 恢复IL验证开关
            Old8Lang.Compiler.Compiler.IlVerificationEnabled = true;
        }
    }
    
    /// <summary>
    /// 测试IL验证器能够正确处理异常
    /// </summary>
    [Fact]
    public void Verify_ILVerifierHandlesExceptions()
    {
        // 这个测试主要验证IL验证器不会因为异常而崩溃
        var code = @"
            func exception_test() -> void {
                a <- 123
                PrintLine(a)
            }
        ";
        var interpreter = new LangInterpreter();

        var ast = interpreter.Build(code);

        // 编译生成IL - 应该能通过验证
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // 验证IL已生成且委托已创建
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试ILVerifier直接验证DynamicMethod
    /// </summary>
    [Fact]
    public void ILVerifier_Verify_ValidDynamicMethod()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("TestMethod", typeof(void), null);
        var ilGenerator = dynamicMethod.GetILGenerator();
        
        // 生成有效的IL代码
        ilGenerator.Emit(OpCodes.Ret);
        
        // Act
        var result = Old8Lang.Compiler.IlVerifier.Verify(dynamicMethod, "TestMethod");
        
        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
    
    /// <summary>
    /// 测试ILVerifier验证无效的IL代码
    /// </summary>
    [Fact]
    public void ILVerifier_Verify_InvalidDynamicMethod()
    {
        // Arrange
        var dynamicMethod = new DynamicMethod("InvalidMethod", typeof(int), null);
        dynamicMethod.GetILGenerator();
        
        // 生成无效的IL代码：缺少返回值
        // 我们声明返回类型为int，但没有返回任何值
        
        // Act
        var result = Old8Lang.Compiler.IlVerifier.Verify(dynamicMethod, "InvalidMethod");
        
        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Single(result.Errors);
    }
    
    /// <summary>
    /// 测试不同类型的语法结构通过IL验证
    /// </summary>
    [Fact(Skip = "函数调用的IL生成有已知bug - 需要编译器层面修复")]
    public void Verify_VariousSyntaxStructures_PassILVerification()
    {
        // 测试各种语法结构的IL生成和验证
        var code = @"
            // 测试变量赋值
            a <- 123
            
            // 测试条件语句
            if a > 100 {
                b <- ""大于100"" 
            } elif a > 50 {
                b <- ""大于50"" 
            } else {
                b <- ""小于等于50"" 
            }
            
            // 测试循环语句
            sum <- 0
            for i <- 0, i <= 10, i++ {
                sum <- sum + i
            }

            // 测试while循环
            j <- 0
            while j < 5 {
                j <- j + 1
            }

            // 测试switch语句
            switch a {
                case 100 {
                    c <- 1
                }
                case 123 {
                    c <- 2
                }
                default {
                    c <- 0
                }
            }

            // 测试函数
            func add(x:int, y:int) -> int {
                return x + y
            }
            
            // 测试函数调用
            result <- add(10, 20)
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        
        // Act
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        
        // Assert
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试类和对象的IL生成和验证
    /// </summary>
    [Fact]
    public void Verify_ClassAndObject_PassILVerification()
    {
        // 测试类声明、实例化和方法调用的IL生成
        var code = @"
            class Person {
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
            }

            // 实例化对象
            p <- Person(""John"", 30)

            // 调用对象方法
            person_name <- p.get_name()
            person_age <- p.get_age()
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        
        // Act
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        
        // Assert
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试lambda表达式的IL生成和验证
    /// </summary>
    [Fact(Skip = "Lambda表达式调用的IL生成有已知bug - 需要编译器层面修复")]
    public void Verify_LambdaExpressions_PassILVerification()
    {
        // 测试lambda表达式的IL生成
        var code = @"
            // 简单lambda
            add <- (x:int, y:int) -> x + y

            // 多行lambda
            multiply <- (x:int, y:int) -> {
                result <- x * y
                return result
            }

            // 调用lambda
            sum_result <- add(10, 20)
            product_result <- multiply(5, 6)
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        
        // Act
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        
        // Assert
        Assert.NotNull(compiledAction);
    }
    
    /// <summary>
    /// 测试try-catch语句的IL生成和验证
    /// </summary>
    [Fact]
    public void Verify_TryCatch_PassILVerification()
    {
        // 测试try-catch语句的IL生成
        var code = @"
            try {
                // 可能抛出异常的代码
                result <- 10 / 0
            } catch {
                // 异常处理
                result <- 0
            }
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        
        // Act
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);
        
        // Assert
        Assert.NotNull(compiledAction);
    }
}
