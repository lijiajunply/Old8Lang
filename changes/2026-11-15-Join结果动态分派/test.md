# Thread.Join 结果与动态语言值的方法分派 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL spawn 带参调用仍不互通」。

## 1. 现象

`spawn` 迁移到新 API 后仍有 4 项失败，其中两类根因在 Join 一侧：

```old8
t <- spawn(processList, {1, 2, 3})
t.Start()
r <- t.Join()
Assert.Equal(5, r.Count())        // 方法 'Count' 未找到
Assert.Equal("hello", t.Join())   // 期望 'hello' 但实际为 '"hello"'
```

| 表达式 | 解释器 | IL（修复前） | 虚拟机 |
|---|---|---|---|
| `task.Join().Count()` | 5 | 方法 'Count' 未找到 | 5 |
| `Assert.Equal("hello", task.Join())` | 通过 | `期望 'hello' 但实际 '"hello"'` | 通过 |

## 2. 根因

1. `ThreadJoinMethod.GetReturnTypeInternal` 把 `Join()` 静态类型写成 `LangValueType`，
   IL 的 `.Count()` 因此按 `LangValueType` 找方法（找不到），而不是按运行期实际值分派；
2. `Assert.Equal` 的实参若静态类型是 `object`，会原样把运行期的 `StringLangValue`
   传给 `AssertHelper.AssertEqual(object,object)`，与原生字符串比较时退化成
   `ToString()` 比较（语言值的 `ToString` 带引号）；
3. `DotOperatorILHelper.InvokeDynamicObjectMethod` 只桥接原生集合 / CLR 反射，
   没有处理运行期是 `LangValueType` 的接收者（`ListLangValue.Count()`）；
4. 函数内局部变量与顶层同名槽位冲突时，`TryEmitGlobalStore` 会把值强写进顶层槽位
   （例如 `result` 槽位是 `LangValueType`，函数内 `result` 是 double）。

## 3. 修复

1. `Join()` 的 IL 返回类型改为 `object`，调用点按运行期值分派；
2. `IlValueBridge.UnwrapDynamic(object?)`：运行期是语言值就取出底层原生值；
   `Assert.*` 的 `object` 形参在静态类型为 `object`/`LangValueType` 时先经过它；
3. `InvokeDynamicObjectMethod` 遇到 `LangValueType` 接收者时走
   `InstanceMethodRegistry.ResolveMethod` + `Execute`，与解释器实例方法表一致；
4. `TryEmitGlobalStore` 在函数作用域内，若顶层槽位类型与赋值表达式静态类型不兼容，
   返回 false 交给局部变量路径（同名局部遮蔽顶层槽位）。

## 4. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `SpawnWithCollections` | 方法 'Count' 未找到 | 通过 |
| `SpawnWithConditional` | 期望 'fast result' 实际 '"fast result"' | 通过 |
| `Compiler.Async` 失败 | 33 | **28** |

`SpawnWithParameters`（函数内 `result` 与顶层槽位同名）仍报
`StringLangValue -> double` 转换错误，`SpawnWithErrorHandling` 的异常消息口径仍待统一；
两者继续留在 Todo。

解释器 1987、虚拟机 1116、Parser 892 均无回归；Compiler 2017 例 / 106 失败 / 26 跳过。
