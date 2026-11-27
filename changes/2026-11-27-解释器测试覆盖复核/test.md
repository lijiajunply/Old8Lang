# 测试报告：解释器测试覆盖待复核

测试日期：2026-11-27

## 运行命令

```bash
# 1. 全量运行 15 个新增/扩充的解释器测试类（默认档）
FILTER="FullyQualifiedName~Old8Lang.Tests.Interpreter.Threading.ThreadSynchronizationTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Threading.ConcurrentPrimitiveTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Extern.ManagedDllExternTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Namespace.ModuleNamespaceTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Modules.Reload.ModuleReloadTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Linq.LinqGroupByTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Linq.LinqQueryContinuationTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Classes.SuperExpressionTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.EdgeCases.ParserErrorTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.EdgeCases.RuntimeTypeErrorTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.EdgeCases.CircularReferenceTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Testing.TestUtilitiesTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Types.TypeTemplateTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Expressions.ConstantFoldingTests|FullyQualifiedName~Old8Lang.Tests.Interpreter.Statements.EnumTests"
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --filter "$FILTER"

# 2. 并发与同步类连续二次复跑验证无抖动
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --no-build --filter "FullyQualifiedName~ThreadSynchronizationTests|FullyQualifiedName~ConcurrentPrimitiveTests"
```

## 实测结果

- 15 个解释器测试类总计：**134 通过，0 失败，0 跳过**（其中新增 14 个测试文件共 110 项用例，`EnumTests.cs` 新增 4 项用例，合计新增 **114 项**）。
- 并发与同步测试（`ThreadSynchronizationTests` + `ConcurrentPrimitiveTests`，共 18 项）连续 3 次运行均 **18/18 通过**（耗时 ~260 ms），无抖动。

| 序号 | 测试类 | 用例数 | 结果 | 说明 |
|---|---|---:|---|---|
| 1a | `Interpreter.Threading.ThreadSynchronizationTests` | 7 | 通过 | `Lock`、`Mutex` 临界区、`CountDownLatch` 顺序、`Channel` 生产消费、`CyclicBarrier` 阶段同步 |
| 1b | `Interpreter.Threading.ConcurrentPrimitiveTests` | 11 | 通过 | `Mutex`、`Semaphore`、`AtomicInt`、`Channel`、`ReadWriteLock`、`CountDownLatch`、`CyclicBarrier` 与并发工具函数 |
| 2 | `Interpreter.Modules.Extern.ManagedDllExternTests` | 9 | 通过 | `C#:` / `cs:` / `csharp:` / `dotnetdll:`、方法块、单方法、别名、命名参数、异常检查 |
| 3a | `Interpreter.Modules.Namespace.ModuleNamespaceTests` | 5 | 通过 | 命名空间隔离、跨模块符号访问、别名解决冲突、提升导入覆盖、模块闭包隔离 |
| 3b | `Interpreter.Modules.Reload.ModuleReloadTests` | 4 | 通过 | `CacheManager`/`ModuleLoader` 缓存管理、清缓存热重载、依赖更新、单次运行重复导入去重 |
| 4a | `Interpreter.Linq.LinqGroupByTests` | 6 | 通过 | `group by` 基础分组、元素投影、聚合、多键复合分组、前后过滤与空源 |
| 4b | `Interpreter.Linq.LinqQueryContinuationTests` | 5 | 通过 | `select/group ... into`、延续中 `let`/`where`/`orderby`、二段查询、作用域不泄漏 |
| 5 | `Interpreter.Classes.SuperExpressionTests` | 7 | 通过 | `super` 调父类方法/重载、读父类字段、`super.init`/`super.ParentClass`、链式调用与报错 |
| 6a | `Interpreter.EdgeCases.ParserErrorTests` | 12 | 通过 | 不完整语句、非法表达式、未闭合模板、行号定位准确性与错误后恢复 |
| 6b | `Interpreter.EdgeCases.RuntimeTypeErrorTests` | 16 | 通过 | 空引用成员/方法/索引/算术错误、数组/列表/字符串/元组正负越界、类型转换与实参错误 |
| 6c | `Interpreter.EdgeCases.CircularReferenceTests` | 8 | 通过 | 自引用/互引用列表与字典、合法深度与相互递归、递归上限守卫、类实例自引用与环 |
| 7 | `Interpreter.Testing.TestUtilitiesTests` | 8 | 通过 | `Assert` 全部断言簇、`TestRunner` 套件/单测/统计、`Mock` 桩/间谍/校验/重置 |
| 8 | `Interpreter.Types.TypeTemplateTests` | 8 | 通过 | `TypeTemplate` 元数据、泛型继承、`new()`/`class`/`struct`/接口/基类/类型参数约束、推断与嵌套泛型 |
| 9 | `Interpreter.Expressions.ConstantFoldingTests` | 4 | 通过 | 常量表达式求值、`:const` 传播与不可变性、`ConstantLangValue` 包装及模块通配符导出 |
| 10 | `Interpreter.Statements.EnumTests` | 24 | 通过 | 原有 20 项 + 新增 4 项（2 的幂标志位组合与检测、`as string`/元数据、跨枚举比较、静态方法调用报错） |

## 复核中发现并记入 `changes/Todo.md` 的无关实现缺口

按开发硬规则 6，本轮未顺手修改解释器实现，已全部登记到 `changes/Todo.md`：

1. **三层及以上继承中连续 `super` 调用栈溢出**：`SuperProxy.GetDirectParentMetadata()` 始终取 `currentInstance.Metadata.ParentClassName`，导致中间父类调 `super` 递归回自身。
2. **集合/元组字面量按 AST 节点缓存单一可变实例**：`InterpreterVisitor.SyntaxAdapters.cs` 复用并原地 `Clear()` 同一个 `TupleLangValue`/`DictionaryLangValue`，导致 LINQ `group ... by (a, b)` 与 `select { ... }` 在多次迭代间互相覆写。
3. **带别名模块导入在调用者当前作用域执行模块体**：`ModuleSystemService` 未为 `import "mod" as alias` 隔离模块作用域，两个别名模块的同名顶层变量会互相覆盖。
4. **`MaxRecursionDepth = 1000` 高于托管线程栈容量**：无限递归在触发语言级递归上限前即触发不可捕获的 CLR `StackOverflowException`。
5. **其他边角缺口**：调用非函数值（如 `num <- 42; num(1)`）未抛错；`LinqParser` 不支持链式多段 `into ... into ...`；`GenericConstraintValidator.ValidateTypeParameterConstraint`（`where T: U`）未解析用户类继承；`EnumLangValue + EnumLangValue` 未自动解包右操作数。

## 结论

`changes/Todo.md`「解释器测试覆盖待复核」全部 10 类测试项均已完成复核、补测并 100% 通过。
