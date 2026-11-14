# IL 原生集合实例方法桥接 — 测试报告

日期：2026-11-14
环境：macOS arm64，系统 `dotnet`（`/usr/local/share/dotnet/dotnet`，SDK 10.0.100 / 运行时 10.0.0）。
（`~/.dotnet` 只有 .NET 11 运行时，直接 `dotnet test` 会因缺少 `Microsoft.NETCore.App 10.0.0` 失败。）

## 1. 新增定向测试

命令：

```bash
/usr/local/share/dotnet/dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~NativeCollectionMethodBridgeTests" --nologo
```

结果：**通过 26 / 失败 0 / 总计 26**。

覆盖范围（`Old8Lang.Tests/Compiler/Collections/Advanced/NativeCollectionMethodBridgeTests.cs`）：

| 分组 | 用例 |
|---|---|
| List 纯函数 | `Chunk` + 链式 `Count/First/Last`、`Flatten`、`Pairwise`、`Window`、`Slice`、`TakeLast`、`SkipLast`、`Aggregate`（无种子/带种子）、`SetEquals`/`Overlaps`/`IsSubsetOf`/`IsSupersetOf`/`SymmetricExcept`、`BubbleSort`/`QuickSort`、`CartesianProduct`/`Combinations`/`Permutations`、`WithIndex`/`GroupAdjacent`、`First/Last`（谓词）、`ElementAtOrDefault`、`LastIndexOf`、`FindAll`、`TakeWhile`、`FlattenDeep`、`SortWith` |
| List 变异 | `Insert`/`Remove`/`RemoveAt`/`Pop`/`Clear`，以及 `Clear` 后别名 `b` 观察到修改 |
| Dictionary | `ContainsKey`/`ContainsValue`/`GetValue`/`GetOrElse`/`Count`/`IsEmpty`、`Update` 原地修改、`Add`/`Remove`/`Clear`/`ToList`、`Clone`/`Merge` 返回独立字典 |
| Array | `Get`/`Set`/`Slice`、`Set` 后别名可见、`Contains`/`GroupAdjacent`/`Permutations`/`SetEquals`/`Overlaps` |
| 旧桩与动态接收者 | `GroupBy`（原 `Ldnull`）、带 zipper 的 `Zip`（原 `Ldnull`）、字典索引得到的 `object` 接收者链式 `Chunk(2)` |
| 解释器对照 | 同一段代码分别用 `LangInterpreter` 与 IL 执行，结果一致 |

## 2. 回归对比（基线 = `HEAD` 工作树）

基线获取方式：`git worktree add /tmp/o8-baseline HEAD`，在新工作树运行同一筛选。

### 2.1 `Compiler/Collections`

```bash
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj \
  --filter "FullyQualifiedName~Old8Lang.Tests.Compiler.Collections"
```

| | 总计 | 通过 | 失败 | 跳过 |
|---|---|---|---|---|
| 基线 | 151 | 133 | 15 | 3 |
| 本次改动 | 177 | 165 | 9 | 3 |

- 新增 26 项全部通过（含字典 `Delete` 别名、`Remove` 返回值与 `ToList` 元素形态）。
- 相对基线**修复 6 项**：`ListIsEmptyMethod_CompilesAndExecutesCorrectly`、
  `CollectionErrorHandling_CompilesAndExecutesCorrectly`、`ListJoinMethod_CompilesAndExecutesCorrectly`、
  `ListDefaultElementReaders_CompileForNativeListsAndArrays`、`List_AddAndRemove_CompilesAndExecutes`、
  `Dictionary_Remove_CompilesAndExecutes`。
- **没有新增失败**。

剩余 9 项失败在基线上同样失败：

1. `Range.RangeTests.RangeEdgeCases_CompilesAndExecutesCorrectly`
2. `InExpression.MatchExpressionTests.MatchExpression_EnumValue_CompilesAndExecutesCorrectly`
3. `InExpression.MatchExpressionTests.MatchExpression_DoubleValue_CompilesAndExecutesCorrectly`
4. `SyncIteratorTests.SyncIterator_GeneratorCreation_WorksCorrectly`
5. `SyncIteratorTests.SyncIterator_GeneratorIteration_WorksCorrectly`
6. `SyncIteratorTests.SyncIterator_ArrayElementIteration_WorksCorrectly`
7. `DictionaryTests.Dictionary_EmptyDictionary_CompilesAndExecutes`
8. `ArrayTests.ArrayConcatenation_CompilesAndExecutesCorrectly`
9. `ArrayTests.NestedArrays_CompilesAndExecutesCorrectly`

### 2.2 其他 IL 组

| 筛选 | 基线 | 本次改动 |
|---|---|---|
| `Compiler.Expressions\|Compiler.Linq\|Compiler.Modules` | 528 总计 / 487 通过 / 41 失败 | 528 / 487 / 41，失败集合完全一致 |
| `Compiler.Functions.HigherOrderTests` | 11 / 3 通过 / 8 失败 | 11 / 3 / 8，失败集合完全一致 |
| `Compiler.Classes`（及和 Statements/Types/Integration 合并） | 测试主机因生成无效 IL 崩溃 | 同样崩溃 |

结论：这些失败与集合实例方法桥接无关，已记录到 `changes/Todo.md`。

## 3. 已知限制

- 桥接按调用包装、解析并执行解释器实现，未做专用 IL 优化；热点方法后续仍需专用 `IIlNativeValueInstanceMethod`。
- 变异同步名单是白名单：新增有副作用的集合方法时必须同步更新
  `IlNativeCollectionBridge.ListMutators` / `DictionaryMutators` / `ArrayMutators`。
- 嵌套字典作为实参时只填充一层 `Value`，更深层元素未做深度填充。
- IL 模式的字典是原生 `Dictionary<object, object>`，无法表示解释器允许的重复键：桥接执行 `Add` 后若同键已存在，
  同步会覆盖旧值，`Count()` 与解释器不同。这是原生表示的固有限制，未在本次解决。
- 字典 `Clear()` 只清 `Value` 不清 `Tuples`；桥接在 `Value` 为空时会按 `Tuples` 重建，
  因此若某个实现返回「清空后的同一字典实例」，`Unwrap` 可能复活旧内容。当前内置方法没有这种写法，属潜在风险。
- 桥接优先于旧的原生 CLR 回退：`List<T>.Remove` 这类同名方法现在按解释器语义执行（返回被删元素，
  缺失时抛语言错误），而不是 CLR 的「返回 bool / 缺失返回 false」；依赖旧 CLR 返回值的代码需改用语言语义。
- 桥接结果中的元组（如 `Zip()` 无 zipper、`WithIndex()`、`Dict.ToList()` 的元素）在 IL 侧还原为
  `object?[]`，不是 CLR `ValueTuple`；目前只保证计数、打印与取元素可用，直接在 IL 里按元组解构尚不支持。
- 63 个方法旧的 `GenerateIlInternal` / helper 仍在，已失效但尚未删除（死代码清理单列 Todo）。

## 4. 结论

本次改动使 `List` / `Dictionary` / `Array` 剩余的实例方法在 IL 模式下不再报「方法未找到」，
纯函数、变异、链式调用与旧 `Ldnull` 桩均有覆盖；既有测试无新增失败，并顺带修复 6 项集合相关失败。
