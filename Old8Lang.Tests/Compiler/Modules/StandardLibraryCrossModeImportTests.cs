using Old8Lang.Bytecode;
using Old8Lang.Interpreter;

namespace Old8Lang.Tests.Compiler.Modules;

/// <summary>标准库 import 在解释 / IL / VM 三种模式下的行为一致性回归。</summary>
public class StandardLibraryCrossModeImportTests
{
    private static void RunIl(string code)
    {
        var interpreter = new LangInterpreter();
        Old8Lang.Compiler.Compiler.Compile(interpreter.Build(code), "test", interpreter)();
    }

    private static void RunVm(string code, bool roundTrip = true)
    {
        var bytecode = new BytecodeCompiler().Compile(new LangInterpreter().Build(code));
        if (roundTrip)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
                bytecode.WriteTo(writer);
            stream.Position = 0;
            using var reader = new BinaryReader(stream);
            bytecode = BytecodeFile.ReadFrom(reader);
        }

        new Old8Lang.Bytecode.VM.VirtualMachine(bytecode).Execute();
    }

    private static void RunInterpreter(string code)
    {
        var interpreter = new LangInterpreter();
        interpreter.Build(code).Run(interpreter.Manager);
    }

    private static void RunAll(string code)
    {
        RunInterpreter(code);
        RunIl(code);
        RunVm(code);
    }

    [Fact]
    public void MathModule_NoArgCall_AndLongResult()
    {
        RunAll(@"
import ""Math""
Assert.True(Math.GetPi() > 3.14)
Assert.Equal(120.0, Math.Factorial(5))
");
    }

    [Fact]
    public void OtherModules_Crypto_Csv_Time_Work()
    {
        RunAll(@"
import ""Crypto""
import ""Csv"" as csv
import ""Time""
Assert.Equal(""hello"", Crypto.XorDecrypt(Crypto.XorEncrypt(""hello"", ""k""), ""k""))
parts <- csv.ParseCsvLine(""a,b,c"")
Assert.Equal(3, parts.Length)
Assert.True(Time.GetUnixTimeSeconds() > 0)
");
    }

    [Fact]
    public void ModuleCalls_InsideFunctions_WithAlias()
    {
        RunAll(@"
import ""Crypto"" as c
func roundTrip(s: string) -> string { return c.XorDecrypt(c.XorEncrypt(s, ""key""), ""key"") }
Assert.Equal(""abc"", roundTrip(""abc""))
");
    }

    [Fact]
    public void NamedArguments_OnModuleFunctions()
    {
        RunAll(@"
import ""Math""
Assert.Equal(8.0, Math.Pow(baseValue: 2, exponent: 3))
Assert.Equal(9.0, Math.Pow(exponent: 2, baseValue: 3))
");
    }

    [Fact]
    public void SelectiveImport_Works()
    {
        RunAll(@"
import { Sqrt, Pow as power } from ""Math""
Assert.Equal(3.0, Sqrt(9))
Assert.Equal(8.0, power(2, 3))
");
    }

    [Fact]
    public void SelectiveImport_InsideFunction_Works()
    {
        RunAll(@"
import { Sqrt } from ""Math""
func root(x: int) -> double { return Sqrt(x) }
Assert.Equal(4.0, root(16))
");
    }

    [Fact]
    public void ClassExports_ModuleQualified_NativeClass()
    {
        RunAll(@"
import ""Vector""
a <- Vector.Vector2(3.0, 4.0)
b <- Vector.Vector2(1.0, 2.0)
Assert.Equal(3.0, a.X)
Assert.Equal(2.0, b.Y)
Assert.Equal(4.0, a.Y)
");
    }

    [Fact]
    public void ClassExports_FromImport_NativeClassAndStaticMember()
    {
        RunAll(@"
import { Vector2 } from ""Vector""
a <- Vector2(3.0, 4.0)
z <- Vector2.Zero
Assert.Equal(3.0, a.X)
Assert.Equal(0.0, z.Y)
");
    }

    [Fact]
    public void ClassExports_NoArgConstructor_FromNetModule()
    {
        RunAll(@"
import ""Net""
client <- Net.HttpWebClient()
client.SetTimeout(5000)
client.Dispose()
");
    }
}
