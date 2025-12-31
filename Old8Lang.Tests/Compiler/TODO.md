# 编译模式测试待办事项

## 测试覆盖情况分析

**当前状态**:
- 解释模式测试: 84 个测试文件
- 编译模式测试: 29 个测试文件
- 覆盖率差距: 55 个测试文件

## 缺失的测试类别

### 1. 异步编程支持 (Async/)
- [ ] `AsyncFunctionTests.cs` - 异步函数基础测试
- [ ] `AsyncGeneratorTests.cs` - 异步生成器测试
- [ ] `AsyncStreamTests.cs` - 异步流测试
- [ ] `AwaitTests.cs` - await 关键字测试
- [ ] `TaskAPITests.cs` - Task API 测试
- [ ] `SpawnTests.cs` - spawn 关键字测试

**优先级**: 高 - 异步编程是现代语言的重要特性

### 2. 高级类功能 (Classes/)
- [ ] `ClassInstantiationTests.cs` - 类实例化测试
- [ ] `ConstructorTests.cs` - 构造函数测试
- [ ] `MemberAccessTests.cs` - 成员访问测试
- [ ] `InheritanceTests.cs` - 继承测试（如果支持）
- [ ] `InterfaceTests.cs` - 接口测试（如果支持）
- [ ] `MixinTests.cs` - Mixin 测试（如果支持）
- [ ] `GenericClassTests.cs` - 泛型类测试

**优先级**: 中高 - 类相关功能需要完整测试覆盖

### 3. 集合高级功能 (Collections/)
- [ ] `CollectionMethodsTests.cs` - 集合方法测试
- [ ] `RangeTests.cs` - Range 类型测试
- [ ] `ExtendedRangeTests.cs` - 扩展 Range 功能测试
- [ ] `InExpressionTests.cs` - in 表达式测试（已有基础版本）

**优先级**: 中 - 集合操作测试

### 4. 边界和错误情况 (EdgeCases/ & ErrorCases/)
- [ ] `EmptyInputTests.cs` - 空输入测试
- [ ] `ExtremeValuesTests.cs` - 极值测试
- [ ] `UnexpectedInputsTests.cs` - 意外输入测试
- [ ] `TypeErrorsTests.cs` - 类型错误测试

**优先级**: 高 - 编译器必须正确处理边界和错误情况

### 5. 异常处理 (Exceptions/)
- [ ] `TryCatchTests.cs` - try-catch 基础测试
- [ ] `ThrowTests.cs` - throw 语句测试
- [ ] `FinallyTests.cs` - finally 块测试
- [ ] `NestedExceptionTests.cs` - 嵌套异常测试
- [ ] `ErrorPropagationTests.cs` - 错误传播测试

**优先级**: 高 - 异常处理是编译模式的核心功能

### 6. 高级函数功能 (Functions/)
- [ ] `ClosureTests.cs` - 闭包测试
- [ ] `HigherOrderTests.cs` - 高阶函数测试
- [ ] `FunctionOverloadTests.cs` - 函数重载测试（如果支持）
- [ ] `GenericFunctionTests.cs` - 泛型函数测试

**优先级**: 中高 - 函数式编程特性

### 7. 模块系统 (Modules/)
- [ ] `SimpleImportTests.cs` - 简单导入测试
- [ ] `WildcardImportTests.cs` - 通配符导入测试
- [ ] `SelectiveImportTests.cs` - 选择性导入测试
- [ ] `AliasImportTests.cs` - 别名导入测试
- [ ] `LazyImportTests.cs` - 延迟导入测试
- [ ] `DynamicImportTests.cs` - 动态导入测试
- [ ] `ConditionalImportTests.cs` - 条件导入测试
- [ ] `CircularDependencyTests.cs` - 循环依赖测试
- [ ] `ImportErrorTests.cs` - 导入错误测试
- [ ] `StandardLibraryImportTests.cs` - 标准库导入测试
- [ ] `ComplexImportScenariosTests.cs` - 复杂导入场景测试
- [ ] `UnifiedModuleArchitectureTests.cs` - 统一模块架构测试

**优先级**: 中 - 模块系统在编译模式下可能有不同的处理方式

### 8. LINQ 支持 (Linq/)
- [ ] `LinqBasicExecutionTests.cs` - LINQ 基础执行测试
- [ ] `LinqLetOrderByTests.cs` - LINQ let 和 orderby 测试
- [ ] `LinqEdgeCasesTests.cs` - LINQ 边界情况测试
- [ ] `LinqErrorTests.cs` - LINQ 错误测试

**优先级**: 中低 - 取决于 LINQ 在编译模式下的支持程度

### 9. 线程和并发 (Threading/)
- [ ] `SyncIteratorTests.cs` - 同步迭代器测试
- [ ] 其他并发相关测试

**优先级**: 中 - 并发功能需要测试

### 10. 高级类型功能 (Types/)
- [ ] `TypeConversionTests.cs` - 类型转换测试
- [ ] `EnumTests.cs` - 枚举测试（如果支持）
- [ ] `GenericTypeInferenceTests.cs` - 泛型类型推断测试
- [ ] `UnionTypesInterpreterTests.cs` - 联合类型测试（如果支持）

**优先级**: 中 - 类型系统完整性

### 11. 模式匹配增强 (Expressions/)
- [ ] `MatchExpressionEnhancedTests.cs` - 增强的模式匹配测试（基础版本已有）

**优先级**: 低 - 已有基础测试

### 12. 循环语句 (Statements/)
- [ ] `LoopTests.cs` - 循环语句综合测试

**优先级**: 中 - 已有基础的控制流测试

### 13. 集成和真实场景 (Integration/)
- [ ] `RealWorldUsageTests.cs` - 真实世界使用场景测试
- [ ] 更多端到端集成测试

**优先级**: 中 - 验证编译模式在实际场景中的表现

## 实施建议

### 阶段 1: 核心功能 (1-2 周)
1. 异常处理全套测试 (Exceptions/)
2. 边界和错误情况测试 (EdgeCases/ & ErrorCases/)
3. 基础异步功能测试 (部分 Async/)

### 阶段 2: 高级特性 (2-3 周)
4. 类高级功能测试 (Classes/)
5. 高级函数功能测试 (Functions/)
6. 类型系统测试 (Types/)

### 阶段 3: 模块和集成 (1-2 周)
7. 模块系统测试 (Modules/)
8. 集成测试 (Integration/)

### 阶段 4: 次要特性 (1-2 周)
9. 集合高级功能 (Collections/)
10. LINQ 支持 (Linq/)
11. 线程和并发 (Threading/)

## 注意事项

1. **编译模式特殊性**: 某些测试可能需要根据编译模式的特性进行调整：
   - 类型注解要求更严格
   - 函数参数和返回值类型必须明确
   - Lambda 表达式参数类型必须指定

2. **测试策略**:
   - 优先测试编译器生成的 IL 代码正确性
   - 重点测试类型系统和类型推断
   - 确保错误处理和边界情况的健壮性

3. **可能不需要的测试**:
   - 某些解释模式特有的动态特性测试可能不适用
   - 部分模块系统测试可能在编译模式下工作方式不同

4. **优先考虑的差异**:
   - 编译模式的类型系统更严格
   - IL 代码生成和验证
   - 编译时优化和静态分析

## 测试文件命名规范

- 保持与解释模式一致的命名风格
- 使用清晰的描述性名称
- 在必要时添加 `Compiler` 后缀以区分（如已有 `GenericCollectionTypesCompilerTests.cs`）

## 相关资源

- 解释模式测试目录: `Old8Lang.Tests/Interpreter/`
- 当前编译模式测试: `Old8Lang.Tests/Compiler/`
- 语法规范: `Old8Lang_Grammar.md`
- EBNF 规范: `Old8Lang/Old8Lang.ebnf`
