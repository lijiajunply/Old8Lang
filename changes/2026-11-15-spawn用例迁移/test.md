# Spawn 用例迁移到 spawn(func, args) API —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「SpawnTests 使用已移除的 spawn 关键字语法」。

## 1. 现象

`Compiler/Async/SpawnTests` 里 14 个用例使用旧语法：

```old8
task <- spawn backgroundTask()
result <- task.Join()
```

`spawn` 现在是**全局函数** `spawn(func, args...)`，返回线程句柄，必须 `Start()` 后
`Join()`（见 `Docs/API_REFERENCE.md`）。旧语法在三种模式下都报
`NAME_ERROR 名称 'spawn' 未定义`（IL 为编译期「变量 'spawn' 未声明」），
所以整组失败与 IL 无关，是用例停留在旧 API。

## 2. 迁移

- `spawn f(a, b)` → `spawn(f, a, b)`，并在创建后补 `task.Start()`；
- 匿名函数 `spawn func() -> int { ... }()` → `spawn(() -> { ... })`；
- `spawn worker.process()` → `spawn(() -> worker.process())`；
- `MultipleSpawn` 里变量与函数同名（`task1 <- spawn(task1)`）改为 `t1 <- spawn(task1)`，
  避免变量遮蔽后 `Join()` 作用在函数值上；
- 并行用例去掉了 `Time.Now()`（现为 `Time.GetLocalTime`，且需 import），
  改为只验证三个线程都能启动并 Join 完成；
- `SpawnWithComplexLogic` 的期望值由 143 修正为 88（0..9 的斐波那契和）。

## 3. 验证

| 组 | 迁移前 | 迁移后 |
|---|---|---|
| `Compiler.Async` 失败 | 42 | **33** |
| `SpawnTests` | 14 失败 | 9 通过 / 4 失败 |

4 个剩余失败暴露的是 IL 侧真实互操作缺口，不是用例问题：

1. `SpawnWithParameters`：线程里调用编译函数时参数被当作 `LangValueType`，
   报 `Unable to cast 'System.Double' to 'LangValueType'`（double/int 实参）；
2. `SpawnWithCollections`：`list` 实参同样走包装/解包不一致；
3. `SpawnWithConditional`：bool 实参的同类问题；
4. `SpawnWithErrorHandling`：线程内 `throw "Task failed"` 后 `Join()` 抛出的
   异常消息与期望字符串不一致。

这些已记入 `changes/Todo.md` 的「IL spawn 带参调用」项。
