using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 运算表达式
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitOperation(Operation node)
    {
        // 特殊处理 Dot 运算符（成员访问和方法调用）
        if (node.Opera == LangTokenType.Dot)
        {
            // 检查是否是 super 表达式
            bool isSuperAccess = node.Left is SuperExpression;

            // 生成左操作数代码（对象或super）
            if (node.Left != null)
                node.Left.Accept(this);

            // 检查右操作数是否是方法调用（Instance）
            if (node.Right is Instance instance)
            {
                // 这是方法调用：object.method(args) 或 super.method(args)
                // 左操作数（对象或super）已经在栈上

                // 生成所有参数的代码
                foreach (var arg in instance.Ids)
                {
                    arg.Accept(this);
                }

                string methodName = instance.Id.IdName;
                int argCount = instance.Ids.Count + 1; // +1 因为对象本身是第一个参数

                if (isSuperAccess)
                {
                    // super.method(args) - 调用父类方法
                    Emit(OpCode.CallSuperMethod, new object[] { argCount, methodName });
                }
                else
                {
                    // object.method(args) - 调用对象方法
                    // 使用 CallMethod 指令，它会在对象的类中查找方法
                    Emit(OpCode.CallMethod, new object[] { argCount, methodName });
                }
            }
            else if (node.Right is LangId memberId)
            {
                // 这是字段访问：object.field 或 super.field
                // 左操作数（对象或super）已经在栈上
                string fieldName = memberId.IdName;

                if (isSuperAccess)
                {
                    // super.field - 访问父类字段
                    Emit(OpCode.GetSuperField, fieldName);
                }
                else
                {
                    // object.field - 访问普通字段
                    Emit(OpCode.GetField, fieldName);
                }
            }
            else if (node.Right is ClassMemberId classMemberId)
            {
                // 这是字段访问：object.field 或 super.field（字段带访问修饰符）
                // 左操作数（对象或super）已经在栈上
                string fieldName = classMemberId.IdName;

                if (isSuperAccess)
                {
                    // super.field - 访问父类字段
                    Emit(OpCode.GetSuperField, fieldName);
                }
                else
                {
                    // object.field - 访问普通字段
                    Emit(OpCode.GetField, fieldName);
                }
            }
            else
            {
                // 其他情况：生成右操作数代码
                if (node.Right != null)
                    node.Right.Accept(this);
                Emit(OpCode.Nop);
            }

            return null;
        }

        // 原有逻辑：处理其他运算符

        // 特殊处理类型转换运算符 (as)
        if (node.Opera == LangTokenType.As)
        {
            // 生成左操作数（要转换的值）
            if (node.Left != null)
                node.Left.Accept(this);

            // 获取目标类型名称
            string typeName;
            if (node.Right is LangId rightId)
            {
                typeName = rightId.IdName;
            }
            else if (node.Right is TypeLangValue typeValue)
            {
                typeName = typeValue.ToString();
            }
            else if (node.Right is StringLangValue stringValue)
            {
                typeName = stringValue.Value;
            }
            else
            {
                throw new Exception($"类型转换运算符 'as' 的右操作数必须是类型名称，实际为: {node.Right?.GetType().Name}");
            }

            // 生成 Cast 指令
            Emit(OpCode.Cast, typeName);
            return null;
        }

        // 特殊处理类型检查运算符 (is)
        if (node.Opera == LangTokenType.Is)
        {
            // 生成左操作数（要检查的值）
            if (node.Left != null)
                node.Left.Accept(this);

            // 获取目标类型名称
            string typeName;
            if (node.Right is LangId rightId)
            {
                typeName = rightId.IdName;
            }
            else if (node.Right is TypeLangValue typeValue)
            {
                typeName = typeValue.ToString();
            }
            else if (node.Right is StringLangValue stringValue)
            {
                typeName = stringValue.Value;
            }
            else
            {
                throw new Exception($"类型检查运算符 'is' 的右操作数必须是类型名称，实际为: {node.Right?.GetType().Name}");
            }

            // 生成 IsType 指令
            Emit(OpCode.IsType, typeName);
            return null;
        }

        // 检查是否是一元运算符
        bool isUnaryOperator = node.Opera == LangTokenType.Exclamation || // !
                                node is { Opera: LangTokenType.Minus, Left: null }; // 一元负号

        if (isUnaryOperator)
        {
            // 一元运算符：只生成右操作数
            if (node.Right != null)
                node.Right.Accept(this);

            // 生成一元运算符指令
            switch (node.Opera)
            {
                case LangTokenType.Exclamation:  // !
                    Emit(OpCode.Not);
                    break;
                case LangTokenType.Minus:  // 一元负号
                    Emit(OpCode.Neg);
                    break;
            }
        }
        else
        {
            // 二元运算符：生成左右操作数
            if (node.Left != null)
                node.Left.Accept(this);

            if (node.Right != null)
                node.Right.Accept(this);

            // 生成二元运算符指令
            switch (node.Opera)
            {
                case LangTokenType.Plus:
                    Emit(OpCode.Add);
                    break;
                case LangTokenType.Minus:
                    Emit(OpCode.Sub);
                    break;
                case LangTokenType.Star:
                    Emit(OpCode.Mul);
                    break;
                case LangTokenType.Slash:
                    Emit(OpCode.Div);
                    break;
                case LangTokenType.Percent:
                    Emit(OpCode.Mod);
                    break;
                case LangTokenType.Caret:  // ^ 幂运算
                    Emit(OpCode.Pow);
                    break;
                case LangTokenType.Equals:  // ==
                    Emit(OpCode.Equal);
                    break;
                case LangTokenType.NotEquals:  // !=
                    Emit(OpCode.NotEqual);
                    break;
                case LangTokenType.GreaterThan:  // >
                    Emit(OpCode.Greater);
                    break;
                case LangTokenType.LessThan:  // <
                    Emit(OpCode.Less);
                    break;
                case LangTokenType.GreaterThanEquals:  // >=
                    Emit(OpCode.GreaterEqual);
                    break;
                case LangTokenType.LessThanEquals:  // <=
                    Emit(OpCode.LessEqual);
                    break;
                case LangTokenType.And:  // &&
                    Emit(OpCode.And);
                    break;
                case LangTokenType.Or:  // ||
                    Emit(OpCode.Or);
                    break;
                default:
                    Emit(OpCode.Nop); // 未支持的运算符
                    break;
            }
        }

        return null;
    }


}
