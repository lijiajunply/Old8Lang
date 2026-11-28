using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.Compiler;

/// <summary>跨模式与 IL 缺陷修复回归测试。</summary>
public class CrossModeDefectsRegressionTests
{
    private static void RunAllThreeModes(string code)
    {
        var interp = new LangInterpreter();
        interp.Build(code).Run(interp.Manager);

        var ilInterp = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(ilInterp.Build(code), "test", ilInterp)();

        var bytecode = new BytecodeCompiler().Compile(new LangInterpreter().Build(code));
        using var ms = new MemoryStream();
        using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, leaveOpen: true))
            bytecode.WriteTo(writer);
        ms.Position = 0;
        using var reader = new BinaryReader(ms);
        var reloaded = BytecodeFile.ReadFrom(reader);
        new VM(reloaded).Execute();
    }

    [Fact]
    public void SteppedAndCharacterRanges_WorkAcrossModesAndBytecodeSerialization()
    {
        RunAllThreeModes("""
            values <- [1~10:2]
            Assert.Equal(5, values.Length)
            Assert.Equal(9, values[4])
            Assert.Equal(0, [5>~5:2].Length)
            Assert.Equal(2147483647, [2147483647~2147483647][0])
            Assert.Equal(2, [4>~<1].Length)

            first <- 'F'
            last <- 'A'
            chars <- [first>~last:2]
            Assert.Equal(2, chars.Length)
            Assert.Equal('D', chars[0])
            Assert.Equal('B', chars[1])
            """);
    }

    [Theory]
    [InlineData("[1~10:0]", "范围步长必须大于 0")]
    [InlineData("[1~10:-2]", "范围步长必须大于 0")]
    [InlineData("[1~10:1.5]", "范围步长必须是整数")]
    [InlineData("[1~'Z']", "范围端点必须同为整数或同为字符")]
    public void RangesRejectInvalidStepsAndMixedEndpoints(string expression, string message)
    {
        var code = $"values <- {expression}";
        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Assert.Contains(message, Assert.ThrowsAny<Old8Exception>(() => ast.Run(interpreter.Manager)).Message);

        var ilInterpreter = new LangInterpreter();
        Assert.Contains(message, Assert.ThrowsAny<Old8Exception>(() =>
            Old8Lang.Compiler.Compiler.Compile(ilInterpreter.Build(code), "test", ilInterpreter)()).Message);

        var bytecode = new BytecodeCompiler().Compile(new LangInterpreter().Build(code));
        Assert.Contains(message, Assert.ThrowsAny<Old8Exception>(() => new VM(bytecode).Execute()).Message);
    }

    [Fact]
    public void SteppedRange_AwaitsStepInIlMode()
    {
        const string code = """
            async func getStep() -> int {
                await Task.Delay(1)
                return 2
            }

            async func checkRange() -> int {
                values <- [1~10:await getStep()]
                Assert.Equal(5, values.Length)
                return values[4]
            }

            result <- checkRange()
            result.Wait()
            Assert.Equal(9, result.Result)
            """;
        var interpreter = new LangInterpreter();

        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    [Fact]
    public void MatchTupleDestructuring_ThreeAndFourElements_WorksAcrossAllThreeModes()
    {
        const string code = @"
            r3 <- match (1, 2, 3) {
                case (x, y, z) -> x + y * 10 + z * 100
                case _ -> -1
            }
            Assert.Equal(321, r3)

            r4 <- match (10, 20, 30, 40) {
                case (10, a, _, b) -> a + b
                case _ -> -1
            }
            Assert.Equal(60, r4)
        ";
        RunAllThreeModes(code);
    }

    [Fact]
    public void GenericClass_CallableFunctionField_WorksAcrossAllThreeModes()
    {
        const string code = @"
            class ValidatedBox<T> {
                private value: T
                public validator: object

                func init(v: T, fn: object) -> void {
                    this.value <- v
                    this.validator <- fn
                }

                func isValid() -> bool {
                    return this.validator(this.value)
                }

                func check(x: T) -> bool {
                    return this.validator(x)
                }
            }

            box <- ValidatedBox<int>(10, (n: int) -> n > 0)
            Assert.True(box.isValid())
            Assert.True(box.check(5))
            Assert.True(!box.check(-3))
            Assert.True(box.validator(42))
        ";
        RunAllThreeModes(code);
    }

    [Fact]
    public void PrivateStaticField_AccessibleInsideStaticMethod_WorksAcrossAllThreeModes()
    {
        const string code = @"
            class Counter {
                private static count <- 0

                public static func inc() -> int {
                    Counter.count <- Counter.count + 1
                    return Counter.count
                }
            }

            Assert.Equal(1, Counter.inc())
            Assert.Equal(2, Counter.inc())
        ";
        RunAllThreeModes(code);
    }

    [Fact]
    public void CollectionTypeAnnotation_InvalidElementsRejectedAcrossAllThreeModes()
    {
        var invalidSnippets = new[]
        {
            @"items: list<int> <- {1, ""hello""}",
            @"nested: list<list<int>> <- {{1, 2}, {3, ""bad""}}",
            @"mapping: dict<string, int> <- {""a"": 1, ""b"": ""bad""}"
        };

        foreach (var code in invalidSnippets)
        {
            // Interpreter rejects with TypeError
            var interp = new LangInterpreter();
            Assert.Throws<TypeError>(() => interp.Build(code).Run(interp.Manager));

            // IL rejects with CompilerException / TypeError
            var ilInterp = new LangInterpreter();
            Assert.ThrowsAny<Exception>(() =>
                Old8Lang.Compiler.Compiler.Compile(ilInterp.Build(code), "test", ilInterp)());

            // VM rejects at runtime
            var vm = new VM(new BytecodeCompiler().Compile(new LangInterpreter().Build(code)));
            Assert.ThrowsAny<Exception>(() => vm.Execute());
        }
    }
}
