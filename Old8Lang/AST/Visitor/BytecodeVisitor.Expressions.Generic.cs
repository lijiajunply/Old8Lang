using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 泛型表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitGenericInstanceExpression(GenericInstanceExpression node)
    {
        // 泛型实例化的字节码生成
        // 策略：在编译时进行泛型特化，生成具体类型的类或函数

        // 获取基础表达式名称
        if (node.BaseExpression is not LangId identifier)
        {
            throw new InvalidOperationException("字节码模式下泛型表达式必须使用简单的标识符");
        }

        var name = identifier.IdName;

        // 判断是泛型类还是泛型函数
        if (_compiler.IsGenericClass(name))
        {
            // 处理泛型类实例化
            HandleGenericClassInstantiation(node, name);
        }
        else if (_compiler.IsGenericFunction(name))
        {
            // 处理泛型函数调用
            HandleGenericFunctionCall(node, name);
        }
        else
        {
            throw new InvalidOperationException($"找不到泛型类或泛型函数定义：{name}");
        }

        return null;
    }


}
