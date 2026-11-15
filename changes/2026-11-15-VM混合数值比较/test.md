# 虚拟机混合数值比较（int / double）结果错误 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「虚拟机混合数值比较偏差」。

## 1. 现象

```old8
b <- 3
PrintLine(b >= 3.0)   // 解释器/IL: true
PrintLine(b == 3.0)   // 解释器/IL: true
PrintLine(3.0 <= b)   // 解释器/IL: true
PrintLine(2.5 < b)    // 三模式都 true
```

虚拟机输出：`false / false / false / true`。

## 2. 根因

`VirtualMachine.Helpers.Comparison.cs`：

- `Equals` 只处理同类型（`int==int`、`double==double` 等），`int == double` 落到
  `object.Equals(a, b)`，不同类型恒为 false；
- `GreaterEqual` / `LessEqual` 的实现是 `Greater(a,b) || Equals(a,b)`，
  而 `Greater` 有混合数值分支（`int > double` 正常），`Equals` 没有，
  于是 `3 >= 3.0` 变成 `false || false`。

## 3. 修复

`Equals` 在单类型分支之后增加混合数值比较：

```csharp
if (IsNumericValue(a) && IsNumericValue(b))
{
    return Convert.ToDouble(a) == Convert.ToDouble(b);
}
```

`IsNumericValue` 覆盖 `byte/sbyte/short/ushort/int/uint/long/ulong/float/double/decimal`。
`GreaterEqual` / `LessEqual` 复用 `Equals`，随之修正。

涉及文件：`Old8Lang.VirtualMachine/VM/Helpers/VirtualMachine.Helpers.Comparison.cs`。

## 4. 验证

### 4.1 新增回归测试

`Old8Lang.Tests/VirtualMachine/Expressions/VMMixedNumericComparisonTests.cs`（2 项）：

- `Equality_AcrossIntAndDouble_UsesNumericValue`
- `Ordering_AcrossIntAndDouble_MatchesInterpreter`

修复前两项失败，修复后通过。

### 4.2 回归面

`dotnet test --filter "FullyQualifiedName~Old8Lang.Tests.VirtualMachine"` 1085 项通过
（原 1083 + 新增 2）。

## 5. 遗留

`decimal` / `long` 参与混合比较时同样走 `Convert.ToDouble`，极大整数可能有精度损失；
当前语言层以 int/double 为主，未单独处理。
