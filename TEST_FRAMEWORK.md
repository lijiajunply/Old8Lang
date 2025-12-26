# Old8Lang 测试框架使用指南

## 概述

Old8Lang 测试框架提供了三个主要库:

1. **AssertLib** - 断言库,用于验证测试条件
2. **TestRunner** - 测试运行器,用于组织和运行测试
3. **MockLib** - Mock 工具库,用于创建测试替身

## 1. AssertLib 断言库

### 导入方式

```old8
native "Old8LangLib" AssertLib *
```

### 基础断言方法

#### 相等性断言

```old8
// 断言两个值相等
AssertEqual(5, 5)
AssertEqual("hello", "hello")

// 断言两个值不相等
AssertNotEqual(5, 10)
AssertNotEqual("foo", "bar")
```

#### 布尔断言

```old8
// 断言条件为真
AssertTrue(true)
AssertTrue(5 > 3)

// 断言条件为假
AssertFalse(false)
AssertFalse(5 < 3)
```

#### Null 检查

```old8
// 断言值为 null
value <- null
AssertNull(value)

// 断言值不为 null
value <- 123
AssertNotNull(value)
```

### 数值比较断言

```old8
// 大于
AssertGreater(10, 5)

// 大于等于
AssertGreaterOrEqual(10, 10)

// 小于
AssertLess(5, 10)

// 小于等于
AssertLessOrEqual(5, 5)
```

### 字符串断言

```old8
text <- "Hello, World!"

// 包含子串
AssertContains(text, "World")

// 不包含子串
AssertNotContains(text, "xyz")

// 以...开头
AssertStartsWith(text, "Hello")

// 以...结尾
AssertEndsWith(text, "!")

// 匹配正则表达式
AssertMatches(text, "^Hello")
```

### 集合断言

```old8
list <- {1, 2, 3, 4, 5}

// 集合包含元素
AssertContainsItem(list, 3)

// 集合不包含元素
AssertNotContainsItem(list, 10)

// 集合为空
emptyList <- {}
AssertEmpty(emptyList)

// 集合不为空
AssertNotEmpty(list)

// 集合长度
AssertLength(list, 5)
```

### 异常断言

**注意**: 由于 Old8Lang 的 lambda 语法限制,异常断言功能暂不可用。

### 类型断言

```old8
// 断言对象是指定类型
obj <- "test"
AssertInstanceOf(obj, typeof(string))

// 断言对象不是指定类型
AssertNotInstanceOf(obj, typeof(int))
```

## 2. TestRunner 测试运行器

### 导入方式

```old8
native "Old8LangLib" TestRunner *
```

### 基础使用

**注意**: 由于 Old8Lang 目前对 lambda 和嵌套函数的支持有限,TestRunner 的完整功能可能无法使用。建议直接使用 AssertLib 进行测试。

### 简单测试模式

推荐直接使用断言进行测试:

```old8
native "Old8LangLib" AssertLib *

PrintLine("=== 测试开始 ===")

// 测试 1: 加法
PrintLine("\n测试 1: 加法")
result <- 2 + 3
AssertEqual(5, result)
PrintLine("✓ 加法测试通过")

// 测试 2: 字符串拼接
PrintLine("\n测试 2: 字符串拼接")
str <- "Hello" + " World"
AssertEqual("Hello World", str)
PrintLine("✓ 字符串拼接测试通过")

PrintLine("\n=== 测试完成 ===")
```

## 3. MockLib Mock 工具库

**注意**: MockLib 当前由于 Old8Lang 类型系统的限制,无法直接在 Old8Lang 代码中使用。Mock 功能主要用于 C# 测试代码。

## 完整测试示例

### 示例 1: 基础功能测试

```old8
native "Old8LangLib" AssertLib *

PrintLine("=== 基础功能测试 ===")

// 数学运算测试
PrintLine("\n数学运算测试:")
AssertEqual(10, 5 + 5)
AssertEqual(15, 5 * 3)
AssertEqual(2, 10 / 5)
PrintLine("✓ 数学运算正确")

// 字符串测试
PrintLine("\n字符串测试:")
text <- "Old8Lang"
AssertContains(text, "Lang")
AssertStartsWith(text, "Old")
AssertEndsWith(text, "Lang")
PrintLine("✓ 字符串操作正确")

// 集合测试
PrintLine("\n集合测试:")
list <- {1, 2, 3}
AssertLength(list, 3)
AssertContainsItem(list, 2)
AssertNotContainsItem(list, 10)
PrintLine("✓ 集合操作正确")

PrintLine("\n=== 所有测试通过 ===")
```

### 示例 2: 比较和条件测试

```old8
native "Old8LangLib" AssertLib *

PrintLine("=== 比较和条件测试 ===")

// 数值比较
PrintLine("\n数值比较:")
AssertGreater(10, 5)
AssertLess(5, 10)
AssertGreaterOrEqual(10, 10)
AssertLessOrEqual(5, 5)
PrintLine("✓ 数值比较正确")

// 布尔逻辑
PrintLine("\n布尔逻辑:")
AssertTrue(true)
AssertFalse(false)
AssertTrue(10 > 5)
AssertFalse(5 > 10)
PrintLine("✓ 布尔逻辑正确")

PrintLine("\n=== 所有测试通过 ===")
```

## 使用建议

1. **使用 AssertLib**: AssertLib 的所有功能都可以正常使用,推荐作为主要测试工具。

2. **组织测试**: 使用 `PrintLine` 输出测试分组和结果,保持测试清晰。

3. **失败处理**: 当断言失败时,会抛出 `AssertionException`,包含详细的错误信息。

4. **测试文件命名**: 建议使用 `*_test.old8` 作为测试文件命名规范。

5. **运行测试**: 使用解释器模式运行测试:
   ```bash
   dotnet run --project Old8Lang.App -- -f test_file.old8
   ```

## 限制和注意事项

1. **Lambda 支持**: 由于 Old8Lang 的 lambda 支持有限,TestRunner 的 `Describe` 和 `It` 方法可能无法使用。

2. **Mock 限制**: MockLib 的 MockObject 类型目前无法在 Old8Lang 中直接使用,主要用于 C# 测试。

3. **可选参数**: 调用带有可选参数的方法时,建议省略可选参数的调用,直接使用两个必需参数。

4. **异常断言**: `AssertThrows` 和 `AssertNotThrows` 由于需要传递 Action 委托,在 Old8Lang 中暂不可用。

## 示例文件位置

- [basic_test.old8](basic_test.old8) - 基础断言测试示例
- [simple_test.old8](simple_test.old8) - 简单测试示例

## 总结

Old8Lang 测试框架的 AssertLib 提供了丰富的断言功能,可以满足大多数测试需求。虽然 TestRunner 和 MockLib 的部分功能受限,但通过合理组织代码和使用 AssertLib,依然可以编写高质量的测试代码。
