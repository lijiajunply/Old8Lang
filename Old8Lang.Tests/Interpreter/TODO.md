# 解释模式测试待办事项

> 清点日期：2026-10-09。以下所有数字均由 `Old8Lang.Tests/Interpreter/` 下的实际文件清点得出，
> 修改本文件前请先重新清点，避免出现「把已有测试列为待补」的情况。

## 当前测试覆盖情况

**已有测试统计**（17 个子目录，共 131 个测试文件）:

- Async: 8 个测试文件 (AsyncConcurrencyTests, AsyncFunctionTests, AsyncGeneratorTests, AsyncStreamTests, AwaitTests, CancellationTokenTests, TaskAPITests, TaskAdvancedTests)
- Basic: 4 个测试文件 (AssignmentTests, ExpressionTests, PreprocessorInterpreterTests, VariableTests)
- Classes: 10 个测试文件 (ClassDeclarationTests, ClassInstantiationTests, ConstructorTests, ExtensionMethodInterpreterTests, GenericClassTests, GenericConstraintExtensionInterpreterTests, InheritanceTests, InterfaceTests, MemberAccessTests, MixinTests)
- Collections: 9 个测试文件 (ArrayTests, CollectionMethodsTests, DictionaryTests, ListComprehensionTests, ListTests, NestedAccessTests, SliceTests, SyncIteratorTests, TupleTests)
- EdgeCases: 5 个测试文件 (BoundaryTests, EmptyInputTests, ExtremeValuesTests, TypeErrorsTests, UnexpectedInputsTests)
- Exceptions: 5 个测试文件 (ErrorPropagationTests, FinallyTests, NestedExceptionTests, ThrowTests, TryCatchTests)
- Expressions: 12 个测试文件 (ArithmeticTests, AsIsExpressionTests, ComparisonTests, ExtendedRangeTests, InExpressionTests, LogicalTests, MatchExpressionEnhancedTests, MatchExpressionTests, RangeTests, StringTemplateTests, TernaryTests, TypeConversionTests)
- FileHeader: 2 个测试文件 (FileHeaderConfigTests, FileHeaderDirectiveTests)
- Functions: 12 个测试文件 (ClosureTests, DecoratorInterpreterTests, FunctionCallTests, FunctionDeclarationTests, FunctionOverloadTests, GenericFunctionTests, GenericTypeInferenceTests, HigherOrderTests, LambdaTests, NamedArgumentsErrorTests, NamedArgumentsTests, ParamsInterpreterTests)
- Integration: 3 个测试文件 (EndToEndTests, InterpreterIntegrationTests, InterpreterTests)
- Linq: 6 个测试文件 (LinqBasicExecutionTests, LinqEdgeCasesTests, LinqErrorTests, LinqJoinTests, LinqLetOrderByTests, ListAdvancedMethodsTests)
- Modules: 28 个测试文件（含基础导入、高级导入、Extern、标准库、错误处理、统一模块架构等；`Modules/Core/` 下为测试基建，未计入）
- Performance: 11 个测试文件 (LongRunningPerformanceTests, LoopExecutionPerformanceTests, MediumProgramPerformanceTests, MemoryStabilityTests, PerformanceMonitorTests, PerformanceOptimizationTests, PerformanceReporterTests, RecursiveCallPerformanceTests, SmallScriptPerformanceTests, VariableCacheTests, VariableLookupPerformanceTests)
- Reflection: 2 个测试文件 (ReflectionTests, TypeLangValueReflectionTests)
- Statements: 9 个测试文件 (ConditionalTests, ControlFlowTests, DeferStatementTests, EnumTests, JumpStatementsTests, LoopTests, SelectStatementTests, SwitchTests, UsingStatementTests)
- Threading: 2 个测试文件 (SpawnTests, ThreadTests)
- Types: 3 个测试文件 (GenericCollectionTypesInterpreterTests, IntersectionTypesInterpreterTests, UnionTypesInterpreterTests)

**总计**: 131 个测试文件

## 已完成的补充测试

以下条目曾作为「待补测试」列在本文件中，对应测试文件现已存在，不再重复列入待办：

| 原待补项 | 已落地文件 |
|---------|-----------|
| 文件头指令测试 | `FileHeader/FileHeaderDirectiveTests.cs`、`FileHeader/FileHeaderConfigTests.cs` |
| 交叉类型测试 | `Types/IntersectionTypesInterpreterTests.cs` |
| 异步并发测试 | `Async/AsyncConcurrencyTests.cs` |
| 取消令牌测试 | `Async/CancellationTokenTests.cs` |
| 线程基础测试 | `Threading/ThreadTests.cs`（含 Thread.Sleep、Join、Name、Priority、IsAlive 等） |
| Task 相关测试 | `Async/TaskAPITests.cs`（已覆盖 TaskCompletionSource / TaskScheduler / TaskFactory）、`Async/TaskAdvancedTests.cs` |
| LINQ Join 测试 | `Linq/LinqJoinTests.cs`（含 `into g` 分组联接） |
| 列表推导式测试 | `Collections/ListComprehensionTests.cs` |
| 集合嵌套访问测试 | `Collections/NestedAccessTests.cs` |
| 性能测试 | `Performance/` 下 11 个文件（循环、递归、内存、变量查找等） |
| 异步 for-in 循环测试 | `Async/AsyncStreamTests.cs`、`Async/AsyncGeneratorTests.cs` |
| 枚举基础测试 | `Statements/EnumTests.cs`（空枚举、单成员、负值、switch 等 20 例） |

## 需要补充的测试

### 1. 线程同步与并发原语测试 (Threading/)

**优先级**: 中高

`ThreadTests.cs` 覆盖了线程本身，但语法文档 §8.4 列出的并发原语在解释器侧没有测试。

- [ ] `ThreadSynchronizationTests.cs` - 线程同步测试
  - 多线程访问共享资源
  - 锁与临界区行为
  - 同步顺序与竞态场景

- [ ] `ConcurrentPrimitiveTests.cs` - 并发原语测试
  - Mutex（互斥锁）加锁/释放
  - Semaphore（信号量）计数与等待
  - AtomicInt（原子整数）并发自增
  - Channel（通道）收发
  - ReadWriteLock（读写锁）、CountDownLatch（倒计时锁）、CyclicBarrier（循环栅栏）

**AST 节点支持**: `LockedVariableLangValue`, `ThreadStaticMethodWrapper`

### 2. .NET 托管方法绑定测试 (Integration/)

**优先级**: 中高

绑定 C# 方法原本写作 `native func ... from ...`，解析器现已统一为 `extern`，`native` 关键字不再存在
（见 `LangParser/Parsers/StatementParser.ImportAndNative.cs`，其中 `Expect(LangTokenType.Extern)`）。
`Modules/Extern/` 下已覆盖 C/C++ P/Invoke（`NativeDllExternTests.cs`）、Python（`PythonExternTests.cs`）、
JavaScript（`JavaScriptExternTests.cs`），但语法文档 §5.15 的 .NET 托管程序集形式（`"C#:"` / `"cs:"` /
`"csharp:"` / `"dotnetdll:"` 前缀）在解释器侧仍无测试。

- [ ] `ManagedDllExternTests.cs` - .NET 托管 DLL 导入测试
  - 方法块导入：`extern "C#:System" Math { func Pow(x:double, y:double) -> double, ... }`
  - 单个方法导入：`extern "C#:System" Math func Pow(x:double, y:double) -> double`
  - 带别名的导入：`func Pow(x:double, y:double) -> double as Power`
  - 导入自定义 DLL：`extern "dotnetdll:MyLibrary.dll" MyMathClass { ... }`（需准备测试程序集）
  - 命名参数调用：`Pow(y: 3.0, x: 2.0)`
  - 程序集不存在时的异常处理（实测报 `IMPORT_ERROR`）
  - `cs:` / `csharp:` 前缀与 `C#:` 的等价性

> 已实测（2026-10-09，解释模式）：上述 `{ func ... }` 块、单个 `func`、`as` 别名、命名参数四种写法均可用；
> 而 `extern "C#:System" Math *` 和 `extern "C#:System" Math as SysMath` 会报
> `语法错误：缺少 'func' 关键字`——这两种写法属于 `NativeStatement` 解析路径，带 `C#:` 等托管前缀时走的是
> `ParseExternStatement`，因此写用例时不要照搬 §5.14.2/§5.14.4 的 `*`、`as` 示例（语法文档 §5.14 的复核说明
> 也已记录同类示例问题）。

**AST 节点支持**: `NativeStatement`, `NativeAnyLangValue`, `NativeStaticAny`

**示例测试场景**:
```old8
extern "C#:System" Math {
    func Pow(x:double, y:double) -> double,
    func Sqrt(x:double) -> double
}
result <- Pow(2.0, 10.0)
```

### 3. 模块命名空间与重载测试 (Modules/)

**优先级**: 中

`Modules/AdvancedImport/` 已覆盖条件导入、动态导入、惰性导入，但命名空间隔离与模块重载仍无测试。

- [ ] `ModuleNamespaceTests.cs` - 模块命名空间测试
  - 模块命名空间隔离
  - 跨模块符号访问
  - 模块符号冲突处理

- [ ] `ModuleReloadTests.cs` - 模块重载测试
  - 模块热重载
  - 模块缓存管理
  - 模块依赖更新

**AST 节点支持**: `UnifiedModule`, `ImportInfo`, `LazySymbolProxy`

### 4. LINQ 查询语法 group by / into 延续测试 (Linq/)

**优先级**: 中低

`ListAdvancedMethodsTests.cs` 覆盖的是 `List.GroupBy` 方法形式，`LinqJoinTests.cs` 中带 `into g` 的用例只有一条；
查询语法 `group x by ...` 在解释器侧尚无执行用例（解析器侧有 `Parser/Linq/LinqAdvancedParsingTests.cs`）。

- [ ] `LinqGroupByTests.cs` - 查询语法 group by 测试
  - `group x by <key>` 基础用法
  - 分组后聚合（Count / Sum / Max）
  - 多键分组
  - 分组后过滤

- [ ] `LinqQueryContinuationTests.cs` - 查询延续测试
  - `into` 关键字用法
  - 查询延续链式调用

**AST 节点支持**: `GroupByClause`, `QueryContinuation`

### 5. Super 表达式测试 (Classes/)

**优先级**: 中低

`super` 目前只在 `MemberAccessTests.cs`、`MixinTests.cs` 中顺带出现，没有专门用例。

- [ ] `SuperExpressionTests.cs` - super 关键字测试
  - super 调用父类方法
  - super 访问父类字段
  - super 在构造函数中的使用
  - super 链式调用

**AST 节点支持**: `SuperExpression`, `SuperProxy`

### 6. 错误处理增强测试 (EdgeCases/)

**优先级**: 中

`EdgeCases/TypeErrorsTests.cs` 已覆盖常见的类型不匹配运算，以下场景仍缺测试。

- [ ] `ParserErrorTests.cs` - 解析器错误测试
  - 语法错误恢复
  - 不完整语句处理
  - 错误提示信息准确性

- [ ] `RuntimeTypeErrorTests.cs` - 运行时类型错误测试
  - 空引用错误
  - 越界访问错误

- [ ] `CircularReferenceTests.cs` - 循环引用测试
  - 数据结构循环引用
  - 函数递归深度
  - 类实例循环引用

> 注：`Modules/ErrorHandling/CircularDependencyTests.cs` 测的是模块循环依赖，与此处的数据结构循环引用不是同一类场景。

### 7. 测试工具类测试 (Testing/)

**优先级**: 中低

AST 中有 `MockLibClassLangValue`, `TestRunnerClassLangValue`, `AssertClassLangValue` 等节点，解释器侧无测试。

- [ ] `TestUtilitiesTests.cs` - 测试工具类测试
  - Assert 类用法
  - TestRunner 类用法
  - MockLib 类用法
  - 测试辅助功能

**AST 节点支持**: `AssertClassLangValue`, `TestRunnerClassLangValue`, `MockLibClassLangValue`

### 8. 类型模板测试 (Types/)

**优先级**: 中

AST 中有 `TypeTemplate` 节点，用于类型参数和泛型。

- [ ] `TypeTemplateTests.cs` - 类型模板测试
  - 泛型类型参数
  - 类型约束
  - 类型参数推断
  - 嵌套泛型类型

**AST 节点支持**: `TypeTemplate`, `GenericParameter`, `GenericInstanceExpression`

### 9. 常量优化测试 (Expressions/)

**优先级**: 低

AST 中有 `ConstantLangValue` 节点，用于常量折叠。

- [ ] `ConstantFoldingTests.cs` - 常量折叠测试
  - 编译时常量计算
  - 常量表达式优化
  - 常量传播

**AST 节点支持**: `ConstantLangValue`

### 10. 枚举增强测试 (Statements/)

**优先级**: 低

`EnumTests.cs` 已覆盖声明、取值、比较、switch，但以下场景仍缺：

- [ ] 在现有 `EnumTests.cs` 中补充：
  - 枚举标志位组合
  - 枚举方法和属性

**AST 节点支持**: `EnumInit`, `EnumTemplate`

## 测试优先级汇总

### 高优先级（建议立即补充）
暂无紧急缺失

### 中高优先级（重要但不紧急）
1. 线程同步与并发原语测试 (Threading/)
2. .NET 托管方法绑定测试 (Integration/)

### 中优先级（后续补充）
3. 模块命名空间与重载测试 (Modules/)
4. 测试工具类测试 (Testing/)
5. 类型模板测试 (Types/)
6. 错误处理增强测试 (EdgeCases/)

### 中低优先级（可选增强）
7. LINQ 查询语法 group by / into 测试 (Linq/)
8. Super 表达式测试 (Classes/)

### 低优先级（长期优化）
9. 常量优化测试 (Expressions/)
10. 枚举增强测试 (Statements/)

## 实施建议

### 阶段 1: 并发与互操作 (1 周)
1. 线程同步与并发原语测试
2. .NET 托管方法绑定测试

### 阶段 2: 类型系统完善 (1 周)
3. 类型模板测试
4. 错误处理增强测试

### 阶段 3: 模块与工具 (1 周)
5. 模块命名空间与重载测试
6. 测试工具类测试

### 阶段 4: 可选优化 (按需)
7. LINQ 查询语法 group by / into 测试
8. Super 表达式测试
9. 常量优化测试
10. 枚举增强测试

## 注意事项

1. **解释模式特点**:
   - 类型注解是可选的
   - 支持完全动态类型
   - 更灵活的函数参数
   - 运行时类型检查

2. **测试策略**:
   - 重点测试动态类型特性
   - 测试运行时错误处理
   - 验证类型推断正确性
   - 确保向后兼容性

3. **与 IL 模式对比**:
   - 两侧规模已接近：解释模式 131 个测试文件，IL 模式 134 个（均按文件名以 `Tests.cs` 结尾清点）
   - 部分动态特性（泛型、运算符重载、Python 互操作）只在解释模式与虚拟机模式下可测
   - 解释模式测试可以作为其他模式测试的参考

4. **代码质量**:
   - 保持测试代码清晰可读
   - 使用描述性测试名称
   - 添加足够的测试注释
   - 遵循现有测试风格

## 测试文件命名规范

- 使用清晰的描述性名称（如 `AsyncConcurrencyTests.cs`）
- 以 `Tests.cs` 结尾
- 放在合适的子目录下
- 与现有测试保持一致的命名风格

## 相关资源

- 当前解释模式测试: `Old8Lang.Tests/Interpreter/`
- IL 模式测试: `Old8Lang.Tests/Compiler/`
- 虚拟机模式测试: `Old8Lang.Tests/VirtualMachine/`
- AST 节点定义: `Old8Lang/AST/`
- 语法规范: `Docs/Old8Lang_Grammar.md`
- EBNF 规范: `Docs/Old8Lang.ebnf`
