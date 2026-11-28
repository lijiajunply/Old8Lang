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
