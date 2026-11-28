# 实施计划

## 阶段 1：核对与建档
- 梳理 10 类测试项对应的源码实现与已有测试，输出 `spec.md`、`design.md`、`plan.md`、`task.md`。

## 阶段 2：并发与托管 Extern 测试（第 1–2 项）
- 新增 `Old8Lang.Tests/Interpreter/Threading/ThreadSynchronizationTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Threading/ConcurrentPrimitiveTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Modules/Extern/ManagedDllExternTests.cs`
- 定向运行并确认全通过。

## 阶段 3：模块、LINQ 与 Super 测试（第 3–5 项）
- 新增 `Old8Lang.Tests/Interpreter/Modules/Namespace/ModuleNamespaceTests.cs`（或 `Modules/` 下规范子目录）
- 新增 `Old8Lang.Tests/Interpreter/Modules/Reload/ModuleReloadTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Linq/LinqGroupByTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Linq/LinqQueryContinuationTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Classes/SuperExpressionTests.cs`
- 定向运行并确认全通过。

## 阶段 4：错误处理、测试工具、类型模板、常量与枚举（第 6–10 项）
- 新增 `Old8Lang.Tests/Interpreter/EdgeCases/ParserErrorTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/EdgeCases/RuntimeTypeErrorTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/EdgeCases/CircularReferenceTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Testing/TestUtilitiesTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Types/TypeTemplateTests.cs`
- 新增 `Old8Lang.Tests/Interpreter/Expressions/ConstantFoldingTests.cs`
- 扩充 `Old8Lang.Tests/Interpreter/Statements/EnumTests.cs`
- 定向运行并确认全通过。

## 阶段 5：汇总验收
- 统一复跑本轮全部新增/修改测试类（并二次验证并发类无抖动）。
- 写入 `test.md`，勾选 `task.md` 与 `changes/Todo.md`。
