# IL 枚举成员被按枚举类型装箱 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 枚举成员访问返回枚举类型」。

## 1. 现象

```old8
enum Color { Red <- 0, Green <- 1 }
PrintLine(Color.Red)
PrintLine(Color.Red.ToStr())

v <- Color.Red
r <- match v {
    case 0 -> "zero"
    case _ -> "other"
}
PrintLine(r)
```

| 表达式 | 解释器 | IL（修复前） | 虚拟机 |
|---|---|---|---|
| `PrintLine(Color.Red)` | `0` | `Red` | `0` |
| `Color.Red.ToStr()` | `0` | `NativeObject(Red)` | `0` |
| `match v { case 0 }` | `zero` | `other` | `zero` |

`Compiler/Collections/InExpression/MatchExpressionTests.MatchExpression_EnumValue_*`
因此失败（该用例还原本用了行分隔的枚举成员，属无效语法）。

## 2. 根因

`DotOperatorILHelper.EmitStaticMemberAccess` 对枚举成员正确发射了 `ldc.i4`，
但**返回类型写的是枚举类型** `Color`。调用方据此把 int32 按 `Color` 装箱：

- `PrintLine` 收到 boxed `Color`，CLR `ToString()` 得到成员名 `Red`；
- `match` 的主题是 boxed `Color`，与 `case 0`（int）比较不相等，落到 `_`；
- `ToStr()` 走 `object.ToString()` 得到 `NativeObject(Red)`。

## 3. 修复

1. `EmitStaticMemberAccess` 的枚举分支返回 `typeof(int)`（成员值的实际 IL 类型）；
2. `TypeInferenceVisitor` 的 `Color.Member` 在 `leftType.IsEnum` 或
   `local.ClassVar[Color]` 是枚举时推断为 `int`；
3. `MatchExpression_EnumValue_*` 用例改为文档语法：枚举成员用逗号分隔、
   `case` 用数值常量（match 不支持 `case Color.Red` 这种成员访问 pattern）。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Types/EnumValueIlTests.cs`（1 项）：
显式值 + 自增值（`Blue=2`、`Purple=3`）、`ToStr()`、算术、`match` 数值分支。

修复前 `Assert.Equal(0, Color.Red)` 就失败；修复后通过。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `PrintLine(Color.Red)` | 0 | 0 | 0 |
| `Color.Red.ToStr()` | 0 | 0 | 0 |
| `match Color.Green { case 1 -> "green" }` | green | green | green |

### 4.3 回归面

`Compiler.Collections` 4 → 3 失败（`MatchExpression_EnumValue` 转通过）；
整组对比见 `changes/Todo.md`。

## 5. 遗留

`type(Color.Red)` 在解释器返回 `Value`、IL 返回 `Int32`、VM 返回 `Int32`；
枚举值的类型名口径仍未统一，未在本轮处理。
