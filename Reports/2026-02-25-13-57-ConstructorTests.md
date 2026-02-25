# 构造函数嵌套对象初始化测试报告

- 日期：2026-02-25
- 范围：Old8Lang.Tests（解释器构造函数相关）
- 目标：修复 `Constructor_WithNestedObjects_InitializesCorrectly` 失败问题，并验证回归

## 背景与根因

在 `Person.init` 中执行 `this.address <- Address(street, city)` 时，右侧 `Address(...)` 会触发嵌套构造过程。由于 `CallInit` 创建的执行作用域在仅有全局作用域时会把 `this` 写入共享的全局作用域，导致外层 `this` 被嵌套构造覆盖，进而把 `address` 字段赋值错误地应用到 `Address` 实例上，引发 `AttributeError`。

## 修复内容

- 调整 `AnyLangValue.CallInit` 中 init 执行作用域的创建顺序：先创建子作用域，再写入 `this`，避免污染共享全局作用域。
- 新增回归用例 `Constructor_NestedConstruction_DoesNotOverwriteThis`，覆盖“构造函数里先 new 再继续使用 this 赋值”的场景。

## 测试结果

### 1) 单测：仅运行失败用例

- 命令：`dotnet test Old8Lang.Tests --configuration Debug --filter "FullyQualifiedName~Constructor_WithNestedObjects_InitializesCorrectly"`
- 结果：总计 1，成功 1，失败 0

### 2) 单测：ConstructorTests 全集

- 命令：`dotnet test Old8Lang.Tests --configuration Debug --filter "FullyQualifiedName~Old8Lang.Tests.Interpreter.Classes.ConstructorTests"`
- 结果：总计 16，成功 16，失败 0

### 3) 全量单测（信息性）

- 命令：`dotnet test Old8Lang.Tests --configuration Debug`
- 结果：测试运行中止，出现大量失败（以 `Old8Lang.Tests.Compiler.Expressions.ArithmeticTests` 为主，包含 `InvalidProgramException`/`ZeroDivisionError` 等），与本次修复目标无直接关联。

