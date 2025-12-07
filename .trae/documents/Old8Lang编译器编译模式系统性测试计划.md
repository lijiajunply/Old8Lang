# Old8Lang编译器编译模式系统性测试计划（更新版）

## 一、测试目标

1. 系统性验证Old8Lang编译器对所有语法结构的支持
2. 确保ClassInit语法结构生成正确的IL类结构，而非错误处理为字典类型
3. 验证类型注解的正确处理
4. 测试完整编译流程：语法解析、类型检查、中间代码生成
5. 识别并记录编译过程中的问题和改进点

## 二、测试范围

### 1. 基本语法结构
- 变量声明与赋值
- 类型注解
- 表达式（算术、逻辑、比较）
- 控制流（if-elif-else、while、for）
- 函数定义与调用
- 类定义与实例化
- 数组与字典

### 2. ClassInit语法重点测试
- 类定义基本结构
- 类字段（不同类型）
- 类方法
- 类实例化
- 类成员访问
- IL代码生成验证

## 三、测试文件结构

```
/CompilerTests/
├── basic_types.old8          # 基本类型测试
├── expressions.old8          # 表达式测试
├── control_flow.old8         # 控制流测试
├── functions.old8            # 函数测试
├── class_basic.old8          # 类基本结构测试
├── class_fields.old8         # 类字段测试
├── class_methods.old8        # 类方法测试
└── complex_class.old8        # 复杂类测试
```

## 四、测试步骤

### 1. 准备测试环境
- 确保项目可以正常编译和运行
- 了解现有测试框架和工具

### 2. 生成测试用Old8Lang代码
- 为每个语法结构创建测试文件
- 使用类型注解明确变量、函数及类的类型
- 特别设计ClassInit相关测试用例
- 避免使用as操作和类继承

### 3. 执行编译测试
- 使用编译器编译每个测试文件
- 检查编译是否成功
- 分析生成的IL代码
- 验证ClassInit生成正确的类结构

### 4. 运行测试用例
- 如果支持解释执行，运行测试用例验证行为
- 比较预期结果与实际结果

### 5. 记录测试结果
- 记录编译成功/失败情况
- 记录生成的IL代码分析
- 记录发现的问题和改进点

## 五、重点测试用例设计

### 1. ClassInit基本结构测试
```old8
class Person:
    name:string <- ""
    age:int <- 0
```

### 2. ClassInit带方法测试
```old8
class Calculator:
    func add(a:int, b:int):int {
        return a + b
    }
    func multiply(a:int, b:int):int {
        return a * b
    }
```

### 3. ClassInit实例化测试
```old8
class Person:
    name:string <- ""
    age:int <- 0
    
    func greet():string {
        return "Hello, my name is " + name
    }

p:Person <- Person()
p.name <- "Alice"
p.age <- 30
result:string <- p.greet()
```

## 六、测试工具与方法

1. **编译器命令行工具**：使用现有编译器工具编译测试文件
2. **ILSpy或dotPeek**：分析生成的IL代码
3. **XUnit测试框架**：编写自动化测试用例
4. **手动代码审查**：检查生成的代码质量和正确性

## 七、预期输出

1. 所有测试用例编译成功
2. ClassInit生成正确的IL类结构，包含字段和方法
3. 类型注解被正确处理
4. 生成的代码可以正常运行并产生预期结果
5. 详细的测试报告，包含成功/失败情况和问题分析

## 八、问题记录与改进

1. 记录编译过程中的错误和警告
2. 分析错误原因
3. 提出改进建议
4. 优先修复ClassInit相关问题

## 九、测试结果输出格式

```
测试文件: class_basic.old8
编译结果: 成功
IL生成: 正确生成Person类，包含name和age字段
运行结果: 成功
问题: 无

测试文件: complex_class.old8
编译结果: 失败
错误信息: 类型转换错误
IL生成: 未生成
运行结果: 未运行
问题: AnyValue被错误处理为字典类型
改进建议: 修改ClassInit.GenerateIl方法，确保生成正确的类结构
```