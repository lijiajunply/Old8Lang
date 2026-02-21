# 关键问题报告 - 解释器单元测试失败

**日期**: 2026-02-21
**严重程度**: 🔴 CRITICAL
**影响范围**: 解释器核心功能

## 问题概述

在执行 speckit.implement 工作流程时，发现解释器单元测试存在严重问题：

1. **栈溢出（Stack Overflow）** - 扩展方法导致无限递归
2. **多个测试失败** - 至少 3 个 Classes 相关测试失败
3. **测试进程崩溃** - 导致测试运行中止

## 测试结果

```
失败!  - 失败: 3，通过: 78，已跳过: 0，总计: 81
测试运行已中止。原因: 测试主机进程崩溃 : Stack overflow.
```

## 问题详情

### 1. 栈溢出问题

**失败测试**: `ExtensionMethodInterpreterTests.Execute_ExtensionMethodReturningDifferentType_ExecutesCorrectly`

**问题代码**:
```old8
extension int {
    func toStr() -> string {
        return this.ToStr()  // ← 这里导致无限递归
    }
}

x <- 42
str <- x.toStr()  // 调用扩展方法
```

**调用栈**（重复 214 次）:
```
Old8Lang.AST.Expression.Operation.Run(VariateManager)
  ↓
Old8Lang.AST.Statement.ReturnStatement.Run(VariateManager)
  ↓
Old8Lang.AST.Statement.BlockStatement.Run(VariateManager)
  ↓
Old8Lang.InstanceMethods.Core.ExtensionMethodWrapper.Execute(...)
  ↓
Old8Lang.AST.Expression.Value.Instance.TryExecuteInstanceMethod(...)
  ↓
Old8Lang.AST.Expression.Value.Instance.FromClassToResult(...)
  ↓
Old8Lang.AST.Expression.LangValueType.Dot(...)
  ↓
[循环回到 Operation.Run]
```

**根本原因**:
1. 用户定义扩展方法 `toStr()`
2. 在扩展方法内部调用 `this.ToStr()`（内置方法）
3. 方法查找系统可能存在以下问题之一：
   - 不区分大小写（`toStr` vs `ToStr`）
   - 扩展方法优先级高于内置方法
   - 方法解析逻辑错误

### 2. 其他失败测试

#### 测试 2: `ExtensionMethodInterpreterTests.Execute_SameMethodNameForDifferentTypes_ExecutesCorrectly`

**错误信息**:
```
Old8Lang.Error.AttributeError : 类型 'StringLangValue' 没有属性 'getValue'
位置: 15:23
```

**问题代码**:
```old8
extension int {
    func getValue() -> int {
        return this
    }
}

extension string {
    func getValue() -> string {
        return this
    }
}

x <- 10
s <- "test"
intValue <- x.getValue()  // ✓ 成功
strValue <- s.getValue()  // ✗ 失败 - 找不到方法
```

**根本原因**: 扩展方法注册或查找系统可能无法正确处理同名方法应用于不同类型的情况。

#### 测试 3: `ExtensionMethodInterpreterTests.Execute_ExtensionMethodUsingBuiltInFunctions_ExecutesCorrectly`

**状态**: 测试被栈溢出中断，未能完成

### 3. 泛型约束测试失败

**失败测试**: `GenericConstraintExtensionInterpreterTests.ComplexScenario_GenericClassWithMultipleConstraints_WorksCorrectly`

**错误信息**:
```
Expected: typeof(Old8Lang.AST.Expression.Value.IntLangValue)
Actual:   typeof(Old8Lang.AST.Expression.Value.FuncLangValue)
```

**根本原因**: 泛型约束验证或类型推断存在问题。

## 影响分析

### 直接影响
- ❌ 扩展方法功能不可用
- ❌ 无法安全地在扩展方法内调用内置方法
- ❌ 同名扩展方法应用于不同类型时失败
- ❌ 泛型约束验证不正确

### 间接影响
- ⚠️ 性能优化工作受阻（无法确保功能正确性）
- ⚠️ 用户代码可能触发栈溢出崩溃
- ⚠️ 测试覆盖率下降（3/81 测试失败）

## 根本原因分析

### 问题 1: 方法查找优先级

**位置**: `Old8Lang/AST/Expression/Value/Special/Instance.InstanceMethods.cs:28-32`

```csharp
var method = InstanceMethodRegistry.Instance.ResolveMethod(
    instanceType,
    Id.IdName,
    Ids,
    null);
```

**可能的问题**:
1. `ResolveMethod` 可能不区分大小写
2. 扩展方法优先级高于内置方法
3. 没有检测递归调用

### 问题 2: 扩展方法执行

**位置**: `Old8Lang/InstanceMethods/Core/ExtensionMethodWrapper.cs:58-96`

```csharp
public LangValueType Execute(
    LangValueType instance,
    List<LangExpression> parameters,
    VariateManager manager,
    SourcePosition position)
{
    // 创建新的作用域
    manager.AddChildren();

    // 绑定 this 关键字到实例
    manager.Set(new LangId("this"), instance);

    // 执行函数体
    function.BlockStatement.Run(manager);  // ← 这里会再次查找方法
}
```

**问题**: 在扩展方法内部调用 `this.Method()` 时，没有机制防止再次匹配到同一个扩展方法。

## 建议的修复方案

### 方案 1: 方法查找优先级调整（推荐）

**优先级顺序**:
1. 内置实例方法（最高优先级）
2. 类定义的方法
3. 扩展方法（最低优先级）

**实现位置**: `Instance.InstanceMethods.cs:TryExecuteInstanceMethod`

```csharp
private bool TryExecuteInstanceMethod(LangValueType instance, VariateManager manager, out LangValueType? result)
{
    result = null;

    // 1. 首先检查是否是内置方法（通过反射）
    var builtInMethod = FindBuiltInMethod(instance, Id.IdName, Ids.Count);
    if (builtInMethod != null)
    {
        // 使用内置方法，不查找扩展方法
        return false; // 让 FromClassToResult 的旧系统处理
    }

    // 2. 然后查找扩展方法
    var method = InstanceMethodRegistry.Instance.ResolveMethod(...);
    // ...
}
```

### 方案 2: 大小写敏感匹配

确保方法名匹配是大小写敏感的：
- `toStr` ≠ `ToStr`
- `getValue` ≠ `GetValue`

**实现位置**: `InstanceMethodRegistry.ResolveMethod`

### 方案 3: 递归检测

在扩展方法执行时，标记当前正在执行的扩展方法，防止递归调用：

```csharp
// 在 VariateManager 中添加
private HashSet<string> _executingExtensionMethods = new();

public bool IsExecutingExtensionMethod(string methodName)
{
    return _executingExtensionMethods.Contains(methodName);
}
```

### 方案 4: 扩展方法作用域隔离

在扩展方法内部，`this` 应该只能访问内置方法和类方法，不能访问扩展方法。

## 紧急行动项

### 立即执行（P0）
1. ✅ 创建此问题报告文档
2. ⏳ 暂停性能优化工作
3. ⏳ 修复栈溢出问题（方案 1 + 方案 2）
4. ⏳ 修复扩展方法类型匹配问题
5. ⏳ 验证所有 Classes 测试通过

### 短期执行（P1）
1. ⏳ 添加递归检测机制（方案 3）
2. ⏳ 添加扩展方法相关的单元测试
3. ⏳ 更新文档说明扩展方法的限制

### 中期执行（P2）
1. ⏳ 重新设计扩展方法系统架构
2. ⏳ 实现扩展方法作用域隔离（方案 4）
3. ⏳ 性能优化工作恢复

## 测试验证计划

修复后需要验证：

```bash
# 1. 运行所有 Classes 测试
dotnet test --filter "FullyQualifiedName~Interpreter.Classes"

# 2. 运行扩展方法测试
dotnet test --filter "FullyQualifiedName~ExtensionMethod"

# 3. 运行泛型约束测试
dotnet test --filter "FullyQualifiedName~GenericConstraint"

# 4. 运行完整测试套件
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj
```

## 相关文件

### 核心文件
- `Old8Lang/AST/Expression/Value/Special/Instance.cs` - 实例方法调用入口
- `Old8Lang/AST/Expression/Value/Special/Instance.InstanceMethods.cs` - 实例方法系统集成
- `Old8Lang/InstanceMethods/Core/ExtensionMethodWrapper.cs` - 扩展方法包装器
- `Old8Lang/InstanceMethods/Core/InstanceMethodRegistry.cs` - 方法注册表

### 测试文件
- `Old8Lang.Tests/Interpreter/Classes/ExtensionMethodInterpreterTests.cs` - 扩展方法测试
- `Old8Lang.Tests/Interpreter/Classes/GenericConstraintExtensionInterpreterTests.cs` - 泛型约束测试

## 结论

这是一个**阻塞性问题**，必须在继续性能优化工作之前解决。栈溢出会导致解释器崩溃，影响所有使用扩展方法的用户代码。

建议采用**方案 1（方法查找优先级调整）+ 方案 2（大小写敏感匹配）**作为快速修复方案，然后在后续版本中实现方案 3 和方案 4 以提供更完善的保护。

---

**报告人**: Claude (Kiro AI Assistant)
**审核状态**: 待审核
**下一步**: 等待用户确认修复方案
