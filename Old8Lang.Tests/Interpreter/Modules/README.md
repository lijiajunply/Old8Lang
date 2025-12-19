# Old8Lang 模块系统测试

## 概述

本目录包含了 Old8Lang 语言模块导入系统的全面测试套件。测试按照功能和复杂度进行了重新组织，提供了更好的测试覆盖率、组织和维护性。

## 测试结构

```
Modules/
├── Core/                           # 核心测试基础设施
│   ├── ModuleImportTestBase.cs     # 测试基类，提供通用功能
│   ├── TestFileSystemHelper.cs     # 文件系统测试助手
│   └── MockModuleProvider.cs       # Mock 模块提供器（待实现）
├── BasicImport/                    # 基础导入功能测试
│   ├── SimpleImportTests.cs        # 基本导入测试
│   ├── AliasImportTests.cs         # 别名导入测试
│   ├── SelectiveImportTests.cs     # 选择性导入测试（待实现）
│   └── WildcardImportTests.cs      # 通配符导入测试（待实现）
├── AdvancedImport/                 # 高级导入功能测试
│   ├── LazyImportTests.cs          # 延迟导入测试（待实现）
│   ├── ConditionalImportTests.cs   # 条件导入测试（待实现）
│   ├── DynamicImportTests.cs       # 动态导入测试（待实现）
│   └── NamespaceImportTests.cs     # 命名空间导入测试（待实现）
├── ErrorHandling/                  # 错误处理测试
│   ├── ImportErrorTests.cs         # 导入错误测试
│   ├── CircularDependencyTests.cs  # 循环依赖测试（待实现）
│   └── ValidationErrorTests.cs     # 验证错误测试（待实现）
├── Performance/                    # 性能测试
│   ├── ImportPerformanceTests.cs   # 导入性能测试
│   └── MemoryUsageTests.cs         # 内存使用测试（待实现）
└── Integration/                    # 集成测试
    ├── ComplexImportScenariosTests.cs # 复杂导入场景测试（待实现）
    └── RealWorldUsageTests.cs      # 真实使用场景测试
```

## 测试覆盖范围

### 1. 基础导入功能 (`BasicImport/`)
- ✅ 简单模块导入 (`SimpleImportTests.cs`)
- ✅ 多函数导入
- ✅ 常量导入
- ✅ 变量状态管理
- ✅ 重复导入处理
- ✅ 别名导入 (`AliasImportTests.cs`)
- ✅ 选择性导入 (`SelectiveImportTests.cs`)
- ✅ 通配符导入 (`WildcardImportTests.cs`)

### 2. 高级导入功能 (`AdvancedImport/`)
- ✅ 延迟导入 (`LazyImportTests.cs`)
- ✅ 条件导入 (`ConditionalImportTests.cs`)
- ✅ 动态导入 (`DynamicImportTests.cs`)
- ✅ 嵌套依赖处理
- ✅ 运行时模块选择

### 3. 错误处理 (`ErrorHandling/`)
- ✅ 文件不存在错误 (`ImportErrorTests.cs`)
- ✅ 循环依赖处理 (`CircularDependencyTests.cs`)
- ✅ 语法错误传播
- ✅ 运行时错误处理
- ✅ 路径验证
- ✅ 只读文件处理
- ✅ 大文件处理
- ✅ 自依赖检测
- ✅ 间接循环依赖
- ✅ 错误恢复机制

### 4. 性能测试 (`Performance/`)
- ✅ 批量模块导入 (`ImportPerformanceTests.cs`)
- ✅ 导入缓存机制
- ✅ 并发安全性
- ✅ 深度嵌套依赖
- ✅ 内存使用监控

### 5. 集成测试 (`Integration/`)
- ✅ 真实使用场景 (`RealWorldUsageTests.cs`)
- ✅ Web 应用场景
- ✅ 数据处理管道
- ✅ 游戏开发场景
- ✅ 配置管理系统
- ✅ 复杂导入场景 (`ComplexImportScenariosTests.cs`)
- ✅ 多级依赖树
- ✅ 钻石依赖模式
- ✅ 插件架构模式
- ✅ 微服务模式模拟

## 运行测试

### 运行所有模块测试
```bash
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules"
```

### 运行特定类别测试
```bash
# 基础导入测试
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.BasicImport"

# 错误处理测试
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.ErrorHandling"

# 性能测试
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Performance"

# 集成测试
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Integration"
```

### 运行单个测试文件
```bash
dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.BasicImport.SimpleImportTests"
```

## 编写新测试

### 1. 继承基类
所有测试类都应该继承 `ModuleImportTestBase`：

```csharp
public class MyNewTests : ModuleImportTestBase
{
    public MyNewTests(ITestOutputHelper output) : base(output)
    {
    }
}
```

### 2. 使用助手方法
```csharp
[Fact]
public void MyTest()
{
    // 执行测试文件
    var (interpreter, exception) = ExecuteCodeFile("my_test.old8");

    // 验证没有异常
    Assert.Null(exception);

    // 验证变量值
    AssertVariableValue(interpreter, "result", expectedValue);
}
```

### 3. 创建临时文件
```csharp
[Fact]
public void TestWithTempFiles()
{
    // 创建临时模块文件
    CreateTempModuleFile("test_module.old8", moduleContent);
    CreateTempModuleFile("my_test.old8", testContent);

    // 执行测试
    var (interpreter, exception) = ExecuteCodeFile("my_test.old8");

    // 验证结果
    // ...
}
```

## 测试原则

### 1. 隔离性
- 每个测试应该独立运行
- 使用临时文件避免测试间相互影响
- 自动清理测试产生的文件

### 2. 可读性
- 测试名称应该清楚描述测试内容
- 使用 Arrange-Act-Assert 模式
- 添加适当的注释说明测试目的

### 3. 可维护性
- 使用基类提供的通用功能
- 避免重复代码
- 参数化测试减少重复

### 4. 全面性
- 覆盖正常流程和异常情况
- 测试边界条件
- 包含性能和安全考虑

## 已完成功能

以下测试类别已经完全实现：

✅ **基础导入功能**
- `SimpleImportTests.cs` - 基础模块导入测试
- `AliasImportTests.cs` - 别名导入功能测试
- `SelectiveImportTests.cs` - 选择性导入测试
- `WildcardImportTests.cs` - 通配符导入测试

✅ **高级导入功能**
- `LazyImportTests.cs` - 延迟导入测试
- `ConditionalImportTests.cs` - 条件导入测试
- `DynamicImportTests.cs` - 动态导入测试

✅ **错误处理**
- `ImportErrorTests.cs` - 导入错误测试
- `CircularDependencyTests.cs` - 循环依赖测试

✅ **性能测试**
- `ImportPerformanceTests.cs` - 导入性能测试

✅ **集成测试**
- `RealWorldUsageTests.cs` - 真实使用场景测试
- `ComplexImportScenariosTests.cs` - 复杂导入场景测试

## 可能的扩展功能

如果需要进一步扩展测试覆盖范围，可以考虑：

1. **NamespaceImportTests.cs** - 命名空间导入测试
2. **ValidationErrorTests.cs** - 验证错误测试
3. **CompatibilityTests.cs** - 版本兼容性测试
4. **SecurityTests.cs** - 安全性测试
5. **InternationalizationTests.cs** - 国际化测试

## 贡献指南

添加新测试时请遵循：

1. 将测试放在适当的类别目录中
2. 继承 `ModuleImportTestBase` 基类
3. 使用清晰的测试命名
4. 添加适当的文档注释
5. 确保测试的独立性和可重复性
6. 考虑边界情况和错误条件

## 性能基准

当前性能测试基准：

- 批量导入 10 个模块：< 5 秒
- 重复导入缓存：< 1 秒
- 大模块导入：< 10MB 内存
- 并发导入：线程安全
- 深度嵌套：< 3 秒

这些基准会根据实际实现情况进行调整。