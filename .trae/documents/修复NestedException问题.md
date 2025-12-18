## 问题分析

在Old8Lang的嵌套异常处理中，当try块或catch块中有return语句时，return语句的效果没有被正确传递到外层函数。这导致了NestedExceptionTests测试用例的失败，特别是`NestedException_NestedInFunctions_FunctionCallChaining`测试用例。

## 根本原因

1. 当try块或catch块中的代码执行到return语句时，会设置`manager.IsReturn = true`并保存返回值到`manager.Result`
2. `BlockStatement.Run`方法会检查`manager.IsReturn`标志，如果为true则立即返回
3. 但是，`TryStatement.Run`方法只捕获`Old8Exception`异常，没有检查`manager.IsReturn`标志
4. 这导致当try块或catch块中有return语句时，`TryStatement.Run`会继续执行，而不是立即返回
5. 从而使得return语句的效果没有被正确传递到外层的`BlockStatement.Run`

## 修复方案

修改`TryStatement.Run`方法，在执行try块和catch块后检查`manager.IsReturn`标志，如果为true则立即返回。

## 具体修改

1. 打开`/Users/luckyfish/Documents/Project/RiderProjects/Old8Lang/Old8Lang/AST/Statement/TryStatement.cs`文件
2. 在`tryBlock.Run(manager)`之后添加对`manager.IsReturn`的检查，如果为true则返回
3. 在每个catch块的`catchBlock.Run(manager)`之后添加对`manager.IsReturn`的检查，如果为true则返回

## 修复后的代码

```csharp
public override void Run(VariateManager manager)
{
    try
    {
        tryBlock.Run(manager);
        // 检查try块是否执行了return语句
        if (manager.IsReturn)
        {
            return;
        }
    }
    catch (Old8Exception ex)
    {
        // 遍历所有catch块，查找匹配的异常类型
        foreach (var (exceptionType, exceptionVar, catchBlock) in catchBlocks)
        {
            // 异常类型匹配逻辑...
            
            // 执行catch块
            catchBlock.Run(manager);
            
            // 检查catch块是否执行了return语句
            if (manager.IsReturn)
            {
                return;
            }
            
            return; // 只执行第一个匹配的catch块
        }

        // 如果没有匹配的catch块，则重新抛出异常
        throw;
    }
    finally
    {
        // 执行finally块（如果存在）
        finallyBlock?.Run(manager);
    }
}
```

## 测试验证

修复后，运行NestedExceptionTests测试用例，确保所有测试都通过，特别是`NestedException_NestedInFunctions_FunctionCallChaining`测试用例。