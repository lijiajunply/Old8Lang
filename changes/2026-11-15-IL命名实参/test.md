# IL 命名实参：编译期校验与局部函数重排 —— 测试报告

日期：2026-11-15
关联待办：`changes/Todo.md`「IL 命名实参：错误不校验 / 局部函数值不支持」。

## 1. 现象

| 代码 | 解释器 / VM | IL（修复前） |
|---|---|---|
| `test(a: 1, invalid: 2)` | 运行期 `ARGUMENT_ERROR`（或编译期） | 忽略命名实参 / 不报错 |
| `test(a: 1, a: 2, c: 3)` | `ARGUMENT_ERROR 命名参数重复` | 不报错 |
| `test()`（缺必需参数） | `ARGUMENT_ERROR` | 编译成功、运行期才报 |
| `multiply(b: 2, a: 1)`（局部 lambda） | 正常 | `NAME_ERROR 'multiply' 未定义` |

`NamedArgumentsErrorTests` 7 项、`NamedArgumentsTests` 3 项失败。

## 2. 修复

- 所有带命名实参的调用统一重建为解释器 `Instance` 执行
  （`IlRuntimeFunctions.InvokeInstance`），命名重排/默认值/错误校验与解释器一致；
- 对已注册函数在**编译期**先做命名实参校验：
  参数名不存在、重复、与位置参数冲突、必需参数未提供、`params` + 命名实参；
  多个重载时逐个候选校验，选第一个能匹配的；
- 局部 lambda 赋值时把形参列表登记到 `LocalManager.FuncParameters`，命名实参按形参名
  重排成位置实参后再走局部委托调用。

## 3. 验证

- `NamedArgumentsErrorTests` 7 → 2 项失败；
- `NamedArgumentsTests` 3 → 1 项失败；
- `Compiler.Functions` 26 → 17 失败（另含本轮迁移掉的高阶函数旧类型注解）；
- 整组回归见 `changes/Todo.md`。

## 4. 遗留

- `calc.compute(op: ..., x: ...)` 类方法命名实参仍不支持（Dot 路径未重排）；
- 高阶函数把命名实参里的 lambda 传给 `IlDelegateFuncLangValue` 时仍报参数缺失；
- 纯位置调用缺参数（`test()`）仍是运行期 ArgumentError，尚未在编译期校验。
