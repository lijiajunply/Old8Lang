# IL 模式 `Len()` 接收 object 时生成无效 IL —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「`Compiler/Collections` 与表达式组既有 IL 失败」中的
`NestedArrays_CompilesAndExecutesCorrectly`（原文记为「生成无效 IL」）。

## 1. 现象

```old8
matrix <- [[1, 2], [3, 4]]
firstRow <- matrix[0]
PrintLine(Len(firstRow))
```

| 模式 | 结果 |
|---|---|
| `-f` | `2` |
| `-il` | `[IL001] 无效的IL代码: Common Language Runtime detected an invalid program.` |
| `-vm` | `2` |

`matrix[0]` 的静态类型是 `object`（`List<object>` 的元素），`Len` 的 IL 实现走到
「静态类型是 object」分支。

## 2. 根因

`LenFunction.GenerateIlInternal` 先把参数值压栈，再按静态类型找
`Length` / `Count` 属性或字段；找不到时对 `object` 分支只发射一句 `ldc.i4.0` 就返回，
**参数值仍留在栈上**，委托因此无法通过验证。

同样的写法在 `arr1 + arr2`（列表相加被推断为 `Int32`）上表现为
`类型 Int32 没有 Length 或 Count 属性`，也不是正确的运行期语义。

## 3. 修复

`Old8Lang.Runtime/GlobalFunctions/Implementations/UtilityFunctions.cs`：

- `object` 分支改为调用新增的静态方法 `LenFunction.GetLengthDynamic(object?)`，
  参数值正好已经在栈上，不再留下悬空栈值；
- `GetLengthDynamic` 按解释器语义求长度：`string.Length` / `ILangList.GetLength()` /
  `ICollection.Count`，其它类型（含 `null`）抛 `InvalidOperationError`。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `Len(matrix[0])`（嵌套列表元素） | 无效 IL | `2`，与 `-f`/`-vm` 一致 |
| `Len(matrix)` / `Len(firstRow[0])` / `Len(len1)`（字符串/列表/字典） | 正常 | 正常 |
| `NestedArrays_CompilesAndExecutesCorrectly` | 失败 | 通过 |

`Compiler.Collections` 组：8 失败 → **7 失败**，其余 18 个子组逐组一致
（Classes 92/18、Expressions 191/41、Functions 109/57、Modules 263/0、
Reflection 3/14、Statements 183/3、Types 50/51、Async 43/65、Basic 185/16、
EdgeCases 77/22、Error 0/12、Exceptions 53/11、Extern 7/3、FileHeader 29/0、
Integration 50/3、Linq 45/0、Threading 15/0、Unit 45/0）。

## 5. 验证过程中确认的跨模式差异（未在本轮修改）

**数组/列表 `+` 拼接只有虚拟机支持**：

```old8
arr1 <- [1, 2, 3]
arr2 <- [4, 5, 6]
combined <- arr1 + arr2
```

| 模式 | 结果 |
|---|---|
| `-f` | `INVALID_OPERATION_ERROR: 不支持类型 'ArrayLangValue' 和 'ArrayLangValue' 的加法操作` |
| `-il` | 列表相加被推断成数值加法，`Len(combined)` 报 `类型 Int32 没有 Length 或 Count 属性` |
| `-vm` | `6`（虚拟机有专门的数组拼接实现） |

因此 `ArrayConcatenation_CompilesAndExecutesCorrectly` 目前只有虚拟机语义能满足；
需要先决定「数组/列表 `+` 是否属于语言特性」，再统一实现或调整用例。已记入
`changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il nested_arrays.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Collections.ArrayTests"
```
