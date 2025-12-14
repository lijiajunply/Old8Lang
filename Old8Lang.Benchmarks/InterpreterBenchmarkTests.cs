using BenchmarkDotNet.Attributes;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;

namespace Old8Lang.Benchmarks;

/// <summary>
/// 解释器性能基准测试
/// 测量解释器执行时间、内存占用和性能优化效果
/// </summary>
public class InterpreterBenchmarkTests
{
    // 测试用代码片段
    private string LoopIntensiveCodeString = "";
    private string FunctionCallIntensiveCodeString = "";
    private string MixedIntensiveCodeString = "";

    /// <summary>
    /// 初始化测试数据
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        // 循环密集型代码：测试循环执行机制优化效果
        LoopIntensiveCodeString = """
                                  func loop_test() {
                                      sum <- 0
                                      for i <- 0, i < 10000, i <- i + 1 {
                                          sum <- sum + i * i
                                          if sum > 100000 {
                                              // 测试continue
                                              continue
                                          }
                                          if sum > 500000 {
                                              // 测试break
                                              break
                                          }
                                      }
                                      return sum
                                  }
                                  result <- loop_test()

                                  """;

        // 函数调用密集型代码：测试函数调用机制优化效果
        FunctionCallIntensiveCodeString = """
                                          func add(a, b) {
                                              return a + b
                                          }

                                          func multiply(a, b) {
                                              return a * b
                                          }

                                          func calculate(a, b) {
                                              temp <- add(a, b)
                                              return multiply(temp, temp)
                                          }

                                          func function_call_test() {
                                              sum <- 0
                                              for i <- 0, i < 1000, i <- i + 1 {
                                                  sum <- sum + calculate(i, i + 1)
                                              }
                                              return sum
                                          }
                                          result <- function_call_test()

                                          """;

        // 混合密集型代码：测试综合优化效果
        MixedIntensiveCodeString = """
                                   func helper(n) {
                                       if n <= 1 {
                                           return 1
                                       }
                                       return n + helper(n - 1)
                                   }

                                   func mixed_test() {
                                       total <- 0
                                       for outer <- 0, outer < 100, outer <- outer + 1 {
                                           sum <- 0
                                           for inner <- 0, inner < 100, inner <- inner + 1 {
                                               sum <- sum + helper(inner % 10)
                                           }
                                           total <- total + sum
                                       }
                                       return total
                                   }
                                   result <- mixed_test()

                                   """;
    }

    /// <summary>
    /// 测试循环密集型代码的执行性能
    /// 主要测试循环执行机制优化效果
    /// </summary>
    [Benchmark(Description = "Loop Intensive Code")]
    public void LoopIntensiveCode()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(LoopIntensiveCodeString);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试函数调用密集型代码的执行性能
    /// 主要测试函数调用机制优化效果
    /// </summary>
    [Benchmark(Description = "Function Call Intensive Code")]
    public void FunctionCallIntensiveCode()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(FunctionCallIntensiveCodeString);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试混合密集型代码的执行性能
    /// 测试综合优化效果
    /// </summary>
    [Benchmark(Description = "Mixed Intensive Code")]
    public void MixedIntensiveCode()
    {
        var interpreter = new LangInterpreter();
        BlockStatement ast = interpreter.Build(MixedIntensiveCodeString);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试多次执行同一代码的性能
    /// </summary>
    [Benchmark(Description = "Multiple Executions")]
    public void MultipleExecutions()
    {
        for (int i = 0; i < 10; i++)
        {
            var interpreter = new LangInterpreter();
            BlockStatement ast = interpreter.Build(LoopIntensiveCodeString);
            ast.Run(interpreter.Manager);
        }
    }

    /// <summary>
    /// 测试深度递归函数调用性能
    /// 主要测试栈帧复用和闭包优化效果
    /// </summary>
    [Benchmark(Description = "Deep Recursion")]
    public void DeepRecursion()
    {
        var interpreter = new LangInterpreter();
        var recursionCode = """
                            func deep_recursion(n) {
                                if n <= 0 {
                                    return 0
                                }
                                return deep_recursion(n - 1) + 1
                            }
                            result <- deep_recursion(500)

                            """;
        BlockStatement ast = interpreter.Build(recursionCode);
        ast.Run(interpreter.Manager);
    }

    /// <summary>
    /// 测试While循环性能
    /// 主要测试while循环执行机制优化效果
    /// </summary>
    [Benchmark(Description = "While Loop Performance")]
    public void WhileLoopPerformance()
    {
        var interpreter = new LangInterpreter();
        var whileCode = """
                        func while_test() {
                            count <- 0
                            sum <- 0
                            while count < 10000 {
                                sum <- sum + count
                                count <- count + 1
                            }
                            return sum
                        }
                        result <- while_test()

                        """;
        BlockStatement ast = interpreter.Build(whileCode);
        ast.Run(interpreter.Manager);
    }
}