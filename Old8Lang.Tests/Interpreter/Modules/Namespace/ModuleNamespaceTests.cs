using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Namespace;

/// <summary>
/// 模块命名空间测试 - 覆盖命名空间隔离、跨模块符号访问与符号冲突处理
/// </summary>
[Collection("Sequential")]
public class ModuleNamespaceTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void ModuleNamespace_SameSymbolNamesInDifferentModules_RemainIsolated()
    {
        var alphaModule = @"
VERSION <- ""1.0-alpha""

func compute(x:int) -> int {
    return x + 10
}
";
        var betaModule = @"
VERSION <- ""2.0-beta""

func compute(x:int) -> int {
    return x * 100
}
";
        var mainCode = @"
import ""ns_iso_alpha"" as alpha
import ""ns_iso_beta"" as beta

alphaVer <- alpha.VERSION
betaVer <- beta.VERSION
alphaRes <- alpha.compute(5)
betaRes <- beta.compute(5)
";

        CreateTempModuleFile("ns_iso_alpha.old8", alphaModule);
        CreateTempModuleFile("ns_iso_beta.old8", betaModule);
        CreateTempModuleFile("ns_iso_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("ns_iso_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "alphaVer", "1.0-alpha");
        AssertVariableValue(interpreter, "betaVer", "2.0-beta");
        AssertVariableValue(interpreter, "alphaRes", 15);
        AssertVariableValue(interpreter, "betaRes", 500);
    }

    [Fact]
    public void ModuleNamespace_CrossModuleSymbolAccess_AccessesFunctionsConstantsAndClasses()
    {
        var mathBase = @"
BASE_FACTOR <- 3

class Multiplier {
    public factor:int <- 1
    func init(f:int) {
        this.factor <- f
    }
    func apply(v:int) -> int {
        return v * this.factor
    }
}

func triple(x:int) -> int {
    return x * BASE_FACTOR
}
";
        var serviceModule = @"
import { triple, BASE_FACTOR, Multiplier } from ""ns_cross_base""

func computeWithService(v:int) -> int {
    m <- Multiplier(BASE_FACTOR + 1)
    return triple(v) + m.apply(v)
}
";
        var mainCode = @"
import ""ns_cross_service"" as svc

result <- svc.computeWithService(5)
";

        CreateTempModuleFile("ns_cross_base.old8", mathBase);
        CreateTempModuleFile("ns_cross_service.old8", serviceModule);
        CreateTempModuleFile("ns_cross_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("ns_cross_main.old8");

        Assert.Null(exception);
        // triple(5) = 15, Multiplier(4).apply(5) = 20 => 35
        AssertVariableValue(interpreter, "result", 35);
    }

    [Fact]
    public void ModuleNamespace_SymbolConflictResolvedByAliases_BothSymbolsCallable()
    {
        var modA = @"
func format(s:string) -> string {
    return ""[A]"" + s
}
TAG <- ""A""
";
        var modB = @"
func format(s:string) -> string {
    return ""[B]"" + s
}
TAG <- ""B""
";
        var mainCode = @"
import { format as formatA, TAG as tagA } from ""ns_conflict_a""
import { format as formatB, TAG as tagB } from ""ns_conflict_b""

resA <- formatA(""hi"")
resB <- formatB(""hi"")
combinedTag <- tagA + ""-"" + tagB
";

        CreateTempModuleFile("ns_conflict_a.old8", modA);
        CreateTempModuleFile("ns_conflict_b.old8", modB);
        CreateTempModuleFile("ns_conflict_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("ns_conflict_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "resA", "[A]hi");
        AssertVariableValue(interpreter, "resB", "[B]hi");
        AssertVariableValue(interpreter, "combinedTag", "A-B");
    }

    [Fact]
    public void ModuleNamespace_DirectImportConflict_LaterHoistedSelectiveImportWinsInScope()
    {
        var modA = @"
func greet() -> string {
    return ""from A""
}
";
        var modB = @"
func greet() -> string {
    return ""from B""
}
";
        // BlockStatement 会先执行全部 import 语句，后导入的同名符号覆盖先前导入
        var mainCode = @"
import { greet } from ""ns_override_a""
import { greet } from ""ns_override_b""
winner <- greet()
";

        CreateTempModuleFile("ns_override_a.old8", modA);
        CreateTempModuleFile("ns_override_b.old8", modB);
        CreateTempModuleFile("ns_override_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("ns_override_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "winner", "from B");
    }

    [Fact]
    public void ModuleNamespace_ModuleAndLocalVariableSameName_PreservesModuleScopedClosure()
    {
        var modCode = @"
secret <- ""module-secret""

func reveal() -> string {
    return secret
}
";
        var mainCode = @"
secret <- ""local-secret""
import { reveal } from ""ns_closure_mod""

fromModule <- reveal()
fromLocal <- secret
";

        CreateTempModuleFile("ns_closure_mod.old8", modCode);
        CreateTempModuleFile("ns_closure_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("ns_closure_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "fromModule", "module-secret");
        AssertVariableValue(interpreter, "fromLocal", "local-secret");
    }
}
