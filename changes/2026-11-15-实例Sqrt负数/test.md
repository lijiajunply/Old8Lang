# 实例 `Sqrt()` 的负数语义统一 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「`(-1.0).Sqrt()` 跨模式行为不一致」。

## 1. 现象

```old8
PrintLine((-1.0).Sqrt())   // -il / -vm: NaN
PrintLine((4.0).Sqrt())    // -il / -vm: 2
```

而标准库写法 `Math.Sqrt(-1.0)` 在 IL 下抛
`RUNTIME_ERROR: 平方根函数的参数必须是非负数`（`MathLib.Sqrt` 有校验）；
解释器则没有 `.Sqrt()` 实例方法（`ATTRIBUTE_ERROR`），文档只写 `Math.Sqrt(value)`。

`EdgeCases/UnexpectedInputsTests.NegativeSquareRoot` 按「负数开方会抛异常」编写，
因此 IL 返回 NaN 时失败。

## 2. 决策

实例 `Sqrt()` 是历史语言用法，但与 `Math.Sqrt` 必须同语义：**负数报错**（不是 NaN）。

## 3. 修复

1. 新增 `IlNumericHelper.Sqrt(double)`（Runtime）：负数抛
   `ArgumentOutOfRangeException「平方根函数的参数必须是非负数」`，与 `Old8LangLib.MathLib.Sqrt` 一致；
2. IL 的 `DotOperatorILHelper` 实例 `Sqrt` 特判改调该助手（原直接 `Math.Sqrt`）；
3. 虚拟机 `InvokeTypeMethod` 在分派前拦截 `Sqrt`（接收者为 `double/int/long/float/decimal`）
   并走同一助手。

解释器没有补实例方法：文档口径仍是 `Math.Sqrt(value)`，避免扩大 API 面。

## 4. 验证

### 4.1 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `(-1.0).Sqrt()` | `ATTRIBUTE_ERROR`（无该方法） | `平方根函数的参数必须是非负数` | 同样的错 |
| `(4.0).Sqrt()` | 无该方法 | 2 | 2 |
| `(16).Sqrt()` | 无该方法 | 4 | 4 |

### 4.2 回归

`Compiler.EdgeCases` 的失败集与改动前逐项 diff 一致（22 项）；`NegativeSquareRoot`
在改动前后都能通过（测试里 try/catch + 断言的具体路径触发了异常），因此测试计数不变，
但 CLI 层面 `(-1.0).Sqrt()` 已从 `NaN` 变为与 `Math.Sqrt` 相同的报错。整组对比见 `changes/Todo.md`。

## 5. 遗留

解释器仍不支持 `.Sqrt()` 实例方法。若后续要补齐，应同时实现 `Abs` / `Log` 等
数值实例方法的校验语义，而不是只加一个 `Sqrt`。
