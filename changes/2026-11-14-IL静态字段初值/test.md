# IL 静态字段初值未初始化 —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

IL 模式下类静态字段的声明期初值从未写入，读到的是 CLR 默认值：

```old8
class M {
    public static PI <- 3.14159
    public static N <- 42
    public static S <- "hi"
}
PrintLine(M.PI)   // -il: 0          -f/-vm: 3.14159
PrintLine(M.N)    // -il: 0          -f/-vm: 42
PrintLine(M.S)    // -il: null       -f/-vm: hi
```

三模式实测：

| 模式 | 输出 |
|---|---|
| `-f`  | `3.14159` / `42` / `hi` |
| `-il` | `0` / `0` / `null` |
| `-vm` | `3.14159` / `42` / `hi` |

失败用例：`Old8Lang.Tests/Compiler/Classes/MemberAccessTests.StaticMemberAccess_CompilesAndExecutesCorrectly`
（`Assert.Equal(3.14159, piValue)` 实际得到 `0`）。

## 2. 根因

`ClassInitRuntimeExtensions.DefineFields` 只把**实例**字段的初值放进
`fieldBuilders` 交给 `DefineConstructor` 写进构造函数；`DefineFields` 里
`if (!memberId.HasModifier(Static)) fieldBuilders.Add(...)` 直接跳过了静态字段，
而 IL 后端也没有为普通类生成 `.cctor`，因此静态字段永远是默认值。

泛型类的特化路径（`GenericClassSpecializer.GenerateStaticFieldInitializer`）已经实现了
同样的 `.cctor`，只是非泛型类没有对应逻辑。

## 3. 修复

- `Old8Lang.Runtime/Execution/LegacyNodeExtensions/Declarations/ClassInitRuntimeExtensions.cs`
  新增 `DefineStaticFieldInitializer`：扫描 `StaticVariates` 中非函数/非重载/非嵌套类型的
  初值，生成 `typeBuilder.DefineTypeInitializer()`（`.cctor`），逐项求值 + 按字段类型
  装箱/拆箱后 `stsfld`，与泛型特化路径的处理保持一致；
- `Old8Lang.Compiler/Visitors/CompilerVisitor.Statements2.cs`
  在 `DefineMethods` 之后调用它（实例字段仍由构造函数初始化，互不重叠）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `static PI/N/S` 三个字段读取 | `0 / 0 / null` | `3.14159 / 42 / hi`，与 `-f`/`-vm` 一致 |
| `MemberAccessTests.StaticMemberAccess_...` | 失败（得到 0） | 通过 |
| `MemberAccessTests` 整组 | 6 通过 / 3 失败 | 7 通过 / 2 失败 |

基线对比（`Old8Lang.Tests.Compiler` 19 个子组）：

| 组 | 修复前（通过/失败） | 修复后（通过/失败） |
|---|---|---|
| Classes | 89 / 21 | **90 / 20** |
| 其余 18 组 | — | 与修复前逐组一致（Async 43/65、Basic 185/16、Collections 165/9、EdgeCases 77/22、Error 0/12、Exceptions 53/11、Expressions 191/41、Extern 7/3、FileHeader 29/0、Functions 109/57、Integration 50/3、Linq 45/0、Modules 263/0、Reflection 3/14、Statements 183/3、Threading 15/0、Types 50/51、Unit 45/0） |

## 5. 验证过程中发现的其他跨模式差异（未在本轮修改）

1. **解释器无法在类内访问 `private static` 字段**：

   ```old8
   class M {
       private static counter <- 0
       public static func inc() -> int {
           M.counter <- M.counter + 1
           return M.counter
       }
   }
   PrintLine(M.inc())
   ```

   `-f` 报 `NAME_ERROR: 名称 'counter' 未定义`；`-il` / `-vm` 输出 `1`。
   把字段改成 `public static` 后三种模式都输出 `1`。已记入 `changes/Todo.md`。

2. **虚拟机不支持集合字面量作为字段初值**：

   ```old8
   class C { public items <- {1, 2, 3} }
   ```

   `-vm` 在字节码编译期抛
   `NotSupportedException: 虚拟机模式不支持非常量默认参数表达式: ListLiteralExpression`
   （`BytecodeCompiler.EvaluateConstantExpression` 只接受标量），
   `-f` / `-il` 正常。已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il static1.old8   # M.PI / M.N / M.S
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes.MemberAccessTests.StaticMemberAccess"
```
