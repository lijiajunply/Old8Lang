# C# 生成器（迭代器）架构分析

## 1. C# 生成器的基本用法

```csharp
// C# 生成器示例
public IEnumerable<int> Generate()
{
    for (int i = 1; i <= 3; i++)
    {
        Console.WriteLine($"Before yield {i}");
        yield return i;
        Console.WriteLine($"After yield {i}");
    }
}

// 使用
foreach (var item in Generate())
{
    Console.WriteLine($"Got {item}");
}

/* 输出：
Before yield 1
Got 1
After yield 1
Before yield 2
Got 2
After yield 2
Before yield 3
Got 3
After yield 3
*/
```

## 2. C# 编译器如何实现生成器

C# 编译器会将包含 `yield` 的方法**重写为状态机类**。关键点：

### 2.1 状态机类结构

```csharp
// 原始代码
public IEnumerable<int> Generate()
{
    for (int i = 1; i <= 3; i++)
    {
        Console.WriteLine($"Before yield {i}");
        yield return i;
        Console.WriteLine($"After yield {i}");
    }
}

// 编译器生成的状态机（简化版）
private sealed class <Generate>d__0 : IEnumerable<int>, IEnumerator<int>
{
    private int <>1__state;        // 状态：-2=disposed, -1=initial, 0+=执行状态
    private int <>2__current;      // 当前值

    // 局部变量被提升为字段
    private int <i>5__1;          // for 循环的 i 变量

    public int Current => <>2__current;

    public bool MoveNext()
    {
        switch (<>1__state)
        {
            case 0:  // 初始状态
                <>1__state = -1;
                <i>5__1 = 1;
                goto IL_LoopStart;

            case 1:  // 从第一个 yield return 恢复
                <>1__state = -1;
                Console.WriteLine($"After yield {<i>5__1}");
                <i>5__1++;
                goto IL_LoopStart;

            IL_LoopStart:
                if (<i>5__1 <= 3)
                {
                    Console.WriteLine($"Before yield {<i>5__1}");
                    <>2__current = <i>5__1;
                    <>1__state = 1;  // 设置恢复点
                    return true;      // yield return
                }
                return false;  // 迭代结束

            default:
                return false;
        }
    }

    public void Reset() => throw new NotSupportedException();
    public void Dispose() { <>1__state = -2; }

    // IEnumerable 实现
    public IEnumerator<int> GetEnumerator()
    {
        if (<>1__state == -2 && <>l__initialThreadId == Thread.CurrentThread.ManagedThreadId)
        {
            <>1__state = 0;
            return this;
        }
        return new <Generate>d__0 { <>1__state = 0 };
    }
}
```

### 2.2 核心设计要点

#### ✅ 状态机设计
- **每个 yield 点对应一个状态值**
- **局部变量提升为字段**（类成员变量），保证跨调用保持状态
- **使用 switch-case 跳转到恢复点**
- **状态在 yield 前保存，在 MoveNext 开始时恢复**

#### ✅ 执行流程
1. **初次调用 MoveNext()**:
   - 状态 = 0（初始）
   - 执行到第一个 yield
   - 保存当前状态（state = 1）
   - 设置 current 值
   - 返回 true

2. **第二次调用 MoveNext()**:
   - 检查状态（state = 1）
   - 通过 switch-case 跳转到恢复点
   - 继续执行 yield 之后的代码
   - 执行到下一个 yield 或结束

#### ✅ 关键特性
- **完全平坦化的代码**：所有嵌套结构（for, while, if）被展开为 goto 和标签
- **没有递归调用**：每次 MoveNext 是一个完整的执行单元
- **状态完全显式**：所有需要保持的状态都存储在字段中

## 3. 对比 Old8Lang 当前实现的问题

### 当前实现方式（AST 解释执行）

```
GeneratorStateMachine.MoveNext()
  → BlockStatement.Run()  (生成器函数体)
    → ForInStatement.Run()  (for 循环)
      → BlockStatement.Run()  (循环体)
        → YieldStatement.Run()  (设置 HasYielded=true)
        ← return
      ← 检查 HasYielded，保存位置，return
    ← return
  ← 检查 HasYielded，返回 true
```

### 问题分析

#### ❌ 问题1：递归调用深度
- AST 节点之间相互调用，形成深层调用栈
- 每个语句的 `Run()` 方法需要知道是否在生成器上下文中
- 恢复时需要重新执行整个调用链

#### ❌ 问题2：状态保存复杂
- 使用 `ExecutionStack` 保存嵌套位置（for 循环索引、语句索引等）
- 需要区分"从 yield 恢复"和"新的迭代"
- 状态保存逻辑分散在多个语句类中（BlockStatement、ForInStatement 等）

#### ❌ 问题3：循环变量作用域问题
- for-in 循环变量在每次迭代时重新设置
- 从 yield 恢复时，变量可能被覆盖或丢失
- 需要复杂的逻辑判断何时设置变量

#### ❌ 问题4：控制流碎片化
- 当前设计：每个 AST 节点负责一小部分控制流
- C# 设计：整个方法体被编译成一个大的 switch-case 状态机
- Old8Lang 的方式导致状态管理极其复杂

## 4. 重新设计方案

### 方案 A：状态机编译（推荐）

**核心思想**：在解析时或首次运行时，将包含 yield 的函数转换为状态机。

#### 实现步骤：

1. **检测生成器函数**
   ```csharp
   // 在 FuncInit 解析时检测是否包含 yield
   public class FuncInit
   {
       public bool IsGenerator { get; set; }  // 包含 yield?
       public List<YieldPoint> YieldPoints { get; set; }  // 所有 yield 位置
   }
   ```

2. **构建状态机**
   ```csharp
   public class GeneratorStateMachine
   {
       private int _state = 0;
       private Dictionary<string, LangValueType> _locals;  // 所有局部变量
       private LangValueType _current;
       private FuncInit _function;

       // 状态点：每个 yield 对应一个恢复点
       private Dictionary<int, Action> _stateHandlers;

       public bool MoveNext()
       {
           // 根据状态跳转到对应的恢复点
           if (_stateHandlers.TryGetValue(_state, out var handler))
           {
               handler();  // 执行到下一个 yield 或结束
               return _current != null;
           }
           return false;
       }
   }
   ```

3. **将 AST 展开为平坦的状态机**
   - 扫描函数 AST，识别所有 yield 点
   - 为每个 yield 点分配状态号
   - 生成状态处理器（展开循环、条件等）

   ```csharp
   // 例如：
   // func gen() {
   //     for i in [1~3] {
   //         PrintLine("before")
   //         yield i
   //         PrintLine("after")
   //     }
   // }

   // 转换为状态机：
   State 0: // 初始
       _locals["i"] = 1
       goto State 1

   State 1: // 循环检查
       if (_locals["i"] > 3) goto State_End
       PrintLine("before")
       _current = _locals["i"]
       _state = 2
       return true  // yield

   State 2: // yield 恢复
       PrintLine("after")
       _locals["i"]++
       goto State 1

   State_End:
       return false
   ```

#### 优点：
- ✅ 完全避免了递归调用和复杂的状态保存
- ✅ 局部变量作为状态机字段，自然保持状态
- ✅ 控制流清晰，易于调试
- ✅ 性能好（类似 C# 编译后的代码）

#### 缺点：
- ⚠️ 需要重写生成器相关逻辑
- ⚠️ 需要实现 AST 到状态机的转换器

### 方案 B：延续（Continuation）风格（备选）

**核心思想**：保存"执行到某处之后要做的事情"。

```csharp
public class GeneratorContinuation
{
    public delegate bool ContinuationFunc(VariateManager manager);

    private ContinuationFunc _continuation;
    private LangValueType _current;

    public bool MoveNext(VariateManager manager)
    {
        if (_continuation == null) return false;
        return _continuation(manager);
    }
}
```

每个语句返回一个延续，而不是直接执行。

#### 优点：
- ✅ 函数式编程风格，理论上优雅

#### 缺点：
- ⚠️ 实现复杂度高
- ⚠️ 性能可能较差（大量闭包和委托调用）

### 方案 C：保持 AST 执行，改进状态管理（最小改动）

**核心思想**：改进当前的 `ExecutionStack` 机制，使其更可靠。

#### 改进点：
1. **统一状态保存格式**
   - 所有循环语句使用相同的状态保存机制
   - 明确定义每个状态帧包含的信息

2. **简化 ForInStatement**
   - 不在 ForInStatement 内部管理作用域
   - 将循环变量提升到外层作用域

3. **添加完整的状态恢复测试**
   - 确保每种控制流组合都能正确恢复

#### 优点：
- ✅ 改动最小
- ✅ 保持 AST 执行方式

#### 缺点：
- ⚠️ 复杂度依然很高
- ⚠️ 难以保证所有边界情况正确

## 5. 推荐实现路径

### 第一阶段：状态机转换器（核心）

1. 创建 `GeneratorStateMachineBuilder`
   - 扫描函数 AST，识别所有 yield 点
   - 为每个 yield 分配状态 ID
   - 提取所有局部变量

2. 创建 `GeneratorState`
   - 保存状态 ID
   - 保存所有局部变量值
   - 保存当前 yield 值

3. 创建扁平化的 `GeneratorExecutor`
   - 根据状态 ID 跳转到对应的执行点
   - 执行一段代码直到下一个 yield 或结束

### 第二阶段：AST 展开器

实现将嵌套的 AST 结构展开为平坦的指令序列：

```csharp
// 输入：AST 树
// 输出：指令序列
List<Instruction> instructions = FlattenAST(functionBody);

// 指令类型示例：
// - JumpIfFalse(condition, targetState)
// - SetVariable(name, value)
// - CallFunction(func, args)
// - Yield(value, nextState)
```

### 第三阶段：集成测试

确保所有生成器测试通过：
- 简单 yield
- 循环中的 yield
- 嵌套循环中的 yield
- 条件语句中的 yield
- 异步生成器

## 6. 参考资源

- [C# 迭代器实现原理](https://sharplab.io) - 可以看到编译后的状态机代码
- [Raymond Chen: How to implement a C# iterator](https://devblogs.microsoft.com/oldnewthing/20210504-00/?p=105176)
- [Jon Skeet: Iterators, iterator blocks and data pipelines](https://codeblog.jonskeet.uk/category/eduasync/)

## 7. 决策建议

对于 Old8Lang 项目，我**强烈推荐方案 A（状态机编译）**：

**理由：**
1. 这是业界标准做法（C#、Python、JavaScript 都这样实现）
2. 可以彻底解决当前的架构问题
3. 性能更好，代码更清晰
4. 虽然初期工作量大，但长期维护成本低

**实现建议：**
1. 先实现简单的状态机（单个 yield、for 循环中的 yield）
2. 逐步支持更复杂的场景（嵌套循环、条件语句）
3. 保留当前实现作为 fallback，直到新实现完全稳定

你希望我帮你实现方案 A 吗？我可以：
1. 先创建 `GeneratorStateMachineBuilder` 的基本框架
2. 实现简单的 AST 展开逻辑（处理最基本的 yield 和循环）
3. 逐步完善直到替换当前实现
