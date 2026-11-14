# 字符串 `in` 运算符在解释器抛异常 / IL 恒为 false —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
PrintLine('a' in "abc")
PrintLine("b" in "abc")
```

| 模式 | 输出 |
|---|---|
| `-f` | `true` / `InvalidCastException: Unable to cast object of type 'System.String' to type 'System.Char'` |
| `-il` | `false` / `false` |
| `-vm` | `abc` / `abc` |

- 解释器：字符形式正确，字符串（子串）形式直接抛 `InvalidCastException`；
- IL：两种形式都恒为 false；
- 虚拟机：`in` 运算符根本没有实现（见 §5）。

失败用例：`Compiler/Expressions/InExpressionTests.InExpression_String_ShouldReturnTrueWhenExists`
（`'a' in "abc"`，IL 得到 false）。

## 2. 根因

1. **解释器**：`StringLangValue.In` 一律 `value.GetValue<char>()`，
   左操作数是 `StringLangValue` 时把字符串当字符拆箱，抛 `InvalidCastException`。
2. **IL**：`GenerateInOperator` 对右侧 `string` 没有专门分支，落到
   `TryGenerateGenericContains`；那里按左操作数类型（`char`）命中
   `string.Contains(char)` ——但发射时左操作数已经被 `box` 成 `object`，
   传参类型不符，运行期得到 false。特化的 `GenerateStringContains` 永远走不到。

## 3. 修复

- `StringLangValue.In`（解释器）：按左操作数类型分派——字符串按序数子串包含，
  字符按单字符包含，其它按显示文本包含；
- `InOperatorHelper.GenerateInOperator`（IL）：在通用 `Contains` 回退**之前**
  增加 `rightType == typeof(string)` 分支，走 `GenerateStringContains`；
- `InOperatorHelper.GenerateStringContains`：改为调用新增的运行期方法
  `ContainsText(string, object?)`，与解释器同一套语义（字符 / 子串 / 其它）。

## 4. 验证

| 用例 | 解释器修复前 | 解释器修复后 | IL 修复前 | IL 修复后 |
|---|---|---|---|---|
| `'a' in "abc"` | `true` | `true` | `false` | `true` |
| `'d' in "abc"` | `false` | `false` | `false` | `false` |
| `"b" in s` | 抛 `InvalidCastException` | `true` | `false` | `true` |
| `"z" in s` | 抛异常 | `false` | `false` | `false` |
| `"bc" in s` | 抛异常 | `true` | `false` | `true` |
| `"" in s` | 抛异常 | `true` | `false` | `true` |

组级：`Compiler.Expressions` 38→37 失败；`Compiler.Collections` 6/168、
`Basic` 16/185、`Statements` 3/183、`VirtualMachine` 1083/0、`Libraries` 95/0
与修复前一致；`Interpreter` 1967 通过 / 1 跳过。

## 5. 验证过程中确认的跨模式差异（未在本轮修改）

**虚拟机的 `in` 运算符未实现**：`BytecodeVisitor.VisitOperation` 的二元运算符
`switch` 里没有 `LangTokenType.In` 分支，落到 `default: Emit(OpCode.Nop)`，
左右操作数都留在栈上，于是表达式的结果就是右操作数：

```old8
PrintLine(1 in [1, 2, 3])   # -vm: [1, 2, 3]（应为 true）
PrintLine("a" in {"a": 1})  # -vm: {"a": 1}（应为 true）
```

需要新增字节码指令（列表 / 数组 / 字典 / 字符串各一套语义）并考虑字节码序列化兼容，
属于独立的虚拟机功能缺口，已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -f in.old8
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il in.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Expressions.InExpressionTests"
```
