# Old8Lang 生成器架构重构方案

**创建时间**: 2025-12-16
**目标**: 将生成器实现与循环语句解耦，参考C#的状态机模式

---

## 问题分析

### 当前架构的问题

1. **循环语句与生成器强耦合**
   - `WhileStatement.cs` 通过 `IsInGenerator` 标志判断执行模式
   - 生成器模式：单次迭代后检查yield
   - 普通模式：标准while循环
   - 这种设计导致了之前的bug（while循环只执行一次）

2. **全局状态管理混乱**
   - `VariateManager.IsInGenerator` - 全局生成器标志
   - `VariateManager.IsYield` - 全局yield标志
   - 多个生成器嵌套或并发时可能互相干扰

3. **生成器状态分散**
   - 执行位置：`GeneratorLangValue.ExecutionPosition`
   - 局部状态：`GeneratorLangValue.LocalState`
   - yield标志：`VariateManager.IsYield`
   - 状态管理不统一

4. **缺乏独立的状态机**
   - 生成器依赖循环语句的特殊处理
   - 没有独立的状态保存和恢复机制
   - 执行流程控制分散在多处

---

## C# 生成器架构分析

### C# 如何实现生成器

```csharp
// C# 代码
IEnumerable<int> CountToThree()
{
    yield return 1;
    yield return 2;
    yield return 3;
}

// 编译器生成的状态机（简化）
class <CountToThree>d__0 : IEnumerable<int>, IEnumerator<int>
{
    int state = 0;
    int current;

    public bool MoveNext()
    {
        switch (state)
        {
            case 0:
                current = 1;
                state = 1;
                return true;
            case 1:
                current = 2;
                state = 2;
                return true;
            case 2:
                current = 3;
                state = 3;
                return true;
            case 3:
                return false;
        }
    }

    public int Current => current;
}
```

### 关键特性

1. **独立的状态机类**: 生成器有自己的状态机类
2. **状态编号**: 每个yield点对应一个状态编号
3. **MoveNext模式**: 通过MoveNext()方法驱动状态转换
4. **局部变量保存**: 状态机保存函数的局部变量
5. **完全解耦**: 与循环语句完全独立

---

## 新架构设计

### 核心思想

1. **生成器 = 函数 + 状态机**
2. **状态机独立管理执行流程**
3. **循环语句恢复原始简单逻辑**
4. **状态保存在生成器对象中**

### 架构组件

#### 1. GeneratorStateMachine（新增）

生成器状态机，负责管理生成器的执行状态。

```csharp
/// <summary>
/// 生成器状态机
/// 参考C#的生成器实现，每个生成器实例都有独立的状态机
/// </summary>
public class GeneratorStateMachine
{
    // 状态枚举
    public enum State
    {
        NotStarted = 0,      // 未开始
        Running = 1,         // 运行中
        Suspended = 2,       // 已暂停（yield）
        Completed = 3        // 已完成
    }

    // 当前状态
    public State CurrentState { get; set; } = State.NotStarted;

    // 当前yield点的索引（类似C#的state编号）
    public int StateIndex { get; set; } = 0;

    // 生成器的局部变量环境（独立副本）
    public VariateManager LocalEnvironment { get; set; }

    // 当前yield的值
    public LangValueType CurrentValue { get; set; }

    // 生成器函数引用
    public FuncLangValue GeneratorFunction { get; set; }

    // BlockStatement的执行位置跟踪
    public Stack<int> ExecutionStack { get; set; } = new Stack<int>();

    /// <summary>
    /// 执行到下一个yield点
    /// </summary>
    public bool MoveNext()
    {
        // 实现状态机的执行逻辑
        // 1. 根据StateIndex恢复执行位置
        // 2. 执行BlockStatement直到遇到yield或结束
        // 3. 保存新的状态和位置
        // 4. 返回是否还有更多值
    }

    /// <summary>
    /// 重置状态机
    /// </summary>
    public void Reset()
    {
        CurrentState = State.NotStarted;
        StateIndex = 0;
        ExecutionStack.Clear();
    }
}
```

#### 2. GeneratorLangValue（改进）

```csharp
/// <summary>
/// 生成器对象（改进版）
/// 不再依赖全局IsInGenerator标志，使用独立的状态机
/// </summary>
public class GeneratorLangValue : LangValueType, ILangList
{
    // 独立的状态机
    private GeneratorStateMachine StateMachine { get; }

    public GeneratorLangValue(FuncLangValue func, VariateManager capturedScope)
    {
        // 创建独立的状态机
        StateMachine = new GeneratorStateMachine
        {
            GeneratorFunction = func,
            // 创建独立的局部环境（深拷贝）
            LocalEnvironment = capturedScope.CloneForGenerator()
        };
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 调用状态机的MoveNext
        if (StateMachine.MoveNext())
        {
            return StateMachine.CurrentValue;
        }
        else
        {
            return new VoidLangValue();
        }
    }
}
```

#### 3. BlockStatement（改进）

BlockStatement需要支持恢复执行位置。

```csharp
/// <summary>
/// 块语句（改进版）
/// 支持生成器的断点恢复
/// </summary>
public class BlockStatement
{
    // 为生成器保存执行位置的上下文
    private class GeneratorExecutionContext
    {
        public int CurrentStatementIndex { get; set; } = 0;
        public Dictionary<int, object> SavedState { get; set; } = new();
    }

    public override void Run(VariateManager manager)
    {
        // 检查是否有生成器上下文
        var genContext = manager.GetGeneratorContext();

        int startIndex = genContext?.CurrentStatementIndex ?? 0;

        for (int i = startIndex; i < Statements.Count; i++)
        {
            var statement = Statements[i];
            statement.Run(manager);

            // 检查是否遇到yield（通过生成器上下文而非全局标志）
            if (genContext != null && genContext.HasYielded)
            {
                // 保存当前位置
                genContext.CurrentStatementIndex = i + 1;
                genContext.HasYielded = false;
                return;
            }

            // 其他控制流检查...
        }
    }
}
```

#### 4. WhileStatement（简化）

**移除所有生成器相关逻辑！**

```csharp
/// <summary>
/// while语句（简化版）
/// 移除了与生成器的耦合，恢复为标准while循环
/// </summary>
public class WhileStatement
{
    public override void Run(VariateManager manager)
    {
        manager.ControlFlowManager.PushState();

        try
        {
            // 标准while循环 - 不再需要IsInGenerator检查！
            while (true)
            {
                manager.ControlFlowManager.ResetCurrentState();

                var value = expression.Run(manager);
                if (value is not BoolLangValue varBool)
                {
                    throw new TypeError(this, "期望布尔类型",
                        $"实际得到了 {value.GetType().Name}");
                }

                if (!varBool.Value)
                {
                    varBool.ReturnToPool();
                    break;
                }
                varBool.ReturnToPool();

                // 执行循环体
                blockStatement.Run(manager);

                // 处理break
                if (manager.ControlFlowManager.BreakFlag)
                {
                    manager.ControlFlowManager.BreakFlag = false;
                    break;
                }

                // 处理continue
                if (manager.ControlFlowManager.ContinueFlag)
                {
                    manager.ControlFlowManager.ContinueFlag = false;
                    continue;
                }

                // 如果在生成器中，yield会由BlockStatement处理
                // 这里不需要特殊逻辑
            }
        }
        finally
        {
            manager.ControlFlowManager.PopState();
        }
    }
}
```

#### 5. VariateManager（简化）

```csharp
/// <summary>
/// 变量管理器（简化版）
/// 移除全局生成器标志，改为生成器上下文
/// </summary>
public class VariateManager
{
    // ❌ 移除: public bool IsInGenerator { get; set; }
    // ❌ 移除: public bool IsYield { get; set; }

    // ✅ 新增: 生成器执行上下文（仅在生成器执行时存在）
    public GeneratorExecutionContext? GeneratorContext { get; set; }

    /// <summary>
    /// 为生成器创建独立的变量管理器
    /// </summary>
    public VariateManager CloneForGenerator()
    {
        var newManager = new VariateManager
        {
            LangInfo = this.LangInfo,
            Path = this.Path,
            Interpreter = this.Interpreter,
            GeneratorContext = new GeneratorExecutionContext()
        };

        // 深拷贝作用域栈（生成器需要独立副本）
        foreach (var scope in Scopes)
        {
            var newScope = new Dictionary<string, LangValueType>(scope);
            newManager.Scopes.Add(newScope);
        }

        return newManager;
    }
}
```

#### 6. YieldStatement（改进）

```csharp
/// <summary>
/// yield语句（改进版）
/// 不再使用全局标志，而是通过生成器上下文通信
/// </summary>
public class YieldStatement
{
    public override void Run(VariateManager manager)
    {
        // 计算yield表达式的值
        var yieldValue = YieldExpression.Run(manager);

        // 获取生成器上下文
        var genContext = manager.GeneratorContext;
        if (genContext == null)
        {
            throw new RuntimeError(Position,
                "yield语句只能在生成器函数中使用");
        }

        // 通过生成器上下文保存值和标志
        genContext.CurrentValue = yieldValue;
        genContext.HasYielded = true;

        // 不再设置全局标志！
        // ❌ manager.IsYield = true;
    }
}
```

---

## 实现步骤

### Phase 1: 创建新组件（不影响现有功能）

1. 创建 `GeneratorStateMachine.cs`
2. 创建 `GeneratorExecutionContext.cs`
3. 在 `VariateManager` 中添加 `GeneratorContext` 属性（可选）
4. 在 `VariateManager` 中添加 `CloneForGenerator()` 方法

### Phase 2: 改造GeneratorLangValue

1. 在 `GeneratorLangValue` 中集成状态机
2. 实现基于状态机的 `Run()` 方法
3. 保持向后兼容（暂时保留旧逻辑）

### Phase 3: 改造BlockStatement

1. 添加生成器执行位置恢复逻辑
2. 支持通过生成器上下文检测yield
3. 保持向后兼容

### Phase 4: 简化循环语句

1. 从 `WhileStatement` 移除 `IsInGenerator` 检查
2. 恢复为标准while循环逻辑
3. 从 `ForStatement` 移除类似逻辑（如果有）

### Phase 5: 改造YieldStatement

1. 使用生成器上下文代替全局标志
2. 添加错误检查（yield只能在生成器中使用）

### Phase 6: 清理和测试

1. 从 `VariateManager` 移除 `IsInGenerator` 和 `IsYield`
2. 运行所有生成器测试
3. 运行所有循环测试
4. 运行所有异步测试

---

## 优势分析

### 相比当前架构

| 方面 | 当前架构 | 新架构 |
|-----|---------|--------|
| 耦合度 | 高（循环与生成器耦合） | 低（完全解耦） |
| 状态管理 | 分散（多个全局标志） | 集中（状态机） |
| 可维护性 | 差（逻辑分散） | 好（职责明确） |
| 可扩展性 | 差（修改影响面大） | 好（独立扩展） |
| Bug风险 | 高（全局状态冲突） | 低（独立状态） |
| 测试性 | 差（难以隔离测试） | 好（组件独立） |

### 新架构优势

1. **完全解耦**: 生成器不依赖循环语句的特殊处理
2. **状态隔离**: 每个生成器有独立的状态机和环境
3. **易于理解**: 参考C#，概念清晰
4. **易于扩展**: 可以添加更多生成器特性（如SendGenerator等）
5. **易于调试**: 状态集中管理，易于追踪
6. **易于测试**: 组件独立，可以单独测试

---

## 测试计划

### 1. 单元测试

- `GeneratorStateMachine` 的状态转换
- `GeneratorExecutionContext` 的状态保存和恢复
- `VariateManager.CloneForGenerator()` 的正确性

### 2. 集成测试

- 简单生成器（无循环）
- 带while循环的生成器
- 带for循环的生成器
- 嵌套循环的生成器
- 生成器中的yield位置恢复

### 3. 回归测试

- 所有现有的生成器测试
- 所有现有的while循环测试
- 所有现有的异步测试
- 确保无回归问题

---

## 风险评估

| 风险 | 影响 | 缓解措施 |
|-----|------|---------|
| 现有测试失败 | 高 | 分阶段实施，保持向后兼容 |
| 性能下降 | 中 | 状态机设计优化，添加性能测试 |
| 状态管理复杂 | 中 | 参考C#成熟方案，详细文档 |
| 实现工作量 | 中 | 分阶段实施，逐步迁移 |

---

## 参考资料

### C# 生成器相关

- [C# Iterator Pattern](https://docs.microsoft.com/en-us/dotnet/csharp/iterators)
- [C# Yield Statement](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/yield)
- [Behind the scenes: the iterator pattern in C#](https://www.c-sharpcorner.com/UploadFile/5ef30d/understanding-iterator-pattern-with-C-Sharp/)

### Python 生成器

- [PEP 255 -- Simple Generators](https://www.python.org/dev/peps/pep-0255/)
- [PEP 342 -- Coroutines via Enhanced Generators](https://www.python.org/dev/peps/pep-0342/)

---

## 建议

1. **优先级**: 高 - 当前架构的耦合度太高，影响维护
2. **实施方式**: 分阶段，保持向后兼容
3. **时间估算**: 2-3天的开发时间 + 1天测试
4. **团队沟通**: 需要团队review新架构设计

---

**文档结束**
