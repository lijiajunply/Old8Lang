# 命名实参收尾：缺参编译期校验与委托包装 —— 测试报告

日期：2026-11-18
关联待办：`changes/Todo.md`「类方法命名实参 + 高阶函数命名 lambda」。

## 1. 现象

- `test()`（函数要求 2 个必需参数）在 IL 编译成功、运行期才报 ArgumentError，
  而 `NamedArgumentsErrorTests` 期望编译期就报；
- `applyOp(operation: add, y: 20, x: 10)` 报
  `参数错误：函数 '' 的参数 'arg' 缺少实参且没有默认值`：
  - 命名实参走解释器时，lambda 被当作普通对象传递，解释器无法按函数值调用；
  - `IlDelegateFuncLangValue` 的形参名取自 DynamicMethod（名称全为 null，
    统一回落成 `arg`），解释器按名字绑定把参数个数算错。

## 2. 修复

- `VisitInstance`：纯位置调用且函数已注册时，编译期检查实参数量是否满足
  「必需参数 ≤ 实参 ≤ 形参（含默认值）」；不满足直接抛 `ArgumentError`。
- `IlRuntimeFunctions.InvokeInstance`：实参是 `Delegate` 时用 `WrapFunction` 包成
  语言函数值，lambda 可作为命名实参传给解释器函数。
- `IlDelegateFuncLangValue.BuildParameterIds`：改用委托类型自身的 `Invoke` 签名
  （闭包委托的 `Method` 多一个捕获环境参数），参数名为空时生成 `arg0/arg1…` 唯一名。

## 3. 验证

| 用例 | 修复前 | 修复后 |
|---|---|---|
| `test()` 缺必需参数 | 运行期 ArgumentError | 编译期 ArgumentError |
| `Compiler.Functions` | 6 失败 | **5 失败** |
| `Basic.FunctionParameterCountMismatch` | 运行期断言（与新校验冲突） | 迁移为编译期 ArgumentError |
| 高阶函数命名 lambda | 「参数 'arg' 缺少实参」 | 仍失败，但已推进到返回值类型校验（匿名包装函数被当作 void） |

剩余 3 个装饰器返回类型用例仍是静态类型限制；`NamedArguments_MethodCall`
需要类方法参数名（生成的方法没有 `DefineParameter`，无参数名可重排）。
