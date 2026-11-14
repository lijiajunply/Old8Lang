# IL 模式把 object 当布尔用（条件恒真、取反恒假）—— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

静态类型为 `object` 的表达式（未标注类字段上的方法调用、动态分派结果）直接参与
条件 / 逻辑运算时，按「引用是否为空」判断，结果静默错误：

```old8
class R {
    private v <- false
    func val() -> bool { return this.v }
}
class S {
    private rm
    func init() { this.rm <- R() }
    func check() -> string {
        if this.rm.val() { return "yes" }
        return "no"
    }
    func negated() -> bool { return !this.rm.val() }
}
```

| 模式 | `check()` | `negated()` |
|---|---|---|
| `-f` | `no` | `true` |
| `-il`（修复前） | **`yes`**（装箱引用永远非空，`brfalse` 不跳转） | **`false`**（`xor` 作用在引用上） |

同理 `obj.method() && ...` 返回的是装箱引用，`three() == true` 为 false，
但 `PrintLine(three())` 会打印 `true`——一个「看起来对、实际是垃圾位模式」的值。
用例：`ConstructorTests.ConstructorWithResourceManagement_...` 断言
`true` 与 `true` 不相等。

## 2. 根因

`LogicalOpILHelper` 的 `&&` / `||` / `xor`、`if` / `elif` / `while` / `for` 条件、
三元条件、一元 `!` 都直接把操作数交给 `brfalse` / `brtrue` / `xor`。
这对原生 `bool` 成立，但 `object` 静态类型在运行期是**装箱 bool 的引用**，
CLR 按引用判空：`brfalse` 永不跳转、`xor` 得到垃圾。

## 3. 修复

- `IlValueBridge.ToBoolean(object?)`：运行期按语言语义把动态值转成原生 `bool`
  （`bool` / `BoolLangValue` → 取布尔值，`null` → false，其它类型报
  `期望布尔类型` 错误，与解释器的严格布尔规则一致）；
- `ConditionIlHelper.EmitBoolOperand`：发射表达式后，**只对静态类型 `object`**
  追加 `ToBoolean` 调用；其它类型保持原有 `brfalse` / `xor` 语义，
  避免改变 `if 0`、字符串等既有真值判断；
- 接入 `LogicalOpILHelper`（And/Or/Xor）、`VisitIfStatement`（if/elif）、
  `VisitWhileStatement`、C 风格 `for` 条件、`VisitTernaryExpression`、一元 `!`；
- 顺带修正 `TypeInferenceVisitor.VisitOperation`：一元 `not` / `!` 恒为 `bool`
  （此前返回 null，调用方兜底成 `object`，会把原生 bool 再转换一次）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `if this.rm.val()`（值 false） | `yes` | `no`，与解释器一致 |
| `!this.rm.val()` | `false` | `true`，与解释器一致 |
| `obj && obj && obj` 后与 `true` 比较 | `false` | `true` |
| `not isActive` | `true` | `true`（并把 `not` 推断修为 bool） |
| `ConstructorWithResourceManagement_...` | 失败 | 通过 |

组级：`Compiler.Classes` 16→15 失败、`Compiler.Functions` 57→56 失败；
`Basic` 16/185、`Statements` 3/183、`Expressions` 35/197、`EdgeCases` 22/77、
`Exceptions` 11/53、`Collections` 6/168、`Types` 50/51、`Async` 43/65 与修复前一致。

过程中曾把非 `object` 的值类型也一并转换，导致 `if 0`、`if ""` 等既有真值用例
与 `not` 用例回归（`Statements`、`Expressions`、`Basic`、`EdgeCases` 共 8 项），
已收窄为「只转换 `object`」并修正一元 `not` 的推断，回归全部消除。

## 5. 验证过程中发现的解释器死循环（未在本轮修改）

```old8
class RM {
    private resources <- {}
    func init() { this.resources.Add("a") }
    func has(name:string) -> bool {
        i <- 0
        while i < this.resources.Count() {
            if this.resources[i] == name {
                return true
            }
            i <- i + 1
        }
        return false
    }
}
r <- RM()
PrintLine(r.has("a"))   # -f 死循环（i 始终为 0）
```

- `-f`：无限循环，循环体每次打印 `i = 0`，`i <- i + 1` 不生效（超时退出）；
- `-il` / `-vm`：正常返回 `true`。

去掉 `if this.resources[i] == name { return true }` 后解释器恢复正常，
单独执行 `this.resources[0] == name` 也正常，说明是「循环内 if 里 `return` +
索引访问字段」的组合问题。属解释器侧缺陷，已记入 `changes/Todo.md`。

## 6. 复现命令

```bash
dotnet Old8Lang.App/bin/Debug/net10.0/Old8Lang.App.dll -il bool_condition.old8
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Classes.ConstructorTests"
```
