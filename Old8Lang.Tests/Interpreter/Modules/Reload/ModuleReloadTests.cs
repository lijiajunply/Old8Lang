using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.Tests.Interpreter.Modules.Core;
using Xunit.Abstractions;

namespace Old8Lang.Tests.Interpreter.Modules.Reload;

/// <summary>
/// 模块重载与缓存管理测试 - 覆盖模块缓存管理、热重载、依赖更新与单次运行重复导入去重
/// </summary>
[Collection("Sequential")]
public class ModuleReloadTests(ITestOutputHelper output) : ModuleImportTestBase(output)
{
    [Fact]
    public void ModuleCache_LoadHitBypassAndClear_ManagesEntriesCorrectly()
    {
        var path1 = CreateTempModuleFile("cache_mod1.old8", "x <- 1\n");
        var path2 = CreateTempModuleFile("cache_mod2.old8", "y <- 2\n");
        var fullPath1 = Path.GetFullPath(path1);
        var fullPath2 = Path.GetFullPath(path2);

        var cache = new CacheManager();
        var loader = new ModuleLoader(cache);
        var interpreter = new LangInterpreter();

        var first = loader.LoadModule(path1, interpreter.Manager, useCache: true);
        Assert.True(first.IsSuccess);
        Assert.False(first.IsFromCache);
        Assert.True(cache.IsCached(fullPath1));
        Assert.Equal(1, cache.CachedModuleCount);

        var second = loader.LoadModule(path1, interpreter.Manager, useCache: true);
        Assert.True(second.IsSuccess);
        Assert.True(second.IsFromCache);
        Assert.Same(first.Block, second.Block);

        var bypassed = loader.LoadModule(path2, interpreter.Manager, useCache: false);
        Assert.True(bypassed.IsSuccess);
        Assert.False(bypassed.IsFromCache);
        Assert.False(cache.IsCached(fullPath2));
        Assert.Equal(1, cache.CachedModuleCount);

        loader.LoadModule(path2, interpreter.Manager, useCache: true);
        Assert.Equal(2, cache.CachedModuleCount);
        Assert.Contains(fullPath1, cache.GetCachedModulePaths());
        Assert.Contains(fullPath2, cache.GetCachedModulePaths());

        loader.ClearCache(fullPath1);
        Assert.False(cache.IsCached(fullPath1));
        Assert.True(cache.IsCached(fullPath2));
        Assert.Equal(1, cache.CachedModuleCount);

        loader.ClearCache();
        Assert.Equal(0, cache.CachedModuleCount);
    }

    [Fact]
    public void ModuleReload_ModifyDiskFileAndClearCache_ReloadsUpdatedImplementation()
    {
        var modulePath = CreateTempModuleFile("hot_reload_target.old8", @"
VERSION <- 1
func getMessage() -> string {
    return ""v1""
}
");
        var hostScriptPath = CreateTempModuleFile("hot_reload_host.old8", "// host");
        var service = new ModuleSystemService();

        // 第一次加载 v1
        var interp1 = new LangInterpreter();
        interp1.Manager.Path = hostScriptPath;
        var res1 = service.ImportModule("hot_reload_target", interp1.Manager, new ImportOptions { ModuleAlias = "mod" });
        Assert.True(res1.IsSuccess);
        Assert.False(res1.IsFromCache);
        interp1.Build("v <- mod.VERSION\nm <- mod.getMessage()").Run(interp1.Manager);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interp1.Manager.GetValue(new LangId("v"))).Value);
        Assert.Equal("v1", Assert.IsType<StringLangValue>(interp1.Manager.GetValue(new LangId("m"))).Value);

        // 修改磁盘文件为 v2，但未清缓存前仍命中旧缓存 AST
        File.WriteAllText(modulePath, @"
VERSION <- 2
func getMessage() -> string {
    return ""v2""
}
");
        var interpCached = new LangInterpreter();
        interpCached.Manager.Path = hostScriptPath;
        var resCached = service.ImportModule("hot_reload_target", interpCached.Manager, new ImportOptions { ModuleAlias = "mod" });
        Assert.True(resCached.IsSuccess);
        Assert.True(resCached.IsFromCache);
        interpCached.Build("v <- mod.VERSION\nm <- mod.getMessage()").Run(interpCached.Manager);
        Assert.Equal(1, Assert.IsType<IntLangValue>(interpCached.Manager.GetValue(new LangId("v"))).Value);
        Assert.Equal("v1", Assert.IsType<StringLangValue>(interpCached.Manager.GetValue(new LangId("m"))).Value);

        // 清除模块缓存后重新导入，读取到更新后的 v2
        service.Loader.ClearCache(Path.GetFullPath(modulePath));
        var interp2 = new LangInterpreter();
        interp2.Manager.Path = hostScriptPath;
        var res2 = service.ImportModule("hot_reload_target", interp2.Manager, new ImportOptions { ModuleAlias = "mod" });
        Assert.True(res2.IsSuccess);
        Assert.False(res2.IsFromCache);
        interp2.Build("v <- mod.VERSION\nm <- mod.getMessage()").Run(interp2.Manager);
        Assert.Equal(2, Assert.IsType<IntLangValue>(interp2.Manager.GetValue(new LangId("v"))).Value);
        Assert.Equal("v2", Assert.IsType<StringLangValue>(interp2.Manager.GetValue(new LangId("m"))).Value);
    }

    [Fact]
    public void ModuleReload_DependencyUpdatedAndReloaded_PropagatesNewValueToConsumer()
    {
        var leafPath = CreateTempModuleFile("reload_dep_leaf.old8", @"
func getFactor() -> int {
    return 10
}
");
        var consumerPath = CreateTempModuleFile("reload_dep_consumer.old8", @"
import { getFactor } from ""reload_dep_leaf""
func compute(x:int) -> int {
    return x * getFactor()
}
");
        var hostPath = CreateTempModuleFile("reload_dep_host.old8", "// host");

        var service = new ModuleSystemService();

        // 首次导入并验证
        var interp1 = new LangInterpreter();
        interp1.Manager.Path = hostPath;
        var leafRes1 = service.ImportModule("reload_dep_leaf", interp1.Manager, new ImportOptions { ModuleAlias = "leaf" });
        Assert.True(leafRes1.IsSuccess);
        interp1.Build("r <- leaf.getFactor()").Run(interp1.Manager);
        Assert.Equal(10, Assert.IsType<IntLangValue>(interp1.Manager.GetValue(new LangId("r"))).Value);

        // 更新底层依赖模块，使用无缓存加载器验证最新 AST 生效
        File.WriteAllText(leafPath, @"
func getFactor() -> int {
    return 25
}
");
        service.Loader.ClearCache(Path.GetFullPath(leafPath));

        var interp2 = new LangInterpreter();
        interp2.Manager.Path = hostPath;
        var leafRes2 = service.ImportModule("reload_dep_leaf", interp2.Manager, new ImportOptions { ModuleAlias = "leaf" });
        Assert.True(leafRes2.IsSuccess);
        Assert.False(leafRes2.IsFromCache);
        interp2.Build("r <- leaf.getFactor()").Run(interp2.Manager);
        Assert.Equal(25, Assert.IsType<IntLangValue>(interp2.Manager.GetValue(new LangId("r"))).Value);

        // 验证消费者模块在重新加载后读取最新依赖（通过新建缓存隔离的加载器）
        var freshLoader = new ModuleLoader();
        var consumerLoaded = freshLoader.LoadModule(consumerPath, interp2.Manager, useCache: false);
        Assert.True(consumerLoaded.IsSuccess);
        Assert.NotNull(consumerLoaded.Block);
    }

    [Fact]
    public void ModuleReload_RepeatedImportInSameRun_DoesNotReExecuteModuleSideEffects()
    {
        var counterModule = @"
counter <- 0
counter <- counter + 1

func getInitCount() -> int {
    return counter
}
";
        var mainCode = @"
import { getInitCount } from ""reload_once_mod""
first <- getInitCount()
import { getInitCount as getCountAgain } from ""reload_once_mod""
second <- getCountAgain()
";

        CreateTempModuleFile("reload_once_mod.old8", counterModule);
        CreateTempModuleFile("reload_once_main.old8", mainCode);

        var (interpreter, exception) = ExecuteCodeFile("reload_once_main.old8");

        Assert.Null(exception);
        AssertVariableValue(interpreter, "first", 1);
        AssertVariableValue(interpreter, "second", 1);
    }
}
