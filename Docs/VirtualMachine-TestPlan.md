# Old8Lang 虚拟机测试计划

## 文档信息

- **文档版本**: 1.0
- **创建日期**: 2026-01-13
- **最后更新**: 2026-01-13
- **状态**: 进行中

## 概述

本文档描述了 Old8Lang 字节码虚拟机的测试计划,包括测试范围、测试策略、测试用例设计和执行进度。

### 测试目标

1. **功能完整性**: 验证虚拟机能正确执行所有支持的字节码指令
2. **正确性**: 确保虚拟机执行结果与解释器模式一致
3. **稳定性**: 验证虚拟机在各种边界情况下的稳定性
4. **性能**: 确保虚拟机性能满足预期

### 测试范围

#### 已支持的功能 (需要测试)

- ✅ 基本数据类型 (int, double, string, bool)
- ✅ 算术运算 (+, -, *, /, %)
- ✅ 逻辑运算 (&&, ||, !)
- ✅ 比较运算 (==, !=, >, <, >=, <=)
- ✅ 变量赋值和访问
- ✅ 函数声明和调用
- ✅ 控制流 (if/else, for, while)
- ✅ 字符串操作
- ✅ 集合操作 (数组, 列表, 字典)
- ✅ 异常处理 (try/catch/finally)
- ✅ 并发原语 (Mutex, Channel, Semaphore)

#### 暂不支持的功能 (暂时跳过)

- ⏸️ 类和对象 (成员访问赋值未实现)
- ⏸️ 递归函数 (存在栈溢出问题)
- ⏸️ 闭包
- ⏸️ 生成器
- ⏸️ 异步函数

## 测试策略

### 1. 测试分类

#### 单元测试
- **目标**: 测试单个功能点
- **工具**: xUnit
- **位置**: `Old8Lang.Tests/VirtualMachine/`

#### 对比测试
- **目标**: 确保虚拟机执行结果与解释器一致
- **方法**: 相同代码在两种模式下执行,比较结果

#### 性能测试
- **目标**: 验证虚拟机性能
- **工具**: BenchmarkDotNet
- **位置**: `Old8Lang.Benchmarks/`

### 2. 测试组织结构

```
Old8Lang.Tests/VirtualMachine/
├── CompileHelper.cs              # 编译辅助类
├── Functions/                    # 函数测试
│   ├── VMFunctionDeclarationTests.cs
│   └── VMFunctionCallTests.cs
├── Expressions/                  # 表达式测试
│   ├── VMArithmeticExpressionTests.cs
│   ├── VMLogicalExpressionTests.cs
│   └── VMComparisonExpressionTests.cs
├── Statements/                   # 语句测试
│   ├── VMControlFlowTests.cs
│   └── VMLoopTests.cs
├── Strings/                      # 字符串测试
│   └── VMStringOperationTests.cs
├── Collections/                  # 集合测试
│   ├── VMArrayTests.cs
│   ├── VMListTests.cs
│   └── VMDictionaryTests.cs
├── Exceptions/                   # 异常测试
│   └── VMExceptionTests.cs
└── Concurrency/                  # 并发测试
    └── VMConcurrencyTests.cs
```

## 测试用例清单

### 1. 函数测试 (Functions/)

#### VMFunctionDeclarationTests.cs ✅ (已完成)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `FunctionDeclaration_NoParameters_ExecutesCorrectly` | ✅ 通过 | 无参数函数声明和调用 |
| `FunctionDeclaration_WithParameters_ExecutesCorrectly` | ✅ 通过 | 带参数函数声明和调用 |
| `FunctionDeclaration_WithTypeAnnotations_ExecutesCorrectly` | ✅ 通过 | 带类型注解的函数 |

#### VMFunctionCallTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `FunctionCall_WithDefaultParameters` | ⏳ 待实现 | 默认参数函数调用 |
| `FunctionCall_NestedCalls` | ⏳ 待实现 | 嵌套函数调用 |
| `FunctionCall_ReturnValue` | ⏳ 待实现 | 函数返回值测试 |

### 2. 表达式测试 (Expressions/)

#### VMArithmeticExpressionTests.cs ✅ (已完成)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `ArithmeticExpression_Addition_ExecutesCorrectly` | ✅ 通过 | 简单加法 `10 + 20` |
| `ArithmeticExpression_ComplexExpression_ExecutesCorrectly` | ✅ 通过 | 复杂表达式 `(10 + 5) * 2 - 3` |

#### VMLogicalExpressionTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `LogicalExpression_And` | ⏳ 待实现 | 逻辑与 `&&` |
| `LogicalExpression_Or` | ⏳ 待实现 | 逻辑或 `||` |
| `LogicalExpression_Not` | ⏳ 待实现 | 逻辑非 `!` |

#### VMComparisonExpressionTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `ComparisonExpression_Equal` | ⏳ 待实现 | 相等比较 `==` |
| `ComparisonExpression_NotEqual` | ⏳ 待实现 | 不等比较 `!=` |
| `ComparisonExpression_GreaterThan` | ⏳ 待实现 | 大于比较 `>` |
| `ComparisonExpression_LessThan` | ⏳ 待实现 | 小于比较 `<` |

### 3. 字符串测试 (Strings/)

#### VMStringOperationTests.cs ✅ (已完成)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `StringConcatenation_TwoStrings_ExecutesCorrectly` | ✅ 通过 | 字符串拼接 `"Hello" + " World"` |
| `StringComparison_Equal_ExecutesCorrectly` | ✅ 通过 | 字符串相等比较 |

#### VMStringOperationTests.cs (待扩展)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `StringLength` | ⏳ 待实现 | 字符串长度 |
| `StringSubstring` | ⏳ 待实现 | 字符串截取 |
| `StringIndexOf` | ⏳ 待实现 | 字符串查找 |

### 4. 集合测试 (Collections/)

#### VMArrayTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `Array_Creation` | ⏳ 待实现 | 数组创建 `[1, 2, 3]` |
| `Array_Access` | ⏳ 待实现 | 数组元素访问 `arr[0]` |
| `Array_Length` | ⏳ 待实现 | 数组长度 |

#### VMListTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `List_Creation` | ⏳ 待实现 | 列表创建 `{1, 2, 3}` |
| `List_Add` | ⏳ 待实现 | 列表添加元素 |
| `List_Remove` | ⏳ 待实现 | 列表删除元素 |

#### VMDictionaryTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `Dictionary_Creation` | ⏳ 待实现 | 字典创建 `{"key": value}` |
| `Dictionary_Access` | ⏳ 待实现 | 字典元素访问 |
| `Dictionary_Keys` | ⏳ 待实现 | 获取所有键 |

### 5. 控制流测试 (Statements/)

#### VMControlFlowTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `IfStatement_TrueBranch` | ⏳ 待实现 | if 语句真分支 |
| `IfStatement_FalseBranch` | ⏳ 待实现 | if 语句假分支 |
| `IfElseStatement` | ⏳ 待实现 | if-else 语句 |
| `IfElifElseStatement` | ⏳ 待实现 | if-elif-else 语句 |

#### VMLoopTests.cs (待创建)

| 测试用例 | 状态 | 描述 |
|---------|------|------|
| `ForLoop_SimpleIteration` | ⏳ 待实现 | for 循环简单迭代 |
| `WhileLoop_Condition` | ⏳ 待实现 | while 循环条件判断 |
| `ForInLoop_Array` | ⏳ 待实现 | for-in 循环遍历数组 |
| `Break_Statement` | ⏳ 待实现 | break 语句 |
| `Continue_Statement` | ⏳ 待实现 | continue 语句 |

## 测试进度统计

### 当前进度 (2026-01-13)

| 测试类别 | 已完成 | 待实现 | 总计 | 完成率 |
|---------|--------|--------|------|--------|
| 函数测试 | 3 | 3 | 6 | 50% |
| 表达式测试 | 2 | 6 | 8 | 25% |
| 字符串测试 | 2 | 3 | 5 | 40% |
| 集合测试 | 0 | 9 | 9 | 0% |
| 控制流测试 | 0 | 9 | 9 | 0% |
| **总计** | **7** | **30** | **37** | **19%** |

### 已通过的测试

✅ **7 个测试全部通过** (通过率: 100%)

1. `FunctionDeclaration_NoParameters_ExecutesCorrectly`
2. `FunctionDeclaration_WithParameters_ExecutesCorrectly`
3. `FunctionDeclaration_WithTypeAnnotations_ExecutesCorrectly`
4. `StringConcatenation_TwoStrings_ExecutesCorrectly`
5. `StringComparison_Equal_ExecutesCorrectly`
6. `ArithmeticExpression_Addition_ExecutesCorrectly`
7. `ArithmeticExpression_ComplexExpression_ExecutesCorrectly`

## 已知问题

### 1. 递归函数栈溢出 ❌

**问题描述**: 递归函数调用导致栈溢出

**测试用例**:
```old8
func factorial(n:int) -> int {
    if n <= 1 { return 1 }
    return n * factorial(n - 1)
}
result <- factorial(5)
```

**错误信息**: `Stack overflow`

**状态**: 待修复

**优先级**: 高

### 2. 类成员访问赋值未实现 ❌

**问题描述**: 无法对类成员进行赋值操作

**测试用例**:
```old8
class Person {
    public name:string
}
p <- Person()
p.name <- "Alice"  // 错误: 不支持的赋值左侧表达式类型
```

**错误信息**: `不支持的赋值左侧表达式类型: Operation`

**状态**: 待实现

**优先级**: 中

## 下一步计划

### 短期目标 (1-2周)

1. **完成基础测试覆盖** (优先级: 高)
   - [ ] 逻辑表达式测试 (3个测试)
   - [ ] 比较表达式测试 (4个测试)
   - [ ] 控制流测试 (4个测试)
   - [ ] 循环测试 (5个测试)

2. **集合操作测试** (优先级: 高)
   - [ ] 数组测试 (3个测试)
   - [ ] 列表测试 (3个测试)
   - [ ] 字典测试 (3个测试)

3. **扩展现有测试** (优先级: 中)
   - [ ] 函数默认参数测试
   - [ ] 嵌套函数调用测试
   - [ ] 字符串方法测试

### 中期目标 (3-4周)

1. **修复已知问题**
   - [ ] 修复递归函数栈溢出问题
   - [ ] 实现类成员访问赋值

2. **高级功能测试**
   - [ ] 异常处理测试
   - [ ] 并发原语测试

### 长期目标 (1-2个月)

1. **完整功能覆盖**
   - [ ] 类和对象测试
   - [ ] 闭包测试
   - [ ] 生成器测试
   - [ ] 异步函数测试

2. **性能测试**
   - [ ] 基准测试套件
   - [ ] 与解释器模式性能对比

## 测试执行指南

### 运行所有虚拟机测试

```bash
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~VirtualMachine"
```

### 运行特定测试类

```bash
# 函数测试
dotnet test --filter "FullyQualifiedName~VMFunctionDeclarationTests"

# 字符串测试
dotnet test --filter "FullyQualifiedName~VMStringOperationTests"

# 表达式测试
dotnet test --filter "FullyQualifiedName~VMArithmeticExpressionTests"
```

### 运行单个测试

```bash
dotnet test --filter "FullyQualifiedName~FunctionDeclaration_NoParameters_ExecutesCorrectly"
```

## 总结

### 当前状态

- ✅ **测试框架已建立**: CompileHelper 辅助类和测试目录结构
- ✅ **基础测试已完成**: 7个测试全部通过
- ✅ **测试计划已制定**: 37个测试用例已规划
- ⚠️ **已知问题**: 2个关键问题待修复

### 成功标准

- [ ] 所有基础功能测试通过 (目标: 30+ 测试)
- [ ] 测试覆盖率达到 80%
- [ ] 所有已知问题已修复
- [ ] 性能测试完成

---

**文档维护**: 请在每次更新测试时同步更新本文档
