# Spec：IL 异步生成器非阻塞挂起、遗留项清理与文档校正

## 背景与目标

`changes/Todo.md` 中最后一项工程待办及三处正文附注遗留项与文档漂移问题，本次一并收敛清理：

1. **IL 异步生成器非阻塞挂起（`Todo.md#L1008`）**
   - 现状：`IlAsyncYieldEmitter.Yield` 使用 `SemaphoreSlim.Wait` 同步阻塞等待消费者恢复，导致每个暂停在 `yield` 上的异步生成器占用一个线程池线程；高并发或长期暂停场景会引发线程池饥饿/死锁。
   - 目标：将 `yield` 纳入 `AsyncStateMachineGenerator` 异步状态机，作为一个真正的非阻塞挂起点（基于 `Task` / `TaskCompletionSource`），暂停期间占 **0 个线程池线程**；支持在 `try`/`catch`/`finally`/`using` 与异步 `finally` 中安全挂起、恢复与提前取消清理；补充并发暂停压力测试。

2. **IL 类内 `this.method()` 优化（`Todo.md#L262-L263`）**
   - 现状：类方法体内对当前未完成 `TypeBuilder` 的方法调用退化为 `InvokeDynamicObjectMethod` 反射分派，且静态类型推断退化为 `typeof(object)`。
   - 目标：在 `ClassInitRuntimeExtensions` 与 `GenericClassSpecializer` 采用两遍方法定义（先声明所有方法签名与 `MethodBuilder` 并登记到 `LocalManager`，再生成方法体），在 `DotOperatorILHelper` 与 `TypeInferenceVisitor` 优先匹配预登记的 `MethodBuilder`（及已完成的父类 `MethodInfo`）发射直接 `callvirt` / `call` 与精确返回类型；闭包 `DynamicMethod`、函数值字段与动态重载场景保持安全回退。

3. **类字段集合初值跨模式语义统一（`Todo.md#L523-L524`）**
   - 现状：IL 与解释器已为每实例独立分配集合字段初值，但 VM 的 `AllocateObjectInstance` 直接共享 `ConstantPool` 中的同一可变集合引用（且导入类 `CreateObjectInstance` 未应用常量字段初值）。
   - 目标：VM 实例化时对可变集合初值（列表/数组/字典，含嵌套）递归深拷贝，确保三模式统一为「每实例独立」。

4. **`type(枚举值)` 与 `type()` 跨模式语义统一（`Todo.md#L712`）**
   - 现状：`EnumLangValue` 未重写 `TypeToString()`，导致解释器下 `type(Color.Red)` 与 `TypeOf(Color.Red)` 返回 `"Value"`；同时 IL/VM 的 `type()` 直接暴露 CLR 类型名（如 `Int32`、`EnumLangValue`）。
   - 目标：`EnumLangValue.TypeToString()` 返回 `"Int"`（与三模式枚举成员底层整数语义对齐）；`TypeFunction` 在 IL 与 VM 均统一通过 `VMReflectionHelper.DescribeTypeName` / `DescribeValueType` 输出语言类型名（如 `"Int"`、`"String"`、`"List"` 等），三模式完全一致。

5. **文档校正（`Todo.md#L1005-L1006`）**
   - 逐项核对并校正 `Docs/MODE_SUPPORT.md` 中过时的历史条目（IL 泛型、装饰器、Mixin、接口默认方法、异步生成器、`async for-in`、标准库 `import`、顶层 `for` 作用域、闭包写回、VM 元组解构、共同差异表等）。
   - 同步更新 `changes/Todo.md` 状态，并按规范精简更新 `Docs/CHANGELOG.md`。

## 范围与不做项

- 不改动 `Todo.md` 中「长期研究与调试方向」两项评估性条目（类型推断深度研究、图形/远程调试评估）。
- 不引入新的同步生成器（IL `yield`）支持，保持 `Docs/MODE_SUPPORT.md` 的既定边界。
- 严格遵守程序集分层边界（`Old8Lang.Runtime` 不引用执行后端，三个后端互不引用），全部架构测试通过。

## 验收标准

1. 同时暂停数百个 IL 异步生成器时线程池不阻塞，全部正确恢复并完成；提前 `break`/`DisposeAsync` 正确触发 `try/finally`（含异步 `finally`）。
2. 类内 `this.method()`（含互相引用方法、重载、私有方法、继承方法、泛型特化类方法）直接生成 `callvirt` 并正确推断返回类型；闭包内 `this.method()` 与函数值字段调用不回归。
3. 三模式下对同一类两个实例的集合字段初值执行变异互不影响。
4. 三模式下 `type(Color.Red)` 与 `TypeOf(Color.Red)` 一致返回 `"Int"`。
5. `Old8Lang.Tests` 全量默认档、`Old8Lang.Tests.Architecture`、新增性能并发测试（`performance.runsettings`）、`run_syntax_tests.sh`、`run_vm_tests.sh` 全部 0 失败。
