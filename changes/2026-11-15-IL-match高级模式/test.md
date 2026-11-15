# IL 模式 match 只支持「值/通配符/变量绑定」，高级模式整批失败 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 模式 match 不支持范围 / 类型 / 元组解构模式与 guard」。

## 1. 现象

```old8
age <- 8
result <- match age {
    case [0~12] -> "Child"
    case [13~19] -> "Teen"
    default -> "Invalid"
}
```

| 模式 | 结果 |
|---|---|
| `-f` | `Child` |
| `-il`（修复前） | `Invalid`（范围模式被当成普通值比较，永远不匹配） |
| `-vm` | `Child` |

`case x:int`、`case x:int if x > 0`、`case (x, y, z)` 同样不匹配。
`Compiler.Expressions.MatchExpressionEnhancedTests` 25 项失败。

## 2. 根因

`VisitMatchExpression` 只识别通配符、变量绑定和普通值三种模式，其余模式被当作「值匹配」比较，
自然永远不相等。解释器侧的完整语义（`MatchCaseRuntimeExtensions.IsMatch`：值 / 绑定 / 元组解构 /
类型 / 范围 / guard）没有被 IL 复用。

另一处连带问题：IL 的元组是 `ValueTuple`，`IlValueBridge.Wrap` 不认识，会包成 `NativeAnyLangValue`，
解释器的元组解构逻辑（要求 `TupleLangValue`）因此也匹配不上。

## 3. 修复

- 新增 `IlMatchRegistry`（Runtime）：编译期把每个 `MatchCase` 与「绑定变量名顺序」登记进去，
  运行期 `TryMatch(id, value, out boundValues)` 直接调用解释器的 `IsMatch`——
  模式语义（含 guard 条件）与解释器完全一致，IL 不再维护第二套匹配逻辑；
- `VisitMatchExpression` 改为「按 id 询问 → 写回绑定变量（object 槽位）→ 计算分支结果」，
  分支顺序、guard 失败后继续尝试下一个分支、无匹配抛错等行为都由这套流程保证；
- `IlValueBridge.Wrap` 新增 `ITuple` 分支：把 `ValueTuple` / `Tuple` 包成真正的
  `TupleLangValue` 并经解释器填充 `ItemValues`，元组解构与元组实例方法都能识别。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `case [0~12]` 范围匹配 | 不匹配 | `Child` |
| `case x:int if x > 0`（类型 + guard） | 不匹配 | `Positive:42` |
| `case (x, y, z)` 元组解构 | 不匹配 | `1,2,3` |
| `case x:int` / `case s:string` 类型匹配 | 不匹配 | `Integer: 42` |
| `Compiler.Expressions` 组 | 35 失败 / 197 通过 | **11 失败 / 221 通过** |

其余 18 个 `Compiler` 子组逐组不变（Async 43/65、Basic 188/13、Classes 96/14、
Collections 168/6、EdgeCases 77/22、Error 0/12、Exceptions 55/9、Extern 7/3、
FileHeader 29/0、Functions 113/53、Integration 50/3、Linq 45/0、Modules 263/0、
Reflection 17/0、Statements 186/0、Threading 15/0、Types 86/15、Unit 45/0）；
非 `Compiler` 组全部通过（Interpreter 1967、VirtualMachine 1083、Parser 889、
Libraries 95、TypeSystem 65、LanguageServer 474）。

## 5. 验证中发现的跨模式差异（未修）

虚拟机在元组解构 match 上报 `Match 表达式没有匹配的分支`：

```old8
triple <- (1, 2, 3)
r <- match triple {
    case (0, 0, 0) -> "Origin"
    case (x, y, z) -> x.ToStr() + "," + y.ToStr() + "," + z.ToStr()
}
```

`-f` 与 `-il` 都输出 `1,2,3`，`-vm` 抛 VM 异常。已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il match_patterns.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Expressions.MatchExpressionEnhancedTests"
```
