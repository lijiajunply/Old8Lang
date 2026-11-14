# IL 原生集合实例方法桥接 — 需求与约束

## 背景

`changes/Todo.md` 的待办条目：

> `List` / `Dictionary` / `Array` 其余实例方法的 IL 实现仍是 `Ldnull` 桩或按旧约定写死
> （例如 `Map` / `Filter` / `Reduce` / `Any` / `All` / `Find` / `Sort` / `IsEmpty` 等），
> 在 `List<T>` 接收者下会「方法未找到」。

2026-11-11 起已按 `IIlNativeValueInstanceMethod` 机制逐个迁移了一批高频方法；
但 `Old8Lang.Runtime/InstanceMethods/Implementations/` 下仍有 63 个 List / Dictionary / Array
实例方法没有实现该接口，其中：

- 大半方法的 `GenerateIlInternal` 按旧的 `ListLangValue` / `DictionaryLangValue` 接收者写死，
  而 IL 模式下列表/字典/数组字面量在栈上是原生 `List<object>` / `Dictionary<object, object>` /
  `object[]`，因此这些实现即便被调用也会类型不匹配；
- `LangListGroupByMethod.GenerateIlInternal` 直接 `Ldnull`，`LangListZipMethod` 带 zipper 的分支、
  `LangListSortByMethod` / `LangListSelectManyMethod` 也有同类桩；
- 其余方法因为未被解析到，IL 编译期直接报「方法 'X' 未找到」，与解释器/VM 行为不一致。

## 目标

1. 让上述剩余 `List` / `Dictionary` / `Array` 实例方法在 IL 模式的原生集合接收者上可用，
   语义与解释器一致（同一套 `ExecuteInternal` 实现）。
2. 覆盖有副作用的方法（`Add` / `Insert` / `Remove` / `RemoveAt` / `Clear` / `Pop` /
   字典 `Add` / `Remove` / `Clear` / `Update` / 数组 `Set`），修改必须落回原生集合本身，
   包括接收者别名共享同一对象的情形。
3. 覆盖返回集合的方法（`Chunk` / `Flatten` / `Slice` / `Permutations` / `Clone` / `Merge` …）
   的链式调用，例如 `.Chunk(2).Count()`、`.GroupBy(...).Keys()`。
4. 类型推断与 IL 发射使用同一张返回类型映射，避免「推断是 object、栈上是 List」导致无效 IL。

## 非目标

- 不逐个重写 63 个方法的手写 IL；本次用统一的桥接机制保证正确性，性能优化留待后续在热点方法上
  重新启用专用 IL。
- 不改解释器 / VM 语义，不改方法注册表与公开 API。
- 不处理 `Tuple` / `Task` / `Thread` 的同类缺口（不在本条 Todo 范围）。
- 不删除各方法现有的 `GenerateIlInternal` 死代码；清理单列 TODO。

## 验收标准

- 新增 IL 定向测试覆盖上述三类（纯函数、变异、链式），全部通过。
- 新增测试在解释模式下结果一致（关键用例手动对照）。
- `dotnet test` 中 `Compiler/Collections` 相关既有用例不回归。
- `changes/Todo.md` 更新该条目的完成范围与遗留项。
