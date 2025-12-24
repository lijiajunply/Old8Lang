# Old8Lang 包管理器集成计划

本文档描述 Old8Lang 语言与 Old8Lang.PackageManager 的集成方案和实施计划。

## 📋 集成目标

将 Old8Lang.PackageManager (o8pm) 与 Old8Lang 语言深度集成，实现：
1. ✅ 在 Old8Lang 代码中导入外部包
2. ✅ 自动解析和加载包依赖
3. ✅ 支持本地包源和远程包源
4. ✅ CLI 工具集成到 Old8Lang.App
5. ✅ 完整的测试覆盖

## 🏗️ 架构设计

### 当前架构

```
Old8Lang/
├── Old8Lang/                    # 核心语言库
│   ├── AST/
│   ├── LangParser/
│   ├── Compiler/
│   └── StandardLibrary/         # 现有标准库加载器
│       ├── StandardLibraryLoader.cs
│       ├── StdLibPath.cs
│       └── DynamicModuleLoader.cs
└── Old8Lang.App/               # CLI 应用

Old8Lang.PackageManager/
├── Old8Lang.PackageManager.Core/    # 包管理核心
│   ├── Services/
│   │   ├── DefaultPackageResolver.cs     # 依赖解析器
│   │   ├── DefaultPackageInstaller.cs    # 包安装器
│   │   ├── PackageSourceManager.cs       # 包源管理
│   │   └── VersionManager.cs             # 版本管理
│   └── Models/
│       ├── Package.cs
│       └── PackageConfiguration.cs       # o8packages.json
└── Old8Lang.PackageManager.Server/      # 服务端
```

### 集成架构

```
Old8Lang 项目
├── o8packages.json                  # 包配置文件（新增）
├── packages/                        # 包安装目录（新增）
│   ├── Logger.1.2.0/
│   │   └── lib/old8lang-1.0/
│   │       └── Logger.o8
│   └── HttpClient.2.0.0/
│       └── lib/old8lang-1.0/
│           └── HttpClient.o8
└── src/
    └── main.o8

[Old8Lang 执行流程]
  1. 解析 import 语句
     ↓
  2. 检查是否为包导入
     ↓
  3. PackagePathResolver 解析包路径
     ↓
  4. 从 packages/ 加载包模块
     ↓
  5. 缓存已加载的包
```

## 🔧 实施方案

### Phase 1: 扩展 Import 语句语法

#### 1.1 当前 Import 语法

```old8
// 现有语法（文件路径）
import "utils/helper.o8"
import "./local.o8"
```

#### 1.2 新增包导入语法

```old8
// 从包导入
import "Logger"                    // 导入包（使用 o8packages.json 中的版本）
import "Logger@1.2.0"              // 指定版本导入
import "HttpClient" as http        // 别名导入
import "Utils/StringHelper"        // 导入包中的子模块
```

#### 1.3 语法规则

- **包名识别**: 不以 `.`, `/`, `./`, `../` 开头的路径视为包名
- **版本指定**: 使用 `@` 符号指定版本（可选）
- **路径分隔符**: 使用 `/` 分隔包内路径

### Phase 2: 实现包路径解析器

创建 `Old8Lang/PackageManagement/PackagePathResolver.cs`:

```csharp
namespace Old8Lang.PackageManagement;

public class PackagePathResolver
{
    private readonly string _projectRoot;
    private readonly string _packagesDir;
    private readonly PackageConfiguration? _config;

    public PackagePathResolver(string projectRoot)
    {
        _projectRoot = projectRoot;
        _packagesDir = Path.Combine(projectRoot, "packages");
        _config = LoadPackageConfiguration();
    }

    /// <summary>
    /// 解析导入路径（文件路径或包路径）
    /// </summary>
    public string? ResolveImportPath(string importPath, string? currentFile = null)
    {
        // 1. 检查是否为文件路径
        if (IsFilePath(importPath))
        {
            return ResolveFilePath(importPath, currentFile);
        }

        // 2. 检查是否为包导入
        if (IsPackageImport(importPath))
        {
            return ResolvePackagePath(importPath);
        }

        return null;
    }

    /// <summary>
    /// 判断是否为文件路径
    /// </summary>
    private bool IsFilePath(string path)
    {
        return path.StartsWith(".")         // ./file.o8
            || path.StartsWith("/")         // /abs/path/file.o8
            || path.Contains(":\\")         // C:\path\file.o8
            || path.EndsWith(".o8");        // relative/file.o8
    }

    /// <summary>
    /// 判断是否为包导入
    /// </summary>
    private bool IsPackageImport(string path)
    {
        return !IsFilePath(path);
    }

    /// <summary>
    /// 解析包路径
    /// </summary>
    private string? ResolvePackagePath(string packageImport)
    {
        // 解析包名和版本
        var (packageName, version, subPath) = ParsePackageImport(packageImport);

        // 从配置文件获取版本
        if (version == null && _config != null)
        {
            var reference = _config.References
                .FirstOrDefault(r => r.PackageId == packageName);
            version = reference?.Version;
        }

        if (version == null)
        {
            throw new Exception($"Package '{packageName}' not found in o8packages.json");
        }

        // 构建包路径
        var packageDir = Path.Combine(_packagesDir, $"{packageName}.{version}");
        var libDir = Path.Combine(packageDir, "lib", _config?.Framework ?? "old8lang-1.0");

        // 查找主模块或子模块
        string targetFile;
        if (string.IsNullOrEmpty(subPath))
        {
            // 主模块：PackageName.o8
            targetFile = Path.Combine(libDir, $"{packageName}.o8");
        }
        else
        {
            // 子模块：SubPath.o8
            targetFile = Path.Combine(libDir, $"{subPath}.o8");
        }

        if (!File.Exists(targetFile))
        {
            throw new Exception($"Module not found: {targetFile}");
        }

        return targetFile;
    }

    /// <summary>
    /// 解析包导入语句
    /// 例如: "Logger@1.2.0" -> ("Logger", "1.2.0", null)
    ///       "Utils/StringHelper" -> ("Utils", null, "StringHelper")
    /// </summary>
    private (string packageName, string? version, string? subPath) ParsePackageImport(string import)
    {
        string packageName;
        string? version = null;
        string? subPath = null;

        // 处理版本
        var versionIndex = import.IndexOf('@');
        if (versionIndex > 0)
        {
            packageName = import.Substring(0, versionIndex);
            version = import.Substring(versionIndex + 1);
        }
        else
        {
            // 处理子路径
            var pathIndex = import.IndexOf('/');
            if (pathIndex > 0)
            {
                packageName = import.Substring(0, pathIndex);
                subPath = import.Substring(pathIndex + 1);
            }
            else
            {
                packageName = import;
            }
        }

        return (packageName, version, subPath);
    }

    /// <summary>
    /// 加载 o8packages.json 配置
    /// </summary>
    private PackageConfiguration? LoadPackageConfiguration()
    {
        var configPath = Path.Combine(_projectRoot, "o8packages.json");
        if (!File.Exists(configPath))
        {
            return null;
        }

        var json = File.ReadAllText(configPath);
        return System.Text.Json.JsonSerializer.Deserialize<PackageConfiguration>(json);
    }
}
```

### Phase 3: 修改 Import 语句处理

修改 `Old8Lang/AST/Statement/ImportStatement.cs`:

```csharp
public class ImportStatement : Statement
{
    public string ImportPath { get; set; }
    public string? Alias { get; set; }

    // 新增：包导入标记
    public bool IsPackageImport { get; set; }
    public string? PackageName { get; set; }
    public string? PackageVersion { get; set; }

    public override object? Run(VariateManager variateManager)
    {
        // 获取包路径解析器
        var resolver = variateManager.GetPackagePathResolver();

        // 解析导入路径
        var resolvedPath = resolver.ResolveImportPath(
            ImportPath,
            variateManager.GetCurrentFilePath()
        );

        if (resolvedPath == null)
        {
            throw new Exception($"Cannot resolve import: {ImportPath}");
        }

        // 检查是否已缓存
        if (variateManager.IsModuleCached(resolvedPath))
        {
            var cachedModule = variateManager.GetCachedModule(resolvedPath);
            if (Alias != null)
            {
                variateManager.Set(Alias, cachedModule);
            }
            return cachedModule;
        }

        // 加载并执行模块
        var moduleContent = File.ReadAllText(resolvedPath);
        var interpreter = new LangInterpreter();
        var module = interpreter.Execute(moduleContent, resolvedPath);

        // 缓存模块
        variateManager.CacheModule(resolvedPath, module);

        // 如果有别名，设置到变量管理器
        if (Alias != null)
        {
            variateManager.Set(Alias, module);
        }

        return module;
    }
}
```

### Phase 4: 扩展 VariateManager

修改 `Old8Lang/LangParser/VariateManager.cs`:

```csharp
public class VariateManager
{
    // 现有字段...

    // 新增：包路径解析器
    private PackagePathResolver? _packagePathResolver;

    // 新增：模块缓存
    private readonly Dictionary<string, object> _moduleCache = new();

    // 新增：当前文件路径栈
    private readonly Stack<string> _filePathStack = new();

    public PackagePathResolver GetPackagePathResolver()
    {
        if (_packagePathResolver == null)
        {
            // 从当前文件路径推断项目根目录
            var projectRoot = DetermineProjectRoot();
            _packagePathResolver = new PackagePathResolver(projectRoot);
        }
        return _packagePathResolver;
    }

    public void SetPackagePathResolver(PackagePathResolver resolver)
    {
        _packagePathResolver = resolver;
    }

    public bool IsModuleCached(string path)
    {
        return _moduleCache.ContainsKey(path);
    }

    public object GetCachedModule(string path)
    {
        return _moduleCache[path];
    }

    public void CacheModule(string path, object module)
    {
        _moduleCache[path] = module;
    }

    public string? GetCurrentFilePath()
    {
        return _filePathStack.Count > 0 ? _filePathStack.Peek() : null;
    }

    public void PushFilePath(string path)
    {
        _filePathStack.Push(path);
    }

    public void PopFilePath()
    {
        if (_filePathStack.Count > 0)
            _filePathStack.Pop();
    }

    private string DetermineProjectRoot()
    {
        var currentPath = GetCurrentFilePath();
        if (currentPath == null)
        {
            return Directory.GetCurrentDirectory();
        }

        // 向上查找包含 o8packages.json 的目录
        var dir = Path.GetDirectoryName(currentPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "o8packages.json")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }

        return Directory.GetCurrentDirectory();
    }
}
```

### Phase 5: CLI 工具集成

在 `Old8Lang.App/Program.cs` 中添加包管理命令：

```csharp
// 新增命令
case "package":
case "pkg":
    return await HandlePackageCommand(args.Skip(1).ToArray());

// ...

private static async Task<int> HandlePackageCommand(string[] args)
{
    if (args.Length == 0)
    {
        Console.WriteLine("Usage: old8lang package <command> [options]");
        Console.WriteLine("Commands:");
        Console.WriteLine("  init                  - 初始化包配置文件");
        Console.WriteLine("  add <name> [version]  - 添加包依赖");
        Console.WriteLine("  remove <name>         - 移除包依赖");
        Console.WriteLine("  restore               - 还原所有包依赖");
        Console.WriteLine("  list                  - 列出已安装的包");
        Console.WriteLine("  search <query>        - 搜索包");
        return 1;
    }

    var command = args[0].ToLower();
    var projectRoot = Directory.GetCurrentDirectory();

    // 使用 Old8Lang.PackageManager.Core 的服务
    var configManager = new DefaultPackageConfigurationManager();
    var sourceManager = new PackageSourceManager(projectRoot);
    var resolver = new DefaultPackageResolver(sourceManager);
    var installer = new DefaultPackageInstaller(projectRoot, resolver);

    switch (command)
    {
        case "init":
            return await InitPackageConfig(configManager, projectRoot);

        case "add":
            if (args.Length < 2)
            {
                Console.WriteLine("Error: Package name required");
                return 1;
            }
            var packageName = args[1];
            var version = args.Length > 2 ? args[2] : null;
            return await AddPackage(installer, configManager, projectRoot, packageName, version);

        case "remove":
            if (args.Length < 2)
            {
                Console.WriteLine("Error: Package name required");
                return 1;
            }
            return await RemovePackage(installer, configManager, projectRoot, args[1]);

        case "restore":
            return await RestorePackages(installer, configManager, projectRoot);

        case "list":
            return ListPackages(projectRoot);

        case "search":
            if (args.Length < 2)
            {
                Console.WriteLine("Error: Search query required");
                return 1;
            }
            return await SearchPackages(sourceManager, args[1]);

        default:
            Console.WriteLine($"Unknown command: {command}");
            return 1;
    }
}
```

### Phase 6: 包配置文件支持

使用 `Old8Lang.PackageManager.Core` 的 `PackageConfiguration` 模型：

```json
{
  "Version": "1.0.0",
  "ProjectName": "MyOld8LangProject",
  "Framework": "old8lang-1.0",
  "InstallPath": "packages",
  "Sources": [
    {
      "Name": "Old8Lang Official",
      "Source": "https://packages.old8lang.org/v3/index.json",
      "IsEnabled": true
    },
    {
      "Name": "Local Packages",
      "Source": "./local-packages",
      "IsEnabled": true
    }
  ],
  "References": [
    {
      "PackageId": "Logger",
      "Version": "1.2.0",
      "IsDevelopmentDependency": false,
      "TargetFramework": "old8lang-1.0"
    }
  ]
}
```

## 🧪 测试方案

### 测试项目结构

```
Old8Lang.Tests/
└── PackageManagement/
    ├── PackagePathResolverTests.cs
    ├── PackageImportTests.cs
    └── IntegrationTests.cs

InterpreterTests/PackageTests/
├── basic_package_import.o8
├── versioned_package_import.o8
└── nested_package_import.o8

CompilerTests/PackageTests/
├── basic_package_import.o8
└── complex_package_dependency.o8
```

### 测试用例

#### 1. 单元测试（PackagePathResolverTests.cs）

```csharp
[Fact]
public void ResolveFilePath_ShouldReturnAbsolutePath()
{
    var resolver = new PackagePathResolver("/project");
    var result = resolver.ResolveImportPath("./local.o8", "/project/src/main.o8");
    Assert.Equal("/project/src/local.o8", result);
}

[Fact]
public void ResolvePackagePath_WithoutVersion_ShouldUseConfigVersion()
{
    // 创建测试配置
    CreateTestConfig("/project", new PackageReference
    {
        PackageId = "Logger",
        Version = "1.2.0"
    });

    var resolver = new PackagePathResolver("/project");
    var result = resolver.ResolveImportPath("Logger");

    Assert.Contains("packages/Logger.1.2.0", result);
}

[Fact]
public void ResolvePackagePath_WithVersion_ShouldUseSpecifiedVersion()
{
    var resolver = new PackagePathResolver("/project");
    var result = resolver.ResolveImportPath("Logger@1.3.0");

    Assert.Contains("packages/Logger.1.3.0", result);
}
```

#### 2. 集成测试（basic_package_import.o8）

```old8
// 测试基本包导入功能
// 需要先安装 Logger 包

import "Logger"

logger <- Logger.Create("TestApp")
logger.Info("Package import works!")

// 预期输出：[INFO] TestApp: Package import works!
```

#### 3. 版本测试（versioned_package_import.o8）

```old8
// 测试指定版本导入

import "Logger@1.2.0" as LoggerOld
import "Logger@2.0.0" as LoggerNew

// 使用旧版本
oldLogger <- LoggerOld.Create("OldApp")
oldLogger.Info("Using old version")

// 使用新版本
newLogger <- LoggerNew.Create("NewApp")
newLogger.Info("Using new version")
```

#### 4. 嵌套依赖测试（complex_package_dependency.o8）

```old8
// 测试复杂依赖解析
// HttpClient 依赖 Logger

import "HttpClient"

client <- HttpClient.Create("https://api.example.com")
response <- client.Get("/users")

PrintLine(response.Status)
```

### 示例包创建

#### Logger 包结构

```
ExamplePackages/Logger/
├── package.json
├── lib/
│   └── old8lang-1.0/
│       └── Logger.o8
└── docs/
    └── README.md
```

**package.json**:
```json
{
  "id": "Logger",
  "version": "1.2.0",
  "description": "Simple logging library for Old8Lang",
  "author": "Old8Lang Team",
  "license": "MIT",
  "keywords": ["logging", "utility"],
  "dependencies": [],
  "frameworks": {
    "old8lang-1.0": {}
  }
}
```

**Logger.o8**:
```old8
// Logger.o8 - 简单的日志库

class Logger {
    private name:string

    constructor(name:string) {
        this.name <- name
    }

    public func Info(message:string) -> void {
        timestamp <- Time.Now().ToStr()
        PrintLine($"[INFO] [{timestamp}] {this.name}: {message}")
    }

    public func Error(message:string) -> void {
        timestamp <- Time.Now().ToStr()
        PrintLine($"[ERROR] [{timestamp}] {this.name}: {message}")
    }

    public func Warn(message:string) -> void {
        timestamp <- Time.Now().ToStr()
        PrintLine($"[WARN] [{timestamp}] {this.name}: {message}")
    }
}

// 导出工厂函数
Create <- (name:string) -> {
    return Logger(name)
}
```

## 📦 包打包流程

### 手动打包

```bash
# 1. 进入包目录
cd ExamplePackages/Logger

# 2. 创建 .o8pkg 文件（ZIP 格式）
zip -r Logger.1.2.0.o8pkg package.json lib/ docs/

# 3. 移动到本地包源
mv Logger.1.2.0.o8pkg ../../MyProject/local-packages/
```

### 使用 o8pm 打包（未来支持）

```bash
# 在包目录中
o8pm pack

# 发布到服务器
o8pm push Logger.1.2.0.o8pkg --source "https://packages.old8lang.org"
```

## 🚀 实施步骤

### Step 1: 创建基础设施（1-2天）

- [x] 创建 `Old8Lang/PackageManagement/` 目录
- [ ] 实现 `PackagePathResolver` 类
- [ ] 引用 `Old8Lang.PackageManager.Core` 项目
- [ ] 添加 JSON 序列化依赖

### Step 2: 修改核心代码（2-3天）

- [ ] 扩展 `ImportStatement` 支持包导入
- [ ] 修改 `VariateManager` 添加模块缓存
- [ ] 更新 `LangParser` 解析包导入语法
- [ ] 测试解析器修改

### Step 3: CLI 集成（1天）

- [ ] 在 `Old8Lang.App` 中添加包管理命令
- [ ] 集成 `Old8Lang.PackageManager.Core` 服务
- [ ] 测试 CLI 命令

### Step 4: 创建测试用例（2-3天）

- [ ] 编写单元测试
- [ ] 创建示例包（Logger, HttpClient）
- [ ] 编写集成测试脚本
- [ ] 测试解释器模式
- [ ] 测试编译器模式

### Step 5: 文档和示例（1天）

- [ ] 更新 `Old8Lang_Grammar.md` 添加包导入语法
- [ ] 创建包开发指南
- [ ] 编写快速开始教程
- [ ] 录制演示视频

### Step 6: 联合测试（1-2天）

- [ ] 启动 `Old8Lang.PackageManager.Server`
- [ ] 上传测试包到本地服务器
- [ ] 测试远程包下载和安装
- [ ] 测试依赖解析
- [ ] 生成测试报告

## ⏱️ 总体时间估算

- **开发时间**: 8-12 天
- **测试时间**: 3-5 天
- **文档时间**: 1-2 天
- **总计**: 12-19 天

## 📊 成功指标

集成完成后应达到：

1. ✅ 可以在 Old8Lang 代码中使用 `import "PackageName"` 导入包
2. ✅ 支持版本指定 `import "PackageName@1.0.0"`
3. ✅ 自动解析和安装包依赖
4. ✅ CLI 命令 `old8lang package add/remove/restore` 正常工作
5. ✅ 解释器和编译器模式都支持包导入
6. ✅ 至少 2 个示例包可用（Logger, HttpClient）
7. ✅ 测试覆盖率 > 80%
8. ✅ 完整的文档和教程

## 🔄 后续优化

集成完成后的改进方向：

1. **性能优化**
   - 包索引缓存
   - 并行包下载
   - 增量更新

2. **功能增强**
   - 包版本锁定（o8packages.lock）
   - 私有包源支持
   - 包签名验证

3. **开发体验**
   - IDE 插件（包导入自动完成）
   - 包文档集成
   - 依赖可视化

4. **生态建设**
   - 官方包仓库
   - 包质量评分
   - 社区包贡献

---

**文档版本**: 1.0.0
**创建日期**: 2025-12-24
**作者**: Old8Lang Team
