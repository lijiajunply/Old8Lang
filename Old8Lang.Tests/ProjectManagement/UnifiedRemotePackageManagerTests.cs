using Old8Lang.Bytecode;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.PackageManager.Core.Models;
using Old8Lang.PackageManager.Core.Services;
using Old8Lang.ProjectManagement;
using VM = Old8Lang.Bytecode.VM.VirtualMachine;

namespace Old8Lang.Tests.ProjectManagement;

/// <summary>
/// 验证统一远程包管理器相关的核心契约：
/// 1. 禁止直连 HTTP/HTTPS URL 导入（NetworkModuleLoader 已移除）
/// 2. 唯一远程仓库地址解析优先级与规范化
/// 3. 统一 {packageId}@{version} 解压布局 + o8package.json main 入口在解释/IL/VM 三种模式下均可导入执行
/// </summary>
public class UnifiedRemotePackageManagerTests
{
    [Theory]
    [InlineData("http://example.com/mod.old8")]
    [InlineData("https://example.com/mod.old8")]
    public void ModuleResolver_ShouldRejectHttpUrlImports(string url)
    {
        var resolver = new ModuleResolver();
        var result = resolver.ResolveModule(url, null);

        Assert.False(result.IsSuccess);
        Assert.Equal(ModuleType.Unknown, result.ModuleType);
        Assert.Contains("old8lang install", result.FailureReason ?? string.Empty);

        var interpreter = new LangInterpreter();
        var ast = interpreter.Build($"import \"{url}\"");
        Assert.Throws<ImportError>(() => ast.Run(interpreter.Manager));
    }

    [Fact]
    public void GlobalPackageConfig_ShouldResolveRegistryPriorityAndNormalizeV3()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"o8-cfg-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var configPath = Path.Combine(tempDir, "config.json");

        var prevEnv = Environment.GetEnvironmentVariable(GlobalPackageConfig.RegistryEnvVar);
        try
        {
            Environment.SetEnvironmentVariable(GlobalPackageConfig.RegistryEnvVar, null);

            // 1. 无配置时回退默认官方源
            Assert.Equal(
                GlobalPackageConfig.DefaultRegistryUrl,
                GlobalPackageConfig.ResolveRegistryUrl(configPath: configPath));

            // 2. 全局配置文件生效并规范化末尾 /v3/
            var globalCfg = new GlobalPackageConfig
            {
                Registry = "https://global.internal.local/v3/",
                ApiKey = "secret-key-123"
            };
            globalCfg.Save(configPath);

            Assert.Equal(
                "https://global.internal.local",
                GlobalPackageConfig.ResolveRegistryUrl(configPath: configPath));
            Assert.Equal("secret-key-123", GlobalPackageConfig.ResolveApiKey(configPath: configPath));

            // 3. 项目 o8package.json 中的 legacy sources 兼容生效
            var projectLegacy = new ProjectConfig
            {
                ProjectName = "demo",
                Sources = [new PackageSource { Name = "Private", Source = "http://legacy.local:5000/v3", IsEnabled = true }]
            };
            Assert.Equal(
                "http://legacy.local:5000",
                GlobalPackageConfig.ResolveRegistryUrl(projectConfig: projectLegacy, configPath: configPath));

            // 4. 项目 o8package.json 中的 registry 字段优先级高于 legacy sources 和全局配置
            projectLegacy.Registry = "http://project.local:5000/v3/";
            Assert.Equal(
                "http://project.local:5000",
                GlobalPackageConfig.ResolveRegistryUrl(projectConfig: projectLegacy, configPath: configPath));

            // 5. 环境变量优先于项目配置
            Environment.SetEnvironmentVariable(GlobalPackageConfig.RegistryEnvVar, "http://env.local:5000/v3");
            Assert.Equal(
                "http://env.local:5000",
                GlobalPackageConfig.ResolveRegistryUrl(projectConfig: projectLegacy, configPath: configPath));

            // 6. CLI --registry 参数优先级最高
            Assert.Equal(
                "http://cli.local:5000",
                GlobalPackageConfig.ResolveRegistryUrl(
                    cliRegistry: "http://cli.local:5000/v3/",
                    projectConfig: projectLegacy,
                    configPath: configPath));
        }
        finally
        {
            Environment.SetEnvironmentVariable(GlobalPackageConfig.RegistryEnvVar, prevEnv);
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task InstalledVersionedPackageWithCustomMain_ShouldImportAcrossInterpreterIlAndVm()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"o8-pkg-import-{Guid.NewGuid():N}");
        var srcPkgDir = Path.Combine(tempRoot, "pkg-src");
        var appDir = Path.Combine(tempRoot, "consumer-app");
        var appPackagesDir = Path.Combine(appDir, "packages");

        try
        {
            Directory.CreateDirectory(Path.Combine(srcPkgDir, "src"));
            Directory.CreateDirectory(appPackagesDir);

            // 1. 构造带自定义 main="src/index.old8" 的包，并用 PackageArchiveService 打包
            await File.WriteAllTextAsync(Path.Combine(srcPkgDir, "o8package.json"), """
            {
              "projectName": "demo_calc",
              "version": "1.2.0",
              "description": "Demo calc library",
              "author": "Test",
              "main": "src/index.old8"
            }
            """);
            await File.WriteAllTextAsync(Path.Combine(srcPkgDir, "src", "index.old8"), """
            func add_nums(a:int, b:int) -> int {
                return a + b
            }
            """);

            var archiveService = new PackageArchiveService();
            var o8pkgPath = await archiveService.PackAsync(srcPkgDir, Path.Combine(tempRoot, "demo_calc.1.2.0.o8pkg"));

            // 2. 按 DefaultPackageInstaller 统一目录布局解包到 {appDir}/packages/demo_calc@1.2.0
            var installedDir = DefaultPackageInstaller.GetVersionedPackageDirectory(appPackagesDir, "demo_calc", "1.2.0");
            await archiveService.UnpackAsync(o8pkgPath, installedDir);

            // 3. 消费者项目（含 o8package.json 与 src/main.old8）
            Directory.CreateDirectory(Path.Combine(appDir, "src"));
            await File.WriteAllTextAsync(Path.Combine(appDir, "o8package.json"), """
            {
              "projectName": "consumer-app",
              "version": "1.0.0",
              "main": "src/main.old8"
            }
            """);
            var consumerFile = Path.Combine(appDir, "src", "main.old8");
            const string qualifiedConsumerCode = """
            import "demo_calc"
            res <- demo_calc.add_nums(19, 23)
            Assert.Equal(42, res)
            """;
            const string directConsumerCode = """
            import "demo_calc"
            res <- add_nums(19, 23)
            Assert.Equal(42, res)
            """;
            await File.WriteAllTextAsync(consumerFile, qualifiedConsumerCode);

            // 4. 解释模式（同时验证模块限定调用与直接符号调用）
            var interp = new LangInterpreter();
            interp.Build(qualifiedConsumerCode, consumerFile).Run(interp.Manager);

            // 5. IL 模式
            var ilInterp = new LangInterpreter();
            var compiled = Old8Lang.Compiler.Compiler.Compile(
                ilInterp.Build(directConsumerCode, consumerFile), consumerFile, ilInterp);
            compiled();

            // 6. 虚拟机模式
            var vmInterp = new LangInterpreter();
            var vmAst = vmInterp.Build(directConsumerCode, consumerFile);
            var bytecode = new BytecodeCompiler().Compile(vmAst);
            var vm = new VM(bytecode, Path.GetDirectoryName(consumerFile));
            vm.Execute();
        }
        finally
        {
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }
    }
}
