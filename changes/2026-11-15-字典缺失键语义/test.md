# 字典缺失键语义统一为 KEY_ERROR —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「字典缺失键的行为三模式不一致」。

## 1. 现象

```old8
d <- {"a": 1}
PrintLine(d["missing"])
```

| 模式 | 修复前 |
|---|---|
| 解释器 | 返回 `Null`（`== null` 为 true） |
| IL | 原生 `Dictionary` 分支抛 CLR `The given key ... was not present`；`dict()` 的 `DictionaryLangValue` 分支返回 null |
| 虚拟机 | `KEY_ERROR` |

`d.GetValue("missing")` 同样不一致：解释器抛 CLR `KeyNotFoundException`，VM 抛带自定义消息的
`KeyNotFoundException`。

## 2. 决策

缺失键**属于错误**（`KEY_ERROR`）：

- 文档提供了 `GetOrElse` / `TryGet` 作为安全访问，索引语法应直接报错；
- IL 与虚拟机的既有测试（`DictionaryKeyNotFound_ThrowsRuntimeException`、
  `UnexpectedInputs_DictionaryKeyNotFound_ThrowsException`）都按报错编写；
- 解释器返回 null 会让拼写错误的键静默变成 null，后续操作才报 NRE，难以定位。

## 3. 修复

1. `DictionaryLangValue.Get`：找不到键时抛 `KeyError(this, key.ToDisplayString())`；
2. `DictGetValueMethod`：解释器与 VM 分支都抛 `KeyError`；
3. IL 原生字典索引：两条路径（泛型 `Dictionary<K,V>` 与 `Dictionary<string, object>`）改调
   `IlIndexHelper.GetDictionaryItem`，内部 `Contains` 检查后抛 `KeyError`；
   泛型值类型结果仍按需 `unbox.any`。

涉及文件：

- `Old8Lang.Runtime/AST/Expression/Value/Collections/DictionaryLangValue.cs`
- `Old8Lang.Runtime/InstanceMethods/Implementations/Dictionary/DictGetValueMethod.cs`
- `Old8Lang.Runtime/Compiler/CodeGeneration/IlIndexHelper.cs`
- `Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Interpreter/Collections/DictionaryMissingKeyTests.cs`（3 项）：
  索引 / `GetValue` 抛 `KeyError`，`GetOrElse` 仍返回默认值；
- `Old8Lang.Tests/Compiler/Collections/DictionaryMissingKeyIlTests.cs`（3 项，同上，断言消息含 `KEY_ERROR`）；
- `Old8Lang.Tests/VirtualMachine/Collections/VMDictionaryMissingKeyTests.cs`（3 项，同上）。

修复前解释器两项返回 null / CLR 异常、IL 一项抛 CLR 异常；修复后 9 项全部通过。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `d["missing"]` | `KEY_ERROR 键 'missing' 不存在` | 同左 | 同左 |
| `d.GetValue("missing")` | 同左 | 同左 | 同左 |
| `d.GetOrElse("missing", 42)` | 42 | 42 | 42 |

### 4.3 回归面

整组对比记录在 `changes/Todo.md`；`Compiler`、解释器与虚拟机均无新增失败。

## 5. 遗留

`d.foo`（点号访问字典键）内部同样走 `Get`，缺失时现在也会抛 `KEY_ERROR`，不再返回 null；
文档未把点号键访问列为正式语法，建议改用 `d["foo"]`。
