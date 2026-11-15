# IL 链式比较 `a < b < c` 的静默错误结果 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 链式比较 `a < b < c` 的行为错误」。

## 1. 现象

```old8
a <- 5
b <- 10
c <- 15
PrintLine(a < b < c)      // -il: true（错误口径）
PrintLine(a < b < 3)      // -il: true（明显错误）
PrintLine(true > false)   // -il: true
```

解释器报 `INVALID_OPERATION_ERROR: 不支持Less操作`，虚拟机报
`无法对类型 Bool 和 Int 执行小于比较`；只有 IL 静默给出布尔结果。

## 2. 根因

链式比较按左结合解析为 `(a < b) < c`，第二段左侧是 `bool`。
`ComparisonOpHelper` 的 `<` / `>` / `<=` / `>=` 没有检查 bool 操作数，
落入数值分支后把 bool 当 int32 执行 `Clt`/`Cgt`，得到 `1 < 15 = true`。

解释器的 `BoolLangValue` 没有重写大小比较，虚拟机的比较实现也要求数值，
因此三种模式的语义应当是「布尔值不能参与大小比较」。

## 3. 修复

`ComparisonOpHelper` 增加 `RejectBooleanOrderingOperands`：`<` / `>` / `<=` / `>=`
在任一操作数是原生 `bool` 或 `BoolLangValue` 时抛
`InvalidOperationError「布尔值不支持大小比较」`（编译期）。

相等/不等比较不受影响（`true == false` 仍可用）。

## 4. 验证

### 4.1 用例调整

`ComparisonTests.ChainedComparisons_CompilesAndExecutesCorrectly` 原来注释
「5 < 10 < 15 = true < 15 = true」并断言成功——这是按 IL 的错误行为写的；
现在改为断言编译期抛错，消息包含「布尔值不支持大小比较」。
`Compiler.Expressions` 组 114 项比较相关测试通过。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `a < b < c` | `INVALID_OPERATION_ERROR` | 编译期「布尔值不支持大小比较」 | `无法对类型 Bool 和 Int 执行小于比较` |
| `true > false` | `INVALID_OPERATION_ERROR` | 同上 | 同样报错 |
| `true == false` | `false` | `false` | `false` |

### 4.3 回归面

整组对比记录在 `changes/Todo.md`；没有按 bool 大小比较编写的合法用例。

## 5. 遗留

如果将来要支持数学链式比较（`a < b and b < c` 的语法糖），需要三种模式一起实现，
并同步修改本条语义；当前口径是「链式比较不是语言特性」。
