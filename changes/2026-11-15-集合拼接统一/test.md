# 数组 / 列表 `+` 拼接统一到三种模式 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「数组 / 列表 `+` 拼接只有虚拟机支持」。

## 1. 现象

```old8
a <- [1, 2]
b <- [3, 4]
PrintLine((a + b).ToStr())     // 解释器：INVALID_OPERATION_ERROR，虚拟机：[1, 2, 3, 4]

l1 <- {1, 2}
l2 <- {3, 4}
PrintLine((l1 + l2).ToStr())   // 三模式都报错
```

同一条语言表达式在三种模式下结果不一致，`Compiler/Collections/ArrayTests.ArrayConcatenation`
也因此在 IL 下失败。

## 2. 决策

`+` 对**同类型**集合按顺序拼接（保留左侧顺序），这是虚拟机已有的行为，本轮把它作为语言语义
统一到解释器与 IL：

- 数组 + 数组 → 数组；
- 列表 + 列表 → 列表；
- 混合（数组 + 列表）仍视为不支持（三种模式一致报错）。

## 3. 修复

1. 解释器：新增 `ArrayLangValue.Plus` / `ListLangValue.Plus`，同类型时返回拼接后的新集合，
   原集合不被修改；其它类型仍走基类的「不支持加法」错误；
2. 虚拟机：`Add` 辅助增加原生 `List<object?>` + `List<object?>` 拼接（数组拼接原有）；
3. IL：`VisitOperation` 的 `+` 分支在字符串拼接之后、数值加法之前识别集合类型：
   数组走 `LangCollectionOperations.ConcatArrays`，`List<T>` 走 `ConcatLists`；
4. 新增共享运行期助手 `Old8Lang.Runtime.LangCollectionOperations`。

涉及文件：

- `Old8Lang.Runtime/AST/Expression/Value/Collections/ArrayLangValue.cs`
- `Old8Lang.Runtime/AST/Expression/Value/Collections/ListLangValue.cs`
- `Old8Lang.Runtime/Runtime/LangCollectionOperations.cs`（新增）
- `Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`
- `Old8Lang.VirtualMachine/VM/Helpers/VirtualMachine.Helpers.Arithmetic.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Interpreter/Collections/CollectionConcatTests.cs`（3 项）：
  数组拼接、列表拼接、混合类型仍报错；
- `Old8Lang.Tests/Compiler/Collections/CollectionConcatIlTests.cs`（2 项）：
  IL 下数组/列表拼接（长度、显示、原集合不变）；
- `Old8Lang.Tests/VirtualMachine/Collections/VMCollectionConcatTests.cs`（2 项）：
  虚拟机原生数组/列表拼接。

修复前：解释器两项报 `INVALID_OPERATION_ERROR`、IL 两项失败、VM 列表项失败；
修复后 7 项新增 + 原有 `ArrayConcatenation` 全部通过。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `[1, 2] + [3, 4]` | `[1, 2, 3, 4]` | `[1, 2, 3, 4]` | `[1, 2, 3, 4]` |
| `{1, 2} + {3, 4}` | `[1, 2, 3, 4]` | `[1, 2, 3, 4]` | `[1, 2, 3, 4]` |
| `[1, 2] + {3, 4}` | 报错 | 报错 | 报错 |

### 4.3 回归面

`Compiler.Collections` 组失败数 6 → 5（`ArrayConcatenation` 转通过）；
其余子组与解释器/虚拟机测试的整组对比记录在 `changes/Todo.md`。

## 5. 遗留

混合数组/列表的拼接没有定义；若后续要支持，需要先统一结果类型（列表还是数组）。
