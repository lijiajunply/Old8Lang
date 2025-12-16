## 修复 TaskLangValue.ThenTask 同步等待异步任务问题

### 问题分析
在 `TaskLangValue.ThenTask` 方法中，存在同步等待异步任务的问题：
```csharp
return result.AwaitAsync().Result;  // ❌ 同步等待异步任务
```

这违反了异步编程最佳实践，可能导致以下问题：
- 线程池饥饿
- 在某些上下文中可能死锁
- 影响应用程序性能

### 修复方案
将同步等待改为异步等待，使用异步 lambda 和 await 关键字，并调用 Unwrap() 方法解包嵌套的 Task：

```csharp
public TaskLangValue ThenTask(Func<LangValueType, TaskLangValue> continuation, SourcePosition position = default)
{
    var thenTask = Task.ContinueWith(async t =>
    {
        if (t.IsFaulted)
        {
            throw (t.Exception?.InnerException ?? t.Exception)!;
        }

        var result = continuation(t.Result);
        return await result.AwaitAsync();
    }, _cancellationToken).Unwrap();
    return new TaskLangValue(thenTask, _cancellationToken, position);
}
```

### 修复位置
文件：`/Old8Lang/AST/Expression/Value/TaskLangValue.cs`
方法：`ThenTask`（第445-458行）

### 修复理由
1. 使用异步 lambda 和 await 关键字符合异步编程最佳实践
2. 调用 Unwrap() 方法解包嵌套的 Task<Task<T>>，返回正确的 Task<T>
3. 避免线程池饥饿和死锁问题
4. 提高应用程序性能和响应性

### 测试建议
修复后，建议进行以下测试：
1. 运行现有的异步测试用例，确保功能正常
2. 测试嵌套的异步任务，确保正确处理
3. 测试异常情况下的行为，确保异常正确传播
4. 进行性能测试，验证修复后的性能提升