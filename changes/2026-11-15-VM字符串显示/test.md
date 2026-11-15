# 虚拟机字符串加法 / 插值的显示规则 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「字符串拼接里的 bool 显示大小写跨模式不一致」。

## 1. 现象

```old8
list <- [1, 2]
PrintLine("x" + true)            // -vm: xTrue
PrintLine("x" + (1 in list))     // -vm: xTrue
PrintLine($"i={1} b={true}")     // -vm: i=1 b=True
```

解释器与 IL 输出 `xtrue` / `i=1 b=true`。

## 2. 根因

VM 的字符串加法与插值都调用 CLR 转换：

- `VirtualMachine.Helpers.Arithmetic.Add`：`if (a is string) return ToString(a) + ToString(b);`
- `System.String::Concat`（插值实现）：`string.Concat(array.Select(ToString))`

`ToString` 对 bool 得到 `True`（CLR 口径），列表得到 `System.Int32[]`。

## 3. 修复

两处都改用语言显示格式化器 `VMValueFormatter.ToDisplayString`：

- `Add` 的字符串分支：`ToDisplayString(a) + ToDisplayString(b)`；
- `System.String::Concat`：逐元素 `ToDisplayString` 后拼接。

与解释器、IL（`EmitConcatOperand`）使用同一个格式化器，null / bool / 集合 / 字典
显示一次对齐。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/VirtualMachine/Expressions/VMStringDisplayTests.cs`（1 项）：
`"x" + b`、`"x" + null`、`"v=" + list`、`$"i={1} b={b} l={list} n={null}"`。

修复前四项分别得到 `xTrue` / `xnull` / `System.Int32[]...`；修复后与解释器一致。

### 4.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `"x" + true` | xtrue | xtrue | xtrue |
| `"x" + (1 in list)` | xtrue | xtrue | xtrue |
| `"x" + null` | xnull | xnull | xnull |
| `$"i={1} b={b} l={list} n={null}"` | i=1 b=true l=[1, 2] n=null | 同左 | 同左 |

`PrintLine(true)` / `PrintLine([1, 2])` / `PrintLine({"a": 1})` 原本就一致。
