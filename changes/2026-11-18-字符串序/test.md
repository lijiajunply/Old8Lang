# 字符串比较与排序：统一为字典序 —— 测试报告

日期：2026-11-18
关联待办：`changes/Todo.md`「Sort 对字符串列表的跨模式差异」。

## 1. 现象

```old8
strings <- {"z", "a", "x", "b", "y", "c"}
PrintLine(strings.Sort())
PrintLine("a" < "b")
```

| 模式 | `Sort()` | `"a" < "b"` |
|---|---|---|
| 解释器 | `["c","z","a","x","b","y"]`（乱序） | `false` |
| IL | 原序不变 | `true` |
| 虚拟机 | `["a","b","c","x","y","z"]` | INVALID_OPERATION |

根因：`StringLangValue.Less/Greater/LessEqual/GreaterEqual` 用 **字符串长度** 比较：
长度相同的单字符字符串互相既不小于也不大于，解释器 QuickSort 与 IL
`LangListSortMethod.SortHelper` 都排不动；VM 的 Sort 走 CLR 字典序、但 `<` 不支持字符串。

## 2. 修复

- `StringLangValue` 的四个比较方法改为 `string.CompareOrdinal`；
- VM 的 `Greater`/`Less` 增加原生字符串分支（`CompareOrdinal`），`>=` / `<=` 复用二者；
- `Docs/MODE_SUPPORT.md` 中“字符串按长度排序”的说明同步改为字典序。

## 3. 验证

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `{"z","a","x","b","y","c"}.Sort()` | `["a","b","c","x","y","z"]` | 同左 | 同左 |
| `"a" < "b"` / `"b" < "a"` | true / false | true / false | true / false |

`Compiler.Collections` 里的 `ListSortMethod` 用例保持通过；全量回归见 `changes/Todo.md`。
