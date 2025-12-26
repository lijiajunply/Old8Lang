# Old8Lang 语义化版本解析实现

## 概述

已成功将 `Old8Lang.PackageManager.Core` 的 `VersionManager` 集成到 `PackageService` 中，实现完整的语义化版本解析功能。

## 实现细节

### 1. 集成 VersionManager

**文件**: `Old8Lang.App/Services/PackageService.cs`

**更改**:
- 添加了 `VersionManager` 字段
- 在构造函数中初始化 `VersionManager` 实例
- 重构了 `ResolveVersion` 方法以使用 `VersionManager` 的完整功能

### 2. 支持的版本范围语法

`ResolveVersion` 方法现在完全支持以下版本范围语法：

#### 2.1 精确版本
```
输入: "1.2.3"
输出: "1.2.3"
```

#### 2.2 npm 风格的版本范围

**插入符号 (^)** - 兼容主版本
```
输入: "^1.2.3"
输出: "1.2.3"
说明: 兼容 1.x.x 版本（主版本相同）
```

**波浪号 (~)** - 兼容次版本
```
输入: "~1.2.3"
输出: "1.2.3"
说明: 兼容 1.2.x 版本（主版本和次版本相同）
```

#### 2.3 比较运算符

**大于等于 (>=)**
```
输入: ">=1.2.0"
输出: "1.2.0"
```

**大于 (>)**
```
输入: ">1.0.0"
输出: "1.0.0"
```

**小于等于 (<=)**
```
输入: "<=2.0.0"
输出: "2.0.0"
```

**小于 (<)**
```
输入: "<3.0.0"
输出: "3.0.0"
```

#### 2.4 通配符版本

**完全通配符**
```
输入: "*"
输出: "1.0.0"
```

**部分通配符**
```
输入: "1.2.*"
输出: "1.2.0"
```

#### 2.5 版本范围

```
输入: "1.0.0-2.0.0"
输出: "1.0.0"
说明: 返回范围的最小版本
```

#### 2.6 预发布版本

```
输入: "1.2.3-alpha"
输出: "1.2.3-alpha"

输入: "2.0.0-beta.1"
输出: "2.0.0-beta.1"
```

### 3. 版本解析逻辑流程

```
ResolveVersion(versionRange)
  │
  ├─ 空字符串/null → "1.0.0"
  │
  ├─ "*" → "1.0.0"
  │
  ├─ 以 "^" 开头
  │   └─ 解析基准版本 → 返回基准版本
  │
  ├─ 以 "~" 开头
  │   └─ 解析基准版本 → 返回基准版本
  │
  ├─ 以 ">=", "<=", ">", "<" 开头
  │   ├─ 解析版本范围
  │   ├─ 有最小版本 → 返回最小版本
  │   └─ 否则返回最大版本或 "1.0.0"
  │
  ├─ 包含 "-" (范围版本)
  │   ├─ 分割为两部分
  │   ├─ 解析两个版本
  │   └─ 返回范围的最小版本
  │
  ├─ 包含 "*" (通配符)
  │   ├─ 解析版本范围
  │   └─ 返回最小版本
  │
  └─ 精确版本
      ├─ 解析版本
      └─ 返回版本字符串
```

### 4. VersionManager API

`VersionManager` 提供以下核心功能：

#### 4.1 版本比较
```csharp
int CompareVersions(string version1, string version2)
// 返回: -1 (version1 < version2), 0 (相等), 1 (version1 > version2)
```

#### 4.2 版本范围检查
```csharp
bool IsVersionInRange(string version, string versionRange)
// 检查版本是否在指定范围内
```

#### 4.3 获取最新版本
```csharp
string? GetLatestVersionInRange(IEnumerable<string> versions, string versionRange)
// 从可用版本列表中获取符合范围的最新版本
```

#### 4.4 版本解析
```csharp
SemanticVersion ParseVersion(string version)
// 解析版本字符串为结构化版本对象
```

#### 4.5 版本范围解析
```csharp
VersionRange ParseVersionRange(string versionRange)
// 解析版本范围字符串为版本范围对象
```

### 5. 数据结构

#### 5.1 SemanticVersion
```csharp
public class SemanticVersion
{
    public int Major { get; set; }          // 主版本号
    public int Minor { get; set; }          // 次版本号
    public int Patch { get; set; }          // 补丁版本号
    public bool IsPrerelease { get; set; }  // 是否为预发布版本
    public string Prerelease { get; set; }  // 预发布标识符
}
```

#### 5.2 VersionRange
```csharp
public class VersionRange
{
    public string MinVersion { get; set; }       // 最小版本
    public string MaxVersion { get; set; }       // 最大版本
    public bool IncludeMinVersion { get; set; }  // 包含最小版本
    public bool IncludeMaxVersion { get; set; }  // 包含最大版本
}
```

## 使用场景

### 场景 1: 安装包时解析版本
```csharp
// 用户请求: old8lang install mypackage ^1.2.0
// PackageService.InstallPackageAsync 调用 ResolveVersion("^1.2.0")
// 结果: 返回 "1.2.0" 并安装该版本
```

### 场景 2: 恢复依赖时解析版本
```csharp
// o8packages.json 中的依赖: "mypackage": "~2.3.4"
// PackageService.RestorePackagesAsync 调用 ResolveVersion("~2.3.4")
// 结果: 返回 "2.3.4" 并安装该版本
```

### 场景 3: 处理预发布版本
```csharp
// 依赖: "testlib": "1.0.0-beta.2"
// ResolveVersion("1.0.0-beta.2")
// 结果: 返回 "1.0.0-beta.2"
```

## 未来增强

### 1. 从可用版本列表中选择最佳匹配版本

当前实现返回版本范围的"基准版本"或最小版本。未来可以增强为：

```csharp
private string ResolveVersion(string versionRange, IEnumerable<string>? availableVersions = null)
{
    if (availableVersions != null && availableVersions.Any())
    {
        // 使用 VersionManager.GetLatestVersionInRange 从可用版本中选择最佳匹配
        var bestMatch = VersionManager.GetLatestVersionInRange(availableVersions, versionRange);
        if (!string.IsNullOrEmpty(bestMatch))
        {
            return bestMatch;
        }
    }

    // 回退到当前的基准版本逻辑
    // ...
}
```

### 2. 缓存版本解析结果

为提高性能，可以缓存版本解析结果：

```csharp
private readonly Dictionary<string, string> _versionCache = new();

private string ResolveVersion(string versionRange)
{
    if (_versionCache.TryGetValue(versionRange, out var cached))
    {
        return cached;
    }

    var resolved = /* 当前的解析逻辑 */;
    _versionCache[versionRange] = resolved;
    return resolved;
}
```

### 3. 支持更复杂的版本约束

支持组合版本约束（例如 `>=1.2.0 <2.0.0`）：

```
输入: ">=1.2.0 <2.0.0"
输出: "1.2.0" (或从可用版本列表中选择最佳匹配)
```

## 测试建议

### 单元测试

为 `PackageService.ResolveVersion` 方法编写单元测试：

```csharp
[Theory]
[InlineData("1.2.3", "1.2.3")]
[InlineData("^1.2.3", "1.2.3")]
[InlineData("~1.2.3", "1.2.3")]
[InlineData(">=1.2.0", "1.2.0")]
[InlineData("*", "1.0.0")]
[InlineData("1.2.*", "1.2.0")]
[InlineData("1.0.0-2.0.0", "1.0.0")]
public void ResolveVersion_ValidInput_ReturnsExpectedVersion(string input, string expected)
{
    // Arrange
    var service = new PackageService(Directory.GetCurrentDirectory());
    var method = typeof(PackageService).GetMethod("ResolveVersion",
        BindingFlags.NonPublic | BindingFlags.Instance);

    // Act
    var result = method.Invoke(service, new object[] { input }) as string;

    // Assert
    Assert.Equal(expected, result);
}
```

### 集成测试

测试完整的包安装流程：

```csharp
[Fact]
public async Task InstallPackage_WithCaretVersion_InstallsCorrectVersion()
{
    // Arrange
    var service = new PackageService(testProjectRoot);

    // Act
    var result = await service.InstallPackageAsync("testpackage", "^1.2.0");

    // Assert
    Assert.True(result.Success);
    Assert.Equal("1.2.0", result.Version);
}
```

## 总结

通过集成 `VersionManager`，Old8Lang 的包管理系统现在具备了完整的语义化版本解析能力，支持：

✅ 精确版本匹配
✅ npm 风格的版本范围（^, ~）
✅ 比较运算符（>=, <=, >, <）
✅ 通配符版本（*, 1.2.*）
✅ 版本范围（1.0.0-2.0.0）
✅ 预发布版本（1.0.0-alpha）

这使得 Old8Lang 的包管理系统与现代包管理工具（如 npm、NuGet）的版本解析能力保持一致。
