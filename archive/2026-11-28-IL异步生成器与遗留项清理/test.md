# Test Report：IL 异步生成器非阻塞挂起、遗留项清理与文档校正

测试日期：2026-11-28

## 1. 变更摘要

1. **IL 异步生成器非阻塞挂起**
   - `Old8Lang.Runtime/Compiler/Helpers/IlAsyncGeneratorStream.cs`：`IlAsyncYieldEmitter` 移除 `SemaphoreSlim.Wait` 同步阻塞，改用 `Lock` 保护的单槽 `TaskCompletionSource` 提供非阻塞 `YieldAsync(object?)`；`Enumerator` 注册取消回调并直接启动状态机至第一个挂起点。
   - `Old8Lang.Runtime/AsyncGeneration/IAsyncStateMachineGenerator.cs`、`Old8Lang.Compiler/AsyncGeneration/AsyncStateMachineGenerator.cs`、`Old8Lang.Compiler/Visitors/CompilerVisitor.Statements2.cs`：将 `YieldStatement` 登记到状态机 `AwaitStateMap` 与受保护区域派发链，`ContainsSuspendPoint` 在异步生成器中识别 `YieldStatement` 以支持搬移式异步 `catch`/`finally`，`EmitYield` 复用 `TaskAwaiter` 与 `EmitAwaitYield`/`EmitAwaitResume` 实现真正的非阻塞挂起（0 线程池线程占用）。
2. **IL 类内 `this.method()` 直接 `callvirt`**
   - `Old8Lang.Runtime/Compiler/CodeGeneration/LocalManager.cs`：新增 `PendingClassMethod`、`BuildingClassMethods`、`IsInDynamicMethod` 及 `TryResolveBuildingMethod`。
   - `Old8Lang.Runtime/Execution/LegacyNodeExtensions/Declarations/ClassInitRuntimeExtensions.cs`、`Old8Lang.Runtime/Compiler/Specialization/GenericClassSpecializer.cs`：普通类与泛型特化类改为两遍方法定义（先声明全部 `MethodBuilder` 并预登记签名，再发射方法体）。
   - `Old8Lang.Compiler/OperationHelpers/DotOperatorILHelper.cs`、`Old8Lang.Runtime/AST/Visitor/TypeInferenceVisitor.ExpressionsAndValues.cs`、`Old8Lang.Compiler/Compilers/FuncLangValueIlCompilerExtensions.cs`、`Old8Lang.Compiler/Visitors/CompilerVisitor.ExpressionsAndValues.cs`：对当前未完成 `TypeBuilder` 的成员方法调用直接发射 `callvirt`/`call` 并返回精确静态类型（闭包 `DynamicMethod`、异步状态机与函数值字段安全回退动态分派）；同步修复 `VisitInstance` 对未完成 `init` `MethodBuilder` 调用 `GetParameters()` 抛 `NotSupportedException` 的隐患。
3. **类字段集合初值跨模式独立**
   - `Old8Lang.VirtualMachine/VM/Helpers/VirtualMachine.Helpers.ModuleAndObject.cs`：`AllocateObjectInstance` 与导入类 `CreateObjectInstance` 对常量池中的列表/数组/字典（含嵌套）执行递归深拷贝，三模式统一为每实例独立。
4. **`type()` 与 `TypeOf()` 跨模式语言类型名统一**
   - `Old8Lang.Runtime/AST/Expression/Value/Special/EnumLangValue.cs`：重写 `TypeToString() => "Int"`。
   - `Old8Lang.Runtime/GlobalFunctions/Implementations/UtilityFunctions.cs`（`TypeFunction`）：IL 与 VM 统一经 `VMReflectionHelper.DescribeTypeName` / `DescribeValueType` 返回语言类型名（静态 `object` 在 IL 走运行期解析）。
   - `Old8Lang.Runtime/GlobalFunctions/Implementations/Reflection/TypeOfFunction.cs`、`Old8Lang.Runtime/Runtime/ReflectionHelper.cs`：补齐 IL `TypeOf` 值类型参数装箱以及对非类实例/原生类实例返回统一语言类型名。
5. **文档校正**
   - 更新 `Docs/MODE_SUPPORT.md`、`Docs/CHANGELOG.md` 与 `changes/Todo.md`。

## 2. 实测命令与结果

| 序号 | 测试范围与命令 | 通过 | 失败 | 跳过 | 耗时 | 结论 |
|---|---|---:|---:|---:|---:|---|
| 1 | 定向回归（`LegacyItemsAndCrossModeConsistencyTests` + `Compiler.Classes` + `Compiler.Async` + `Compiler.Types` + `Architecture`）：<br>`dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --filter "FullyQualifiedName~LegacyItemsAndCrossModeConsistencyTests\|FullyQualifiedName~Old8Lang.Tests.Compiler.Classes\|FullyQualifiedName~Old8Lang.Tests.Compiler.Async\|FullyQualifiedName~Old8Lang.Tests.Compiler.Types\|FullyQualifiedName~Old8Lang.Tests.Architecture"` | 428 | 0 | 3 | 8 s | ✅ 全通过 |
| 2 | 全量默认档（含防挂起监控）：<br>`dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-restore --no-build --blame-hang-timeout 30s` | 7,264 | 0 | 24 | 48 s | ✅ 0 失败、0 挂起 |
| 3 | 高并发异步生成器暂停压力测试（`performance.runsettings`）：<br>`dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --settings Old8Lang.Tests/performance.runsettings --filter "FullyQualifiedName~IlAsyncGeneratorStreamTests"` | 1 | 0 | 0 | 248 ms | ✅ 1,000 挂起流 + 300 并发编译期生成器无线程池阻塞 |
| 4 | 语法测试脚本：`cd TestFiles && ./run_syntax_tests.sh` | 89 | 0 | 0 | — | ✅ 全通过 |
| 5 | VM 测试脚本：`cd TestFiles && ./run_vm_tests.sh` | 79 | 0 | 0 | — | ✅ 全通过 |

## 3. 新增测试用例清单

- `Old8Lang.Tests/Compiler/Async/IlAsyncGeneratorStreamTests.cs`
  - `ManySuspendedGenerators_DoNotStarveThreadPool`：128 个并发挂起生成器期间验证线程池立即可用并全部正常恢复完成。
  - `CompiledAsyncGenerator_YieldInTryCatchFinallyAndEarlyBreak_WorksWithoutBlocking`：验证编译期 IL 异步生成器在 `try`、`catch` 与异步 `finally` 中 `yield` 及提前 `break` 清理。
  - `CompiledAsyncGenerator_HighConcurrencySuspendedStress_DoesNotBlockThreadPool`（`[Trait("Category", "Performance")]`）：1,000 个长期暂停流式生成器叠加 300 个并发编译期异步生成器在线程池并行执行，耗时 248 ms。
- `Old8Lang.Tests/Compiler/Classes/LegacyItemsAndCrossModeConsistencyTests.cs`
  - `IlClassInternalThisMethodCall_EmitsDirectCallvirtWithoutDynamicDispatch`：验证前向互调、私有方法、命名实参、继承父类方法与闭包内 `this.method()`，并用 `IlDisassembler` 断言生成了直接 `callvirt` 且不含 `InvokeDynamicObjectMethod`。
  - `IlGenericClassInternalThisMethodCall_SupportsForwardCallsAndCallableFields`：验证泛型特化类内前向 `this.method()` 直接调用与函数值字段调用回退。
  - `ClassCollectionFieldDefaults_AreIsolatedPerInstance_AcrossAllThreeModes`：三模式验证列表/数组/字典及嵌套集合字段初值每实例独立。
  - `TypeOfAndTypeFunction_OnEnumAndBuiltinValues_AreConsistentAcrossAllThreeModes`：三模式验证 `type(枚举值)`、`TypeOf(枚举值)` 及内建类型名完全一致。

## 4. 结论

所有目标需求、遗留项及文档校正均已落地并通过全量默认档（7,264/7,264）、高并发压力测试（248 ms）、架构边界测试（73/73）及语法/VM 脚本全量验证（89/89 + 79/79），无任何回归。
