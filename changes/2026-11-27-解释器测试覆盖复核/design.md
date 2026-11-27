# 设计与核对结论

## 现有覆盖与实现核对

1. **Threading/**：已有 `ThreadTests.cs` 与 `SpawnTests.cs`，但缺少多线程同步专题与 7 类并发原语（`Mutex`、`Semaphore`、`AtomicInt`、`Channel`、`ReadWriteLock`、`CountDownLatch`、`CyclicBarrier`）在解释模式下的系统测试；同时覆盖 `LockedVariableLangValue` 的 `.Value`/`.Get()`/`.Set()`/`.Add()`/`.Increment()`/`.Decrement()`/`.Update()`。
2. **Modules/Extern/**：`NativeDllExternTests.cs`、`PythonExternTests.cs`、`JavaScriptExternTests.cs` 均放在 `Interpreter/Modules/Extern/` 且继承 `ModuleImportTestBase`，故 `ManagedDllExternTests.cs` 放在同目录保持一致。自定义 `dotnetdll:` 通过测试程序集自身暴露的公共静态帮助类加载，无需引入外部二进制依赖。
3. **Modules/**：`ModuleSystemService` 由 `ImportStatementRuntimeExtensions.ModuleService` 持有单例 `ModuleLoader` / `CacheManager`，并按 `VariateManager.LoadedModuleExports` 缓存单次执行中的导出。`ModuleNamespaceTests.cs` 覆盖命名空间隔离、跨模块符号访问与符号冲突处理；`ModuleReloadTests.cs` 覆盖 `CacheManager` / `ModuleLoader` 缓存管理、磁盘文件更新后清缓存重载及依赖链更新。
4. **Linq/**：`LinqQueryExecutor` 已完整支持 `GroupByClause`（返回含 `Key`/`Values` 键的字典列表）与 `QueryContinuation`（`into` 递归执行后续查询），但解释器侧缺专用测试。
5. **Classes/**：`SuperProxy` 已支持 `super.init(...)`、`super.ParentClass(...)`、`super.method(...)`、`super.field` 及父类成员缺失/无父类报错，需补充专门用例。
6. **EdgeCases/**：现有 `UnexpectedInputsTests.UnexpectedInputs_RecursionLimit_HandlesExcessiveRecursion` 实际调用 `infinite(0)` 未触发递归。补充 `ParserErrorTests.cs`、`RuntimeTypeErrorTests.cs`、`CircularReferenceTests.cs`（覆盖数据结构循环引用、真实 `MaxRecursionDepth=1000` 触发与类实例环）。
7. **Testing/**：`LangInterpreter` 预注册全局对象 `Assert`、`TestRunner`、`Mock`（注意全局名是 `Mock` 而非 `MockLib`）。因 `TestRunner` 使用静态状态，`TestUtilitiesTests` 标 `[Collection("Sequential")]` 并在用例首尾调用 `TestRunner.Clear()`。
8. **Types/**：`TypeTemplate` 与 `GenericConstraintValidator` 支持 `new()`、`class`、`struct`、接口/基类、类型参数约束及嵌套泛型，补充 `Types/TypeTemplateTests.cs` 覆盖现有 `GenericClassTests`/`GenericFunctionTests` 未测到的约束与嵌套场景。
9. **Expressions/**：经源码核实，`ConstantLangValue` 是模块通配符导出常量时使用的 `ImportInfo` 包装类（`SymbolExtractor.WrapConstantsAsImportInfo`），并非编译期常量折叠 AST 节点；解释器当前无独立常量折叠优化遍。`ConstantFoldingTests.cs` 覆盖常量声明与不可变性、常量表达式求值/传播，以及 `ConstantLangValue` 在模块导出中的实际行为。
10. **Statements/EnumTests.cs**：经源码核实，Old8Lang 无按位运算符（`|`/`&` 仅用于联合/交叉类型注解），`EnumTemplate` 仅支持成员访问、`EnumLangValue` 支持整数算术/比较/显式转换（`as int` / `as string` / `as double` / `as enum`）与元数据属性。在 `EnumTests.cs` 中补充 2 的幂标志位组合与检测、枚举转换与 `EnumTemplate`/`EnumLangValue` 行为、跨枚举比较错误与不允许的静态方法调用错误。
