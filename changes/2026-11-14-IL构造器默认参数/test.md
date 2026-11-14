# IL 模式带默认参数的构造器生成无效 IL —— 测试报告

日期：2026-11-14
关联待办：`changes/Todo.md`「优先顺序 1. …再核实其他 IL 结果错误和跨模式差异」。

## 1. 现象

```old8
class U {
    public name <- ""
    public age <- 0
    func init(name:string, age: 18) {
        this.name <- name
        this.age <- age
    }
}
a <- U("Alice")
```

| 模式 | 结果 |
|---|---|
| `-f` | `Alice:18` |
| `-il` | `[IL001] 无效的IL代码: Common Language Runtime detected an invalid program.` |
| `-vm` | `Alice:18` |

失败用例：`Compiler/Classes/ConstructorTests.ConstructorWithOptionalParameters_...` 与
`ConstructorWithDefaultParameters_...`。

## 2. 根因

类实例化（`U("Alice")`）走的是「按实参个数在类里找 `init`」的路径
（`CompilerVisitor.ExpressionsAndValues` 的类构造分支）：它只按 `node.Ids.Count`
压入实参，随后直接 `callvirt init`，**没有补默认值的逻辑**。`init` 声明了 2 个形参，
只压 1 个实参就调用，栈不平衡 → 无效 IL。

函数调用路径（`name(...)`）有补默认值的代码，但类构造这条路径没复用。

## 3. 修复

`ClassInitRuntimeExtensions.DefineMethodInternal` 在定义 `init` 之后，为带默认值的
`init` 生成「前缀重载」（`DefineInitializerOverloads`）：

- 例如 `init(name:string, age: 18, email: "")` 额外生成 `init(name)` 与 `init(name, age)`；
- 重载体内部依次 `ldarg.0`、传入实参、补上剩余形参的默认值（按目标类型装箱/拆箱），
  再 `call` 完整实现并 `ret`；
- 这样类实例化路径按实参个数就一定能找到匹配构造，无需改动调用点。

默认值表达式在独立的 `LocalManager` 里求值，可解析已声明的类与静态成员。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `U("Alice")`（省 1 个参数） | 无效 IL | `Alice:18` |
| `U("Bob", 25)`（全参） | 正常 | 正常 |
| `ConstructorWithOptionalParameters_...` | 失败 | 通过 |
| `ConstructorWithDefaultParameters_...` | 失败 | 通过 |
| `Compiler.Classes` 组 | 18 失败 / 92 通过 | **16 失败 / 94 通过** |

## 5. 遗留

当前类在自身方法里构造自身（`local.InClassEnv == classType` 分支）仍直接用完整
`init`，少传参数会走到同一条栈不平衡路径；该场景较少见，未在本轮处理。
`ConstructorTests` 剩余 2 项失败属另外两个缺口（见 change
`2026-11-14-IL布尔条件` 与异常消息转字符串）。
