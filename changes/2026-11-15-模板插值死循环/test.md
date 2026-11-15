# 模板插值中的嵌套引号导致解析器死循环 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「模板插值中的嵌套同种引号会让词法器死循环」。

## 1. 现象

```old8
PrintLine($"s={"ab"}")
```

`-s` / `-f` / `-il` / `-vm` 全部挂死（8 秒超时，只能杀进程），没有任何错误输出。

## 2. 根因

1. 词法器把 `$\"...\"` 当成普通字符串扫描，遇到第一个未转义 `"` 就结束，
   于是 `$"s={"ab"}"` 被截成字符串 `s={`、标识符 `ab`、字符串 `"`（不完整）；
2. `PrimaryParser.ParseStringTemplate` 拿到 `s={` 后，扫到末尾的 `{` 时
   `i + 1 < len` 不成立、也不进入 `}`/普通字符分支，循环体**没有推进 `i`**，
   形成死循环。

## 3. 修复

在模板解析循环里加两道保险：

1. `{` 位于字符串末尾时抛
   `SyntaxError「字符串模板中的花括号未闭合：缺少 '}'」`；
2. 每轮记录 `iterationStart`，若本轮结束 `i` 未前进，抛
   `SyntaxError「字符串模板无法继续解析：请检查花括号与引号是否配对」`。

涉及文件：`Old8Lang.Syntax/LangParser/Parsers/PrimaryParser.StringTemplate.cs`。

> 本次只保证**不再挂死**，没有让词法器支持插值内的同种引号。需要该写法时可在
> 插值里改用单引号或先赋值给变量。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Parser/Advanced/StringTemplateTests.cs` 增加
`ParseProgram_StringTemplateNestedQuotes_ThrowsSyntaxErrorInsteadOfHanging`：
断言抛 `SyntaxError` 且消息包含「花括号」（修复前该测试会挂住整个测试主机）。

### 4.2 手工验证

| 输入 | 修复前 | 修复后 |
|---|---|---|
| `PrintLine($"s={"ab"}")` | 挂死 | `SYNTAX_ERROR: 字符串模板中的花括号未闭合` |
| `PrintLine($"v={x}")` | 正常 | 正常 |
| `PrintLine($"i={1} b={true}")` | 正常 | 正常 |

## 5. 遗留

词法器仍不支持插值表达式内的同种双引号（`$"s={"ab"}"`），本轮只把挂死改为明确
语法错误；要支持需让词法器在 `$"..."` 中跟踪花括号深度与嵌套字符串状态。
