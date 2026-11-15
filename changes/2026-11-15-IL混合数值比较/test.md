# IL 模式 `double` 与 `int` 比较按位解释，静默得到错误结果 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
func init(owner:string, amount:double) {
    if amount < 0 {
        throw "neg balance"
    }
}
```

`D("Alice", 1000.0)` 正常，随后的 `D("Bob", -500.0)` **不抛异常**：

| 模式 | 结果 |
|---|---|
| `-f` | 抛出「neg balance」 |
| `-il`（修复前） | 不抛，程序按成功路径继续 |
| `-vm` | 抛出 |

最小复现：`a <- -500.0; PrintLine(a < 0)` → `-il` 输出 `false`。

## 2. 根因

`ComparisonOpHelper` 的四个顺序比较（`>` `<` `>=` `<=`）在加载两个操作数后**直接发 `clt` / `cgt`**，
没有做数值类型提升。`clt` 要求两个操作数是同一类型；一侧 `double`、另一侧 `int` 时，
CLR 会把 4 字节整数当成 double 的一部分来读，结果取决于内存布局——`-500.0 < 0` 被判成 `false`。
`==` / `!=` 的 `ceq` 分支同样受影响。

算术运算（`+ - * / %`）一直有 `ConvertToDoubleIfNeeded` 做提升，比较运算漏了这一步。

## 3. 修复

`ComparisonOpHelper` 新增 `EmitNumericPromotion`（复用 `NumericBinaryOpHelper.ConvertToDoubleIfNeeded`），
在四个顺序比较与相等比较的「普通数值」分支里、发 `clt`/`cgt`/`ceq` 之前调用；
任一侧静态类型是 `object` 时跳过（那条路径走装箱 + `IComparable` / `object.Equals`）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `a <- -500.0; a < 0` | `false` | `true`，与 `-f` 一致 |
| `0 > a`、`a > 0`、`b < 2.5`、`b >= 3.0` | 部分错误 | 全部与 `-f` 一致 |
| `try { D("Bob", -500.0) } catch` | 不抛、catch 不执行 | 正常抛出并被捕获 |
| `Classes/ConstructorTests.ConstructorWithValidation_...` | 失败 | 通过 |

组级：`Compiler.Classes` 15→14 失败，其余 18 个子组与非 `Compiler` 组不变。

**顺带发现（未修）**：虚拟机下 `b >= 3.0`（`b <- 3`）返回 `false`，解释器与 IL 都是 `true`——
虚拟机的混合数值比较也有一处偏差，已记入 `changes/Todo.md`。

## 5. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il mixed_compare.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes.ConstructorTests"
```
