# 需求规格：解释器测试覆盖待复核清零

## 目标

逐项复核 `changes/Todo.md` 中「解释器测试覆盖待复核」一节列出的 10 类场景（14 项测试清单），在核对现有测试与实际实现的基础上补齐缺失的解释器单元测试，并据实记录核对中发现的实现现状与设计差异。

## 范围

1. **Threading/**：`ThreadSynchronizationTests.cs`、`ConcurrentPrimitiveTests.cs`
2. **Modules/Extern/**：`ManagedDllExternTests.cs`（`.NET` 托管程序集 `C#:` / `cs:` / `csharp:` / `dotnetdll:` 导入）
3. **Modules/**：`ModuleNamespaceTests.cs`、`ModuleReloadTests.cs`
4. **Linq/**：`LinqGroupByTests.cs`、`LinqQueryContinuationTests.cs`
5. **Classes/**：`SuperExpressionTests.cs`
6. **EdgeCases/**：`ParserErrorTests.cs`、`RuntimeTypeErrorTests.cs`、`CircularReferenceTests.cs`
7. **Testing/**：`TestUtilitiesTests.cs`（`Assert`、`TestRunner`、`Mock`）
8. **Types/**：`TypeTemplateTests.cs`
9. **Expressions/**：`ConstantFoldingTests.cs`（核实 `ConstantLangValue` 实际用途为模块导出常量包装，解释器无独立常量折叠遍，覆盖常量表达式求值、不可变性及 `ConstantLangValue` 模块常量导出）
10. **Statements/**：在 `EnumTests.cs` 中补充枚举标志位组合、枚举转换/成员属性/跨枚举比较与 `EnumTemplate` 行为测试

## 明确不做

- 不顺手修改与本任务无关的既有失败测试或编译器/VM 缺陷（发现问题记入 `changes/Todo.md`）。
- 不依赖 `Thread.Sleep` 墙钟竞态断言；并发测试优先使用确定性同步原语（`Join`、`CountDownLatch`、`CyclicBarrier`、`Channel`、`Mutex`），避免引入默认档抖动。
- 不为不存在的语言特性强行修改语言语法或语义（例如解释器没有独立常量折叠优化遍、枚举不支持自定义方法体或按位运算符），按当前真实实现覆盖并如实记录。

## 验收标准

1. 上述 10 类场景的测试全部补齐且位于 `Old8Lang.Tests/Interpreter/` 对应目录。
2. 所有新增及修改的测试类在默认档下稳定通过（0 失败、0 跳过），且重复运行无抖动。
3. 架构测试不受影响；`changes/2026-11-27-解释器测试覆盖复核/test.md` 记录完整命令与实测结果；`changes/Todo.md` 对应条目更新勾选状态与核对结论。
