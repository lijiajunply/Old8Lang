# IL 模式 for-in 里的 switch 与 try 内的 break/continue 生成无效 IL —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

两种控制流组合在 IL 模式下直接生成无法验证的 IL：

```old8
// A: for-in 里的 switch（switch 值静态类型是 object）
for i in [1~3] {
    switch i {
        case 1 { PrintLine("one") }
        default { PrintLine("other") }
    }
}
```

```old8
// B: try 内的 break / continue
for i in [1~10] {
    try {
        if i == 7 { break }
        if i == 3 { continue }
    } catch { }
}
```

| 用例 | `-f` | `-il`（修复前） |
|---|---|---|
| A | `one other other` | `[IL001] 无效的IL代码` |
| B | 正常结束 | `[IL001] 无效的IL代码` |

失败用例：`Compiler/Statements/JumpStatementsTests` 的
`BreakStatement_WithSwitch_BreaksOutOfSwitchInLoop`、
`ContinueStatement_WithSwitch_SkipsInSwitch`、
`BreakContinue_WithErrorHandling_HandlesBreakInTryCatch`。

对照：`switch` 在顶层或 `while` 里正常（switch 值是 `int`，走 `ceq` 分支）。

## 2. 根因

**A. switch 的 object 匹配分支**：`VisitSwitchStatement` 的 else 分支按
「实例方法」调用 `object.Equals(object)`，但发射时只压了 case 值、且未装箱：

```csharp
var equalsMethod = switchValueType.GetMethod("Equals", [switchValueType]) ??
                   switchValueType.GetMethod("Equals", [typeof(object)]);
ilGenerator.Emit(OpCodes.Call, equalsMethod);   // this = ? / 实参类型不符
```

switch 值静态类型是 `object` 时（for-in 循环变量来自 `IEnumerator.Current`），
算出的方法就是 `object.Equals(object)`，栈上却只有 case 值，验证必然失败。

**B. try 内跳出受保护区域**：`break` / `continue` 一律发射 `br`。
CLR 不允许 `br` 跨越受保护区域边界，必须用 `leave`。

## 3. 修复

- `VisitSwitchStatement`：把普通匹配拆成三条自洽路径——
  值类型走 `ceq`、字符串走 `string.Equals(string, string)`、
  其它（含 `object`）逐个操作数装箱后调用 `object.Equals(object, object)`；
- `LocalManager.IsInProtectedRegion`：新增标记，`VisitTryStatement` 在生成 try 块与
  各 catch 块时置位；
- `VisitBreakStatement` / `VisitContinueStatement`：该标记为真时发射 `leave`，否则仍用 `br`。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| for-in 里的 switch | 无效 IL | `one other other` |
| switch 内 `break`（循环计数） | 无效 IL | `3`，与 `-f` 一致 |
| try 内 `break` / `continue` | 无效 IL | 正常结束 |
| 顶层 switch、while 里的 switch | 正常 | 正常（未变） |

组级：`Compiler.Statements` **3→0 失败（186/186 全通过）**；
其余 18 个 `Compiler` 子组与修复前逐组一致
（Async 43/65、Basic 185/16、Classes 95/15、Collections 168/6、EdgeCases 77/22、
Error 0/12、Exceptions 54/10、Expressions 197/35、Extern 7/3、FileHeader 29/0、
Functions 110/56、Integration 50/3、Linq 45/0、Modules 263/0、Reflection 3/14、
Threading 15/0、Types 50/51、Unit 45/0）。

非 `Compiler` 组全部通过：Interpreter 1967、VirtualMachine 1083、Parser 889、Libraries 95。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il switch_in_forin.old8
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il break_in_try.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Statements"
```
