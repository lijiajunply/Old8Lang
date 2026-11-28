# Design：IL 异步生成器非阻塞挂起与遗留项清理

## 1. IL 异步生成器非阻塞状态机挂起

### 涉及程序集
- `Old8Lang.Runtime`：
  - `Compiler/Helpers/IlAsyncGeneratorStream.cs`（`IlAsyncYieldEmitter` 与 `IlAsyncGeneratorStream`）
  - `AsyncGeneration/IAsyncStateMachineGenerator.cs`（`EmitYield` 签名新增 `YieldStatement` 参数）
- `Old8Lang.Compiler`：
  - `AsyncGeneration/AsyncStateMachineGenerator.cs`
  - `Visitors/CompilerVisitor.Statements2.cs`

### 协议与状态机设计
1. `IlAsyncYieldEmitter` 移除 `SemaphoreSlim` 阻塞等待，改为基于 `lock (_sync)` 保护的单槽 `TaskCompletionSource? _resumeTcs`：
   - `public Task YieldAsync(object? value)`：
     - 加锁检查 `_cancelled || _cancellationToken.IsCancellationRequested`；若已取消直接返回 `Task.FromCanceled(...)`。
     - 创建 `new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)` 并写入 `_resumeTcs`，解锁后 `_channel.Writer.TryWrite(value)`，返回 `resumeTcs.Task`。
     - 先发布 `_resumeTcs` 再写入通道，保证消费者读到元素并触发 `Resume()` 时 `_resumeTcs` 必然可见，无丢失唤醒竞态。
   - `internal void Resume()`：取出并清空 `_resumeTcs`，调用 `tcs?.TrySetResult()`。
   - `internal void Cancel()`：标记 `_cancelled = true`，取出并清空 `_resumeTcs`，调用 `tcs?.TrySetCanceled(...)`。
   - 保留 `public void Yield(object? value)` 兼容包装（委托给 `YieldAsync(value).GetAwaiter().GetResult()`），避免外部直接调用破坏。
2. `IlAsyncGeneratorStream.Enumerator`：
   - 注册 `_cts.Token.Register(static s => ((IlAsyncYieldEmitter)s!).Cancel(), _emitter)`，确保无论 `DisposeAsync()` 还是外部 `CancellationToken` 取消都能立即唤醒挂起的生成器状态机。
   - 首次 `MoveNextAsync()` 直接调用 `_producerTask = RunProducerAsync()` 启动状态机至第一个挂起点（无需 `Task.Run` 额外占用线程）；`DisposeAsync()` 取消并等待 `_producerTask` 完成清理后释放 `CancellationTokenRegistration` 与 `_cts`。
3. `AsyncStateMachineGenerator`：
   - `AnalyzeStatement` 在 `IsAsyncGenerator == true` 时将 `YieldStatement` 登记到 `AwaitStateMap` 和当前 `region.DirectAwaitStates`（与 `AsyncForInStatement` 一致）；若位于 `unsupportedContext`（如 `catch` 过滤器、`defer`）则编译期明确报错。
   - `ContainsSuspendPoint(OldStatement?)` 改为实例方法，`case YieldStatement` 返回 `IsAsyncGenerator || ContainsSuspendPoint(yieldStmt.YieldExpression)`，使位于 `catch` / `finally` 中的 `yield` 自动启用搬移式异步 `catch`/`finally`，避免在 CLR 异常处理器内发射非法 `leave`。
   - `EmitYield(ILGenerator il, YieldStatement yieldStmt, Type valueType)`：
     - 将栈顶值按需装箱存入 `tempValue`，调用 `this.<>y__emitter.YieldAsync(tempValue)` 获得 `Task`。
     - 调用 `Task.GetAwaiter()` 获取 `TaskAwaiter`，检查 `IsCompleted`；未完成时复用 `EmitAwaitYield(il, stateIndex, awaiterLocal)` 与 `EmitAwaitResume(il, stateIndex, awaiterLocal)` 完成状态机挂起与恢复；最后调用 `awaiter.GetResult()`（取消时在此抛出 `OperationCanceledException` 触发 `finally`）。

## 2. IL 类内 `this.method()` 直接 `callvirt`

### 涉及程序集
- `Old8Lang.Runtime`：
  - `Compiler/CodeGeneration/LocalManager.cs`：新增 `PendingClassMethod` 描述（`MethodBuilder`、`Name`、`ParameterTypes`、`ParameterNames`、`ReturnType`、`IsStatic`）与 `Dictionary<TypeBuilder, List<PendingClassMethod>> BuildingClassMethods`（子方法管理器共享同一字典），以及 `bool IsInDynamicMethod` 标记。
  - `Execution/LegacyNodeExtensions/Declarations/ClassInitRuntimeExtensions.cs`：`DefineMethods` 改为两遍：
    - Pass 1：为所有实例方法与静态方法解析参数类型与返回类型、调用 `typeBuilder.DefineMethod` + `DefineParameter` 并将签名登记到 `local.BuildingClassMethods[typeBuilder]`（同时处理 `init` 前缀重载）。
    - Pass 2：依次编译各方法体（此时所有同行方法的 `MethodBuilder` 均已就绪，支持任意顺序互调）。
  - `Compiler/Specialization/GenericClassSpecializer.cs`：`GenerateInstanceMethods` 与 `GenerateStaticMethods` 同样改为两遍并预登记到 `local.BuildingClassMethods[typeBuilder]`。
  - `AST/Visitor/TypeInferenceVisitor.ExpressionsAndValues.cs`：当 `leftType is TypeBuilder tb` 且非字段函数值时，优先在 `local.BuildingClassMethods` 与已完成的 `tb.BaseType` 查找匹配方法并返回精确 `ReturnType`。
- `Old8Lang.Compiler`：
  - `OperationHelpers/DotOperatorILHelper.cs`：当 `leftType is TypeBuilder tb`、当前不在 `DynamicMethod` 闭包中且未命中同名字段函数值时，优先从 `local.BuildingClassMethods` 与已完成的 `tb.BaseType` 解析目标方法（含命名实参重排与形参类型转换/装箱/拆箱），直接发射 `OpCodes.Callvirt`（实例）或 `OpCodes.Call`（静态）；无法静态匹配时安全回退到 `GenerateDynamicObjectMethodCall`。

## 3. 类字段集合初值统一（VM 深拷贝）

### 涉及程序集
- `Old8Lang.VirtualMachine`：`VM/Helpers/VirtualMachine.Helpers.ModuleAndObject.cs`
  - 新增 `CloneFieldDefaultValue(object? value)`，递归克隆 `List<object?>`、`object?[]`、`Dictionary<object, object?>`（及 `ListLangValue`/`ArrayLangValue`/`DictionaryLangValue`）。
  - `AllocateObjectInstance` 对 `ConstantPool.GetConstant(index)` 的返回值先调用 `CloneFieldDefaultValue` 再写入 `instance.Fields`。
  - `CreateObjectInstance` 对模块导入类的字段默认值同步读取 `DefaultValueIndex` 并调用 `CloneFieldDefaultValue`。

## 4. `type(枚举值)` 与 `type()` 跨模式统一

### 涉及程序集
- `Old8Lang.Runtime`：
  - `AST/Expression/Value/Special/EnumLangValue.cs`：重写 `public override string TypeToString() => "Int";`（枚举成员三模式底层值均为整数；类型注解校验继续直接读 `EnumLangValue.EnumTypeName` 不受影响）。
  - `GlobalFunctions/Implementations/UtilityFunctions.cs`（`TypeFunction`）：
    - `GenerateIlInternal`：当推断类型已知且非 `typeof(object)` 时发射 `Ldstr(VMReflectionHelper.DescribeTypeName(typeIdType))`；当为 `typeof(object)` 或未知时，对参数求值并调用运行期 `VMReflectionHelper.DescribeValueType(object?)`。
    - `ExecuteInVMInternal`：返回 `VMReflectionHelper.DescribeValueType(arguments[0])`。

## 5. 文档校正

- 核对 `Docs/MODE_SUPPORT.md` 的支持矩阵、`已知限制`、`三种模式共同的跨模式差异`、`IL 模式`、`虚拟机模式` 与 `测试覆盖情况`，修正全部已过时标记。
- 更新 `changes/Todo.md` 对应条目与核对记录，并在 `Docs/CHANGELOG.md` 按规则补充面向使用者的行为统一说明。
