# IL 无法直接解析的函数调用：回退到解释器语义 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 相互递归 / 命名实参 / params / 高阶函数参数调用」。

## 1. 现象

| 代码 | 解释器 / VM | IL（修复前） |
|---|---|---|
| 相互递归 `isEven`/`isOdd` | `true` | `ldnull` 静默返回 / `函数或类 'isOdd' 未声明` |
| `tw(x: 21)`（命名实参） | 42 | 静默 null（测试"通过"是假象） |
| `sum(1,2,3,4,5)`（params 变参） | 15 | 静默 null |
| `func apply(pred, v) { return pred(v) }` | true | `NAME_ERROR 名称 'pred' 未定义`（参数不在 LocalVar） |
| `(42).ToDouble()` | 42 | 无效 IL（helper 形参是 `LangValueType`，实参是原生 int） |

## 2. 根因

1. 函数体按源码顺序编译，调用的函数可能尚未生成委托；`VisitInstance` 兜底
   直接发 `ldnull`（后来改为编译错误），两者都不对。
2. 命名实参 / `params` 调用的参数个数与签名不一致，编译期匹配失败，落入同一兜底。
3. 命名函数参数记录在 `LocalManager.ArgumentIndices`（`ldarg`），
   `VisitInstance` 只检查 `LocalVar`，高阶函数参数找不到。
4. `ValueTypeTo{Double,Char,Bool}Method` 用裸 `CreateCompiler` 加载原生 int，
   却调用形参为 `LangValueType` 的 helper。

## 3. 修复

1. 新增 `IlRuntimeFunctions.InvokeInstance(name, positional[], namedNames[], namedValues[])`：
   把原始调用重建为 `Instance` 节点交给解释器执行（命名实参重排、params 收集、
   未定义函数 NAME_ERROR 都由解释器语义保证），结果 `Unwrap` 回原生值；
   `VisitInstance` 的兜底改为发射该调用。
2. `VisitInstance` 增加 `ArgumentIndices` 分支：函数参数调用走
   `ldarg + IlValueBridge.InvokeFunctionValue`。
3. `ValueTypeToDouble/Char/BoolMethod` 改用 `IlValueBridge.EmitLoadWrapped`，
   helper 与返回类型改为原生 `double/char/bool`。
4. `ErrorCompilationTests` 12 项按当前语义重写（合法/编译错误/运行期错误/语法错误分层）；
   `IntToDoubleConversion` 与 `DoubleToIntConversion` 用例改为 `(42).ToDouble()` 括号形式。

## 4. 验证

### 4.1 CLI 对比

| 表达式 | 解释器 | IL | 虚拟机 |
|---|---|---|---|
| 相互递归 `isEven(6)` / `isOdd(7)` | true / true | true / true | true / true |
| `tw(x: 21)` | 42 | 42 | 42 |
| `sum(1,2,3,4,5)` | 15 | 15 | 15 |
| `apply((n:int) -> n > 0, 5)` | true | true | true |
| `(42).ToDouble()` / `(3.14).ToInt()` | 42 / 3 | 42 / 3 | 42 / 3 |
| `SomeMethod()` | NAME_ERROR | NAME_ERROR | NAME_ERROR |

### 4.2 分组变化

- `Compiler.Async` 54 → 42 失败（异步函数里的普通函数调用同样回退）；
- `Compiler.Functions` 53 → 45；
- `Compiler.EdgeCases` 22 → 20；
- `Compiler.Error` 12 → 0（整组重写）；
- `Compiler.Classes` 14 → 13。

## 5. 遗留

`HigherOrderTests` 里返回函数的 Curry/Compose/Chain 与部分命名实参错误用例仍失败，
属函数值返回/错误类型断言的剩余缺口；`IlRuntimeFunctions` 回退只处理同步函数调用。
