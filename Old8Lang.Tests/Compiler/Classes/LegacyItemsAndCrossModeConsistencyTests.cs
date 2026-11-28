using System.Reflection;
using Old8Lang.Bytecode;
using Old8Lang.Compiler.Verification;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Classes;

/// <summary>
/// 遗留项清理回归测试：
/// 1. IL 类内 <c>this.method()</c> 预登记 <see cref="System.Reflection.Emit.MethodBuilder"/> 恢复直接 <c>callvirt</c>；
/// 2. 类字段集合初值三模式每实例独立；
/// 3. <c>type(枚举值)</c> 与 <c>type()</c> 三模式语言类型名统一。
/// </summary>
[Collection("Sequential")]
public class LegacyItemsAndCrossModeConsistencyTests
{
    [Fact]
    public void IlClassInternalThisMethodCall_EmitsDirectCallvirtWithoutDynamicDispatch()
    {
        const string code = @"
            class CalculatorBase {
                public baseOffset:int <- 10

                public func addBase(v:int) -> int {
                    return v + this.baseOffset
                }
            }

            class DirectDispatchTarget extends CalculatorBase {
                public factor:int <- 2

                public func entry(x:int) -> int {
                    // 前向调用定义在后面的方法 + 重载 + 命名参数 + 父类方法
                    step1 <- this.laterMethod(x)
                    step2 <- this.combine(b: 3, a: step1)
                    return this.addBase(step2)
                }

                private func laterMethod(v:int) -> int {
                    if v <= 0 {
                        return 0
                    }
                    return v * this.factor + this.helperPing(v - 1)
                }

                private func helperPing(v:int) -> int {
                    if v <= 0 {
                        return 1
                    }
                    return this.laterMethod(v)
                }

                public func combine(a:int, b:int) -> int {
                    return a + b
                }

                public func closureCallsThis(x:int) -> int {
                    fn <- (n:int) -> this.combine(n, this.factor)
                    return fn(x)
                }
            }

            obj <- DirectDispatchTarget()
            // laterMethod(2) = 4 + helperPing(1) = 4 + laterMethod(1) = 4 + (2 + helperPing(0)) = 7
            // combine(7, 3) = 10; addBase(10) = 20
            Assert.Equal(20, obj.entry(2))
            Assert.Equal(7, obj.closureCallsThis(5))
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "direct_callvirt_test", interpreter)();

        // 反汇编最近生成的 DirectDispatchTarget.entry，验证已直接 callvirt 且不再回退 InvokeDynamicObjectMethod。
        var targetType = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.IsDynamic)
            .Reverse()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return []; }
            })
            .FirstOrDefault(t => t.Name == "DirectDispatchTarget");

        Assert.NotNull(targetType);
        var entryMethod = targetType!.GetMethod("entry", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(entryMethod);
        var disassembly = IlDisassembler.Disassemble(entryMethod!);
        Assert.Contains("callvirt", disassembly, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("InvokeDynamicObjectMethod", disassembly, StringComparison.Ordinal);
    }

    [Fact]
    public void IlGenericClassInternalThisMethodCall_SupportsForwardCallsAndCallableFields()
    {
        const string code = @"
            class GenericBox<T> {
                public value:T
                public transform:object

                public func init(v:T, fn:object) {
                    this.value <- v
                    this.transform <- fn
                }

                public func compute() -> T {
                    return this.forwardTarget()
                }

                private func forwardTarget() -> T {
                    return this.value
                }

                public func applyTransform() -> object {
                    return this.transform(this.value)
                }
            }

            doubler <- (x:int) -> x * 2
            box <- GenericBox<int>(21, doubler)
            Assert.Equal(21, box.compute())
            Assert.Equal(42, box.applyTransform())
        ";

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ast, "generic_direct_callvirt_test", interpreter)();
    }

    [Fact]
    public void ClassCollectionFieldDefaults_AreIsolatedPerInstance_AcrossAllThreeModes()
    {
        const string code = @"
            class Bag {
                public items <- {1, 2}
                public arr <- [10, 20]
                public meta <- {""a"": 1}
                public nested <- {{100}}
            }

            b1 <- Bag()
            b2 <- Bag()

            b1.items.Add(3)
            b1.arr[0] <- 99
            b1.meta[""b""] <- 2
            b1.nested[0].Add(200)

            Assert.Equal(3, b1.items.Count())
            Assert.Equal(2, b2.items.Count())
            Assert.Equal(99, b1.arr[0])
            Assert.Equal(10, b2.arr[0])
            Assert.Equal(2, b1.meta.Count())
            Assert.Equal(1, b2.meta.Count())
            Assert.Equal(2, b1.nested[0].Count())
            Assert.Equal(1, b2.nested[0].Count())
        ";

        RunInAllThreeModes(code);
    }

    [Fact]
    public void TypeOfAndTypeFunction_OnEnumAndBuiltinValues_AreConsistentAcrossAllThreeModes()
    {
        const string code = @"
            enum Color {
                Red <- 0,
                Green <- 1,
                Blue <- 2
            }

            c <- Color.Green
            Assert.Equal(""Int"", type(Color.Red).ToStr())
            Assert.Equal(""Int"", type(c).ToStr())
            Assert.Equal(""Int"", TypeOf(Color.Blue).ToStr())
            Assert.Equal(""Int"", type(42).ToStr())
            Assert.Equal(""Double"", type(3.14).ToStr())
            Assert.Equal(""String"", type(""hello"").ToStr())
            Assert.Equal(""Bool"", type(true).ToStr())
            Assert.Equal(""List"", type({1, 2}).ToStr())
            Assert.Equal(""Array"", type([1, 2]).ToStr())
            Assert.Equal(""Dictionary"", type({""k"": 1}).ToStr())
        ";

        RunInAllThreeModes(code);
    }

    private static void RunInAllThreeModes(string code)
    {
        // 1. 解释器
        var interpreter = new LangInterpreter();
        var interpAst = interpreter.Build(code);
        interpAst.Run(interpreter.Manager);

        // 2. IL 模式
        var ilInterpreter = new LangInterpreter();
        var ilAst = ilInterpreter.Build(code);
        Old8Lang.Compiler.Compiler.Compile(ilAst, "cross_mode_legacy_test", ilInterpreter)();

        // 3. VM 模式
        var vmInterpreter = new LangInterpreter();
        var vmAst = vmInterpreter.Build(code);
        var bytecodeFile = new BytecodeCompiler().Compile(vmAst);
        var vm = new Old8Lang.Bytecode.VM.VirtualMachine(bytecodeFile);
        vm.Execute();
    }
}
