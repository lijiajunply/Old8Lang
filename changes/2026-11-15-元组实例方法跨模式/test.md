# 元组实例方法的 IL / 虚拟机支持 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「元组实例方法只有解释器可用」。

## 1. 现象

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `(1,2,3).Get(1)` | `2` | `INVALID_OPERATION_ERROR: 方法 'Get' 未找到` | `实例必须是 Tuple 类型` |
| `(1,2,3).Slice(0,2)` | `(1, 2)` | 方法未找到 | — |
| `(1,2,3).ToList()` | `[1, 2, 3]` | 方法未找到 | — |
| `(1,2,3).All(...)` | `true` | 方法未找到 | 参数必须是函数 / 实例必须是 Tuple |

IL 的元组字面量是扁平 `ValueTuple`，VM 是右嵌套 `Tuple<object?,object?>`；实例方法都注册在
`TupleLangValue` 上，三种模式的表示不一致导致只有解释器能用。

## 2. 修复

1. **IL**：`InstanceMethodTypeResolver.TryGetEquivalentLangValueType` 把原生 `ITuple`
   映射到 `TupleLangValue`，`IlNativeCollectionBridge` 放行元组接收者并保留元组结果
   （避免显示退化为数组的 `[1, 2]`）；`IlValueBridge.Wrap` 原本已能把 `ValueTuple`
   转成 `TupleLangValue`。
2. **虚拟机**：`InvokeTypeMethod` 在接收者是原生 `ITuple` 时先按 VM 的右嵌套编码
   （`FlattenVmTuple`）展平、转成 `TupleLangValue` 再按普通实例方法分派，
   lambda 参数因此走 `ExecuteInVMInternal` 的 `vm.CallFunctionObject` 路径。
3. **VM 方法实现**：`TupleSliceMethod.ExecuteInVMInternal` 接受 `TupleLangValue`
   （直接复用解释器 `Slice`）；`LangListContainsMethod.ExecuteInVMInternal`
   把原生实参包成 `LangValueType` 后再调用 `ILangList.In`。
4. `TupleGetMethod.ExecuteInVMInternal` 返回前 `IlValueBridge.Unwrap`，与列表 `Get`
   一样给调用方原生值。

## 3. 验证

### 3.1 新增回归测试

- `Old8Lang.Tests/Compiler/Collections/TupleMethodIlTests.cs`（1 项，9 个断言）：
  `Get` / `Slice` / `ToList` / `Reverse` / `Contains` / `All` / `Any` / `Filter` / `Map`；
- `Old8Lang.Tests/VirtualMachine/Collections/VMTupleMethodTests.cs`（1 项，8 个断言）。

修复前两个用例分别报「方法未找到」与「实例必须是 Tuple 类型」；修复后通过。

### 3.2 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| `(1,2,3).Get(1)` | 2 | 2 | 2 |
| `(1,2,3).Slice(0,2).ToStr()` | `(1, 2)` | `(1, 2)` | `(1, 2)` |
| `(1,2,3).ToList().ToStr()` | `[1, 2, 3]` | `[1, 2, 3]` | `[1, 2, 3]` |
| `(1,2,3).Reverse().ToStr()` | `[3, 2, 1]` | `[3, 2, 1]` | `[3, 2, 1]` |
| `(1,2,3).Map((x) -> x * 2)` | `[2, 4, 6]` | `[2, 4, 6]` | `[2, 4, 6]` |

### 3.3 回归面

整组对比：Compiler 2011 例 / 206 失败 / 1780 通过 / 25 跳过；
VirtualMachine 1111 项、Interpreter 1984 项（1 跳过）全通过。

## 4. 遗留

1. VM 的元组用右嵌套表示，`(1, (2, 3))` 与 `(1, 2, 3)` 无法区分，展平时按「末元素是
   元组则继续展开」处理（与 VM 既有 `FlattenVmTuple` 一致）；
2. VM 展平会忽略 `null` 元素（现有 `FlattenVmTuple` 的约定），`(1, null)` 在元组方法里
   只会看到 1 个元素；
3. `d.Add(...)` 的返回值此前解释器是元组、IL/VM 是数组，本轮顺带统一为元组
   （`DictAddMethod` 的 helper 与 VM 分支）。
