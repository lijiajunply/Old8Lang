# ILangList 通用实例方法

## 概述

为了避免代码重复，我们为所有实现 `ILangList` 接口的类型创建了通用的实例方法。这些方法可以被以下类型使用：

- `ListLangValue`
- `ArrayLangValue`
- `TupleLangValue`
- `DictionaryLangValue`（部分支持）
- `AsyncStreamLangValue`
- `GeneratorLangValue`
- `AsyncGeneratorLangValue`

## 已实现的通用方法

### 基础方法

1. **LangListCountMethod** - `Count()` / `Length()` / `Len()`
   - 返回列表长度
   - 适用于所有 ILangList 实现

2. **LangListContainsMethod** - `Contains(item)`
   - 检查是否包含指定元素
   - 使用 `ILangList.In()` 方法实现

3. **LangListReverseMethod** - `Reverse()`
   - 反转列表元素顺序
   - 返回新的 `ListLangValue`

### 高阶方法

4. **LangListFilterMethod** - `Filter(predicate)` / `Where(predicate)`
   - 过滤列表元素
   - 参数：接受一个返回布尔值的函数

5. **LangListMapMethod** - `Map(mapper)` / `Select(mapper)`
   - 映射列表元素
   - 参数：接受一个转换函数

6. **LangListAnyMethod** - `Any(predicate?)`
   - 检查是否有任意元素满足条件
   - 无参数时检查列表是否非空

7. **LangListAllMethod** - `All(predicate)`
   - 检查是否所有元素都满足条件
   - 参数：接受一个返回布尔值的函数

## 使用方式

### 方式一：为特定类型注册通用方法

如果你想让某个类型（如 `ArrayLangValue`）使用通用方法，可以在 `InstanceMethodInitializer` 中注册：

```csharp
// 为 Array 注册通用方法
registry.Register(new Generic.LangListCountMethod());
registry.Register(new Generic.LangListContainsMethod());
registry.Register(new Generic.LangListReverseMethod());
// ... 其他方法
```

### 方式二：创建特定类型的包装类

为了更好的类型安全和控制，可以为特定类型创建包装类：

```csharp
public class ArrayCountMethod : LangListCountMethod
{
    public override Type TargetType => typeof(ArrayLangValue);
}
```

### 方式三：直接使用通用方法

通用方法的 `TargetType` 默认为 `ILangList`，可以直接注册使用。

## 注意事项

1. **方法冲突**：如果某个类型已经有了特定的实现（如 `ListCountMethod`），通用方法不会覆盖它。实例方法注册器会优先使用更具体的类型。

2. **返回类型**：大多数通用方法返回 `ListLangValue`，而不是原始类型。例如，`tuple.Reverse()` 返回 `ListLangValue` 而不是 `TupleLangValue`。

3. **编译模式支持**：高阶函数方法（Filter、Map、All、Any with predicate）在编译模式下支持有限。

4. **VM 模式支持**：高阶函数方法在 VM 模式下暂不支持。

## 扩展建议

如果需要为特定类型添加更多通用方法，可以：

1. 在 `Generic` 目录下创建新的通用方法类
2. 继承 `BaseLangListMethod`
3. 使用 `GetItems()` 和 `GetLength()` 辅助方法
4. 在 `InstanceMethodInitializer` 中注册

## 示例

```old8
// List 使用通用方法
list <- [1, 2, 3, 4, 5]
count <- list.Count()           // 5
contains <- list.Contains(3)    // true
reversed <- list.Reverse()      // [5, 4, 3, 2, 1]
filtered <- list.Filter(func(x) { return x > 3 })  // [4, 5]

// Array 使用通用方法
arr <- array(3)
arr.Set(0, 10)
arr.Set(1, 20)
arr.Set(2, 30)
count <- arr.Count()            // 3
contains <- arr.Contains(20)    // true
reversed <- arr.Reverse()       // [30, 20, 10]

// Tuple 使用通用方法
tuple <- (100, 200, 300)
count <- tuple.Count()          // 3
contains <- tuple.Contains(200) // true
reversed <- tuple.Reverse()     // [300, 200, 100]
```

## 性能考虑

通用方法通过 `ILangList` 接口调用，可能比直接访问具体类型的字段稍慢。对于性能敏感的场景，建议为特定类型创建优化的实现。

## 未来改进

1. 支持更多通用方法（如 `First`、`Last`、`Skip`、`Take` 等）
2. 改进编译模式和 VM 模式的支持
3. 添加泛型类型推断，使返回类型更准确
4. 优化性能，减少不必要的列表复制
