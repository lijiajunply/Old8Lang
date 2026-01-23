using Old8Lang.Bytecode;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.VirtualMachine.Modules;

/// <summary>
/// 虚拟机模块导入测试
/// </summary>
public class VMImportTests
{
    private string ExecuteVMCode(string code, string? baseDirectory = null)
    {
        try
        {
            var interpreter = new LangInterpreter();
            var ast = interpreter.Build(code);

            var compiler = new BytecodeCompiler();
            var bytecodeFile = compiler.Compile(ast);

            var originalOut = Console.Out;
            var originalErr = Console.Error;
            using var stringWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(stringWriter);
            Console.SetError(errorWriter);

            try
            {
                var vm = new VM(bytecodeFile, baseDirectory);
                vm.Execute();
                var errorOutput = errorWriter.ToString();
                var stdOutput = stringWriter.ToString().Trim();

                // 恢复控制台输出以便调试
                Console.SetOut(originalOut);
                Console.SetError(originalErr);

                if (!string.IsNullOrEmpty(errorOutput))
                {
                    throw new System.Exception($"VM执行错误: {errorOutput}");
                }
                return stdOutput;
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
            }
        }
        catch (System.Exception ex)
        {
            throw new System.Exception($"执行失败: {ex.Message}", ex);
        }
    }

    [Fact]
    public void TestSimpleImport_ExecutesCorrectly()
    {
        // 创建临时目录和模块文件
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建模块文件
            var moduleCode = @"
func add(a:int, b:int) -> int {
    return a + b
}
";
            File.WriteAllText(Path.Combine(tempDir, "test_module.old8"), moduleCode);

            // 创建主程序
            var mainCode = @"
import { add } from ""test_module""

func main() -> void {
    result <- add(10, 20)
    PrintLine(result.ToStr())
}

main()
";

            var output = ExecuteVMCode(mainCode, tempDir);
            Assert.Equal("30", output);
        }
        finally
        {
            // 清理临时目录
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void TestAliasImport_ExecutesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建模块文件
            var moduleCode = @"
func multiply(a:int, b:int) -> int {
    return a * b
}
";
            File.WriteAllText(Path.Combine(tempDir, "math_module.old8"), moduleCode);

            // 创建主程序 - 使用别名
            var mainCode = @"
import { multiply as mul } from ""math_module""

func main() -> void {
    result <- mul(5, 6)
    PrintLine(result.ToStr())
}

main()
";

            var output = ExecuteVMCode(mainCode, tempDir);
            Assert.Equal("30", output);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void TestMultipleImports_ExecutesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建模块文件
            var moduleCode = @"
func add(a:int, b:int) -> int {
    return a + b
}

func subtract(a:int, b:int) -> int {
    return a - b
}
";
            File.WriteAllText(Path.Combine(tempDir, "calc_module.old8"), moduleCode);

            // 创建主程序 - 导入多个函数
            var mainCode = @"
import { add, subtract } from ""calc_module""

func main() -> void {
    result1 <- add(100, 50)
    result2 <- subtract(100, 50)
    PrintLine(result1.ToStr())
    PrintLine(result2.ToStr())
}

main()
";

            var output = ExecuteVMCode(mainCode, tempDir);
            var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, lines.Length);
            Assert.Equal("150", lines[0]);
            Assert.Equal("50", lines[1]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void TestModuleWithClass_ExecutesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建包含类的模块
            var moduleCode = @"
class Point {
    public x:int
    public y:int

    func init(x:int, y:int) {
        this.x <- x
        this.y <- y
    }

    func distance() -> int {
        return this.x * this.x + this.y * this.y
    }
}
";
            File.WriteAllText(Path.Combine(tempDir, "geometry_module.old8"), moduleCode);

            // 创建主程序 - 导入类
            var mainCode = @"
import { Point } from ""geometry_module""

func main() -> void {
    p <- Point(3, 4)
    dist <- p.distance()
    PrintLine(dist.ToStr())
}

main()
";

            var output = ExecuteVMCode(mainCode, tempDir);
            Assert.Equal("25", output);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public void TestNestedModuleDependency_ExecutesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            // 创建基础模块
            var baseModuleCode = @"
func twice(x:int) -> int {
    return x * 2
}
";
            File.WriteAllText(Path.Combine(tempDir, "base_module.old8"), baseModuleCode);

            // 创建依赖基础模块的模块
            var middleModuleCode = @"
import { twice } from ""base_module""

func quadruple(x:int) -> int {
    return twice(twice(x))
}
";
            File.WriteAllText(Path.Combine(tempDir, "middle_module.old8"), middleModuleCode);

            // 创建主程序
            var mainCode = @"
import { quadruple } from ""middle_module""

func main() -> void {
    result <- quadruple(5)
    PrintLine(result.ToStr())
}

main()
";

            var output = ExecuteVMCode(mainCode, tempDir);
            Assert.Equal("20", output);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}
