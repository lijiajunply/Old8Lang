using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

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
            if (_manager == null)
            {
                throw new NameError(node.Position, "this");
            }

            var thisValue = _manager.GetValue(new LangId("this"));
            if (thisValue != null)
            {
                return thisValue;
            }

            // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
            throw new NameError(node.Position, "this");
        }

        // 先尝试获取普通变量
        if (_manager != null!)
        {
            var value = _manager.GetValue(node);
            if (value != null)
            {
                return value;
            }

            // 如果不是普通变量，尝试获取类或函数
            var anyValue = _manager.GetAny(node);
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
}
