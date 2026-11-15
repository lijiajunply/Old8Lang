# IL 字符串拼接用 CLR ToString 导致显示不一致 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」之后的结果错误核实。

## 1. 现象

`string + X` 在 IL 模式下直接调用 `string.Concat(object, object)`，走 CLR `ToString`：

| 表达式 | 解释器 | IL（修复前） |
|---|---|---|
| `"x" + true` | `xtrue` | `xTrue` |
| `"x" + false` | `xfalse` | `xFalse` |
| `"x" + {1, 2}` | `x[1, 2]` | `xSystem.Collections.Generic.List…`（CLR 类型名） |
| `"x" + {1, "a"}` | `x[1, "a"]` | 同上（类型名） |
| `"x" + {"a": 1}` | `x{"a": 1}` | `xSystem.Collections.Generic.Dictionary…` |
| `"x" + 1` / `"x" + 1.5` | `x1` / `x1.5` | 一致 |

`PrintLine(true)` 早已修成小写 `true`（见 `2026-11-11-11-11-IL打印格式测试.md`），
但拼接路径没有走同一套显示规则。

## 2. 根因

`CompilerVisitor.VisitOperation` 的 `LangTokenType.Plus` 字符串分支：

```csharp
operation.Left?.Accept(...);  if (leftType != typeof(string)) Box(leftType!);
operation.Right?.Accept(...); if (rightType != typeof(string)) Box(rightType!);
ilGenerator.Emit(OpCodes.Call, typeof(string).GetMethod("Concat", [typeof(object), typeof(object)])!);
```

`Concat` 内部调用 `object.ToString()`：`bool` 得到 `True/False`，原生 `List<object>` 未重写
`ToString` 于是得到类型名。解释器则走 `LangValueType` 的显示规则（小写布尔、语言集合格式）。

## 3. 修复

两个非 `string` 操作数在拼接前先转成语言显示文本：

1. 值类型操作数先 `box`；
2. 调用 `VMValueFormatter.ToDisplayString(object?)`（`PrintLine`/`Assert` 的字符串参数已在用同一助手）；
3. 最后调用 `string.Concat(string, string)`。

涉及文件：`Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`。

`"x" + null` 仍输出 `xnull`——与已有的「`null` 显示大小写」条目一致（IL/VM 用 `null`）。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Expressions/StringConcatDisplayTests.cs`（4 项）：

- `BoolOperand_UsesLanguageLowerCaseDisplay`
- `ListOperand_UsesLanguageCollectionDisplay`
- `DictionaryOperand_UsesLanguageDictionaryDisplay`
- `ScalarAndStringOperands_KeepExistingBehavior`

修复前 4 项中 3 项失败（标量/字符串用例本来就通过）；修复后 4 项全部通过。

### 4.2 CLI 对比

`disp4.old8`（false / 列表含字符串 / 字典）修复后 `-f` 与 `-il` 输出逐行一致：

```
xfalse
x[1, "a"]
x{"a": 1}
```

### 4.3 回归面

19 个 `Compiler.*` 子组逐组运行：失败数与修复前完全一致
（Async 54、Basic 13、Classes 14、Collections 6、EdgeCases 22、Error 12、Exceptions 8、
Expressions 9、Extern 3、Functions 53、Integration 3、Types 15，其余全通过）。
`TestFiles/CompilerTests/` 的 157 个用例 IL 扫描仍为段错误 0、invalid program 0、超时 0。

## 5. 遗留

1. 虚拟机模式仍用 CLR `ToString` 拼接：`"x" + true` 输出 `xTrue`、`"x" + {1, 2}` 输出
   `x{1, 2}`；与解释器/IL 不一致，属虚拟机侧显示规则缺口，已记入 `changes/Todo.md`。
2. `null` 的显示大小写仍是三模式不统一（解释器 `Null`、IL/VM `null`）。
