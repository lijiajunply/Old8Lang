# 渐进式类型推断系统 (Progressive Type Inference)

## 概述

Old8Lang 编译器现在支持 TypeScript 风格的渐进式类型推断系统，可以智能推断函数参数和返回值类型，减少编译模式下的类型注解负担。

## 功能特性

### 1. 智能类型推断

系统可以从多个来源推断类型：

- **默认值推断**：从参数默认值推断类型
- **return 语句推断**：从函数体的 return 语句推断返回类型
- **函数调用推断**：从函数调用处的实参类型推断参数类型（计划中）
- **赋值推断**：从赋值表达式推断变量类型

### 2. 类型约束求解

使用约束求解系统：

- **约束收集**：遍历 AST 收集类型约束
- **约束求解**：多轮迭代求解类型约束
- **类型精化**：合并多个约束选择最具体的类型
- **置信度机制**：每个约束都有置信度评分

### 3. 渐进式类型系统

类似 TypeScript 的设计理念：

- **可选类型注解**：类型注解是可选的，推断失败时回退到 `object` 类型
- **显式优先**：显式类型注解优先级高于推断类型
- **向后兼容**：不影响现有代码的运行

## 架构设计

### 核心组件

```
TypeInferenceEngine (引擎)
├── TypeConstraintCollector (约束收集器)
├── TypeConstraintSolver (约束求解器)
├── TypeInferenceContext (推断上下文)
└── TypeInferenceConfig (配置)
```

### 类型约束种类

```csharp
public enum TypeConstraintKind
{
    Equality,      // 相等约束：T = SomeType
    Subtype,       // 子类型约束：T <: SomeType
    Call,          // 调用约束：从函数调用推断
    Assignment,    // 赋值约束：从赋值操作推断
    Return         // 返回约束：从return语句推断
}
```

## 使用方法

### 1. 启用/禁用类型推断

```csharp
// 方式1：通过 Compiler 类
Compiler.EnableTypeInference = true;  // 启用（默认禁用）
Compiler.TypeInferenceDebugOutput = true;  // 启用调试输出

// 方式2：通过配置类
TypeInferenceConfig.Instance.EnableTypeInference = true;
TypeInferenceConfig.Instance.DebugOutput = true;
```

### 2. 配置选项

```csharp
var config = TypeInferenceConfig.Instance;

// 启用/禁用类型推断
config.EnableTypeInference = true;

// 从函数调用处推断参数类型
config.InferParameterTypesFromCalls = true;

// 从return语句推断返回类型
config.InferReturnTypesFromBody = true;

// 无法推断时回退到动态类型
config.FallbackToDynamic = true;

// 置信度阈值（0.0-1.0）
config.MinimumConfidence = 0.5;

// 调试输出
config.DebugOutput = false;
```

### 3. 编译器模式下使用

#### 无类型注解（需启用推断）

```old8
// 启用类型推断后，可省略类型注解
func add(a, b) -> int {
    return a + b
}

// 从调用处推断参数类型
result <- add(10, 20)  // 推断 a:int, b:int
```

#### 部分类型注解

```old8
// 混合使用：返回类型自动推断
func multiply(x:int, y:int) {
    return x * y  // 推断返回类型为 int
}
```

#### 默认值推断

```old8
// 从默认值推断参数类型
func greet(name:string, message: "Hello") -> void {
    // message 推断为 string 类型
    PrintLine(message + ", " + name)
}
```

## 实现细节

### 约束收集阶段

1. **函数声明分析**
   - 收集显式类型注解
   - 收集默认值类型约束
   - 收集返回类型约束

2. **函数调用分析**
   - 从调用处实参推断形参类型
   - 记录函数调用信息

3. **赋值语句分析**
   - 从赋值右值推断左值类型

### 约束求解阶段

1. **按置信度排序约束**
2. **多轮迭代求解**
   - 每轮处理可解决的约束
   - 传播约束信息
   - 类型精化
3. **验证解决方案**
4. **应用推断结果到 LocalManager**

### 类型精化规则

- `object` 可被任何类型精化
- `int` 可被 `double` 精化
- 子类型优先于父类型
- 考虑类型兼容性（IsAssignableFrom）

## 限制和已知问题

### 当前限制

1. **默认状态禁用**：类型推断默认禁用，需手动启用
2. **基础实现**：当前为第一版实现，功能有限
3. **调用处推断未完善**：从函数调用处推断参数类型的功能尚未完全实现

### 未来改进

- [ ] 完善从调用处推断参数类型
- [ ] 支持泛型类型推断
- [ ] 支持联合类型 (Union Types)
- [ ] 改进多分支控制流的类型推断
- [ ] 添加类型缩窄 (Type Narrowing)

## 示例代码

### 示例 1：简单类型推断

```old8
// 启用类型推断
// TypeInferenceConfig.Instance.EnableTypeInference = true;

func add(a, b) -> int {
    return a + b
}

result <- add(10, 20)
PrintLine("10 + 20 = " + result.ToStr())
```

### 示例 2：多类型推断

```old8
// 从默认值推断
func calculate(x, y:int, operation:"add") {
    if operation == "add" {
        return x + y
    } elif operation == "multiply" {
        return x * y
    } else {
        return 0
    }
}

result1 <- calculate(10, 20)            // operation = "add"
result2 <- calculate(10, 20, "multiply")
```

### 示例 3：混合注解

```old8
// 部分显式注解 + 部分推断
func max(a:int, b:int) {
    // 返回类型从 return 语句推断为 int
    if a > b {
        return a
    } else {
        return b
    }
}

result <- max(10, 20)
PrintLine("Max: " + result.ToStr())
```

## API 参考

### TypeInferenceConfig

类型推断配置类（单例）。

**属性**：
- `EnableTypeInference: bool` - 启用类型推断
- `InferParameterTypesFromCalls: bool` - 从调用处推断参数
- `InferReturnTypesFromBody: bool` - 从函数体推断返回类型
- `FallbackToDynamic: bool` - 回退到动态类型
- `MinimumConfidence: double` - 最小置信度阈值
- `DebugOutput: bool` - 调试输出

### TypeInferenceEngine

类型推断引擎。

**方法**：
- `InferTypes(IOldLangTree program): bool` - 对整个程序推断类型
- `InferFunctionTypes(FuncInit funcInit): bool` - 对单个函数推断类型
- `NeedsTypeInference(FuncInit funcInit): bool` - 检查是否需要推断
- `GetInferredType(string variableName): Type?` - 获取推断类型
- `GetStatistics(): (int, int, int)` - 获取统计信息

### Compiler

编译器类的新增属性。

**属性**：
- `EnableTypeInference: bool` - 启用/禁用类型推断
- `TypeInferenceDebugOutput: bool` - 类型推断调试输出
- `TypeInferenceConfig: TypeInferenceConfig` - 获取配置实例

## 调试和诊断

### 启用调试输出

```csharp
Compiler.TypeInferenceDebugOutput = true;
TypeInferenceConfig.Instance.DebugOutput = true;
```

### 调试信息示例

```
=== 开始渐进式类型推断 ===
注册函数: add
分析函数调用: add

=== 类型推断迭代 1 ===
  ✓ 绑定类型: add$param$0$a = Int32 (置信度: 0.80)
  ✓ 绑定类型: add$param$1$b = Int32 (置信度: 0.80)
  ✓ 绑定类型: add$return = Int32 (置信度: 0.90)

=== 类型推断结果 ===
总约束数: 3
已解决类型变量数: 3

类型绑定:
  add$param$0$a = Int32
  add$param$1$b = Int32
  add$return = Int32

✓ 函数 add 类型推断成功
```

## 性能考虑

- 类型推断在编译阶段执行，不影响运行时性能
- 约束求解使用多轮迭代，最多 10 轮
- 可以通过禁用推断功能来跳过推断开销（默认禁用）

## 贡献指南

欢迎贡献改进类型推断系统：

1. 改进约束收集算法
2. 优化约束求解性能
3. 添加更多类型推断场景
4. 改进错误提示信息
5. 添加测试用例

## 参考资料

- TypeScript 类型推断: https://www.typescriptlang.org/docs/handbook/type-inference.html
- Hindley-Milner 类型推断算法
- 约束求解理论 (Constraint Solving)

---

**版本**: 1.0.0 (实验性)
**状态**: 默认禁用，需手动启用
**最后更新**: 2025-01-XX
