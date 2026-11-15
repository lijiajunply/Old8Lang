# null 显示与 `null.ToStr()` 的跨模式统一 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「`null` 的显示大小写跨模式不一致」「`null.ToStr()` 只在解释器可用」。

## 1. 现象

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `PrintLine(null)` | `Null` | `null` | `null` |
| `null.ToStr()` | `Null` | `RUNTIME_ERROR` | `NULL_REFERENCE_ERROR` |
| `$"v={x}"`（x 为 null） | `v=Null` | `v=`（空串） | `v=null` |

`NullLangValue.Converse(String)` 返回的又是 `"null"`，文档中也有 `Task(Completed: Null)` 的写法，
口径不统一。

## 2. 决策

统一为**小写 `null`**：

- 与字面量写法、JSON 风格一致；
- IL/虚拟机已经是 `null`，只需改解释器；
- 类型名仍由 `TypeToString()` 返回 `Null`（`type(null)` 不受影响）。

## 3. 修复

1. `NullLangValue.ToString()` 改为 `"null"`（影响 `PrintLine`、集合元素显示、`ToStr()`）；
2. IL 的 `ToStr()`：
   - 静态发射路径加空引用分支（`dup / brtrue / pop / ldstr "null"`）；
   - `InvokeDynamicObjectMethod`（静态类型为 `object` 的链式调用）在接收者为 null 时
     对 `ToStr` 返回 `"null"`，不再抛 `NullReferenceException`；
3. IL 字符串插值：非字符串元素先经 `VMValueFormatter.ToDisplayString`，
   `string.Concat` 不再把 null 变成空串（同时统一 bool/集合的显示）；
4. 虚拟机：`CallMethod` 与 `InvokeTypeMethod` 的空引用检查里放行
   `ToStr`（返回 `"null"`），其他方法仍报 `NULL_REFERENCE_ERROR`。

涉及文件：

- `Old8Lang.Runtime/AST/Expression/Value/Primitives/NullLangValue.cs`
- `Old8Lang.Compiler/OperationHelpers/DotOperatorILHelper.cs`（两处）
- `Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`（字符串模板）
- `Old8Lang.VirtualMachine/VM/Instructions/VirtualMachine.Instructions.Exception.cs`
- `Old8Lang.VirtualMachine/VM/Helpers/VirtualMachine.Helpers.ReflectionAndTasks.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Interpreter/Expressions/NullDisplayTests.cs`（1 项）：
  `x.ToStr()`、插值、`TypeToString()` 仍是 `Null`；
- `Old8Lang.Tests/Compiler/Expressions/NullDisplayIlTests.cs`（1 项，3 个断言）；
- `Old8Lang.Tests/VirtualMachine/Expressions/VMNullDisplayTests.cs`（1 项）；
- `UnionTypesInterpreterTests` 的旧期望 `"Null"` 改为 `"null"`。

相关用例共 124 项通过（含 `TypeConversionTests` / 联合类型测试）。

### 4.2 CLI 对比

```
-f:  null / null / v=null
-il: null / null / v=null
-vm: null / null / v=null
```

顺带验证插值：`$"i={1} b={true} l={[1, 2]}"` → IL 与解释器都是
`i=1 b=true l=[1, 2]`（虚拟机布尔显示仍为 `True`，属另一个已知差异）。

### 4.3 回归面

整组对比记录在 `changes/Todo.md`；`Compiler` / 解释器 / 虚拟机均无新增失败。

## 5. 遗留

1. 虚拟机字符串插值的布尔显示仍是 `True/False`（解释器/IL 为 `true/false`），
   已由 Todo 中「VM 侧字符串拼接显示」条目跟踪；
2. 模板插值里写同种双引号（如 `$"s={"ab"}"`）会让词法器挂死，是独立问题，
   未在本轮处理。
