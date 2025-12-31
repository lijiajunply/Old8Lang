# 解释模式测试待办事项

## 当前测试覆盖情况

**已有测试统计**:
- Async: 5 个测试文件 (AsyncFunctionTests, AsyncGeneratorTests, AsyncStreamTests, AwaitTests, TaskAPITests)
- Basic: 3 个测试文件 (AssignmentTests, ExpressionTests, VariableTests)
- Classes: 8 个测试文件 (ClassDeclarationTests, ClassInstantiationTests, ConstructorTests, GenericClassTests, InheritanceTests, InterfaceTests, MemberAccessTests, MixinTests)
- Collections: 7 个测试文件 (ArrayTests, CollectionMethodsTests, DictionaryTests, ListTests, SliceTests, SyncIteratorTests, TupleTests)
- EdgeCases: 5 个测试文件 (BoundaryTests, EmptyInputTests, ExtremeValuesTests, TypeErrorsTests, UnexpectedInputsTests)
- Exceptions: 5 个测试文件 (ErrorPropagationTests, FinallyTests, NestedExceptionTests, ThrowTests, TryCatchTests)
- Expressions: 11 个测试文件 (ArithmeticTests, ComparisonTests, ExtendedRangeTests, InExpressionTests, LogicalTests, MatchExpressionEnhancedTests, MatchExpressionTests, RangeTests, StringTemplateTests, TernaryTests, TypeConversionTests)
- Functions: 8 个测试文件 (ClosureTests, FunctionCallTests, FunctionDeclarationTests, FunctionOverloadTests, GenericFunctionTests, GenericTypeInferenceTests, HigherOrderTests, LambdaTests)
- Integration: 3 个测试文件 (EndToEndTests, InterpreterIntegrationTests, InterpreterTests)
- Linq: 4 个测试文件 (LinqBasicExecutionTests, LinqEdgeCasesTests, LinqErrorTests, LinqLetOrderByTests)
- Modules: 16 个测试文件（包含基础导入、高级导入、错误处理、集成测试等）
- Statements: 6 个测试文件 (ConditionalTests, ControlFlowTests, EnumTests, JumpStatementsTests, LoopTests, SwitchTests)
- Threading: 1 个测试文件 (SpawnTests)
- Types: 2 个测试文件 (GenericCollectionTypesInterpreterTests, UnionTypesInterpreterTests)

**总计**: 84 个测试文件

## 需要补充的测试

### 1. 文件头指令测试 (FileHeader/)

**优先级**: 中

文件头指令是 Old8Lang 的重要特性，但目前缺少系统测试。

- [ ] `FileHeaderDirectiveTests.cs` - 文件头指令基础测试
  - 测试元数据指令：encoding, author, version, date, description
  - 测试编译器配置指令：debug, verify-il, type-inference, optimize
  - 测试指令解析规则（必须在文件开头、大小写不敏感等）
  - 测试无效指令的处理

**示例测试场景**:
```old8
#!encoding utf-8
#!author 测试作者
#!version 1.0.0
#!debug true
```

### 2. 交叉类型测试 (Types/)

**优先级**: 中高

语法文档中提到了交叉类型（Intersection Types），但当前没有专门测试。

- [ ] `IntersectionTypesInterpreterTests.cs` - 交叉类型测试
  - 泛型约束中的交叉类型
  - 多接口实现的交叉类型
  - 交叉类型的兼容性规则
  - 交叉类型错误处理

**示例测试场景**:
```old8
func sort<T>(items: List<T>) -> List<T> where T: IComparable & ICloneable {
    // 测试泛型约束
}
```

### 3. 异步并发增强测试 (Async/)

**优先级**: 中

当前已有基础异步测试，但可能需要更全面的测试覆盖。

- [ ] `AsyncConcurrencyTests.cs` - 异步并发场景测试
  - 多个异步任务并发执行
  - 异步任务同步和协调
  - 异步任务超时和取消
  - 异步任务异常处理

- [ ] `CancellationTokenTests.cs` - 取消令牌测试
  - CancellationToken 基础用法
  - CancellationTokenSource 创建和管理
  - 任务取消传播
  - 取消后的清理工作

**AST 节点支持**: `CancellationTokenLangValue`, `CancellationTokenSourceLangValue`

### 4. 线程相关测试增强 (Threading/)

**优先级**: 中

当前只有 `SpawnTests`，但语法文档和 AST 节点显示有更多线程功能。

- [ ] `ThreadTests.cs` - 线程基础测试
  - Thread 类基础用法
  - 线程创建和启动
  - 线程状态检查（IsAlive等）
  - Thread.Sleep 等静态方法
  - Thread.CurrentThread() 获取当前线程

- [ ] `ThreadSynchronizationTests.cs` - 线程同步测试
  - 线程锁和同步机制
  - 多线程访问共享资源
  - 死锁检测和避免

**AST 节点支持**: `ThreadClassLangValue`, `ThreadLangValue`, `ThreadStaticMethodWrapper`, `LockedVariableLangValue`

### 5. 任务相关测试增强 (Async/)

**优先级**: 中

AST 中有丰富的 Task 相关节点，但当前测试覆盖不够全面。

- [ ] `TaskCompletionSourceTests.cs` - TaskCompletionSource 测试
  - TaskCompletionSource 创建和使用
  - 手动完成任务
  - 任务结果设置
  - 任务异常设置

- [ ] `TaskSchedulerTests.cs` - TaskScheduler 测试
  - 任务调度器基础用法
  - 自定义任务调度
  - 任务优先级

- [ ] `TaskFactoryTests.cs` - TaskFactory 测试
  - TaskFactory 创建任务
  - 任务创建选项
  - 任务延续

**AST 节点支持**: `TaskCompletionSourceLangValue`, `TaskSchedulerClassLangValue`, `TaskFactoryClassLangValue`

### 6. 本地方法绑定测试 (Integration/)

**优先级**: 中

语法文档提到 `native` 关键字用于绑定 C# 方法，需要测试。

- [ ] `NativeMethodBindingTests.cs` - 本地方法绑定测试
  - native 语句基础用法
  - 绑定静态方法
  - 绑定实例方法
  - 参数类型映射
  - 返回值类型映射
  - 异常处理

**AST 节点支持**: `NativeStatement`, `NativeAnyLangValue`, `NativeStaticAny`

**示例测试场景**:
```old8
native func WriteLine(s:string) -> void from System.Console.WriteLine
```

### 7. 模块高级功能测试 (Modules/)

**优先级**: 中低

当前模块测试已较完善，但可能需要更多边界情况测试。

- [ ] `ModuleNamespaceTests.cs` - 模块命名空间测试
  - 模块命名空间隔离
  - 跨模块符号访问
  - 模块符号冲突处理

- [ ] `ModuleReloadTests.cs` - 模块重载测试
  - 模块热重载
  - 模块缓存管理
  - 模块依赖更新

**AST 节点支持**: `UnifiedModule`, `ImportInfo`, `LazySymbolProxy`

### 8. LINQ 高级查询测试 (Linq/)

**优先级**: 中低

当前 LINQ 测试较完善，但可能需要更多复杂查询场景。

- [ ] `LinqJoinTests.cs` - LINQ Join 操作测试
  - join 子句基础用法
  - 多表关联查询
  - 左连接、内连接
  - join 性能测试

- [ ] `LinqGroupByTests.cs` - LINQ GroupBy 操作测试
  - group by 子句基础用法
  - 分组聚合
  - 多键分组
  - 分组后过滤

- [ ] `LinqQueryContinuationTests.cs` - LINQ 查询延续测试
  - into 关键字用法
  - 查询延续链式调用

**AST 节点支持**: `JoinClause`, `GroupByClause`, `QueryContinuation`

### 9. 列表推导式测试 (Collections/)

**优先级**: 中

AST 中有 `ListComprehension` 节点，但可能缺少专门测试。

- [ ] `ListComprehensionTests.cs` - 列表推导式测试
  - 基础列表推导式
  - 带条件的推导式
  - 多重循环推导式
  - 嵌套推导式

**AST 节点支持**: `ListComprehension`

**示例测试场景**:
```old8
// [expression for item in iterable if condition]
squares <- [x * x for x in range(10) if x % 2 == 0]
```

### 10. Super 表达式测试 (Classes/)

**优先级**: 中低

AST 中有 `SuperExpression` 和 `SuperProxy` 节点，测试类继承时需要覆盖。

- [ ] `SuperExpressionTests.cs` - super 关键字测试
  - super 调用父类方法
  - super 访问父类字段
  - super 在构造函数中的使用
  - super 链式调用

**AST 节点支持**: `SuperExpression`, `SuperProxy`

### 11. 错误处理增强测试 (ErrorCases/)

**优先级**: 中

可以添加更多错误场景测试，确保解释器健壮性。

- [ ] `ParserErrorTests.cs` - 解析器错误测试
  - 语法错误恢复
  - 不完整语句处理
  - 错误提示信息准确性

- [ ] `RuntimeTypeErrorTests.cs` - 运行时类型错误测试
  - 类型不匹配错误
  - 空引用错误
  - 越界访问错误

- [ ] `CircularReferenceTests.cs` - 循环引用测试
  - 数据结构循环引用
  - 函数递归深度
  - 类实例循环引用

### 12. 枚举增强测试 (Statements/)

**优先级**: 低

当前有 `EnumTests`，但可能需要更全面的测试。

- [ ] 在现有 `EnumTests.cs` 中补充：
  - 枚举自定义值
  - 枚举值转换
  - 枚举标志位组合
  - 枚举方法和属性

**AST 节点支持**: `EnumInit`, `EnumTemplate`

### 13. 性能和压力测试 (Performance/)

**优先级**: 低

添加性能基准测试，虽然有独立的 Benchmarks 项目，但解释器测试中也可以包含一些性能验证。

- [ ] `InterpreterPerformanceTests.cs` - 解释器性能测试
  - 大数据集处理
  - 深度递归
  - 复杂表达式计算
  - 内存使用测试

### 14. 集合嵌套访问测试 (Collections/)

**优先级**: 中

AST 中有 `NestedIndexAccess` 和 `NestedSliceAccess` 节点。

- [ ] `NestedAccessTests.cs` - 嵌套访问测试
  - 多维数组嵌套索引
  - 嵌套字典访问
  - 嵌套切片操作
  - 混合嵌套访问

**AST 节点支持**: `NestedIndexAccess`, `NestedSliceAccess`

**示例测试场景**:
```old8
matrix <- [[1, 2], [3, 4]]
value <- matrix[0][1]  // 嵌套索引访问
```

### 15. Mock 和测试工具类测试 (Testing/)

**优先级**: 低

AST 中有 `MockLibClassLangValue`, `TestRunnerClassLangValue`, `AssertClassLangValue` 等节点。

- [ ] `TestUtilitiesTests.cs` - 测试工具类测试
  - Assert 类用法
  - TestRunner 类用法
  - MockLib 类用法
  - 测试辅助功能

**AST 节点支持**: `AssertClassLangValue`, `TestRunnerClassLangValue`, `MockLibClassLangValue`

### 16. 类型模板测试 (Types/)

**优先级**: 中

AST 中有 `TypeTemplate` 节点，用于类型参数和泛型。

- [ ] `TypeTemplateTests.cs` - 类型模板测试
  - 泛型类型参数
  - 类型约束
  - 类型参数推断
  - 嵌套泛型类型

**AST 节点支持**: `TypeTemplate`, `GenericParameter`, `GenericInstanceExpression`

### 17. 异步 for-in 循环测试 (Async/)

**优先级**: 中

AST 中有 `AsyncForInStatement` 节点。

- [ ] 在现有异步测试中补充或创建新测试：
  - async for-in 循环基础用法
  - 异步迭代器
  - 异步序列处理

**AST 节点支持**: `AsyncForInStatement`

### 18. 常量优化测试 (Expressions/)

**优先级**: 低

AST 中有 `ConstantLangValue` 节点，用于常量折叠。

- [ ] `ConstantFoldingTests.cs` - 常量折叠测试
  - 编译时常量计算
  - 常量表达式优化
  - 常量传播

**AST 节点支持**: `ConstantLangValue`

## 测试优先级汇总

### 高优先级（建议立即补充）
暂无紧急缺失

### 中高优先级（重要但不紧急）
1. 交叉类型测试 (Types/)
2. 文件头指令测试 (FileHeader/)

### 中优先级（后续补充）
3. 异步并发增强测试 (Async/)
4. 线程相关测试增强 (Threading/)
5. 任务相关测试增强 (Async/)
6. 本地方法绑定测试 (Integration/)
7. 列表推导式测试 (Collections/)
8. 集合嵌套访问测试 (Collections/)
9. 类型模板测试 (Types/)
10. 异步 for-in 循环测试 (Async/)
11. 错误处理增强测试 (ErrorCases/)

### 中低优先级（可选增强）
12. 模块高级功能测试 (Modules/)
13. LINQ 高级查询测试 (Linq/)
14. Super 表达式测试 (Classes/)
15. 枚举增强测试 (Statements/)

### 低优先级（长期优化）
16. 性能和压力测试 (Performance/)
17. 常量优化测试 (Expressions/)
18. Mock 和测试工具类测试 (Testing/)

## 实施建议

### 阶段 1: 类型系统完善 (1 周)
1. 交叉类型测试
2. 类型模板测试
3. 文件头指令测试

### 阶段 2: 异步和并发 (2 周)
4. 异步并发增强测试
5. 线程相关测试增强
6. 任务相关测试增强
7. 异步 for-in 循环测试

### 阶段 3: 集合和表达式 (1 周)
8. 列表推导式测试
9. 集合嵌套访问测试
10. 本地方法绑定测试

### 阶段 4: 其他增强 (2 周)
11. 错误处理增强测试
12. Super 表达式测试
13. LINQ 高级查询测试
14. 模块高级功能测试

### 阶段 5: 可选优化 (按需)
15. 枚举增强测试
16. 性能和压力测试
17. 常量优化测试
18. Mock 和测试工具类测试

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

3. **与编译模式对比**:
   - 解释模式测试覆盖更全面（84 vs 29 个测试文件）
   - 许多特性在解释模式下更容易测试
   - 解释模式测试可以作为编译模式测试的参考

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
- 编译模式测试: `Old8Lang.Tests/Compiler/`
- AST 节点定义: `Old8Lang/AST/`
- 语法规范: `Docs/Old8Lang_Grammar.md`
- EBNF 规范: `Old8Lang/Old8Lang.ebnf`
