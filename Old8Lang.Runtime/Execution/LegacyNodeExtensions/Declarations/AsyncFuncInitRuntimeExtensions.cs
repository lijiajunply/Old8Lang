using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// AsyncFuncInit 的编译/装饰器辅助实现（阶段 2 从语法节点迁出）。
/// </summary>
public static partial class AsyncFuncInitRuntimeExtensions
{
    /// <summary>
    /// 应用单个装饰器
    /// </summary>
    internal static LangValueType ApplySingleDecorator(FunctionDecorator decorator, LangValueType targetFunc, VariateManager manager)
    {
        // 将目标函数临时注册
        var tempVarName = $"__decorator_target_{Guid.NewGuid():N}";

        if (targetFunc is AsyncFuncLangValue asyncTarget)
        {
            var tempAsyncFunc = new AsyncFuncLangValue(
                new LangId(tempVarName, position: decorator.Position),
                asyncTarget.Ids,
                asyncTarget.BlockStatement,
                asyncTarget.Position
            );
            manager.AddClassAndFunc(tempAsyncFunc);
        }
        else if (targetFunc is FuncLangValue funcTarget)
        {
            var tempFunc = new FuncLangValue(
                new LangId(tempVarName, position: decorator.Position),
                funcTarget.Ids ?? [],
                funcTarget.BlockStatement,
                funcTarget.GenericParameters,
                funcTarget.Position,
                isLambda: false
            );
            manager.AddClassAndFunc(tempFunc);
        }

        LangValueType result;

        // 检查装饰器是否有参数
        if (decorator.Arguments is not null && decorator.Arguments.Count > 0)
        {
            // 带参数的装饰器：先调用装饰器函数获取包装器，然后用包装器包装目标函数
            var decoratorCallExpr = new FunctionCallExpression(
                new LangId(decorator.Name, position: decorator.Position),
                decorator.Arguments,
                decorator.Position
            );
            var wrapperFunc = decoratorCallExpr.Accept(RuntimeVisitors.CreateInterpreter(manager));

            if (wrapperFunc is not FuncLangValue wrapper)
            {
                throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须返回一个函数");
            }

            // 将包装器临时注册
            var wrapperVarName = $"__decorator_wrapper_{Guid.NewGuid():N}";
            var wrapperWithId = new FuncLangValue(
                new LangId(wrapperVarName, position: decorator.Position),
                wrapper.Ids ?? [],
                wrapper.BlockStatement,
                wrapper.GenericParameters,
                wrapper.Position,
                isLambda: false
            );
            manager.AddClassAndFunc(wrapperWithId);

            // 调用包装器，传入目标函数
            var wrapperCallExpr = new FunctionCallExpression(
                new LangId(wrapperVarName, position: decorator.Position),
                [new LangId(tempVarName, position: decorator.Position)],
                decorator.Position
            );
            result = wrapperCallExpr.Accept(RuntimeVisitors.CreateInterpreter(manager));
        }
        else
        {
            // 无参数的装饰器：直接调用装饰器函数，传入目标函数
            var callExpr = new FunctionCallExpression(
                new LangId(decorator.Name, position: decorator.Position),
                [new LangId(tempVarName, position: decorator.Position)],
                decorator.Position
            );
            result = callExpr.Accept(RuntimeVisitors.CreateInterpreter(manager));
        }

        return result;
    }
    /// <summary>
    /// 应用装饰器到异步函数
    /// </summary>
    internal static AsyncFuncLangValue ApplyDecorators(AsyncFuncLangValue originalFunc, VariateManager manager)
    {
        if (originalFunc.Decorators is null || originalFunc.Decorators.Count == 0)
        {
            return originalFunc;
        }

        // 对于异步函数，装饰器的处理稍微复杂一些
        // 因为装饰器可能返回普通函数或异步函数
        // 这里我们简化处理：装饰器必须返回异步函数

        LangValueType currentFunc = originalFunc;

        // 从下到上应用装饰器（最接近函数的装饰器最先应用）
        for (int i = originalFunc.Decorators.Count - 1; i >= 0; i--)
        {
            var decorator = originalFunc.Decorators[i];
            currentFunc = ApplySingleDecorator(decorator, currentFunc, manager);
        }

        // 确保最终结果是异步函数
        if (currentFunc is not AsyncFuncLangValue asyncFunc)
        {
            throw new InvalidOperationError(originalFunc.Position, "异步函数的装饰器必须返回异步函数");
        }

        return asyncFunc;
    }
}
