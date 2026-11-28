# 当前优先处理清零 — 测试记录

日期：2026-11-23。以下结果针对当前工作区，包含未提交改动。

- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter 'FullyQualifiedName~IlAsyncGeneratorStreamTests|FullyQualifiedName~VMUnexpectedInputsTests|FullyQualifiedName~AsyncGeneratorTests'`：71 通过、0 失败、1 跳过。覆盖私有字段不能借外部函数首参或调用栈获取权限、生成器逐项暂停及释放等待清理，以及 `IAsyncEnumerable<int>` 适配。
- `dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --filter 'FullyQualifiedName~Old8Lang.Tests.Compiler.Async|FullyQualifiedName~Old8Lang.Tests.Compiler.EdgeCases|FullyQualifiedName~Old8Lang.Tests.Compiler.Exceptions|FullyQualifiedName~Old8Lang.Tests.VirtualMachine.Async|FullyQualifiedName~Old8Lang.Tests.VirtualMachine.EdgeCases|FullyQualifiedName~Old8Lang.Tests.VirtualMachine.Classes|FullyQualifiedName~Old8Lang.Tests.VirtualMachine.Reflection|FullyQualifiedName~Old8Lang.Tests.Interpreter.Async|FullyQualifiedName~Old8Lang.Tests.Architecture'`：719 通过、1 失败、5 跳过。
- 唯一失败是既有 `Architecture.ScalarLiteralExpressionTests.AsyncStateMachine_FindsAwaitInsideSyntaxCollections`：用例中的表达式内部 await 已被编译器明确拒绝，详见全局 Todo。故完整 Architecture 验收尚未通过；没有改动该与本次范围无关的用例。
- 程序集依赖及后端、语法边界四个 Architecture 测试类：27 通过、0 失败（筛选 `ArchitectureProjectDependencyTests|BackendAssemblyBoundaryTests|SyntaxBackendBoundaryTests|SyntaxParserBoundaryTests`）。
- `git diff --check`：通过。

只读审查发现 VM 权限检查借用了任意调用帧和首参，以及异步生成器提前执行和释放不等待清理；已修复并补回归。审查对值类型异步流反射的疑虑经实际测试未复现。生成器目前通过线程池线程等待下一项，存在并发规模限制，已记录到全局 Todo。
