# 集合 `.Length` 属性的跨模式统一 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「集合 `.Length` 属性的解释器缺口」。

## 1. 现象

```old8
arr <- [1, 2, 3]
PrintLine(arr.Length)
```

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `[1,2,3].Length` | `NAME_ERROR: 名称 'Length' 未定义` | 3 | 3 |
| `{1,2}.Length` | `NAME_ERROR` | 2 | 2 |
| `"abc".Length` | `NAME_ERROR` | 3 | 3 |
| `{a:1}.Length` | `NAME_ERROR` | 1 | `ATTRIBUTE_ERROR` |

`API_REFERENCE.md` 原文写「数组没有 `.Length` 属性，用 `len(a)` 或 `a.Count()`」，
但 IL 与虚拟机早已支持，且 `Compiler/Collections` 的多个用例依赖它。

## 2. 决策

保留 `.Length` 并把它定义为语言属性（而不是从 IL/VM 收回）：

- 数组、列表、字符串、字典的 `.Length` 等价 `len(x)`；
- 字典上 `.Length` 是键值对数量；键名恰为 `"Length"` 时用 `d["Length"]` 访问；
- 三种模式行为一致，文档与用例都以该语义为准。

## 3. 修复

1. `ArrayLangValue.Dot` / `ListLangValue.Dot` / `StringLangValue.Dot`（新增 override）
   / `DictionaryLangValue.Dot` 处理 `LangId("Length")`；
2. 解释器的数组分支在 `dotRight is LangId` 时先交给 `array.Dot`，不再把 `Length`
   当普通表达式求值（那会抛 NAME_ERROR），变量下标 `arr.i` 仍由 `Dot` 内部回退处理；
3. 虚拟机属性访问补字典分支：`IDictionary` 的 `.Length` 返回 `Count`；
   其他属性名继续走反射（`Keys` / `Values` / `Count` 不受影响）。

涉及文件：

- `Old8Lang.Runtime/AST/Expression/Value/Collections/ArrayLangValue.cs`
- `Old8Lang.Runtime/AST/Expression/Value/Collections/ListLangValue.cs`
- `Old8Lang.Runtime/AST/Expression/Value/Collections/DictionaryLangValue.cs`
- `Old8Lang.Runtime/AST/Expression/Value/Primitives/StringLangValue.cs`
- `Old8Lang.Interpreter/Visitors/InterpreterVisitor.Expressions.cs`
- `Old8Lang.VirtualMachine/VM/Instructions/VirtualMachine.Instructions.Exception.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Interpreter/Expressions/LengthPropertyTests.cs`（5 项）：
  四种类型的 `.Length` 与 `len()` 一致，以及字典键 `"Length"` 与属性共存；
- `Old8Lang.Tests/Compiler/Expressions/LengthPropertyIlTests.cs`（1 项，8 个断言）；
- `Old8Lang.Tests/VirtualMachine/Expressions/VMLengthPropertyTests.cs`（4 项）。

修复前解释器 4 项全部 NAME_ERROR、VM 字典项 ATTRIBUTE_ERROR；修复后 10 项全部通过。

### 4.2 CLI 对比

```
-f: 3 2 3 1
-il: 3 2 3 1
-vm: 3 2 3 1
```

`arr.Length + 1`、`dict.Count`、`arr[i]` 等原有写法保持正常。

### 4.3 回归面

整组对比见 `changes/Todo.md` 记录；`Compiler` 的 `Collections`/`Expressions`、
解释器与虚拟机测试均未出现新增失败。

## 5. 遗留

字典的 `.Length` 会遮蔽同名键的属性形式（`d.Length` 恒为大小），已写入文档；
变量下标 `arr.i`（点号 + 变量）只有解释器支持，IL/VM 仍要求 `arr[i]`，
属于另一条跨模式差异，未在本轮处理。
