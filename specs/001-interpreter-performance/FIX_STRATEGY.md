# 扩展方法栈溢出修复方案讨论

**日期**: 2026-02-21
**问题**: 扩展方法内调用内置方法导致无限递归
**严重程度**: 🔴 CRITICAL

## 问题重现

```old8
extension int {
    func toStr() -> string {
        return this.ToStr()  // ← 期望调用内置方法，实际再次调用扩展方法
    }
}

x <- 42
str <- x.toStr()  // 栈溢出！
```

## 方案对比

### 方案 1: 内置方法优先级（推荐）⭐

**核心思想**: 在查找扩展方法之前，先检查是否存在内置方法。

**优点**:
- ✅ 符合直觉：内置方法应该优先于扩展方法
- ✅ 修改范围小：只需修改 `Instance.InstanceMethods.cs`
- ✅ 性能影响小：只增加一次反射查找
- ✅ 向后兼容：不影响现有代码

**缺点**:
- ⚠️ 需要维护内置方法列表
- ⚠️ 反射查找有轻微性能开销

**实现位置**: `Old8Lang/AST/Expression/Value/Special/Instance.InstanceMethods.cs:17`

```csharp
private bool TryExecuteInstanceMethod(LangValueType instance, VariateManager manager, out LangValueType? result)
{
    result = null;

    // 确保实例方法系统已初始化
    InstanceMethodInitializer.EnsureInitialized();

    // 【新增】1. 首先检查是否是内置方法
    if (HasBuiltInMethod(instance.GetType(), Id.IdName, Ids.Count))
    {
        // 内置方法存在，不使用扩展方法，让旧系统处理
        return false;
    }

    // 2. 查找扩展方法
    var instanceType = instance.GetType();
    var method = InstanceMethodRegistry.Instance.ResolveMethod(
        instanceType,
        Id.IdName,
        Ids,
        null);

    if (method == null)
    {
        return false;
    }

    // 执行扩展方法...
}

// 【新增】辅助方法：检查内置方法是否存在
private bool HasBuiltInMethod(Type instanceType, string methodName, int paramCount)
{
    // 使用反射查找内置方法（非扩展方法）
    var methods = instanceType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
        .Where(m => !m.IsStatic && // 排除静态方法（扩展方法）
                    m.Name.Equals(methodName, StringComparison.Ordinal) && // 大小写敏感
                    m.GetParameters().Length == paramCount)
        .ToArray();

    return methods.Length > 0;
}
```

**测试验证**:
```csharp
// 应该调用内置方法 ToStr()，而不是扩展方法 toStr()
extension int {
    func toStr() -> string {
        return this.ToStr()  // ✓ 调用内置方法，不会递归
    }
}
```

---

### 方案 2: 大小写敏感匹配（必需）⭐

**核心思想**: 确保方法名匹配严格区分大小写。

**优点**:
- ✅ 符合大多数编程语言的行为
- ✅ 避免意外的方法名冲突
- ✅ 实现简单

**缺点**:
- ⚠️ 可能破坏依赖大小写不敏感的现有代码

**实现位置**: `Old8Lang/InstanceMethods/Core/InstanceMethodRegistry.cs`

需要检查的地方：
1. 方法注册时的键生成
2. 方法查找时的键匹配
3. 方法名比较逻辑

```csharp
// 确保使用 StringComparison.Ordinal 而不是 OrdinalIgnoreCase
public IInstanceMethod? ResolveMethod(
    Type targetType,
    string methodName,
    List<LangExpression> parameters,
    LocalManager? local)
{
    // 【修改】使用大小写敏感匹配
    var candidates = _methods
        .Where(m => m.TargetType.IsAssignableFrom(targetType) &&
                    m.Names.Any(n => n.Equals(methodName, StringComparison.Ordinal))) // ← 关键修改
        .ToList();

    // ...
}
```

---

### 方案 3: 递归检测机制（防御性）⭐

**核心思想**: 在 VariateManager 中跟踪正在执行的扩展方法，防止递归调用。

**优点**:
- ✅ 提供额外的安全保障
- ✅ 可以给出清晰的错误信息
- ✅ 防止其他类型的递归问题

**缺点**:
- ⚠️ 增加运行时开销（每次方法调用都需要检查）
- ⚠️ 需要在多个地方维护状态

**实现位置**:
1. `Old8Lang/Interpreter/VariateManager.cs` - 添加跟踪机制
2. `Old8Lang/InstanceMethods/Core/ExtensionMethodWrapper.cs` - 启用检测

```csharp
// VariateManager.cs
public class VariateManager
{
    // 【新增】跟踪正在执行的扩展方法调用栈
    private Stack<string> _extensionMethodCallStack = new();
    private const int MaxExtensionMethodDepth = 100; // 最大递归深度

    public void EnterExtensionMethod(string methodSignature)
    {
        if (_extensionMethodCallStack.Count >= MaxExtensionMethodDepth)
        {
            throw new StackOverflowError(
                $"扩展方法递归深度超过限制 ({MaxExtensionMethodDepth})。" +
                $"调用栈: {string.Join(" -> ", _extensionMethodCallStack)}");
        }

        // 检查是否已经在调用栈中（直接递归）
        if (_extensionMethodCallStack.Contains(methodSignature))
        {
            throw new RecursionError(
                $"检测到扩展方法递归调用: {methodSignature}。" +
                $"调用栈: {string.Join(" -> ", _extensionMethodCallStack)} -> {methodSignature}");
        }

        _extensionMethodCallStack.Push(methodSignature);
    }

    public void ExitExtensionMethod()
    {
        if (_extensionMethodCallStack.Count > 0)
        {
            _extensionMethodCallStack.Pop();
        }
    }

    public bool IsInExtensionMethod(string methodSignature)
    {
        return _extensionMethodCallStack.Contains(methodSignature);
    }
}

// ExtensionMethodWrapper.cs
public LangValueType Execute(
    LangValueType instance,
    List<LangExpression> parameters,
    VariateManager manager,
    SourcePosition position)
{
    // 【新增】生成方法签名用于递归检测
    var methodSignature = $"{instance.GetType().Name}.{function.Id.IdName}";

    // 【新增】进入扩展方法，启用递归检测
    manager.EnterExtensionMethod(methodSignature);

    try
    {
        // 创建新的作用域
        manager.AddChildren();

        try
        {
            // 绑定 this 关键字到实例
            manager.Set(new LangId("this"), instance);

            // 绑定用户定义的参数
            for (int i = 0; i < parameters.Count; i++)
            {
                var paramName = function.Ids[i].IdName;
                var paramValue = parameters[i].Run(manager);
                manager.Set(new LangId(paramName), paramValue);
            }

            // 执行函数体
            function.BlockStatement.Run(manager);

            // 检查是否有返回值
            if (manager.IsReturn)
            {
                return manager.Result;
            }

            // 如果没有显式返回，返回 null
            return new NullLangValue();
        }
        finally
        {
            manager.RemoveChildren();
        }
    }
    finally
    {
        // 【新增】退出扩展方法，清理递归检测状态
        manager.ExitExtensionMethod();
    }
}
```

**错误信息示例**:
```
Old8Lang.Error.RecursionError: 检测到扩展方法递归调用: IntLangValue.toStr
调用栈: IntLangValue.toStr -> IntLangValue.toStr
位置: test.old8:3:16
```

---

### 方案 4: 扩展方法作用域隔离（长期方案）

**核心思想**: 在扩展方法内部，`this` 只能访问内置方法和类方法，不能访问扩展方法。

**优点**:
- ✅ 从根本上解决问题
- ✅ 符合扩展方法的语义
- ✅ 避免意外的方法解析

**缺点**:
- ⚠️ 实现复杂度高
- ⚠️ 可能破坏某些合理的使用场景
- ⚠️ 需要重新设计方法查找系统

**实现**: 暂不推荐，留待后续版本

---

## 推荐的修复策略

### 阶段 1: 快速修复（立即执行）

**组合方案**: 方案 1 + 方案 2

1. **实现内置方法优先级** (T001-BUG)
   - 修改 `Instance.InstanceMethods.cs`
   - 添加 `HasBuiltInMethod` 辅助方法
   - 在查找扩展方法前先检查内置方法

2. **确保大小写敏感匹配** (T002-BUG)
   - 检查 `InstanceMethodRegistry.cs` 中的所有字符串比较
   - 将 `StringComparison.OrdinalIgnoreCase` 改为 `StringComparison.Ordinal`
   - 更新方法注册和查找逻辑

3. **运行测试验证** (T005-BUG)
   ```bash
   dotnet test --filter "FullyQualifiedName~ExtensionMethodInterpreterTests.Execute_ExtensionMethodReturningDifferentType_ExecutesCorrectly"
   ```

**预期结果**: 栈溢出问题解决，测试通过

---

### 阶段 2: 防御性增强（短期执行）

**添加方案 3**: 递归检测机制

1. **在 VariateManager 中添加跟踪** (T003-BUG)
   - 添加 `_extensionMethodCallStack` 字段
   - 实现 `EnterExtensionMethod` 和 `ExitExtensionMethod` 方法
   - 定义自定义异常类型 `RecursionError`

2. **在 ExtensionMethodWrapper 中启用检测** (T004-BUG)
   - 在 `Execute` 方法开始时调用 `EnterExtensionMethod`
   - 在 `finally` 块中调用 `ExitExtensionMethod`
   - 确保异常情况下也能正确清理

3. **添加单元测试**
   ```csharp
   [Fact]
   public void ExtensionMethod_DirectRecursion_ThrowsRecursionError()
   {
       var code = @"
           extension int {
               func bad() -> int {
                   return this.bad()  // 直接递归
               }
           }
           x <- 5
           result <- x.bad()
       ";

       var interpreter = new LangInterpreter();
       var ast = interpreter.Build(code);

       Assert.Throws<RecursionError>(() => ast.Run(interpreter.Manager));
   }
   ```

**预期结果**: 即使方案 1 失效，也能捕获递归并给出清晰错误

---

### 阶段 3: 长期改进（中期执行）

**考虑方案 4**: 扩展方法作用域隔离

- 设计新的方法查找架构
- 实现扩展方法上下文标记
- 在扩展方法内部禁用扩展方法查找

---

## 实现顺序

```
T000: 分析现有代码
  ↓
T001-BUG: 实现内置方法优先级 ← 核心修复
  ↓
T002-BUG: 确保大小写敏感 ← 核心修复
  ↓
T005-BUG: 运行测试验证
  ↓
T003-BUG: 添加递归检测 ← 防御性增强
  ↓
T004-BUG: 启用递归检测
  ↓
T012-BUG: 完整回归测试
```

---

## 风险评估

### 方案 1 风险
- **低风险**: 只影响扩展方法查找逻辑
- **缓解**: 充分的单元测试和集成测试

### 方案 2 风险
- **中风险**: 可能破坏依赖大小写不敏感的代码
- **缓解**:
  1. 先运行完整测试套件，记录所有失败
  2. 分析失败原因，确认是否是合理的破坏性变更
  3. 更新文档说明方法名大小写敏感

### 方案 3 风险
- **低风险**: 纯防御性代码，不影响正常流程
- **缓解**: 确保 `finally` 块正确清理状态

---

## 成功标准

1. ✅ 所有 `ExtensionMethodInterpreterTests` 测试通过
2. ✅ 所有 `Interpreter.Classes` 测试通过
3. ✅ 完整测试套件通过率 100%
4. ✅ 无性能回归（<5% 性能下降）
5. ✅ 文档更新完成

---

## 下一步

1. 用户确认修复方案
2. 开始实现 T000-T005
3. 验证修复效果
4. 继续修复其他问题（类型匹配、泛型约束）

---

**讨论状态**: 待用户确认
**预计工作量**: 2-4 小时
**优先级**: P0 - CRITICAL
