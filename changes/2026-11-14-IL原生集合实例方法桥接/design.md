# IL 原生集合实例方法桥接 — 方案

## 现状与问题根因

IL 模式下集合字面量生成原生 CLR 表示：

| 语言值 | IL 表示 |
|---|---|
| `{1, 2}` / `[...]` 列表 | `List<object>` |
| `[1, 2]` 数组 | `object[]` |
| 字典 | `Dictionary<object, object>` |

实例方法注册表按 `LangValueType` 家族注册。`InstanceMethodTypeResolver.ResolveInstanceMethod`
在接收者是原生集合时，会再按「等价 LangValueType」（`ListLangValue` / `ArrayLangValue` /
`DictionaryLangValue`）解析一次，但**只采纳实现了 `IIlNativeValueInstanceMethod` 的方法**。
未实现接口的 63 个方法因此解析不到，IL 发射继续走旧反射回退，最终抛
`方法 'X' 未找到`。

若把这些方法直接标记为 `IIlNativeValueInstanceMethod`，它们的 `GenerateIlInternal` 又会把
原生接收者传给形参为 `ListLangValue` / `List<object?>` / `LangValueType` 的 helper，类型不一致
（这些实现分散写于不同阶段，约定并不统一）；`GroupBy` 等仍是 `Ldnull` 桩。

## 备选方案

### A. 逐个改写 63 个方法的 IL（原 Todo 指定机制）

- 优点：遵循既有 `IIlNativeValueInstanceMethod` + `IlValueBridge` + 静态 helper 约定，性能最好。
- 缺点：工作量大（63 个方法各自要核对 helper 签名、参数装箱、返回表示），且易引入新的
  无效 IL；本次无法在一个阶段内全部完成并逐一验证。

### B. 统一桥接：编译期在原位调用解释器实现（选定）

在编译期仍按注册表解析方法；对「原生集合接收者 + 方法存在但未实现 IL 接口」的情况，发射：

```
wrap(receiver)                       // IlValueBridge.Wrap → ListLangValue / ArrayLangValue / DictionaryLangValue
"MethodName"
new object[] { wrap(arg0), wrap(arg1), ... }
IlNativeCollectionBridge.Invoke(...) // Runtime：解析 → Execute → Unwrap
```

运行期桥接内部：

1. `IlValueBridge.Wrap` 把原生接收者包成等价 LangValue；字典额外执行一次 `Accept`
   以把 `Tuples` 填充进 `Value`（`IlValueBridge.Wrap` 只构造 `Tuples`）。
2. 用 `InstanceMethodRegistry.ResolveMethod` 按接收者语言类型与实参解析具体方法。
3. 调用 `IInstanceMethod.Execute`（即解释器实现），lambda 实参由既有
   `IlDelegateFuncLangValue` 承接，回调仍调用编译好的委托。
4. 对变异方法（名单见下）把包装值的变化同步回原生集合。
5. `IlValueBridge.Unwrap` 把结果转回原生值；新增 `TupleLangValue → object?[]` 分支。

- 优点：一次覆盖全部剩余方法，语义与解释器完全同源；无新增手写 IL，风险集中在少量代码。
- 缺点：调用走一次包装/解析，性能低于专用 IL；对单个方法的热点优化要后续再做（已在 Todo 记录）。

## 变异方法同步名单

| 原生接收者 | 方法名（忽略大小写） |
|---|---|
| `Array` | `Set` |
| `IList`（`List<T>`） | `Add`、`AddList`、`AddRange`、`Insert`、`Remove`、`RemoveAt`、`Clear`、`Pop` |
| `IDictionary` | `Add`、`Remove`、`Clear`、`Update` |

解释器中 `Sort` / `Reverse` / `Merge` / `Clone` 等返回新集合，不做同步。

判定用解析到的方法实例的规范名（`Names[0]`），而不是调用点写法：这样字典的 `Delete` / `delete`
别名也会命中 `Remove` 的同步规则。同步前先完成全部值的还原与元素类型校验，再清空/写回原生容器，
避免中途失败留下半截状态。

## 返回类型映射（IL 栈类型）

`GetReturnType` 的声明值与 IL 栈类型必须一致，否则后续装箱/字段访问会生成无效 IL：

| 声明类型 | IL 返回类型 |
|---|---|
| `ListLangValue` / `List<object?>` | `List<object?>` |
| `ArrayLangValue` | `object?[]` |
| `DictionaryLangValue` / `Dictionary<object,object>` | `Dictionary<object,object?>` |
| `TupleLangValue` | `object?[]` |
| `void` / `VoidLangValue` | `void`（调用后 `pop`） |
| `int` / `bool` / `double` / `char` / `string` / `T[]` | 原类型（值类型 `unbox.any`，引用类型 `castclass`） |
| `LangValueType` / `object` / 其他 | `object` |

映射集中在 `InstanceMethodTypeResolver.MapIlBridgeReturnType`，IL 发射与
`TypeInferenceVisitor` 共用；类型推断对新路径返回同一映射结果。

## 受影响文件

- `Old8Lang.Runtime/InstanceMethods/Core/IlNativeCollectionBridge.cs`（新增，运行期桥接）
- `Old8Lang.Runtime/InstanceMethods/Core/IlValueBridge.cs`（`Unwrap` 增加元组）
- `Old8Lang.Runtime/Execution/OperationHelpers/InstanceMethodTypeResolver.cs`（桥接解析与返回类型映射）
- `Old8Lang.Compiler/OperationHelpers/DotOperatorILHelper.cs`
  （静态类型为原生集合时的桥接发射；`object` 动态分派补桥接兜底）
- `Old8Lang.Tests/Compiler/Collections/Advanced/NativeCollectionMethodBridgeTests.cs`（新增测试）
- `changes/Todo.md`、`Docs/CHANGELOG.md`

## 风险与回滚

- 桥接会遮蔽原生 CLR 同名方法（如 `List<T>.Contains`）：这是有意为之，语言注册表优先，
  保证跨模式语义一致；若出现意外差异，按方法名加入「专用 IL」白名单即可逐步收敛。
- 变异同步按名单执行；若某方法实际有副作用但未列入名单，表现为「原生集合未变」。
  测试用例覆盖每个名单项，后续新增方法需同步名单。
- 回滚点：`DotOperatorILHelper` 中桥接调用处直接返回 `false` 即恢复旧行为，其余代码可保留。
