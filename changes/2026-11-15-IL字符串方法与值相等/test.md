# IL 字符串实例方法与语言值相等比较 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」（数据来源：
`TestFiles/InterpreterTests` 语料扫描）。

## 1. 现象

`TestFiles/InterpreterTests/class_modifiers_test.old8` 在 IL 模式下生成无效 IL。
二分后最小复现：

```old8
PrintLine("=".Repeat(5))
```

另外 `"a".Repeat(1) == "a"` 在 IL 下为 `false`（解释器/虚拟机为 true）。

## 2. 根因

### 2.1 `StringRepeatMethod.GenerateIlInternal` 没有包装操作数

```csharp
instance.Accept(CreateCompiler(...));        // 原生 string
parameters[0].Accept(CreateCompiler(...));   // 原生 int
Call StringRepeatMethod.RepeatHelper(StringLangValue, IntLangValue)
```

形参是语言值，实参是 CLR 原生值，调用点生成无效 IL。

### 2.2 语言值的 `==` 用了 `object.Equals` 绑定

`ComparisonOpHelper.GenerateEquals` 的语言值分支：

```csharp
var equalsMethod = typeof(LangValueType).GetMethod("Equals", [typeof(LangValueType)])!;
```

`LangValueType` 只定义了 `Equal(LangValueType)`，没有 `Equals(LangValueType)`；
`GetMethod` 的默认绑定按可赋值规则命中 `object.Equals(object)`。而
`StringLangValue` 等只重写了 `Equal`，于是 `Equals` 走引用比较：
`"a".Repeat(1)`（StringLangValue）与 `Wrap("a")` 是两个不同实例 → false。

`GenerateNotEquals` 也没有语言值分支，直接 `Ceq` 比较引用。

## 3. 修复

1. `StringRepeatMethod.GenerateIlInternal` 改用
   `IlValueBridge.EmitLoadWrapped(instance/parameters[0])`，先包装再调用 helper；
2. `GenerateEquals` 的语言值分支改为调用 `LangValueType.Equal`；
3. `GenerateNotEquals` 增加语言值分支（同样 `Equal` 后取反）。

涉及文件：

- `Old8Lang.Runtime/InstanceMethods/Implementations/String/StringRepeatMethod.cs`
- `Old8Lang.Compiler/OperationHelpers/ComparisonOpHelper.cs`

## 4. 验证

### 4.1 新增回归测试

- `Old8Lang.Tests/Compiler/Expressions/StringInstanceMethodIlTests.cs`（1 项，三个断言）：
  `"ab".Repeat(3)`、`"a".Repeat(2) == "aa"`、`"=".Repeat(4) == "===="`；
- 语言值相等由该用例间接覆盖（`Repeat` 的返回值是 `StringLangValue`）。

修复前 `"=".Repeat(5)` 生成无效 IL、`"a".Repeat(1) == "a"` 为 false；修复后均正确。

### 4.2 CLI 对比

| 表达式 | 修复前 | 修复后 | 解释器 |
|---|---|---|---|
| `"=".Repeat(5)` | 无效 IL | `=====` | `=====` |
| `"a".Repeat(1) == "a"` | false | true | true |
| `"a" == "a"` | true | true | true |
| `"a" != "b"` | true | true | true |

### 4.3 语料

`TestFiles/InterpreterTests/class_modifiers_test.old8` 由 invalid program 变为正常退出（exit 0）。

## 5. 遗留

其他按「原生实参 + 语言值 helper」手写 IL 的实例方法如果同样漏了 `EmitLoadWrapped`，
会在传原生值时生成无效 IL；本轮只修了已复现的 `Repeat`。
