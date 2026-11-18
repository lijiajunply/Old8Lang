# 装饰器转发：同步顶层全局槽位 —— 测试报告

日期：2026-11-18
关联待办：`changes/Todo.md`「装饰器转发里 wrapper 读不到顶层变量」。

## 1. 现象

装饰器 wrapper 是解释器函数，IL 装饰器转发时（`IlRuntimeFunctions.InvokeByName`）
只把参数交给解释器管理器。wrapper 里访问的顶层变量（缓存 `cacheDict`、计数器
`counter`/`callCount`）在 IL 里存在 `RuntimeGlobals` 槽位中，解释器看不到：

- `Interpreter_DecoratorWithCache_CachesResults`
- `Interpreter_DecoratorAccessingClosureVariables_WorksCorrectly`
- `Compiler_DecoratorWithVoidFunction_CompilesAndRuns`

均报 `NAME_ERROR 名称 'counter'/'callCount' 未定义`。

## 2. 修复

- `RuntimeGlobals.Snapshot()`：返回当前线程已赋值槽位的快照；
- `IlRuntimeFunctions.InvokeByName`：调用解释器函数前把快照写入管理器
  （`RuntimeGlobalWriter.Write`），调用后把管理器里同名变量的值解包写回槽位。
  这样 wrapper 读写的顶层变量与 IL 顶层代码共享同一份数据。

## 3. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `DecoratorWithVoidFunction`（counter） | NAME_ERROR | 通过 |
| `DecoratorWithCache`（cacheDict/callCount） | NAME_ERROR | 通过 |
| `DecoratorAccessingClosureVariables` | NAME_ERROR | 通过 |
| 装饰器两组 | 10 失败 | 3 失败（均为改变返回类型） |

错误断言用例同步迁移：未定义装饰器在 IL 编译期报 `NameError`（解释器运行期），
装饰器返回非函数在编译期报 `InvalidOperationError`。

## 4. 遗留

`Interpreter_DecoratorReturningDifferentType`、`Compiler_DecoratorReturningDifferentType`、
`Compiler_DecoratorChangingReturnType` 仍是静态类型限制：转发委托按原返回注解类型，
装饰器把 int 改成 string 时调用点 cast 失败。需要让调用点对被装饰函数按 object 推断。
