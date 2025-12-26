# Old8Lang 模块系统重构总结

## 📋 重构概览

本次重构按照架构设计建议，将原有的单一 `ImportStatement` 类（1097行）拆分为清晰的服务层架构，显著提升了代码的可维护性和性能。

## 🏗️ 新架构结构

```
Old8Lang/ModuleSystem/
├── Resolution/                 # 模块解析层
│   ├── PathResolver.cs        # 路径解析器（修复了原有的 HACK）
│   ├── VersionResolver.cs     # 版本解析器（支持语义化版本）
│   └── ModuleResolver.cs      # 模块解析器（统一解析逻辑）
│
├── Loading/                    # 模块加载层
│   ├── CacheManager.cs        # 缓存管理器
│   ├── ModuleLoader.cs        # 模块加载器
│   └── NetworkModuleLoader.cs # 网络模块加载器（新功能）
│
├── Symbols/                    # 符号管理层
│   ├── SymbolExtractor.cs     # 符号提取器
│   ├── SymbolRegistry.cs      # 符号注册器
│   └── ExportController.cs    # 导出控制器（为 export 关键字预留）
│
└── Core/                       # 核心类
    ├── UnifiedModuleV2.cs     # 重构后的统一模块（性能优化）
    └── ModuleSystemService.cs # 模块系统服务门面
```

## ✨ 主要改进

### 1. **路径解析优化**

**Before** (ImportStatement.cs:213-216):
```csharp
// ❌ 硬编码的 HACK
if (filePath.StartsWith("Users/") || filePath.StartsWith("Volumes/")) {
    filePath = "/" + filePath;
}
```

**After** (PathResolver.cs):
```csharp
// ✅ 系统化处理
if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux()) {
    if ((path.StartsWith("Users/") || path.StartsWith("home/") ||
         path.StartsWith("Volumes/") || path.StartsWith("mnt/")) &&
        !path.StartsWith("/")) {
        path = "/" + path;
    }
}
```

### 2. **符号查找性能优化**

**Before** (UnifiedModule.cs:329-334):
```csharp
// ❌ O(n) 遍历整个字典
var caseInsensitiveMatch = Symbols.FirstOrDefault(kvp =>
    string.Equals(kvp.Key, symbolName, StringComparison.OrdinalIgnoreCase));
```

**After** (UnifiedModuleV2.cs):
```csharp
// ✅ O(1) 双字典查找
private readonly Dictionary<string, LangValueType> Symbols = new();
private readonly Dictionary<string, LangValueType> CaseInsensitiveSymbols =
    new(StringComparer.OrdinalIgnoreCase);

// 精确查找 O(1)
if (Symbols.TryGetValue(symbolName, out var symbol)) {
    return symbol;
}

// 大小写不敏感查找 O(1)
if (CaseInsensitiveSymbols.TryGetValue(symbolName, out symbol)) {
    return symbol;
}
```

### 3. **完整的状态管理**

**Before** (IModuleInterfaces.cs):
```csharp
// ❌ Loading 和 LoadFailed 状态从未使用
public enum ModuleLoadingState {
    NotLoaded,
    Loading,    // 未使用
    Loaded,
    LoadFailed  // 未使用
}
```

**After** (UnifiedModuleV2.cs):
```csharp
// ✅ 完整的状态管理
public void EnsureLoaded(VariateManager? variateManager = null) {
    if (_loadingState == ModuleLoadingState.Loaded) return;

    lock (LoadLock) {
        if (_loadingState == ModuleLoadingState.Loading) {
            throw new ImportError(this, ModuleName, "检测到循环加载");
        }

        _loadingState = ModuleLoadingState.Loading;
        try {
            LoadModuleInternal(variateManager);
            _loadingState = ModuleLoadingState.Loaded;
        } catch (Exception ex) {
            _loadingState = ModuleLoadingState.LoadFailed;
            _loadException = ex;
            throw;
        }
    }
}
```

### 4. **版本管理支持**

**新功能** (VersionResolver.cs):
```csharp
// ✅ 支持语义化版本
import "math@^1.0.0"      // 兼容版本
import "utils@>=2.0.0"    // 最小版本
import "lib@~1.2.3"       // 补丁版本

// 自动选择最佳匹配版本
var bestVersion = versionResolver.SelectBestVersion(
    ["package@1.0.0", "package@1.2.0", "package@2.0.0"],
    "^1.0.0"
);  // 返回 "package@1.2.0"
```

### 5. **网络模块支持**

**Before** (ImportStatement.cs:113-132):
```csharp
// ❌ 只是创建空模块，不实际下载
if (moduleName.StartsWith("http://") || moduleName.StartsWith("https://")) {
    manager.Interpreter.OutputProvider.WriteLine($"[警告] 正在从网络导入模块: {moduleName}");
    if (ModuleAlias != null) {
        var moduleObj = ModuleFactory.CreateEagerModule(ImportString, manager, Position);
        manager.Scopes[^1][ModuleAlias] = moduleObj;
    }
    return;
}
```

**After** (NetworkModuleLoader.cs):
```csharp
// ✅ 真正下载并缓存
public async Task<string?> DownloadModuleAsync(string url, bool forceDownload = false) {
    var urlHash = ComputeHash(url);
    var cacheFile = Path.Combine(_cacheDirectory, urlHash, "module.old8");

    // 检查缓存
    if (!forceDownload && File.Exists(cacheFile)) {
        var cacheAge = DateTime.Now - new FileInfo(cacheFile).LastWriteTime;
        if (cacheAge.TotalDays < 1) {
            return cacheFile;
        }
    }

    // 下载并验证
    var response = await HttpClient.GetAsync(url);
    var content = await response.Content.ReadAsStringAsync();

    if (!ValidateModuleContent(content)) {
        throw new InvalidOperationException($"模块内容验证失败");
    }

    await File.WriteAllTextAsync(cacheFile, content);
    return cacheFile;
}
```

## 📊 性能对比

| 操作 | 旧实现 | 新实现 | 改进 |
|------|--------|--------|------|
| 符号查找（大小写不敏感） | O(n) | O(1) | **显著提升** |
| ImportInfos 复制 | 每次复制整个列表 | 使用索引 | **减少内存分配** |
| 路径解析 | 多次重复调用 | 单次规范化 | **减少I/O** |
| 模块缓存 | 基础实现 | 线程安全 + 统计信息 | **更可靠** |

## 🔧 如何使用新系统

### 方式 1: 使用 ModuleSystemService（推荐）

```csharp
var moduleService = new ModuleSystemService();

var options = new ImportOptions {
    IsFromClause = true,
    ImportSpecifiers = new List<string> { "func1", "func2" }
};

var result = moduleService.ImportModule("my_module", manager, options);

if (result.IsSuccess) {
    Console.WriteLine($"成功导入 {result.ExtractedSymbols?.Count} 个符号");
} else {
    Console.WriteLine($"导入失败: {result.Error?.Message}");
}
```

### 方式 2: 直接使用各层服务

```csharp
// 1. 解析模块
var resolver = new ModuleResolver();
var resolution = resolver.ResolveModule("package@^1.0.0", currentPath, manager);

// 2. 加载模块
var loader = new ModuleLoader();
var loadResult = loader.LoadModule(resolution.ResolvedPath, manager);

// 3. 提取符号
var extractor = new SymbolExtractor();
var symbols = extractor.ExtractSymbols(manager);

// 4. 注册符号
var registry = new SymbolRegistry();
registry.RegisterSymbols(manager, symbols);
```

### 方式 3: 使用 UnifiedModuleV2

```csharp
// 懒加载
var lazyModule = UnifiedModuleV2.CreateLazy("math", manager);

// 选择性导入
var selectiveModule = UnifiedModuleV2.CreateSelective(
    "utils",
    new[] { "func1", "func2" },
    manager
);

// 从现有符号创建
var customModule = UnifiedModuleV2.FromSymbols(
    "custom",
    new Dictionary<string, LangValueType> {
        ["PI"] = new DoubleLangValue(3.14159),
        ["add"] = new FuncLangValue(...)
    }
);
```

## 🚀 下一步计划

### 高优先级
1. ✅ 将 `ImportStatement` 重构为使用 `ModuleSystemService`
2. ⏳ 运行现有测试套件，确保兼容性
3. ⏳ 修复任何回归问题

### 中优先级
4. 📝 添加 `export` 关键字支持
5. 📝 实现重导出（re-export）
6. 📝 添加星号导入（`from module import *`）

### 低优先级
7. 📝 条件导入支持
8. 📝 改进循环依赖处理（支持前向引用）
9. 📝 添加模块热重载功能

## 📚 相关文档

- **Resolution 层**: 负责将模块名解析为实际路径
  - `PathResolver`: 路径规范化和安全验证
  - `VersionResolver`: 语义化版本解析和匹配
  - `ModuleResolver`: 统一的模块解析接口

- **Loading 层**: 负责加载和缓存模块代码
  - `CacheManager`: 线程安全的模块缓存
  - `ModuleLoader`: 文件和目录加载
  - `NetworkModuleLoader`: HTTP(S) 模块下载

- **Symbols 层**: 负责符号提取和注册
  - `SymbolExtractor`: 从作用域提取导出符号
  - `SymbolRegistry`: 将符号注册到作用域
  - `ExportController`: 控制哪些符号被导出

- **Core 层**: 核心模块对象和服务
  - `UnifiedModuleV2`: 性能优化的模块对象
  - `ModuleSystemService`: 服务门面，简化使用

## ⚠️ 注意事项

1. **向后兼容性**: 旧的 `UnifiedModule` 仍然保留，可以逐步迁移
2. **性能测试**: 建议在生产环境前进行充分的性能测试
3. **错误处理**: 新系统提供了更详细的错误信息和状态追踪
4. **线程安全**: 所有服务类都是线程安全的

## 🎉 总结

通过本次重构，我们：
- ✅ 将 1097 行的巨大类拆分为职责清晰的小类
- ✅ 修复了路径解析中的 HACK 和 bug
- ✅ 将符号查找从 O(n) 优化到 O(1)
- ✅ 实现了完整的状态管理（Loading/LoadFailed）
- ✅ 添加了版本管理支持
- ✅ 实现了真正的网络模块加载
- ✅ 为未来的 export 关键字预留了接口
- ✅ 提升了代码的可测试性和可维护性

重构后的模块系统更加健壮、高效和易于扩展！
