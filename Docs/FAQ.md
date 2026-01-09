# Old8Lang 常见问题 (FAQ)

本文档收集 Old8Lang 使用过程中的常见问题和解答。

## 目录

- [安装和配置](#安装和配置)
- [语法和基础概念](#语法和基础概念)
- [编译器和解释器](#编译器和解释器)
- [类型系统](#类型系统)
- [错误处理](#错误处理)
- [性能相关](#性能相关)
- [并发编程](#并发编程)
- [互操作性](#互操作性)
- [开发工具](#开发工具)

---

## 安装和配置

### Q: Old8Lang 需要什么运行环境?

**A**: Old8Lang 需要 .NET 10.0 SDK 或更高版本。

安装步骤:
1. 下载并安装 [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
2. 克隆 Old8Lang 仓库
3. 运行 `dotnet build Old8Lang.sln`

### Q: 如何验证安装成功?

**A**: 运行以下命令:
```bash
dotnet run --project Old8Lang.App -- --version
```

### Q: 如何配置 VS Code 支持 Old8Lang?

**A**: 参考 [LSP_VSCode_Documentation.md](LSP_VSCode_Documentation.md) 配置 Language Server Protocol 支持,获得语法高亮、自动补全等功能。

---

## 语法和基础概念

### Q: Old8Lang 的注释语法是什么?

**A**: Old8Lang 使用 `//` 表示单行注释,**不支持** `#`。

```old8
// 这是正确的注释
# 这是错误的,会导致语法错误
```

### Q: 为什么使用 `<-` 而不是 `=` 赋值?

**A**: Old8Lang 借鉴 Go 语言设计,使用 `<-` 表示赋值,使变量声明更清晰:

```old8
a <- 10        // 变量声明和赋值
a:int <- 10    // 带类型标注的赋值
```

### Q: 列表、数组、字典的语法有什么区别?

**A**:
- **列表**: 使用花括号 `{1, 2, 3}` (动态大小)
- **数组**: 使用方括号 `[1, 2, 3]` (固定大小)
- **字典**: 使用花括号带冒号 `{"key": value}`

```old8
list <- {1, 2, 3}             // 列表
array <- [1, 2, 3]            // 数组
dict <- {"name": "Old8"}      // 字典
```

### Q: 为什么是 `.ToStr()` 而不是 `.ToString()`?

**A**: Old8Lang 使用简洁的命名约定,所有内置方法名称简短易记:
- `.ToStr()` 而非 `.ToString()`
- `.ToInt()` 而非 `.ToInteger()`

### Q: Old8Lang 支持类继承吗?

**A**: **不支持**。Old8Lang 设计为简单的语言,类不支持继承、泛型等复杂特性。推荐使用组合而非继承。

---

## 编译器和解释器

### Q: 编译器模式和解释器模式有什么区别?

**A**:

| 特性 | 编译器模式 (`-c`) | 解释器模式 (`-f`) |
|------|------------------|------------------|
| 执行速度 | 快 (5-10x) | 慢 |
| 类型要求 | 严格（必须标注） | 宽松（可选） |
| 启动时间 | 中等 | 快 |
| 适用场景 | 生产环境 | 开发调试 |

### Q: 编译器模式下为什么必须标注类型?

**A**: 编译器模式生成优化的IL代码,需要在编译时确定类型信息。解决方法:

```old8
// ✅ 显式类型标注
func add(a:int, b:int) -> int {
    return a + b
}

// ✅ 使用默认值推断类型
func greet(name:string, prefix: "Hello") -> string {
    return prefix + ", " + name
}
```

### Q: 如何运行 Old8Lang 代码?

**A**:
```bash
# 解释器模式
dotnet run --project Old8Lang.App -- -f mycode.old8

# 编译器模式
dotnet run --project Old8Lang.App -- -c mycode.old8

# 语法测试（不执行）
dotnet run --project Old8Lang.App -- -s mycode.old8
```

---

## 类型系统

### Q: Old8Lang 是静态类型还是动态类型?

**A**: Old8Lang 是**动态类型**语言,但支持**可选类型标注**:
- 解释器模式: 完全动态,类型标注可选
- 编译器模式: 要求函数签名必须有类型

### Q: 类型推断如何工作?

**A**: Old8Lang 使用 TypeScript 风格的渐进式类型推断:

```old8
a <- 10          // 推断为 int
b <- 3.14        // 推断为 double
c <- "hello"     // 推断为 string
d <- {1, 2, 3}   // 推断为 list<int>
```

详细文档: [TypeInference.md](TypeInference.md)

### Q: 如何禁用类型推断?

**A**: 在 C# 代码中设置:
```csharp
Compiler.EnableTypeInference = false;
TypeInferenceConfig.Instance.EnableTypeInference = false;
```

### Q: 支持哪些基本类型?

**A**:
- **数值**: `int`, `double`, `long`, `decimal`
- **文本**: `string`, `char`
- **布尔**: `bool`
- **集合**: `array`, `list`, `dict`, `tuple`
- **特殊**: `void`, `object`, `any`

---

## 错误处理

### Q: Old8Lang 如何处理异常?

**A**: 使用 `try-catch` 语句:

```old8
try {
    riskyOperation()
} catch e {
    PrintLine("错误: " + e.ToStr())
}
```

### Q: 如何主动抛出异常?

**A**: 使用 `throw` 语句:

```old8
func validateAge(age:int) -> void {
    if age < 0 {
        throw "年龄不能为负数"
    }
}
```

### Q: 常见语法错误有哪些?

**A**:

1. **使用 `#` 注释**
   ```old8
   # 错误
   // 正确
   ```

2. **使用 `=` 赋值**
   ```old8
   a = 10     // 错误
   a <- 10    // 正确
   ```

3. **使用 `.ToString()`**
   ```old8
   num.ToString()  // 错误
   num.ToStr()     // 正确
   ```

4. **编译器模式缺少类型标注**
   ```old8
   func add(a, b) { return a + b }  // 错误
   func add(a:int, b:int) -> int { return a + b }  // 正确
   ```

---

## 性能相关

### Q: Old8Lang 程序运行很慢,如何优化?

**A**: 按以下步骤优化:

1. **使用编译器模式** (`-c`) 而非解释器模式
2. **添加类型标注** 到所有函数
3. **使用性能分析器** 找到热点:
   ```bash
   dotnet run --project Old8Lang.App -- -f code.old8 --profile
   ```
4. **优化算法** 降低时间复杂度
5. **选择合适的数据结构** (数组/列表/字典)

详细参考: [PERFORMANCE_GUIDE.md](PERFORMANCE_GUIDE.md)

### Q: 如何进行性能分析?

**A**: 使用内置性能分析器:

```bash
dotnet run --project Old8Lang.App -- -f mycode.old8 --profile
```

生成的报告位于 `profiler-report-{timestamp}.txt`

### Q: 并发编程时如何提升性能?

**A**:
- 使用 **AtomicInt** 替代 Mutex (计数器场景,快3-5倍)
- 使用 **Channel** 替代共享内存+锁 (避免锁竞争)
- 使用 **ReadWriteLock** (读多写少场景)
- **减小锁粒度** (只锁必要代码)

---

## 并发编程

### Q: Old8Lang 支持哪些并发原语?

**A**: Old8Lang 提供 8 种内置并发原语:
- **Mutex** (互斥锁)
- **Semaphore** (信号量)
- **AtomicInt** (原子整数)
- **Channel** (通道)
- **ReadWriteLock** (读写锁)
- **CountDownLatch** (倒计时门闩)
- **CyclicBarrier** (循环栅栏)
- **CancellationTokenSource** (取消令牌源)

详细文档: [API_REFERENCE.md](API_REFERENCE.md#并发原语)

### Q: 如何创建和管理互斥锁?

**A**: 推荐使用 `using` 语句自动管理:

```old8
using mutex <- MutexCreate() {
    MutexLock(mutex)
    // 临界区代码
    MutexUnlock(mutex)
}  // 自动调用 MutexDispose
```

### Q: 什么是 select 语句?

**A**: `select` 语句类似 Go 语言,用于通道多路复用:

```old8
ch1 <- ChannelCreate()
ch2 <- ChannelCreate()

select {
    case ch1 <- 100 -> {
        PrintLine("发送到 ch1")
    }
    case val from ch2 -> {
        PrintLine("从 ch2 接收")
    }
    default -> {
        PrintLine("没有通道就绪")
    }
}
```

### Q: 如何避免资源泄漏?

**A**: 使用 `using` 语句确保自动释放:

```old8
// ✅ 推荐
using ch <- ChannelCreate() {
    // 使用通道
}  // 自动调用 ChannelDispose

// ❌ 不推荐
ch <- ChannelCreate()
// ... 使用
ChannelDispose(ch)  // 可能忘记调用
```

---

## 互操作性

### Q: Old8Lang 如何调用 C# 代码?

**A**: 使用 `native` 语句绑定 C# 方法:

```old8
native DateTime.Now() -> object
native Console.WriteLine(value:string) -> void

now <- DateTime.Now()
Console.WriteLine("Current time: " + now.ToStr())
```

### Q: 如何导入外部库?

**A**: 使用 `import` 语句:

```old8
import "Old8LangLib"    // 导入内置库
import "MyCustomLib"     // 导入自定义库
```

### Q: Old8Lang 支持哪些外部库?

**A**: 官方提供:
- **Old8LangLib**: 操作系统、文件、网络功能
- **Old8Lang.NetLib**: MQTT、高级网络功能
- **Old8Lang.SerializationLib**: 序列化支持
- **Old8Lang.MachineLearningLib**: 机器学习功能

---

## 开发工具

### Q: 有 IDE 支持吗?

**A**: 支持 VS Code,通过 LSP (Language Server Protocol):
- 语法高亮
- 自动补全
- 错误诊断
- 跳转定义

配置方法: [LSP_VSCode_Documentation.md](LSP_VSCode_Documentation.md)

### Q: 如何调试 Old8Lang 代码?

**A**: 使用内置调试器:

```bash
dotnet run --project Old8Lang.App -- -f mycode.old8 --debug
```

详细文档: [DEBUGGER_GUIDE.md](DEBUGGER_GUIDE.md)

### Q: 如何运行测试?

**A**:
```bash
# 运行所有单元测试
dotnet test Old8Lang.Tests/Old8Lang.Tests.csproj

# 运行语法测试
./run_syntax_tests.sh

# 运行解释器测试
./run_interpreter_tests.sh

# 运行编译器测试
./run_compiler_tests.sh
```

### Q: 如何提交 Bug 或功能请求?

**A**: 在 GitHub Issues 中提交,请包含:
- Old8Lang 版本
- 操作系统和 .NET 版本
- 重现步骤
- 预期行为 vs 实际行为
- 最小可重现示例代码

---

## 其他常见问题

### Q: Old8Lang 支持模式匹配吗?

**A**: 支持。详见 [PatternMatching.md](PatternMatching.md)

```old8
match value {
    1 -> PrintLine("One")
    2 | 3 -> PrintLine("Two or Three")
    _ -> PrintLine("Other")
}
```

### Q: Old8Lang 支持生成器(Generator)吗?

**A**: 支持,使用 `yield` 语句:

```old8
func range(start:int, end:int) -> object {
    for i in start..end {
        yield i
    }
}

for num in range(1, 10) {
    PrintLine(num)
}
```

### Q: 命名参数如何使用?

**A**: 调用函数时可以指定参数名:

```old8
func greet(name:string, age:int) -> void {
    PrintLine(name + " is " + age.ToStr())
}

greet(age: 25, name: "Alice")  // 命名参数,可以乱序
```

详见: [NamedArguments.md](NamedArguments.md)

### Q: Old8Lang 支持泛型吗?

**A**: 支持泛型类型推断,但不支持用户定义泛型类。详见:
- [GenericTypeInference.md](GenericTypeInference.md)
- [GenericCollections.md](GenericCollections.md)

### Q: 如何处理 JSON 数据?

**A**: 使用内置 JSON 函数:

```old8
// 解析 JSON
json <- JsonParse("{\"name\":\"Old8\",\"version\":1}")
PrintLine(json["name"])  // "Old8"

// 序列化为 JSON
data <- {"name": "Old8", "version": 1}
jsonStr <- JsonStringify(data)
```

### Q: Old8Lang 的文件扩展名是什么?

**A**: `.old8`

### Q: Old8Lang 是开源的吗?

**A**: 是的,Old8Lang 是开源项目。欢迎贡献! 参见 [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 还有问题?

如果您的问题未在此列出:

1. **查阅文档**:
   - [Old8Lang_Grammar.md](Old8Lang_Grammar.md) - 完整语法参考
   - [API_REFERENCE.md](API_REFERENCE.md) - API 文档
   - [PERFORMANCE_GUIDE.md](PERFORMANCE_GUIDE.md) - 性能优化

2. **查看示例代码**: `TestFiles/` 目录包含大量示例

3. **提交 Issue**: 在 GitHub 上提问

4. **查看变更日志**: [CHANGELOG.md](CHANGELOG.md) 了解最新变化

