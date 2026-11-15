# IL：两个 object 操作数的大小比较恒为 false —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 两个 object 操作数的大小比较」。

## 1. 现象

```old8
arr <- [5, 3, 8, 1, 4]
min <- arr[0]
max <- arr[0]
for item in arr {
    if item < min { min <- item }
    if item > max { max <- item }
}
```

| 模式 | `min` / `max` |
|---|---|
| 解释器 | 1 / 8 |
| IL | 5 / 5（比较恒 false） |
| 虚拟机 | 1 / 8 |

`SyncIterator_ArrayElementIteration_WorksCorrectly` 因此失败。

## 2. 根因

数组元素赋给变量后静态类型是 `object`。`ComparisonOpHelper` 的顺序比较分支：

- `object` vs 数值 有 `IsObjectVsNumericComparison` 专门处理；
- `object` vs `object` 两个都是 object 时没有动态分支，最终落到数值/引用比较
  （对引用执行 `Clt`/`Cgt`），结果恒为 false。

## 3. 修复

新增 `IlDynamicComparison.Evaluate(op, left, right)`：把两个操作数包装成语言值后调用
`LangValueType.Less/Greater/LessEqual/GreaterEqual`，与解释器语义一致。
`ComparisonOpHelper` 的 `>` / `<` / `>=` / `<=` 在两侧静态类型都是 `object` 时改走该助手
（返回 bool，不涉及赋值类型问题）。

`x < y`（object vs 数值）保持原有路径，不受影响。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Expressions/DynamicObjectComparisonIlTests.cs`（2 项）：

- for-in 求 min/max（`item < min` / `item > max`）；
- 字典取值后再互相比大小（`y > x` / `x <= y` 等四种运算符）。

修复前两项都因比较恒 false 失败；修复后通过。

### 4.2 同步迭代器用例

`SyncIterator_ArrayElementIteration_WorksCorrectly` 由失败转为通过；
另外两个生成器用例按 MODE_SUPPORT 的既有结论（IL 不支持同步生成器 yield）
改为断言编译期 `CompilerException` 含「同步生成器」。

### 4.3 回归面

`Compiler.Collections` 由 3 失败降为 0；整组对比见 `changes/Todo.md`。

## 5. 遗留

两侧都是 object 且实际是类实例、依赖自定义运算符重载的比较，会走语言值包装
（`NativeAnyLangValue`）而不是 `LangObject._lt`；这类动态重载尚未验证。
