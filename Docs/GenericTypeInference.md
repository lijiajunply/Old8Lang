# 泛型类型推断实现说明

## 概述

实现了从函数调用参数自动推断泛型类型参数的功能，使得调用泛型函数时不再需要显式指定类型参数。

## 功能特性

### 1. 自动类型推断

用户现在可以省略泛型类型参数，由编译器自动推断：

```old8
func identity<T>(value:T) -> T {
    return value
}

// 以前：需要显式指定类型
result1 <- identity<int>(42)

// 现在：自动推断 T=int
result2 <- identity(42)
```

### 2. 支持的推断场景

#### 2.1 从字面量推断
```old8
identity(42)        // 推断 T=int
identity("hello")   // 推断 T=string
identity(3.14)      // 推断 T=double
identity(true)      // 推断 T=bool
```

#### 2.2 从变量推断
```old8
x <- 100
result <- identity(x)  // 推断 T=int
```

#### 2.3 多个类型参数
```old8
func makePair<K, V>(key:K, value:V) -> string {
    return key.ToStr() + ":" + value.ToStr()
}

pair <- makePair("age", 25)  // 推断 K=string, V=int
```

#### 2.4 同一类型参数在多个位置
```old8
func add<T>(a:T, b:T) -> string {
    return a.ToStr() + "+" + b.ToStr()
}

sum <- add(10, 20)  // 推断 T=int
```

#### 2.5 嵌套函数调用
```old8
func wrap<T>(value:T) -> string {
    return "[" + value.ToStr() + "]"
}

// 推断内层和外层的类型
result <- wrap(identity(42))  // 先推断 identity 的 T=int，再推断 wrap 的 T=int
```

## 实现细节

### 核心组件

#### 1. GenericTypeInference 类
位置：`Old8Lang/TypeSystem/GenericTypeInference.cs`

主要方法：
- `InferFunctionTypeArguments()`: 从函数调用参数推断泛型类型参数
- `InferArgumentType()`: 推断单个参数的类型
- `MatchTypePattern()`: 匹配类型模式，提取泛型参数映射
- `ExtractGenericParametersFromType()`: 从类型注解中提取泛型参数

#### 2. 推断触发点

在 `Instance.cs` 的三个位置添加了推断逻辑：
- 找到匹配的重载函数时（第158-189行）
- 第一个 else 分支中的函数调用（第217-250行）
- 第二个 else 分支中的函数调用（第260-292行）

#### 3. 推断流程

```
Instance.Run()
  → 检查是否为泛型函数且未实例化
  → GenericTypeInference.InferFunctionTypeArguments()
    → 遍历每个参数
    → InferArgumentType() 获取参数类型
    → MatchTypePattern() 建立类型映射
    → 验证所有泛型参数都已推断
  → FuncLangValue.InstantiateGeneric() 创建实例化函数
  → 调用实例化后的函数
```

### 类型推断算法

1. **遍历参数**：逐个检查函数参数和调用参数
2. **提取泛型参数**：从参数类型注解中提取泛型参数名（如 "T"、"List<T>" 中的 "T"）
3. **推断参数类型**：
   - 字面量：直接获取类型（int、string、double、bool、char）
   - 变量：从变量管理器中获取值的类型
   - 函数调用：执行函数调用并获取结果类型
4. **匹配类型模式**：
   - 单个类型参数（如 "T"）：直接映射到实际类型
   - 泛型类型（如 "List<T>"）：递归提取类型参数
5. **验证完整性**：确保所有泛型参数都已成功推断
6. **实例化函数**：使用推断的类型参数创建函数实例

### 类型传递机制

推断的类型映射通过以下方式传递到类型检查器：

```csharp
// FuncLangValue.cs
instantiated.TypeArgumentMapping = typeArguments;

// ValidateParameterTypes 方法
var typeMapping = executionManager?.CurrentFunctionTypeArgumentMapping ?? TypeArgumentMapping;

// TypeChecker.ValidateParameterTypes
if (typeArgumentMapping.TryGetValue(expectedType, out var value)) {
    expectedType = value.Name;  // T -> int
}
```

## 测试覆盖

### 单元测试（11个测试用例）
文件：`Old8Lang.Tests/Interpreter/Functions/GenericTypeInferenceTests.cs`

测试场景：
1. 单个类型参数推断（int、string、double、bool）
2. 多个类型参数推断
3. 同一类型参数在多个位置
4. 从变量推断类型
5. 显式类型参数仍然有效
6. 嵌套函数调用
7. 混合类型推断
8. 正确的返回类型

### 集成测试
文件：`InterpreterTests/generics_type_inference.old8`

## 向后兼容性

- ✅ 显式类型参数语法仍然有效
- ✅ 现有泛型函数测试全部通过（10个测试）
- ✅ 现有泛型类测试全部通过（8个测试）
- ✅ 不影响非泛型函数的调用

## 已知限制

1. **执行副作用**：在推断嵌套函数调用的类型时，会实际执行内层函数，可能产生副作用
2. **复杂类型**：目前主要支持基本类型和简单泛型类型，复杂的嵌套泛型类型推断有限
3. **类型约束**：推断过程会验证类型约束，但不会尝试找到满足约束的最佳类型

## 未来改进方向

1. 增强嵌套泛型类型的推断（如 `List<List<T>>`）
2. 支持从返回值类型推断（反向推断）
3. 优化嵌套调用的推断，避免不必要的执行
4. 支持更复杂的类型约束推断

## 相关文件

- `Old8Lang/TypeSystem/GenericTypeInference.cs` - 推断引擎
- `Old8Lang/AST/Expression/Value/Instance.cs` - 推断触发点
- `Old8Lang/AST/Expression/Value/FuncLangValue.cs` - 泛型函数实例化
- `Old8Lang/TypeSystem/TypeChecker.cs` - 类型验证
- `Old8Lang.Tests/Interpreter/Functions/GenericTypeInferenceTests.cs` - 测试用例
