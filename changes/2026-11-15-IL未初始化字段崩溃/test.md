# IL 模式「未标注类型字段」写入不装箱导致进程段错误 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. 排查会使测试进程崩溃的 IL 问题」。

## 1. 现象

用 IL 模式批量跑 `TestFiles/CompilerTests/` 的 157 个用例时，
`40_uninitialized_fields.old8` 直接以 **SIGSEGV（退出码 139）** 结束：

```bash
dotnet Old8Lang.App.dll -il TestFiles/CompilerTests/40_uninitialized_fields.old8
# 输出到 "y 的值: 2" 后：Segmentation fault: 11
```

## 2. 最小复现

```old8
class C {
    public x          // 无类型注解、无初值
}

c <- C()
c.x <- 100
PrintLine("x=" + c.x)   // 段错误
```

二分结论：

| 变体 | 结果 |
|---|---|
| `c.x <- 100` 后不再读取 | 正常退出 |
| `PrintLine(c.x)` 直接读 | `NullReferenceException` |
| `s <- c.x` / `"" + c.x` / `c.x == 100` | 段错误 |
| 字段写成 `public x <- 0`（有初值） | 正常 |
| `this.x <- 100`（类方法内部） | 正常（该路径本来就会装箱） |

## 3. 根因

`VisitSetStatement` 里，非 `this` 形式的字段写入（`c.x <- 100`）只加载接收者与右值，
然后直接发 `stfld`，没有按字段类型做任何装箱/拆箱/数值转换：

```csharp
var field = leftType.GetField(memberId.IdName);
if (field is not null)
{
    ilGenerator.Emit(OpCodes.Stfld, field);   // 右值可能是未装箱的 int
}
```

未标注类型的字段在 `DefineFields` 里被定为 `object`。于是 `c.x <- 100` 把
**裸 int32** 写进了 `object` 字段（JIT 不校验 `stfld` 的栈类型，只按位复制）。
后续 `ldfld object` 读出来的就是一个非法对象引用：轻则 NRE，重则按引用访问直接段错误。

对比：`this.x <- 100` 的路径、静态字段写入、局部变量赋值都有装箱/数值转换，唯独
非 `this` 的实例字段写入漏了。

## 4. 修复

在 `CompilerVisitor.Statements.cs` 的非 `this` 成员写入分支加
`EmitFieldValueConversion(targetType, valueType)`：

- 目标为 `object`、右值为值类型 → `box`；
- 目标为值类型、右值为 `object` → `unbox.any`；
- 两侧都是数值类型 → 按目标类型做数值转换。

字段与属性 setter 两条路径都接入；右值类型由 `TypeInferenceVisitor` 静态推断。

涉及文件：`Old8Lang.Compiler/Visitors/CompilerVisitor.Statements.cs`。

## 5. 验证

### 5.1 新增回归测试

`Old8Lang.Tests/Compiler/Classes/UninitializedFieldIlTests.cs`（5 项）：

- `AssignIntThenRead_DoesNotCrash`
- `AssignDoubleThenRead_PreservesValue`
- `AssignStringThenRead_PreservesValue`
- `ReadBeforeAssign_ReturnsNullLikeInterpreter`
- `FieldValueInStringConcat_DoesNotCrash`

修复前这些用例会触发段错误（测试主机消失）或 NRE；修复后 5 项全部通过。

### 5.2 CLI 与跨模式对比

| 用例 | 修复前 | 修复后 | 解释器 | 虚拟机 |
|---|---|---|---|---|
| `40_uninitialized_fields.old8` | 段错误 139 | 正常，退出码 0 | 正常 | 正常 |
| `c.x <- 100; PrintLine(c.x)` | NRE | `100` | `100` | `100` |
| `c.x <- 1.5; "x=" + c.x` | 段错误 | `x=1.5` | `x=1.5` | `x=1.5` |
| `c.x == 100` | 段错误 | `true` | `true` | `true` |
| 未赋值就读 `c.x` | NRE | `null`（IL 打印为空串，见遗留） | `Null` | `null` |

### 5.3 语料扫描

`TestFiles/CompilerTests/*.old8` 共 157 个文件，逐个用 `-il` 运行（10 秒超时）：
本修复前 **1 个段错误**，修复后 **0 个**。

## 6. 遗留

1. `null` 的显示大小写跨模式差异仍在（`changes/Todo.md` 已有条目）：
   `"x=" + c.x`（未赋值）在解释器为 `Null`、IL/VM 为 `null`。
2. 同类转换缺口还可能有别的成员写入入口（例如扩展方法/闭包环境字段），
   本轮只修了 `VisitSetStatement` 的非 `this` 字段与属性路径。
