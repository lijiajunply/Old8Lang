using Old8Lang.AST.Expression;
using Old8Lang.Error;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Collections;

/// <summary>
/// 同步迭代器测试 - 验证数组和生成器的迭代功能
/// </summary>
[Collection("Sequential")]
public class SyncIteratorTests
{
    [Fact]
    public void SyncIterator_ArrayIteration_WorksCorrectly()
    {
        // 测试数组的 for...in 迭代
        var code = @"
            arr <- [1, 2, 3, 4, 5]
            sum <- 0
            count <- 0
            
            for item in arr {
                sum <- sum + item
                count <- count + 1
            }
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
        
        var sum = (IntLangValue)interpreter.Manager.GetValue(new LangId("sum"))!;
        var count = (IntLangValue)interpreter.Manager.GetValue(new LangId("count"))!;
        
        Assert.Equal(15, sum.Value); // 1+2+3+4+5=15
        Assert.Equal(5, count.Value);
    }

    [Fact]
    public void SyncIterator_GeneratorIteration_WorksCorrectly()
    {
        // 测试生成器的 for...in 迭代
        var code = @"
            func generator() {
                yield 10
                yield 20
                yield 30
            }
            
            sum <- 0
            count <- 0
            
            for item in generator() {
                sum <- sum + item
                count <- count + 1
            }
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // IL 模式不支持同步生成器（yield）：按 MODE_SUPPORT 的已知限制，
        // 编译期应给出明确的 CompilerException，而不是生成无效 IL。
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("同步生成器", exception.Message);
    }

    [Fact]
    public void SyncIterator_EmptyCollectionIteration_WorksCorrectly()
    {
        // 测试空集合迭代
        var code = @"
            empty_arr <- []
            count <- 0
            
            for item in empty_arr {
                count <- count + 1
            }
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
        
        Assert.Equal(0, ((IntLangValue)interpreter.Manager.GetValue(new LangId("count"))!).Value);
    }

    [Fact]
    public void SyncIterator_GeneratorCreation_WorksCorrectly()
    {
        // 测试生成器创建
        var code = @"
            func generator() {
                yield 1
                yield 2
                yield 3
            }
            
            gen <- generator()
            
            // 检查生成器是否创建成功
            is_gen <- gen != null
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);

        // 同步生成器在 IL 模式不支持：编译期给出明确错误（见 MODE_SUPPORT 已知限制）。
        var exception = Assert.Throws<CompilerException>(() =>
            Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter));
        Assert.Contains("同步生成器", exception.Message);
    }

    [Fact]
    public void SyncIterator_ArrayElementIteration_WorksCorrectly()
    {
        // 测试数组元素的正确迭代顺序
        var code = @"
            arr <- [5, 3, 8, 1, 4]
            min <- arr[0]
            max <- arr[0]
            
            for item in arr {
                if item < min {
                    min <- item
                }
                if item > max {
                    max <- item
                }
            }
        ";
        
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        var compiledAction = Old8Lang.Compiler.Compiler.Compile(ast, "test", interpreter);

        // Assert - 验证编译成功
        Assert.NotNull(compiledAction);

        // 执行编译后的代码 - 验证执行不抛出异常
        var exception = Record.Exception(() => compiledAction());
        Assert.Null(exception);
        
        var min = (IntLangValue)interpreter.Manager.GetValue(new LangId("min"))!;
        var max = (IntLangValue)interpreter.Manager.GetValue(new LangId("max"))!;
        
        Assert.Equal(1, min.Value);
        Assert.Equal(8, max.Value);
    }
}