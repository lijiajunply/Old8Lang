# IL 模式 match 表达式返回垃圾值 / 装箱顺序错误 —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「`Compiler/Collections` 与表达式组既有 IL 失败」中的
`MatchExpression_*` 与 `Match_*`。

## 1. 现象

```old8
func getNumber(x:int) -> int {
    return match x {
        case 0 -> 100
        case 1 -> 200
        case 2 -> 300
        case _ -> -1
    }
}
PrintLine(getNumber(0))
```

| 模式 | 输出 |
|---|---|
| `-f` | `100` |
| `-il` | `853912040`（每次运行都是不同的垃圾整数） |
| `-vm` | `100` |

## 2. 根因（两处）

1. **结果类型不一致**：`VisitMatchExpression` 把每个分支的结果装箱后存进
   `object` 局部变量，最后 `ldloc` 直接返回——栈上是**装箱引用**；而
   `TypeInferenceVisitor.VisitMatchExpression` 返回第一个分支的类型（这里是 `int`）。
   `return` 阶段看到「期望 int、推断 int」于是不做转换，直接把引用当整数返回，
   调用方读到的是对象头指针的位模式，于是出现 853912040 这类数字。
2. **比较时装箱顺序错误**：非 int/bool/char/string 的匹配先把 match 值和 pattern
   一起压栈、再统一 `box`，`box` 只作用于栈顶，match 值保持未装箱，
   `object.Equals(object, object)` 收到的第一个参数不是对象引用，生成无效 IL。
   （与 2026-11-14 比较运算符那处是同一类错误。）

## 3. 修复

- `CompilerVisitor.VisitMatchExpression`：
  - 三个比较分支各自「加载左操作数 → 需要时装箱 → 加载 pattern → 需要时装箱」；
  - 结尾 `ldloc resultLocal` 之后，按推断出的 match 静态类型做 `unbox.any`，
    与装箱存储保持一致。
- `TypeInferenceVisitor.VisitMatchExpression`：改为「所有分支结果类型一致时取该类型，
  否则退回 `object`」，不再只看第一个分支——混合类型的 match 在 IL 里统一按
  `object` 存储，推断成具体值类型会让调用方错误地 `unbox.any`。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| 整数分支 `match x { case 0 -> 100 ... }` | 垃圾整数 | `100` / `300` / `-1` |
| 混合类型分支（int / string / bool） | 垃圾值 | `100` / `one` / `true` |
| 字符串与布尔 match | 正常 | 正常 |
| `g(0) + g(1)`（match 结果参与运算） | 结果错误 | `30` |
| `Compiler.Expressions.MatchTests` | 3 项失败 | **17/17 通过** |
| `Collections.InExpression.MatchExpressionTests` | 2 项失败 | 27/28 通过 |

组级：`Compiler.Expressions` 41→38 失败、`Compiler.Collections` 7→6 失败；
`Basic` 16/185、`Statements` 3/183、`Types` 50/51、`Functions` 109/57、
`EdgeCases` 77/22 与修复前一致。

## 5. 遗留

`MatchExpressionEnhancedTests` 仍有 25 项失败，都是 IL 的 `VisitMatchExpression`
**没有实现**的模式种类：范围模式（`Match_Range_*`）、类型模式（`Match_TypeMatch_*`）、
元组解构（`Match_TupleDeconstruction_*`）与 guard（`Match_Guard_*`）。
当前实现只识别通配符、变量绑定和普通值比较，这些模式会被当成普通值比较而永远不匹配。
属功能缺口，已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il match.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Expressions.MatchTests"
```
