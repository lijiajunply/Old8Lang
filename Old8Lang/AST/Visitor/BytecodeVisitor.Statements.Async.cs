using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 异步和生成器
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitYieldStatement(YieldStatement node)
    {
        // Yield 语句（生成器）
        // 1. 生成 yield 表达式的字节码（将值压入栈）
        node.YieldExpression.Accept(this);

        // 2. 根据当前函数是否是异步函数，生成不同的指令
        if (_compiler.IsCurrentFunctionAsync)
        {
            // 异步生成器：生成 AwaitYield 指令
            Emit(OpCode.AwaitYield);
        }
        else
        {
            // 普通生成器：生成 Yield 指令
            Emit(OpCode.Yield);
        }

        return null;
    }


    public Instruction? VisitAsyncFuncInit(AsyncFuncInit node)
    {
        // 编译异步函数定义
        var funcValue = node.AsyncFuncValue;
        var funcName = funcValue.Id?.IdName ?? "<async_lambda>";

        // 检查函数是否已经被编译过（避免重复编译）
        if (_compiler.GetFunctionIndex(funcName) >= 0)
        {
            // 函数已经在预处理阶段被编译过，跳过
            return null;
        }

        var paramNames = funcValue.Ids?.Select(id => id.IdName).ToList() ?? [];
        var paramTypes = funcValue.Ids?.Select(id => id.AssumptionType ?? "").ToList() ?? [];

        // 提取默认参数值和params参数索引
        var defaultValues = new List<object?>();
        int paramsIndex = -1;
        if (funcValue.Ids != null)
        {
            for (int i = 0; i < funcValue.Ids.Count; i++)
            {
                var param = funcValue.Ids[i];

                // 检查是否是params参数
                if (param.IsParams)
                {
                    paramsIndex = i;
                }

                if (param.DefaultValue != null)
                {
                    var defaultValue = EvaluateConstantExpression(param.DefaultValue);
                    defaultValues.Add(defaultValue);
                }
                else
                {
                    defaultValues.Add(null);
                }
            }
        }

        // 检测函数体是否包含 yield 语句
        bool containsYield = _compiler.ContainsYieldStatement(funcValue.BlockStatement);

        // 获取返回类型
        var returnType = funcValue.Id?.AssumptionType ?? "";

        // 根据是否包含 yield 调用不同的编译方法
        if (containsYield)
        {
            // 异步生成器函数
            _compiler.CompileAsyncGeneratorFunction(funcName, paramNames, paramTypes, defaultValues, funcValue.BlockStatement, paramsIndex, returnType);
        }
        else
        {
            // 普通异步函数
            _compiler.CompileAsyncFunction(funcName, paramNames, paramTypes, defaultValues, funcValue.BlockStatement, paramsIndex, returnType);
        }

        return null;
    }


}
