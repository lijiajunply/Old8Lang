using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Concurrency;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// Using语句，用于资源自动管理（类似于C#的using语句）
/// </summary>
/// <param name="variableName">可选的变量名，用于存储资源</param>
/// <param name="resourceExpression">返回资源ID的表达式</param>
/// <param name="blockStatement">using块中的语句</param>
/// <param name="position">位置信息</param>
public partial class UsingStatement(
    string? variableName,
    LangExpression resourceExpression,
    BlockStatement blockStatement,
    SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 获取变量名（可能为null）
    /// </summary>
    public string? VariableName => variableName;

    /// <summary>
    /// 获取资源表达式
    /// </summary>
    public LangExpression ResourceExpression => resourceExpression;

    /// <summary>
    /// 获取using块
    /// </summary>
    public BlockStatement BlockStatement => blockStatement;

    public override void Run(VariateManager manager)
    {
        // 检查是否在生成器上下文中
        if (manager.GeneratorContext != null)
        {
            RunWithGeneratorContext(manager);
        }
        else
        {
            RunStandard(manager);
        }
    }

    /// <summary>
    /// 标准模式执行（非生成器）
    /// </summary>
    private void RunStandard(VariateManager manager)
    {
        // 1. 执行资源表达式
        var resource = resourceExpression.Run(manager);

        // 2. 如果有变量名，注册到作用域
        if (variableName != null)
        {
            manager.Set(new LangId(variableName), resource);
        }

        try
        {
            // 3. 执行using块
            blockStatement.Run(manager);
        }
        finally
        {
            // 4. 自动调用Dispose
            DisposeResource(resource);
        }
    }

    /// <summary>
    /// 生成器上下文模式执行
    /// </summary>
    private void RunWithGeneratorContext(VariateManager manager)
    {
        var context = manager.GeneratorContext!;

        // 检查是否从using块内恢复
        bool isResumingFromUsingBlock = !string.IsNullOrEmpty(context.ExecutionPath) &&
                                         context.ExecutionPath.Contains("/using_block");

        if (isResumingFromUsingBlock)
        {
            // 从using块内恢复，继续执行
            try
            {
                blockStatement.Run(manager);
            }
            finally
            {
                // 清理资源
                if (variableName != null)
                {
                    var resource = manager.GetValue(new LangId(variableName));
                    DisposeResource(resource);
                }
            }
        }
        else
        {
            // 首次进入using语句
            var resource = resourceExpression.Run(manager);

            if (variableName != null)
            {
                manager.Set(new LangId(variableName), resource);
            }

            // 设置执行路径
            var oldPath = context.ExecutionPath;
            context.ExecutionPath = $"{oldPath}/using_block";

            try
            {
                blockStatement.Run(manager);
            }
            finally
            {
                // 恢复执行路径
                context.ExecutionPath = oldPath;

                // 清理资源
                DisposeResource(resource);
            }
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    private void DisposeResource(LangValueType resource)
    {
        // 如果是整数值（资源ID），尝试通过ResourceManager释放
        if (resource is IntLangValue intVal)
        {
            ResourceManager.TryDispose(intVal.Value);
        }
        // 如果实现了IDisposable接口，直接调用Dispose
        else if (resource is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 创建局部变量存储资源
        resourceExpression.LoadIlValue(ilGenerator, local);
        var resourceType = resourceExpression.OutputType(local);
        var resourceLocal = ilGenerator.DeclareLocal(resourceType);
        ilGenerator.Emit(OpCodes.Stloc, resourceLocal);

        // 2. 如果有变量名，添加到局部变量
        if (variableName != null)
        {
            local.AddLocalVar(variableName, resourceLocal);
            local.LocalVarTypes[variableName] = resourceType;
        }

        // 3. 生成try-finally
        ilGenerator.BeginExceptionBlock();

        // try块：执行BlockStatement
        blockStatement.GenerateIl(ilGenerator, local);

        // finally块
        ilGenerator.BeginFinallyBlock();

        // 调用ResourceManager.TryDispose(id)
        ilGenerator.Emit(OpCodes.Ldloc, resourceLocal);

        // 如果资源类型不是int，需要进行转换或跳过Dispose调用
        if (resourceType == typeof(int))
        {
            var disposeMethod = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryDispose));
            ilGenerator.Emit(OpCodes.Call, disposeMethod);
        }
        else
        {
            // 对于非int类型，尝试调用IDisposable.Dispose
            var disposableType = typeof(IDisposable);
            if (disposableType.IsAssignableFrom(resourceType))
            {
                var disposeMethod = disposableType.GetMethod(nameof(IDisposable.Dispose));
                if (disposeMethod != null)
                {
                    if (resourceType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, resourceType);
                    }
                    ilGenerator.Emit(OpCodes.Callvirt, disposeMethod);
                }
            }
            else
            {
                // 不支持Dispose的类型，不生成任何清理代码
                ilGenerator.Emit(OpCodes.Pop);
            }
        }

        ilGenerator.EndExceptionBlock();
    }

    public override OldStatement this[int index]
    {
        get
        {
            // Using 语句只包含一个块语句
            if (index == 0)
            {
                return blockStatement;
            }

            // 超出范围，返回空语句
            return new BlockStatement(new List<OldStatement>());
        }
    }

    public override int Count
    {
        get
        {
            return 1; // 只有 using 块
        }
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        // 目前visitor pattern尚未完全实现using语句的支持
        // 暂时返回默认值，等待visitor pattern完善后补充
        return default!;
    }
}
