# 字符串实例方法的 IL 操作数包装与谓词返回类型 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 字符串方法操作数包装」「其他按原生实参 + 语言值 helper 手写 IL 的实例方法」。

## 1. 现象

IL 模式下，接收者为**原生 string** 的常见方法会生成无效 IL，或在运行期访问越界崩溃：

```old8
PrintLine("abc".StartsWith("a"))
```

```
[编译错误] IL 验证失败：Common Language Runtime detected an invalid program
```

`result <- $"{line}!"; result.StartsWith("hello")` 则会
`System.AccessViolationException`（进程级崩溃，不只是测试失败）。

## 2. 根因

`Old8Lang.Runtime/InstanceMethods/Implementations/String/` 下 19 个方法的
`GenerateIlInternal` 直接 `CreateCompiler(...)` 加载原生 string / int，
再调用形参为 `StringLangValue` / `IntLangValue` 的 helper：

```csharp
instance.Accept(CreateCompiler(...));        // 原生 string
parameters[0].Accept(CreateCompiler(...));
Call StartsWithHelper(StringLangValue, StringLangValue)   // 无效 IL / 越界读
```

另外 `StartsWith` / `EndsWith` / `Contains` 的 `GetReturnTypeInternal` 与 helper
都返回 `BoolLangValue`，而 `Assert.True(...)`、`if` 等布尔上下文按原生 `bool` 处理，
导致 `Assert.True("abc".StartsWith("a"))` 无效 IL、`x <- ...; Assert.True(x)` 报
「Assert 方法 'True' 未找到」。

## 3. 修复

1. 19 个字符串实例方法的操作数统一改用
   `IlValueBridge.EmitLoadWrapped(instance/parameters[i], ...)`（原生值先包装成语言值）；
2. `StartsWith` / `EndsWith` / `Contains` 的 helper 返回原生 `bool`，
   `GetReturnTypeInternal` 同步为 `typeof(bool)`，与其它 25 个返回 bool 的实例方法一致；
3. 13 个返回字符串的方法（`Trim` / `ToUpper` / `Replace` / `Substring` /
   `PadLeft` / `ToBase64` / `FromBase64` / `Reverse` 等）的 helper 返回原生 `string`，
   `GetReturnTypeInternal` 同步为 `typeof(string)`，避免 `Assert.Equal` 拿到
   `StringLangValue` 的带引号显示（`'"aXc"'`）。

涉及文件：`Old8Lang.Runtime/InstanceMethods/Implementations/String/` 共 19 个方法文件。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/Compiler/Expressions/StringInstanceMethodIlTests.cs` 增加
`StringMethods_OnNativeLiteralReceiver_Work`，覆盖：

- `StartsWith` / `EndsWith` / `Contains`（谓词返回 bool，可用于 `Assert.True`）；
- `IndexOf` / `Replace` / `Split` / `Trim` / `ToLower` / `ToUpper` / `Substring` /
  `Reverse` / `PadLeft` / `ToCharArray` / `ToBase64`；
- 模板结果（原生 string）上的链式 `StartsWith` / `Contains`。

修复前该用例在创建委托时报 `InvalidProgramException`；修复后通过。

### 4.2 CLI 对比

同一段字符串方法调用在 `-f` / `-il` / `-vm` 下输出完全一致（14 行）；
修复前 `-il` 直接无效 IL。

### 4.3 回归面

整组对比见 `changes/Todo.md` 记录；期间同时修复了模板解析的进程挂死（见
`changes/2026-11-15-模板插值死循环/test.md`）。

## 5. 遗留

- `Dictionary` / `List` / `Array` / `Char` 等命名空间下仍有约 100 个
  `GenerateIlInternal` 使用裸 `CreateCompiler`，其中形参是语言值、实参是原生值的
  同样会生成无效 IL；本轮只覆盖了字符串命名空间，其余未逐一核对；
- `Split` / `ToCharArray` 仍返回 `ListLangValue`，与原生列表断言混用时需注意显示形式。
