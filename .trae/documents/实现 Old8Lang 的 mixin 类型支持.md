# 实现 Old8Lang 的 mixin 类型支持

## 1. 概述
为 Old8Lang 添加 mixin 类型支持，允许类从多个 mixin 类中继承成员，实现更灵活的代码复用机制。

## 2. 实现步骤

### 2.1 语法定义扩展
1. **添加关键字和令牌类型**
   - 在 `LangTokenType` 枚举中添加 `Mixin` 和 `With` 令牌
   - 在 `KeywordType` 枚举中添加 `Mixin` 和 `With` 关键字

2. **扩展类声明语法**
   - 支持 mixin 类定义：`mixin MixinName { ... }`
   - 支持类应用多个 mixin：`class ClassName extends ParentClass with Mixin1, Mixin2 { ... }`

### 2.2 解析器修改
1. **修改 ClassParser**
   - 扩展 `ParseClassDeclaration` 方法，支持解析 mixin 类定义
   - 添加解析 `with` 子句的逻辑，支持多个 mixin 类
   - 修改类声明语法处理，区分普通类和 mixin 类

### 2.3 类型系统修改
1. **修改 TypeTemplate 类**
   - 添加 `IsMixin` 属性，标识是否为 mixin 类
   - 添加 `MixinNames` 列表，存储应用的 mixin 类名
   - 扩展 `GetAllParentMembers` 方法，支持递归获取 mixin 成员
   - 修改 `CreateInstance` 方法，合并所有 mixin 成员

2. **修改实例化逻辑**
   - 确保在创建类实例时，正确合并所有 mixin 类的成员
   - 处理 mixin 成员与类自身成员的冲突（类自身成员优先级更高）

### 2.4 语义检查
1. **添加 mixin 特定检查**
   - 确保 mixin 类不能被实例化
   - 确保 mixin 类不能继承自普通类（可选，根据设计决定）
   - 检查循环依赖

### 2.5 测试
1. **创建测试用例**
   - 编写 mixin 类定义测试
   - 编写类应用单个 mixin 测试
   - 编写类应用多个 mixin 测试
   - 编写 mixin 成员与类成员冲突测试
   - 编写 mixin 继承测试（mixin 可以继承其他 mixin）

## 3. 语法示例

```old8
// 定义 mixin
mixin Logger {
    func log(message) {
        PrintLine("[LOG] " + message)
    }
}

mixin Serializable {
    func serialize() {
        return "Serialized object"
    }
}

// 应用单个 mixin
class User with Logger {
    name <- ""
    
    func init(name) {
        this.name <- name
        log("User created: " + name) // 使用 Logger mixin 的方法
    }
}

// 应用多个 mixin
class Product extends BaseClass with Logger, Serializable {
    name <- ""
    price <- 0
    
    func init(name, price) {
        this.name <- name
        this.price <- price
        log("Product created: " + name) // 使用 Logger mixin 的方法
    }
}
```

## 4. 实现要点

1. **Mixin 与继承的区别**
   - Mixin 类不能被实例化
   - 一个类可以应用多个 mixin
   - Mixin 可以继承其他 mixin
   - 类自身成员优先级高于 mixin 成员

2. **成员合并策略**
   - 类自身成员 > mixin 成员 > 父类成员
   - 多个 mixin 之间的成员冲突，后面应用的 mixin 覆盖前面的

3. **递归处理**
   - 确保正确处理 mixin 的继承链
   - 避免循环依赖

## 5. 文件修改清单

1. `Old8Lang/LangParser/LangTokenType.cs` - 添加关键字和令牌类型
2. `Old8Lang/LangParser/Parsers/ClassParser.cs` - 修改类解析逻辑
3. `Old8Lang/AST/Expression/Value/TypeTemplate.cs` - 修改类型模板和实例化逻辑
4. `Old8Lang/Old8Lang_Grammar.md` - 更新语法文档
5. 测试文件 - 添加各种 mixin 测试用例