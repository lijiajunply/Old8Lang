# IL：包装后的字典显示为 `{(), ()}` —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 包装字典的 ToStr 输出空元组」。

## 1. 现象

```old8
d <- {"a": 1, "b": 2}
PrintLine(d)
PrintLine(d.ToStr())
```

| 模式 | `PrintLine(d)` | `d.ToStr()` |
|---|---|---|
| 解释器 | `{"a": 1, "b": 2}` | `{"a": 1, "b": 2}` |
| IL | `{"a": 1, "b": 2}` ✓ | `{(), ()}` ✗ |
| 虚拟机 | `{"a": 1, "b": 2}` | `{"a": 1, "b": 2}` |

字段字典 `h.lookup.ToStr()` 同样输出 `{(), ()}`。

## 2. 根因

IL 的实例方法/桥接路径用 `IlValueBridge.Wrap(IDictionary)` 把原生字典包成
`DictionaryLangValue`。该构造函数只把条目存进 `Tuples`（未求值的表达式对），
`Value` 列表为空；而 `DictionaryLangValue.ToString()` / `Get` 读的是 `Value`：

- `Value.Count == 0` 时 `ToString()` 退化为 `"{" + join(Tuples) + "}"`，
  其中的 `TupleLangValue` 还没求值，`ToString()` 得到 `()`；
- `Get` 遍历 `Value` 找不到键（本轮之前还会被当成缺失键，统一 KEY_ERROR 后会误抛）。

## 3. 修复

`IlValueBridge.Wrap(IDictionary)` 构造 `DictionaryLangValue` 后立即
`Accept(解释器)` 求值一次填充 `Value`，与同文件里元组分支（`TupleLangValue` 必须
`Accept` 一次填 `ItemValues`）保持一致。

`Unwrap` 里原本已有「Value 为空则按 Tuples 求值」的兜底，但只在还原原生值时触发；
本轮把填充提前到包装时。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Collections/WrappedDictionaryToStrIlTests.cs`（1 项）：

- `d.ToStr()` 为 `{"a": 1, "b": 2}`；
- `d["a"]` 仍为 1，`d.Keys().Count()` 为 2；
- 类字段字典 `h.lookup.ToStr()` 与索引读取。

修复前 `d.ToStr()` 得到 `{(), ()}`；修复后通过（与解释器/VM 一致）。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `{"a":1,"b":2}.ToStr()` | `{"a": 1, "b": 2}` | 同左 | 同左 |
| 字段 `lookup.ToStr()` | 同左 | 同左 | 同左 |

### 4.3 回归面

`Compiler.Collections` 失败数不变（4）；整组对比见 `changes/Todo.md`。

## 5. 遗留

`DictionaryLangValue.ToString()` 在 `Value` 为空时仍会退化为 Tuples 的拼接；
正常构造路径现在都会先填充 Value，但若有第三方代码直接 new 出空 Value 的字典，
显示仍可能异常。可考虑在该处直接返回 `{}` 或抛明确错误。
