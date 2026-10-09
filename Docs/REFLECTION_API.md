# Old8Lang Reflection API

本文档基于以下实现整理：

- `Old8Lang/GlobalFunctions/Implementations/Reflection/`
- `Old8Lang/InstanceMethods/Implementations/TypeValue/`

## 概览

Old8Lang 的反射能力分为两类：

- 全局反射函数：用于查询类型、成员、函数信息，以及动态创建实例、调用方法、读写字段。
- `TypeLangValue` 实例方法：用于对类型值本身做判断和查询（如父类、接口、方法名、字段名）。

## 全局反射函数

| 函数 | 签名 | 返回值 | 说明 |
| --- | --- | --- | --- |
| `GetAllTypes` | `GetAllTypes()` | `List<TypeLangValue>` | 获取所有已注册类型。 |
| `GetType` | `GetType(typeName)` | `TypeLangValue` | 通过类型名获取类型值。 |
| `TypeOf` | `TypeOf(obj)` | `TypeLangValue` | 获取对象实例的类型值。 |
| `GetTypeInfo` | `GetTypeInfo(typeName)` | `Dictionary` | 获取类型的完整元信息。 |
| `GetClassInfo` | `GetClassInfo(obj)` | `Dictionary` | 获取对象所属类的完整信息。 |
| `GetFunctionInfo` | `GetFunctionInfo(functionOrName[, methodName])` | `Dictionary` | 获取全局函数/函数对象/类方法信息。 |
| `GetMemberInfo` | `GetMemberInfo(obj, memberName)` | `Dictionary` | 自动识别字段或方法并返回元信息。 |
| `HasMember` | `HasMember(obj, memberName)` | `Bool` | 判断对象是否存在该成员（方法或字段）。 |
| `GetField` | `GetField(obj, fieldName)` | `Any` | 动态读取字段值。 |
| `SetField` | `SetField(obj, fieldName, value)` | `Void` | 动态设置字段值。 |
| `InvokeMethod` | `InvokeMethod(obj, methodName, args)` | `Any` | 动态调用方法，`args` 必须是列表。 |
| `CreateInstance` | `CreateInstance(className, args)` | `AnyLangValue / BytecodeObjectInstance` | 动态创建实例并尝试调用 `init`。 |
| `IsInstanceOf` | `IsInstanceOf(obj, className)` | `Bool` | 判断对象是否为指定类实例（当前为类名精确匹配）。 |

## 主要返回结构

### `GetTypeInfo(typeName)`

返回字典字段：

- `name`
- `isInterface`
- `isAbstract`
- `isMixin`
- `baseClass`
- `interfaces`
- `mixins`
- `methods`
- `fields`
- `isGeneric`

### `GetClassInfo(obj)`

返回字典字段：

- `className`
- `methods`
- `fields`
- `isInterface`
- `isAbstract`
- `isMixin`
- `baseClass`
- `interfaces`

### `GetMemberInfo(obj, memberName)`

若成员是方法，典型字段：

- `name`
- `type`（值为 `method`）
- `isStatic`
- `isPublic`
- `isPrivate`
- `parameterCount`
- `overloadCount`

若成员是字段，典型字段：

- `name`
- `type`（值为 `field`）
- `isStatic`
- `isPublic`
- `isPrivate`

### `GetFunctionInfo(...)`

根据输入类型返回不同结构：

- 全局函数名（字符串） -> `type = global_function`
- 用户函数对象（`FuncLangValue`） -> `type = user_function`
- 原生函数（`FuncLangValue.Method != null`） -> `type = native_function`
- 对象 + 方法名 -> `type = class_method`

常见字段包括：

- `name`
- `type`
- `parameters` 或 `names`
- `parameterCount` / `minParameterCount` / `maxParameterCount`
- `isStatic` / `isPublic` / `isPrivate` / `isAbstract` / `isVirtual`
- `returnType`

## `TypeLangValue` 实例方法

| 方法 | 签名 | 返回值 | 说明 |
| --- | --- | --- | --- |
| `IsClass` | `type.IsClass()` | `Bool` | 是否为类（非接口、非 mixin）。 |
| `IsInterface` | `type.IsInterface()` | `Bool` | 是否为接口。 |
| `IsPrimitive` | `type.IsPrimitive()` | `Bool` | 是否为基本类型。 |
| `IsGeneric` | `type.IsGeneric()` | `Bool` | 是否为泛型类型。 |
| `IsAssignableFrom` | `type.IsAssignableFrom(otherType)` | `Bool` | 类型兼容性检查。 |
| `GetBaseType` | `type.GetBaseType()` | `TypeLangValue or Null` | 获取父类型。 |
| `GetInterfaces` | `type.GetInterfaces()` | `List<TypeLangValue>` | 获取实现接口列表。 |
| `GetMethodNames` | `type.GetMethodNames()` | `List<String>` | 获取类型的全部方法名。 |
| `GetFieldNames` | `type.GetFieldNames()` | `List<String>` | 获取类型的全部字段名。 |

## 模式差异和当前限制

以下差异来自当前实现行为：

- VM 模式下，`GetFunctionInfo(obj, methodName)`（类方法反射）暂不支持，会抛错。
- VM 模式下，`Type.IsGeneric()` 当前固定返回 `false`（未实现完整检查）。
- VM 模式下，`Type.IsAssignableFrom()` 当前固定返回 `false`（未实现完整检查）。
- `IsInstanceOf` 当前是“类名精确匹配”，不进行继承链或接口实现链判断。
- 多处 API 在解释模式返回 `LangValue`（如 `DictionaryLangValue`），VM 模式返回原生对象字典/列表；使用方应按运行模式做适配。

## 简单示例

```old8
t <- GetType("MyClass")
PrintLine(t.IsClass())
PrintLine(t.GetMethodNames())

obj <- CreateInstance("MyClass", [])
PrintLine(GetClassInfo(obj))

if HasMember(obj, "count") {
    v <- GetField(obj, "count")
    SetField(obj, "count", v + 1)
}

result <- InvokeMethod(obj, "Run", [1, 2, 3])
PrintLine(result)
```

