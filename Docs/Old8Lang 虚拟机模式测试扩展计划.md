
# Old8Lang 虚拟机模式测试扩展计划

## 1. 背景分析

### 1.1 当前测试覆盖情况

虚拟机测试目录（`Old8Lang.Tests/VirtualMachine/`）已有 **55 个测试类**，**563 个测试方法**，覆盖了大部分核心功能：

**已完全覆盖的领域**：
- ✅ 基础算术和逻辑运算
- ✅ 变量赋值和作用域
- ✅ 控制流（if/for/while/for-in）
- ✅ 函数声明、调用、默认参数、可变参数
- ✅ 类声明、构造函数、接口
- ✅ 数组、列表、字典基础操作
- ✅ 异步函数和 await
- ✅ 泛型函数和泛型类（基础）
- ✅ 异常处理（try-catch-finally）
- ✅ 模式匹配（match 表达式）
- ✅ 枚举类型

**测试覆盖不足的领域**：
- ⚠️ Select 语句（13 个跳过的测试）
- ⚠️ Defer 语句（缺少全面测试）
- ⚠�� Using 语句（缺少异常场景测试）
- ⚠️ 并发原语（8 种原语缺少系统测试）
- ⚠️ 泛型高级特性（约束、可空类型参数、嵌套泛型）
- ⚠️ 联合类型和交叉类型
- ⚠️ 字符串模板（`$"..."`）
- ⚠️ Range 操作
- ⚠️ Lambda 表达式（缺少专门测试类）
- ⚠️ Switch 语句（缺少传统 switch 测试）
- ⚠️ 类型转换（缺少全面测试）
- ⚠️ 静态成员（缺少测试）
- ⚠️ Super 表达式（只有基础测试）

### 1.2 虚拟机模式支持的特性

根据语法文档分析，虚拟机模式（`-vm`）是功能最完整的执行模式，支持：
- ✅ 所有基础语法和表达式
- ✅ 所有控制流语句（包括 select、defer、using）
- ✅ 完整的泛型支持（泛型函数、泛型类）
- ✅ 完整的异步编程（async/await、异步生成器、Task API）
- ✅ 完整的多线程支持（spawn、所有并发原语）
- ✅ 完整的类型系统（泛型集合、联合类型、交叉类型）
- ✅ 所有内置并发原语函数（Mutex、Semaphore、AtomicInt、Channel、ReadWriteLock、CountDownLatch、CyclicBarrier、CancellationTokenSource）

**不支持的特性**：
- ❌ 运算符重载（仅解释器模式）
- ❌ Python 互操作（仅解释器模式）

## 2. 测试扩展目标

### 2.1 高优先级测试（核心功能）

#### 2.1.1 并发原语全面测试
**目标**：为 8 种并发原语创建系统测试

**新增测试类**：
- `VMConcurrencyMutexTests.cs` - Mutex 测试（5 个函数）
- `VMConcurrencySemaphoreTests.cs` - Semaphore 测试（5 个函数）
- `VMConcurrencyAtomicIntTests.cs` - AtomicInt 测试（8 个函数）
- `VMConcurrencyChannelTests.cs` - Channel 测试（8 个函数）
- `VMConcurrencyReadWriteLockTests.cs` - ReadWriteLock 测试（8 个函数）
- `VMConcurrencyCountDownLatchTests.cs` - CountDownLatch 测试（6 个函数）
- `VMConcurrencyCyclicBarrierTests.cs` - CyclicBarrier 测试（6 个函数）
- `VMConcurrencyCancellationTokenTests.cs` - CancellationTokenSource 测试（4 个函数）

**测试场景**：
- 基本功能测试（创建、操作、释放）
- 超时场景测试（TryLock、TryAcquire、TryReceive 等）
- 异常安全性测试（资源泄漏、异常处理）
- 多线程并发测试（竞态条件、死锁检测）
- 边界条件测试（空 Channel、满 Channel、计数归零等）

#### 2.1.2 Select 语句测试
**目标**：修复跳过的测试，添加完整的 select 语句测试

**新增测试类**：
- `VMSelectStatementTests.cs` - Select 语句专项测试

**测试场景**：
- 发送操作（`case ch <- value`）
- 接收操作（`case val from ch`）
- 默认分支（`default`）
- 多个 case 的轮询
- 阻塞和非阻塞场景
- 与 Channel 的集成测试

#### 2.1.3 Defer 语句测试
**目标**：全面测试 defer 语句的所有特性

**新增测试类**：
- `VMDeferStatementTests.cs` - Defer 语句专项测试

**测试场景**：
- 基本 defer 执行
- LIFO 执行顺序（多个 defer）
- defer 访问局部变量
- defer 与 return 的交互
- defer 中的异常处理
- defer 与 using 的交互
- defer 代码块（`defer { ... }`）

#### 2.1.4 Using 语句测试
**目标**：补充 using 语句的异常场景测试

**新增测试类**：
- `VMUsingStatementTests.cs` - Using 语句专项测试

**测试场景**：
- 基本资源管理（Mutex、Channel）
- 异常中的资源释放
- 嵌套 using 语句
- using 与 defer 的交互
- 资源泄漏检测

#### 2.1.5 泛型高级特性测试
**目标**：补充泛型的高级特性测试

**新增测试类**：
- `VMGenericConstraintsTests.cs` - 泛型约束测试
- `VMGenericNullableTests.cs` - 可空泛型类型参数测试
- `VMGenericNestedTests.cs` - 嵌套泛型测试

**测试场景**：
- 泛型约束（单个约束、多个约束、where 子句）
- 可空类型参数（`T?`）
- 嵌套泛型类型（`List<List<T>>`、`Dict<K, List<V>>`）
- 泛型类型推断
- 泛型与接口的交互

### 2.2 中优先级测试（高级特性）

#### 2.2.1 Lambda 表达式测试
**新增测试类**：
- `VMLambdaExpressionTests.cs` - Lambda 表达式专项测试

**测试场景**：
- 表达式形式 Lambda（`(x) -> x * 2`）
- 块形式 Lambda（`(x) -> { return x * 2 }`）
- 闭包捕获（捕获外部变量）
- Lambda 作为参数传递
- Lambda 作为返回值
- 高阶函数（map、filter、reduce）

#### 2.2.2 Switch 语句测试
**新增测试类**：
- `VMSwitchStatementTests.cs` - Switch 语句专项测试

**测试场景**：
- 基本 switch-case
- 多个 case 分支
- default 分支
- case 穿透（fall-through）
- 嵌套 switch

#### 2.2.3 字符串模板测试
**新增测试类**：
- `VMStringTemplateTests.cs` - 字符串模板专项测试

**测试场景**：
- 基本字符串模板（`$"text {expr}"`）
- 转义大括号（`$"{{escaped}}"`）
- 复杂表达式嵌入（`$"{a + b}"`）
- 嵌套字符串模板
- 多个表达式（`$"{a} and {b}"`）

#### 2.2.4 Range 操作测试
**新增测试类**：
- `VMRangeTests.cs` - Range 操作专项测试

**测试场景**：
- 包含两端（`[1~10]`）
- 不包含右端（`[1~<10]`）
- 不包含左端（`[1>~10]`）
- 两端都不包含（`[1>~<10]`）
- Range 在循环中的使用
- Range 的边界条件

#### 2.2.5 联合类型和交叉类型测试
**新增测试类**：
- `VMUnionTypeTests.cs` - 联合类型测试
- `VMIntersectionTypeTests.cs` - 交叉类型测试

**测试场景**：
- 联合类型的声明和赋值（`int | string`）
- 联合类型的兼容性规则
- 可空联合类型（`int? | string?`）
- 交叉类型的约束检查（`A & B`）
- 泛型约束中的交叉类型

### 2.3 低优先级测试（边界情况）

#### 2.3.1 类型转换测试
**新增测试类**：
- `VMTypeConversionTests.cs` - 类型转换专项测试

**测试场景**：
- 基本类型转换（`as` 关键字）
- 数值类型转换（int ↔ double）
- 字符串转换（数值 ↔ string）
- 布尔值转换（bool ↔ string）
- 转换失败的异常处理

#### 2.3.2 静态成员测试
**新增测试类**：
- `VMStaticMemberTests.cs` - 静态成员专项测试

**测试场景**：
- 静态字段
- 静态方法
- 静态构造函数
- 静态成员访问

#### 2.3.3 Super 表达式测试
**新增测试类**：
- `VMSuperExpressionTests.cs` - Super 表达式专项测试

**测试场景**：
- super 调用父类方法
- super 访问父类字段
- super 在构造函数中的使用
- 多层继承中的 super

## 3. 测试文件组织

### 3.1 目录结构

  ```                                                                                                                                                   
  Old8Lang.Tests/VirtualMachine/                                                                                                                        
  ├── Concurrency/                                                                                                                                      
  │   ├── VMConcurrencyMutexTests.cs                                                                                                                    
  │   ├── VMConcurrencySemaphoreTests.cs                                                                                                                
  │   ├── VMConcurrencyAtomicIntTests.cs                                                                                                                
  │   ├── VMConcurrencyChannelTests.cs                                                                                                                  
  │   ├── VMConcurrencyReadWriteLockTests.cs                                                                                                            
  │   ├── VMConcurrencyCountDownLatchTests.cs                                                                                                           
  │   ├── VMConcurrencyCyclicBarrierTests.cs                                                                                                            
  │   └── VMConcurrencyCancellationTokenTests.cs                                                                                                        
  ├── Statements/                                                                                                                                       
  │   ├── VMSelectStatementTests.cs                                                                                                                     
  │   ├── VMDeferStatementTests.cs                                                                                                                      
  │   ├── VMUsingStatementTests.cs                                                                                                                      
  │   └── VMSwitchStatementTests.cs                                                                                                                     
  ├── Generics/                                                                                                                                         
  │   ├── VMGenericConstraintsTests.cs                                                                                                                  
  │   ├── VMGenericNullableTests.cs                                                                                                                     
  │   └── VMGenericNestedTests.cs                                                                                                                       
  ├── Expressions/                                                                                                                                      
  │   ├── VMLambdaExpressionTests.cs                                                                                                                    
  │   ├── VMStringTemplateTests.cs                                                                                                                      
  │   └── VMRangeTests.cs                                                                                                                               
  ├── Types/                                                                                                                                            
  │   ├── VMUnionTypeTests.cs                                                                                                                           
  │   ├── VMIntersectionTypeTests.cs                                                                                                                    
  │   ├── VMTypeConversionTests.cs                                                                                                                      
  │   └── VMStaticMemberTests.cs                                                                                                                        
  └── Classes/                                                                                                                                          
  └── VMSuperExpressionTests.cs                                                                                                                         
  ```                                                                                                                                                   

### 3.2 测试命名规范

**测试类命名**：`VM{功能}{子功能}Tests`
- 例如：`VMConcurrencyMutexTests`、`VMSelectStatementTests`

**测试方法命名**：`{功能}_{场景}_{预期结果}`
- 例如：`MutexCreate_BasicUsage_ExecutesCorrectly`
- 例如：`SelectStatement_SendOperation_ExecutesCorrectly`

## 4. 测试实现模式

### 4.1 基本测试模式

  ```csharp                                                                                                                                             
  [Fact]                                                                                                                                                
  public void Feature_Scenario_ExpectedResult()                                                                                                         
  {                                                                                                                                                     
  // Arrange                                                                                                                                            
  var code = @"                                                                                                                                         
  // Old8Lang 代码                                                                                                                                      
  ";                                                                                                                                                    
                                                                                                                                                        
  // Act                                                                                                                                                
  var bytecodeFile = CompileHelper.CompileToBytecode(code);                                                                                             
  var vm = new VirtualMachine(bytecodeFile);                                                                                                            
  vm.Execute();                                                                                                                                         
                                                                                                                                                        
  // Assert                                                                                                                                             
  var result = vm.GetGlobalVariable("result");                                                                                                          
  Assert.Equal(expectedValue, result);                                                                                                                  
  }                                                                                                                                                     
  ```                                                                                                                                                   

### 4.2 输出验证模式

  ```csharp                                                                                                                                             
  private string ExecuteVMCode(string code)                                                                                                             
  {                                                                                                                                                     
  var interpreter = new LangInterpreter();                                                                                                              
  var ast = interpreter.Build(code);                                                                                                                    
  var compiler = new BytecodeCompiler();                                                                                                                
  var bytecodeFile = compiler.Compile(ast);                                                                                                             
                                                                                                                                                        
  var originalOut = Console.Out;                                                                                                                        
  using var stringWriter = new StringWriter();                                                                                                          
  Console.SetOut(stringWriter);                                                                                                                         
                                                                                                                                                        
  try                                                                                                                                                   
  {                                                                                                                                                     
  var vm = new VirtualMachine(bytecodeFile);                                                                                                            
  vm.Execute();                                                                                                                                         
  return stringWriter.ToString().Trim();                                                                                                                
  }                                                                                                                                                     
  finally                                                                                                                                               
  {                                                                                                                                                     
  Console.SetOut(originalOut);                                                                                                                          
  }                                                                                                                                                     
  }                                                                                                                                                     
                                                                                                                                                        
  [Fact]                                                                                                                                                
  public void Feature_Scenario_ProducesCorrectOutput()                                                                                                  
  {                                                                                                                                                     
  var code = @"                                                                                                                                         
  PrintLine(""Hello, World!"")                                                                                                                          
  ";                                                                                                                                                    
                                                                                                                                                        
  var output = ExecuteVMCode(code);                                                                                                                     
  Assert.Equal("Hello, World!", output);                                                                                                                
  }                                                                                                                                                     
  ```                                                                                                                                                   

### 4.3 异常测试模式

  ```csharp                                                                                                                                             
  [Fact]                                                                                                                                                
  public void Feature_ErrorScenario_ThrowsException()                                                                                                   
  {                                                                                                                                                     
  var code = @"                                                                                                                                         
  throw ""Test exception""                                                                                                                              
  ";                                                                                                                                                    
                                                                                                                                                        
  var bytecodeFile = CompileHelper.CompileToBytecode(code);                                                                                             
  var vm = new VirtualMachine(bytecodeFile);                                                                                                            
                                                                                                                                                        
  Assert.Throws<RuntimeError>(() => vm.Execute());                                                                                                      
  }                                                                                                                                                     
  ```                                                                                                                                                   

### 4.4 参数化测试模式

  ```csharp                                                                                                                                             
  [Theory]                                                                                                                                              
  [InlineData(1, 2, 3)]                                                                                                                                 
  [InlineData(10, 20, 30)]                                                                                                                              
  [InlineData(-5, 5, 0)]                                                                                                                                
  public void Addition_VariousInputs_ProducesCorrectResult(int a, int b, int expected)                                                                  
  {                                                                                                                                                     
  var code = $@"                                                                                                                                        
  result <- {a} + {b}                                                                                                                                   
  ";                                                                                                                                                    
                                                                                                                                                        
  var bytecodeFile = CompileHelper.CompileToBytecode(code);                                                                                             
  var vm = new VirtualMachine(bytecodeFile);                                                                                                            
  vm.Execute();                                                                                                                                         
                                                                                                                                                        
  var result = vm.GetGlobalVariable("result");                                                                                                          
  Assert.Equal(expected, result);                                                                                                                       
  }                                                                                                                                                     
  ```                                                                                                                                                   

## 5. 实施步骤

### 阶段 1：高优先级测试（核心功能）
1. 创建并发原语测试类（8 个文件）
2. 创建 Select 语句测试类
3. 创建 Defer 语句测试类
4. 创建 Using 语句测试类
5. 创建泛型高级特性测试类（3 个文件）

**预计测试数量**：约 150-200 个测试方法

### 阶段 2：中优先级测试（高级特性）
1. 创建 Lambda 表达式测试类
2. 创建 Switch 语句测试类
3. 创建字符串模板测试类
4. 创建 Range 操作测试类
5. 创建联合类型和交叉类型测试类（2 个文件）

**预计测试数量**：约 80-100 个测试方法

### 阶段 3：低优先级测试（边界情况）
1. 创建类型转换测试类
2. 创建静态成员测试类
3. 创建 Super 表达式测试类

**预计测试数量**：约 40-50 个测试方法

## 6. 验证方法

### 6.1 运行测试

  ```bash                                                                                                                                               
  # 运行所有虚拟机测试                                                                                                                                  
  dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj --filter "FullyQualifiedName~VirtualMachine"                                                         
                                                                                                                                                        
  # 运行特定测试类                                                                                                                                      
  dotnet test --filter "FullyQualifiedName~VMConcurrencyMutexTests"                                                                                     
                                                                                                                                                        
  # 运行特定测试方法                                                                                                                                    
  dotnet test --filter "FullyQualifiedName~VMConcurrencyMutexTests.MutexCreate_BasicUsage_ExecutesCorrectly"                                            
  ```                                                                                                                                                   

### 6.2 测试覆盖率

使用 `dotnet-coverage` 工具生成测试覆盖率报告：

  ```bash                                                                                                                                               
  dotnet test --collect:"XPlat Code Coverage"                                                                                                           
  ```                                                                                                                                                   

### 6.3 测试报告

生成测试报告到 `Reports/` 目录：
- 文件名格式：`YYYY-MM-DD-HH-mm-虚拟机测试扩展报告.md`
- 包含内容：
- 新增测试类列表
- 测试方法数量统计
- 测试通过率
- 失败测试分析
- 覆盖率提升情况

## 7. 关键文件

### 7.1 需要创建的文件

**并���原语测试**（8 个文件）：
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyMutexTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencySemaphoreTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyAtomicIntTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyChannelTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyReadWriteLockTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyCountDownLatchTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyCyclicBarrierTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Concurrency/VMConcurrencyCancellationTokenTests.cs`

**语句测试**（4 个文件）：
- `/Old8Lang.Tests/VirtualMachine/Statements/VMSelectStatementTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Statements/VMDeferStatementTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Statements/VMUsingStatementTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Statements/VMSwitchStatementTests.cs`

**泛型测试**（3 个文件）：
- `/Old8Lang.Tests/VirtualMachine/Generics/VMGenericConstraintsTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Generics/VMGenericNullableTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Generics/VMGenericNestedTests.cs`

**表达式测试**（3 个文件）：
- `/Old8Lang.Tests/VirtualMachine/Expressions/VMLambdaExpressionTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Expressions/VMStringTemplateTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Expressions/VMRangeTests.cs`

**类型测试**（4 个文件）：
- `/Old8Lang.Tests/VirtualMachine/Types/VMUnionTypeTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Types/VMIntersectionTypeTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Types/VMTypeConversionTests.cs`
- `/Old8Lang.Tests/VirtualMachine/Types/VMStaticMemberTests.cs`

**类测试**（1 个文件）：
- `/Old8Lang.Tests/VirtualMachine/Classes/VMSuperExpressionTests.cs`

**总计**：23 个新测试文件

### 7.2 需要参考的文件

- `/Old8Lang.Tests/VirtualMachine/CompileHelper.cs` - 编译辅助类
- `/Docs/Old8Lang_Grammar.md` - 语法文档
- `/Old8Lang/Old8Lang.ebnf` - EBNF 语法定义
- 现有测试文件（作为模板参考）

## 8. 预期成果

### 8.1 测试数量提升
- 当前：55 个测试类，563 个测试方法
- 新增：23 个测试类，约 270-350 个测试方法
- 预期：78 个测试类，约 830-910 个测试方法

### 8.2 覆盖率提升
- 补充并发原语的系统测试
- 修复 Select 语句的跳过测试
- 补充 Defer、Using 的异常场景测试
- 补充泛型高级特性测试
- 补充 Lambda、Switch、字符串模板等缺失测试

### 8.3 测试质量提升
- 统一的测试命名规范
- 清晰的测试组织结构
- 完善的边界条件测试
- 全面的异常场景测试

## 9. 风险和注意事项

### 9.1 潜在风险
- Select 语句可能存在实现问题（13 个跳过的测试）
- 某些并发原语可能在虚拟机中有限制
- 泛型高级特性可能需要虚拟机支持

### 9.2 注意事项
- 遵循现有的测试命名规范
- 使用 `CompileHelper.CompileToBytecode()` 编译代码
- 使用 `[Collection("Sequential")]` 标记需要顺序执行的测试
- 使用 `[Theory]` 和 `[InlineData]` 进行参数化测试
- 捕获控制台输出进行验证
- 验证异常处理和资源释放

## 10. 总结

本计划旨在为 Old8Lang 虚拟机模式添加约 **270-350 个新测试方法**，覆盖当前测试不足的领域，特别是：
- 并发原语的系统测试
- Select、Defer、Using 语句的完整测试
- 泛型高级特性测试
- Lambda、Switch、字符串模板等缺失测试

通过这些测试，将显著提升虚拟机模式的测试覆盖率和代码质量，确保虚拟机模式的稳定性和可靠性。