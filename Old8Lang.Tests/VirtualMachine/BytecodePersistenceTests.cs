using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.VirtualMachine;

/// <summary>
/// 字节码文件往返兼容性测试：编译产物写入磁盘后重新读入并执行，行为保持不变。
/// </summary>
[Collection("Sequential")]
public class BytecodePersistenceTests
{
    [Fact]
    public void BytecodeFile_RoundTripsThroughDiskAndExecutes()
    {
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build("result <- 19 + 23\nPrintLine(result.ToStr())");
        var compiler = new BytecodeCompiler();
        var original = compiler.Compile(ast);
        var path = Path.Combine(Path.GetTempPath(), $"old8lang-{Guid.NewGuid():N}.o8c");

        try
        {
            original.SaveToFile(path);
            Assert.True(new FileInfo(path).Length > 0);

            var loaded = BytecodeFile.LoadFromFile(path);

            Assert.Equal(original.Functions.Count, loaded.Functions.Count);
            Assert.Equal(original.EntryPointIndex, loaded.EntryPointIndex);
            Assert.Equal(original.GlobalVariables.Count, loaded.GlobalVariables.Count);

            var originalOut = Console.Out;
            using var stringWriter = new StringWriter();
            Console.SetOut(stringWriter);
            try
            {
                new Bytecode.VM.VirtualMachine(loaded).Execute();
            }
            finally
            {
                Console.SetOut(originalOut);
            }

            Assert.Equal("42", stringWriter.ToString().Trim());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
