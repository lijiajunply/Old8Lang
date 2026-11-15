# 函数内赋值与顶层槽位定型 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL spawn 带参调用仍不互通」的最后一类根因。

## 1. 现象

```old8
func process(name:string, value:int, multiplier:double) -> string {
    Sleep(30)
    result <- value * multiplier        // 函数内局部 result
    return name + ": " + result.ToStr()
}

task <- spawn(process, "test", 10, 2.5)
task.Start()
result <- task.Join()                   // 顶层同名 result
Assert.Equal("test: 25", result)
```

IL 报 `Unable to cast 'StringLangValue' to 'System.Double'`；顶层把 `Join()` 的结果
写进槽位时，槽位已经被函数内的 `result` 收窄成 `double`。

## 2. 根因

`SetStatementRuntimeExtensions.TryEmitGlobalStore` 在槽位类型还是 `object`（尚未定型）时，
无论当前是顶层还是函数作用域，都用赋值的静态类型收窄槽位：

- 函数体 `result <- value * multiplier` 先被编译，把 `result` 槽位定型为 `double`；
- 随后顶层 `result <- task.Join()` 是对象（`StringLangValue`），
  `RuntimeGlobals.EmitStore(double, object)` 强转失败。

## 3. 修复

`TryEmitGlobalStore` 在写入时检查槽位类型与本次值类型：两者不兼容（如槽位 `double`、
值是 `object`）就把槽位**退化为 `object`** 再写入，而不是强转。这样：

- 函数内 `result <- value * multiplier` 仍按原逻辑把槽位定型为 `double`；
- 顶层 `result <- task.Join()` 发现 `object` 不能赋给 `double`，把槽位放宽为 `object`，
  `StringLangValue` 正常写入；
- 后续读取按 `object` 分派，`Assert.Equal` 走动态解包。

（尝试过“函数作用域一律保持 object”的方案，会让泛型特化里的 `matrix`/`row` 槽位
无法收窄，导致 `NestedGenericTypes` 等用例回归，已放弃。）

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `SpawnWithParameters` | `StringLangValue -> double` 转换错误 | 通过 |
| `Compiler.Async.SpawnTests` | 1 失败（12 通过） | **13 全通过** |
| `Compiler.Async` 失败 | 27 | **26** |

回归面（Async/Basic/Classes/.../Types）见 `changes/Todo.md` 当日条目。
