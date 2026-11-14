# IL 原生集合实例方法桥接 — 阶段计划

## 阶段 1：运行期桥接（Runtime）

1. 新增 `IlNativeCollectionBridge`：
   - `Invoke` / `TryInvoke`：包装接收者与实参、解析方法、执行、还原结果；
   - 字典包装后 `Accept` 填充 `Value`；
   - 变异方法同步回原生 `Array` / `IList` / `IDictionary`。
2. `IlValueBridge.Unwrap` 增加 `TupleLangValue → object?[]`。
3. 验证点：Runtime 可编译；编写最小单测前先由阶段 4 的端到端用例驱动。

依赖：无。

## 阶段 2：编译期接入（Compiler）

1. `InstanceMethodTypeResolver` 增加：
   - `ResolveIlBridgeMethod`（原生集合 → 等价 LangValueType 的桥接方法解析）；
   - `MapIlBridgeReturnType`（声明类型 → IL 栈类型）；
   - `InferIlBridgeReturnType`，并接入 `InferInstanceMethodReturnType` 的兜底。
2. `DotOperatorILHelper.GenerateRegularInstanceMethodCall`：
   - 注册表解析失败后尝试桥接发射（接收者 + 方法名 + 包装实参数组）；
   - 按映射结果做 `pop` / `unbox.any` / `castclass`。
3. `InvokeDynamicObjectMethod`：`object` 静态接收者的动态路径补语言桥接兜底
   （用于字典索引得到的集合、链式调用中的 `object` 结果）。

依赖：阶段 1。

## 阶段 3：类型推断一致性

- 确认 `TypeInferenceVisitor` 走 `InferInstanceMethodReturnType` 时得到与 IL 栈类型相同的类型；
- 若与 `OperationTypeHelper.GetBuiltInMethodReturnType` 冲突（如 `Count` / `ToArray` / `Length`），
  以先命中者为准，但两者结果需一致。

依赖：阶段 2。

## 阶段 4：测试与文档

1. 新增 `NativeCollectionMethodBridgeTests`（IL 模式）：
   - 纯函数：`Chunk`、`Flatten`、`Pairwise`、`Slice`、`TakeLast`、`SkipLast`、`WithIndex`、
     `Aggregate`、`CartesianProduct`、`Combinations`、`SetEquals`、`Overlaps`、
     `IsSubsetOf` / `IsSupersetOf`、`SymmetricExcept`、`GroupAdjacent`、排序算法方法；
   - 字典：`ContainsKey` / `ContainsValue` / `GetValue` / `GetOrElse` / `IsEmpty` / `Clone` /
     `Merge` / `ToList` / `Count`；
   - 数组：`Get` / `Set` / `Slice` / `Contains` / `Reverse` / `Permutations` / `SetEquals` /
     `Overlaps` / `GroupAdjacent`；
   - 变异：列表增删改清、字典增删改清、数组 `Set`，并验证别名观察同一对象；
   - 链式：`Chunk(2).Count()`、`GroupBy(...).Keys()`、`Filter(...).Count()`；
   - 旧桩：`GroupBy`、`Zip(other, zipper)`（若语法支持）。
2. 解释模式对照（同一段代码在 `LangInterpreter` 下执行）至少覆盖变异与链式用例。
3. 跑 `--filter "FullyQualifiedName~NativeCollectionMethodBridgeTests"` 与
   `--filter "FullyQualifiedName~Collections"`。
4. 更新 `changes/Todo.md`、`Docs/CHANGELOG.md`，写 `test.md`。

依赖：阶段 2、3。

## 回滚点

- 阶段 2 完成后若出现大面积回归，先把桥接调用降级为「仅当方法名不在专用 IL 白名单」，
  再逐项排查；阶段 1 代码可独立保留。
