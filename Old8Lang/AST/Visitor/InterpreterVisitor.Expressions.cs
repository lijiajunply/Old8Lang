using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// InterpreterVisitor - Expression 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 LangId 节点
    /// </summary>
    public LangValueType VisitLangId(LangId node)
    {
        // 迁移自 LangId.Run()
        if (node.IdName == "this")
        {
            // 直接从变量储存器中获取名为"this"的变量
            if (manager == null)
            {
                throw new NameError(node.Position, "this");
            }

            var thisValue = manager.GetValue(new LangId("this"));
            if (thisValue != null)
            {
                return thisValue;
            }

            // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
            throw new NameError(node.Position, "this");
        }

        // 先尝试获取普通变量
        if (manager != null!)
        {
            var value = manager.GetValue(node);
            if (value != null)
            {
                return value;
            }

            // 如果不是普通变量，尝试获取类或函数
            var anyValue = manager.GetAny(node);
            if (anyValue != null)
            {
                return anyValue as LangValueType ?? throw new NameError(node.Position, node.IdName);
            }
        }

        // 如果都没有找到，检查是否是类型关键字
        var supportedTypes = new[]
            { "int", "double", "string", "bool", "char", "void", "list", "dict", "array", "dictionary", "tuple" };
        if (supportedTypes.Contains(node.IdName))
        {
            return new TypeLangValue(node.IdName);
        }

        throw new NameError(node.Position, node.IdName);
    }

    /// <summary>
    /// 访问 Operation 节点
    /// </summary>
    public LangValueType VisitOperation(Operation node)
    {
        // 迁移自 Operation.Run()
        // Operation 逻辑非常复杂（900+行），包含所有运算符的处理
        // 暂时调用原方法，后续再详细迁移
        return node.Run(manager);
    }

    /// <summary>
    /// 访问 FunctionCallExpression 节点
    /// </summary>
    public LangValueType VisitFunctionCallExpression(FunctionCallExpression node)
    {
        // 迁移自 FunctionCallExpression.Run()
        // FunctionCallExpression 逻辑非常复杂，包含函数调用、方法重载等
        // 暂时调用原方法，后续再详细迁移
        return node.Run(manager);
    }

    /// <summary>
    /// 访问 TernaryExpression 节点
    /// </summary>
    public LangValueType VisitTernaryExpression(TernaryExpression node)
    {
        // 完整迁移自 TernaryExpression.Run()
        // 执行条件判断
        var condition = node.Condition.Accept(this);
        if (condition is not BoolLangValue boolValue)
        {
            throw new InvalidOperationError(node, "三元条件表达式的条件必须是Bool类型");
        }

        // 根据条件结果返回相应的表达式值
        return boolValue.Value
            ? node.TrueExpression.Accept(this)
            : node.FalseExpression.Accept(this);
    }

    /// <summary>
    /// 访问 AwaitExpression 节点
    /// </summary>
    public LangValueType VisitAwaitExpression(AwaitExpression node)
    {
        // 迁移自 AwaitExpression.Run()
        // 暂时调用原方法
        return node.Run(manager);
    }

    /// <summary>
    /// 访问 AsyncStreamExpression 节点
    /// </summary>
    public LangValueType VisitAsyncStreamExpression(AsyncStreamExpression node)
    {
        // 迁移自 AsyncStreamExpression.Run()
        // 暂时调用原方法
        return node.Run(manager);
    }

    /// <summary>
    /// 访问 SuperExpression 节点
    /// </summary>
    public LangValueType VisitSuperExpression(SuperExpression node)
    {
        // 迁移自 SuperExpression.Run()
        // 暂时调用原方法
        return node.Run(manager);
    }
}
