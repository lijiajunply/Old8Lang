using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression;

public class Operation(LangExpression? left, LangTokenType opera, LangExpression? right, SourcePosition position = default)
    : LangExpression(position)
{
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);
    
    private string OperaToString()
    {
        if (Opera == LangTokenType.Plus)
            return "+";
        if (Opera == LangTokenType.Minus)
            return "-";
        if (Opera == LangTokenType.Star)
            return "*";
        if (Opera == LangTokenType.Slash)
            return "/";
        if (Opera == LangTokenType.Caret)
            return "^";
        if (Opera == LangTokenType.GreaterThan)
            return ">";
        if (Opera == LangTokenType.LessThan)
            return "<";
        if (Opera == LangTokenType.Equals)
            return "==";
        if (Opera == LangTokenType.NotEquals)
            return "!=";
        if (Opera == LangTokenType.Dot)
            return ".";
        if (Opera == LangTokenType.Percent)
            return "%";
        if (Opera == LangTokenType.LessThanEquals)
            return "<=";
        if (Opera == LangTokenType.GreaterThanEquals)
            return ">=";
        if (Opera == LangTokenType.Xor)
            return "^";
        if (Opera == LangTokenType.And)
            return "&&";
        if (Opera == LangTokenType.Or)
            return "||";
        if (Opera == LangTokenType.As)
            return "as";
        return "";
    }

    public override string ToString() => $"{Left}{OperaToString()}{Right}";
    private Type? Type { get; set; }
    public LangExpression? Left { get; set; } = left;
    public LangExpression? Right { get; set; } = right;
    public LangTokenType Opera { get; set; } = opera;

    public override LangValueType Run(VariateManager manager)
    {
        // 处理一元运算符
        if (Left == null)
        {
            if (Opera == LangTokenType.Exclamation)
            {
                var rightValue = Right?.Run(manager) as BoolLangValue ??
                                 throw new InvalidOperationError(this, "NOT运算符只支持布尔类型");
                return new BoolLangValue(!rightValue.Value);
            }

            if (Opera == LangTokenType.Minus)
            {
                var rightValue = Right?.Run(manager);
                if (rightValue is IntLangValue intValue)
                    return new IntLangValue(-intValue.Value);
                if (rightValue is DoubleLangValue doubleValue)
                    return new DoubleLangValue(-doubleValue.Value);
                throw new InvalidOperationError(this, "一元负号运算符只支持整数和浮点数");
            }

            throw new InvalidOperationError(this, "不支持的一元运算符");
        }

        // 处理this.id => dot_value
        if (Opera == LangTokenType.Dot && Left is LangId { IdName: "this" } && Right != null)
        {
            var thisValue = Left.Run(manager);
            if (thisValue is AnyLangValue anyValue)
            {
                return anyValue.Dot(Right);
            }

            throw new NameError(Left, "this");
        }

        // 处理逻辑AND操作 - 短路求值
        if (Opera == LangTokenType.And)
        {
            var leftValue = Left.Run(manager) as BoolLangValue ??
                            throw new InvalidOperationError(this, "AND运算符只支持布尔类型");
            if (!leftValue.Value)
            {
                return new BoolLangValue(false);
            }

            var rightValue = Right?.Run(manager) as BoolLangValue ??
                             throw new InvalidOperationError(this, "AND运算符只支持布尔类型");
            return new BoolLangValue(leftValue.Value && rightValue.Value);
        }

        // 处理逻辑OR操作 - 短路求值
        if (Opera == LangTokenType.Or)
        {
            var leftValue = Left.Run(manager) as BoolLangValue ?? throw new InvalidOperationError(this, "OR运算符只支持布尔类型");
            if (leftValue.Value)
            {
                return new BoolLangValue(true);
            }

            var rightValue = Right?.Run(manager) as BoolLangValue ??
                             throw new InvalidOperationError(this, "OR运算符只支持布尔类型");
            return new BoolLangValue(leftValue.Value || rightValue.Value);
        }

        // 处理点操作
        if (Opera == LangTokenType.Dot)
        {
            var dotLeftResult = Left.Run(manager);
            if (dotLeftResult is AnyLangValue any)
            {
                if (Right is Instance r1)
                {
                    var ids = r1.Ids.Select(x => x.Run(manager)).OfType<LangExpression>().ToList();
                    var newInstance = new Instance(r1.Id, ids, r1.Position);
                    // 设置外部管理器，确保能访问最新的外部变量
                    any.ExternalManager = manager;
                    return any.Dot(newInstance);
                }

                if (Right != null)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    any.ExternalManager = manager;
                    return any.Dot(Right);
                }
            }
            else if (dotLeftResult is ListLangValue list)
            {
                if (Right is Instance instance)
                {
                    var ids = instance.Ids.Select(x => x.Run(manager)).OfType<LangExpression>().ToList();
                    var newInstance = new Instance(instance.Id, ids);
                    return list.Dot(newInstance);
                }

                if (Right != null)
                {
                    // 检查是否是索引访问
                    var listIndexResult = Right.Run(manager);
                    if (listIndexResult is IntLangValue intValue)
                    {
                        return list.Get(intValue);
                    }
                    // 如果不是整数索引，则作为方法调用处理
                    return list.Dot(Right);
                }
            }
            else if (dotLeftResult is NativeStaticAny native)
            {
                if (Right is not Instance r1) throw new InvalidOperationError(this, "原生静态类型操作需要实例");
                var newInstance = new Instance(r1.Id, r1.Ids);
                return native.Dot(newInstance);
            }
            // 处理静态成员访问：ClassName.staticMember
            else if (dotLeftResult is TypeTemplate typeTemplate)
            {
                if (Right is Instance r1)
                {
                    // 处理静态方法调用
                    var newInstance = new Instance(r1.Id, r1.Ids, r1.Position);
                    return typeTemplate.Dot(newInstance, manager);
                }

                if (Right != null)
                {
                    // 处理静态成员访问
                    return typeTemplate.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is ArrayLangValue array)
            {
                // 处理数组索引访问，需要先运行Right表达式
                if (Right != null)
                {
                    var arrayIndexResult = Right.Run(manager);
                    if (arrayIndexResult is IntLangValue intValue)
                    {
                        return array.Get(intValue);
                    }
                    throw new InvalidOperationError(this, $"数组索引必须是整数类型，当前为 '{arrayIndexResult.GetType().Name}'");
                }
            }
            else if (dotLeftResult != null! && Right != null)
            {
                return dotLeftResult.Dot(Right);
                // throw new InvalidOperationError(this, $"类型 '{dotLeftResult.GetType().Name}' 不支持点操作");
            }
            else
            {
                throw new InvalidOperationError(this, "连接运算符左右操作数均不能为空");
            }
        }

        // 处理其他二元运算符，运行左右操作数
        // 注意：直接在当前作用域中运行两个操作数
        // Set方法已经被修复，只在当前作用域中设置变量，不会修改外部变量
        var leftResult = Left.Run(manager);

        if (Opera == LangTokenType.As)
        {
            // 处理类型转换操作：left as right
            // 右侧应该是一个类型标识符，如 int, double, string 等
            // 直接从 Right 表达式获取类型名称，而不是从运行结果
            string typeName;
            switch (Right)
            {
                case LangId rightLangId:
                    typeName = rightLangId.IdName;
                    break;
                case TypeLangValue rightTypeLangValue:
                    typeName = rightTypeLangValue.ToString();
                    break;
                default:
                {
                    // 如果是其他表达式，尝试获取其值作为类型
                    var rightAsResult = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");

                    if (rightAsResult is TypeLangValue typeLangValue)
                    {
                        return leftResult.Converse(typeLangValue, manager);
                    }

                    typeName = rightAsResult.ToString();
                    break;
                }
            }

            // 创建或获取类型对象
            var type = new TypeLangValue(typeName);
            return leftResult.Converse(type, manager);
        }

        var rightResult = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");

        // left xor right
        if (leftResult is BoolLangValue boolLeft && rightResult is BoolLangValue boolRight &&
            Opera == LangTokenType.Xor)
        {
            return new BoolLangValue(!boolLeft.Equal(boolRight));
        }

        // == , < , > 
        if (leftResult != null! && rightResult != null!)
        {
            switch (Opera)
            {
                case LangTokenType.Equals:
                    return new BoolLangValue(leftResult.Equal(rightResult));
                case LangTokenType.LessThan:
                    return new BoolLangValue(leftResult.Less(rightResult));
                case LangTokenType.GreaterThan:
                    return new BoolLangValue(leftResult.Greater(rightResult));
                case LangTokenType.NotEquals:
                    return new BoolLangValue(!leftResult.Equal(rightResult));
                case LangTokenType.LessThanEquals:
                    return new BoolLangValue(leftResult.LessEqual(rightResult));
                case LangTokenType.GreaterThanEquals:
                    return new BoolLangValue(leftResult.GreaterEqual(rightResult));
                // r (+-*/%) l
                case LangTokenType.Plus:
                    return leftResult.Plus(rightResult);
                case LangTokenType.Minus:
                    return leftResult.Minus(rightResult);
                case LangTokenType.Star:
                    return leftResult.Times(rightResult);
                case LangTokenType.Slash:
                    return leftResult.Divide(rightResult);
                case LangTokenType.Percent:
                    return leftResult.Mod(rightResult);
                case LangTokenType.Caret:
                    return leftResult.Power(rightResult);
            }
        }

        throw new InvalidOperationError(this, $"不支持的操作类型: {OperaToString()}");
    }


    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 首先生成计算值的IL指令，将结果压入栈中
        LoadIlValue(ilGenerator, local);
        // 然后获取结果类型
        var type = OutputType(local);
        if (type == null) return;
        // 声明局部变量或使用已存在的
        var existingLocal = local.GetLocalVar(idName);
        if (existingLocal != null)
        {
            if (existingLocal.LocalType != type)
            {
                // 类型不匹配，重新声明
                local.RemoveLocalVar(idName);
                var newLocal = ilGenerator.DeclareLocal(type);
                local.AddLocalVar(idName, newLocal);
                ilGenerator.Emit(OpCodes.Stloc, newLocal.LocalIndex);
            }
            else
            {
                // 类型匹配，直接存储
                ilGenerator.Emit(OpCodes.Stloc, existingLocal.LocalIndex);
            }
        }
        else
        {
            // 首次声明变量
            var newLocal = ilGenerator.DeclareLocal(type);
            local.AddLocalVar(idName, newLocal);
            ilGenerator.Emit(OpCodes.Stloc, newLocal.LocalIndex);
        }
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 调用带有ILGenerator参数的OutputType方法，该方法会生成实际的IL指令
        Type = OutputType(ilGenerator, local);
    }

    public override Type? OutputType(LocalManager local)
    {
        if (Type != null) return Type;
        // 直接返回类型信息，不创建临时方法
        var leftType = Left?.OutputType(local);
        var rightType = Right?.OutputType(local);

        // 如果leftType是TypeBuilder，返回typeof(object)，避免后续访问TypeBuilder的成员
        if (leftType is TypeBuilder)
        {
            return typeof(object);
        }

        // 处理成员访问（Dot操作符）
        if (Opera == LangTokenType.Dot && Right is LangId rightId)
        {
            if (leftType != null)
            {
                // 尝试获取字段类型
                var field = leftType.GetField(rightId.IdName);
                if (field != null)
                {
                    return field.FieldType;
                }

                // 尝试获取属性类型
                var property = leftType.GetProperty(rightId.IdName);
                if (property != null)
                {
                    return property.PropertyType;
                }
            }
            return typeof(object);
        }

        // 对于加法运算，如果任一操作数是字符串，则返回字符串类型
        if (Opera == LangTokenType.Plus && (leftType == typeof(string) || rightType == typeof(string)))
        {
            return typeof(string);
        }

        // 对于二元运算，根据操作类型返回合适的类型
        if (Opera == LangTokenType.Star || Opera == LangTokenType.Plus || Opera == LangTokenType.Minus ||
            Opera == LangTokenType.Slash || Opera == LangTokenType.Percent || Opera == LangTokenType.Caret)
        {
            // 对于数值运算，返回int类型
            return typeof(int);
        }

        return leftType == typeof(object) ? rightType : leftType;
    }

    private Type OutputType(ILGenerator ilGenerator, LocalManager local)
    {
        var leftType = Left?.OutputType(local);
        var rightType = Right?.OutputType(local);

        // 如果leftType是TypeBuilder，返回typeof(object)，避免后续访问TypeBuilder的成员
        if (leftType is TypeBuilder)
        {
            leftType = typeof(object);
        }

        // 如果rightType是TypeBuilder，返回typeof(object)，避免后续访问TypeBuilder的成员
        if (rightType is TypeBuilder)
        {
            rightType = typeof(object);
        }

        if (Left == null)
        {
            // 处理单目运算符
            switch (Opera)
            {
                case LangTokenType.Exclamation:
                    Right?.LoadIlValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
                    ilGenerator.Emit(OpCodes.Xor); // 进行异或运算
                    return typeof(bool);
                case LangTokenType.Minus:
                    Right?.LoadIlValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Neg);
                    return rightType ?? throw new InvalidOperationError(this, "右操作数不能为空");

                default:
                    throw new InvalidOperationError(this, $"不支持的一元运算符: {Opera}");
            }
        }

        switch (Opera)
        {
            case LangTokenType.Plus:
                if (leftType == typeof(string) || rightType == typeof(string))
                {
                    // 处理字符串连接 - 需要调用string.Concat方法
                    // 加载左操作数并立即装箱（如果需要）
                    Left?.LoadIlValue(ilGenerator, local);
                    if (leftType != typeof(string))
                    {
                        ilGenerator.Emit(OpCodes.Box, leftType!);
                    }

                    // 加载右操作数并立即装箱（如果需要）
                    Right?.LoadIlValue(ilGenerator, local);
                    if (rightType != typeof(string))
                    {
                        ilGenerator.Emit(OpCodes.Box, rightType!);
                    }

                    // 调用string.Concat(object, object)
                    var concatMethod = typeof(string).GetMethod("Concat", [typeof(object), typeof(object)])!;
                    ilGenerator.Emit(OpCodes.Call, concatMethod);
                    return typeof(string);
                }

                // 处理数值类型加法
                Left?.LoadIlValue(ilGenerator, local);
                // 如果左操作数是object类型，拆箱为int
                if (leftType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                    leftType = typeof(int);
                }

                Right?.LoadIlValue(ilGenerator, local);
                // 如果右操作数是object类型，拆箱为int
                if (rightType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                    rightType = typeof(int);
                }

                if (leftType == typeof(double) || rightType == typeof(double))
                {
                    // 确保两个操作数都是double类型
                    if (leftType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    if (rightType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    ilGenerator.Emit(OpCodes.Add);
                    return typeof(double);
                }

                // 整数加法
                ilGenerator.Emit(OpCodes.Add);
                return typeof(int);
            case LangTokenType.Minus:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                if (leftType == typeof(double) || rightType == typeof(double))
                {
                    // 确保两个操作数都是double类型
                    if (leftType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    if (rightType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    ilGenerator.Emit(OpCodes.Sub);
                    return typeof(double);
                }

                ilGenerator.Emit(OpCodes.Sub);
                return typeof(int);
            case LangTokenType.Star:
                // 简化处理，只处理基本的int和double类型
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);

                // 处理不同类型的乘法
                if (leftType == typeof(double) || rightType == typeof(double))
                {
                    // 确保两个操作数都是double类型
                    if (leftType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    if (rightType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    ilGenerator.Emit(OpCodes.Mul);
                    return typeof(double);
                }
                // 整数乘法
                else
                {
                    // 确保两个操作数都是int类型
                    if (leftType != typeof(int))
                    {
                        // 对于值类型，使用适当的转换指令
                        if (leftType == typeof(double))
                        {
                            ilGenerator.Emit(OpCodes.Conv_I4);
                        }
                        else if (leftType == typeof(bool))
                        {
                            // 布尔值转换为int，true->1, false->0
                        }
                        // 对于引用类型，拆箱为int
                        else if (leftType is { IsValueType: false })
                        {
                            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                        }
                    }

                    if (rightType != typeof(int))
                    {
                        // 对于值类型，使用适当的转换指令
                        if (rightType == typeof(double))
                        {
                            ilGenerator.Emit(OpCodes.Conv_I4);
                        }
                        else if (rightType == typeof(bool))
                        {
                            // 布尔值转换为int，true->1, false->0
                        }
                        // 对于引用类型，拆箱为int
                        else if (rightType is { IsValueType: false })
                        {
                            ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                        }
                    }

                    ilGenerator.Emit(OpCodes.Mul);
                    return typeof(int);
                }
            case LangTokenType.Slash:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                if (leftType == typeof(double) || rightType == typeof(double))
                {
                    // 确保两个操作数都是double类型
                    if (leftType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    if (rightType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    ilGenerator.Emit(OpCodes.Div);
                    return typeof(double);
                }

                ilGenerator.Emit(OpCodes.Div);
                return typeof(int);
            case LangTokenType.Percent:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                if (leftType == typeof(double) || rightType == typeof(double))
                {
                    // 确保两个操作数都是double类型
                    if (leftType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    if (rightType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }

                    ilGenerator.Emit(OpCodes.Rem);
                    return typeof(double);
                }

                ilGenerator.Emit(OpCodes.Rem);
                return typeof(int);
            case LangTokenType.Caret:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                // 确保两个操作数都是double类型，因为Math.Pow需要double参数
                if (leftType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }

                if (rightType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_R8);
                }

                // 调用Math.Pow方法
                var powMethod = typeof(Math).GetMethod("Pow", [typeof(double), typeof(double)])!;
                ilGenerator.Emit(OpCodes.Call, powMethod);
                // 如果两个操作数都是int类型，返回int类型
                if (leftType == typeof(int) && rightType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Conv_I4);
                    return typeof(int);
                }

                // 否则返回double类型
                return typeof(double);
            case LangTokenType.GreaterThan:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                return typeof(bool);
            case LangTokenType.LessThan:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                return typeof(bool);
            case LangTokenType.Equals:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                return typeof(bool);
            case LangTokenType.NotEquals:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case LangTokenType.And:
            {
                // 实现短路求值：如果左操作数为false，则跳过右操作数
                var endLabel = ilGenerator.DefineLabel();
                var falseLabel = ilGenerator.DefineLabel();

                // 加载左操作数
                Left?.LoadIlValue(ilGenerator, local);
                // 如果左操作数为false，跳转到falseLabel
                ilGenerator.Emit(OpCodes.Brfalse, falseLabel);
                // 加载右操作数
                Right?.LoadIlValue(ilGenerator, local);
                // 右操作数已经在栈上，直接跳转到endLabel
                ilGenerator.Emit(OpCodes.Br, endLabel);
                // 左操作数为false的情况
                ilGenerator.MarkLabel(falseLabel);
                ilGenerator.Emit(OpCodes.Ldc_I4_0); // 加载false
                // 结果为true或false的情况
                ilGenerator.MarkLabel(endLabel);
                return typeof(bool);
            }
            case LangTokenType.Or:
            {
                // 实现短路求值：如果左操作数为true，则跳过右操作数
                var endLabel = ilGenerator.DefineLabel();
                var trueLabel = ilGenerator.DefineLabel();

                // 加载左操作数
                Left?.LoadIlValue(ilGenerator, local);
                // 如果左操作数为true，跳转到trueLabel
                ilGenerator.Emit(OpCodes.Brtrue, trueLabel);
                // 加载右操作数
                Right?.LoadIlValue(ilGenerator, local);
                // 右操作数已经在栈上，直接跳转到endLabel
                ilGenerator.Emit(OpCodes.Br, endLabel);
                // 左操作数为true的情况
                ilGenerator.MarkLabel(trueLabel);
                ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载true
                // 结果为true或false的情况
                ilGenerator.MarkLabel(endLabel);
                return typeof(bool);
            }
            case LangTokenType.Xor:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case LangTokenType.LessThanEquals:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case LangTokenType.GreaterThanEquals:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case LangTokenType.As:
                // 处理类型转换操作：left as right
                // 右侧应该是一个类型标识符，如 int, double, string 等
                if (Right is LangId rightLangId)
                {
                    string typeName = rightLangId.IdName;
                    // 加载左侧值
                    Left!.LoadIlValue(ilGenerator, local);
                    // 根据类型名称生成转换指令
                    Type targetType = typeName switch
                    {
                        "int" => typeof(int),
                        "double" => typeof(double),
                        "string" => typeof(string),
                        "bool" => typeof(bool),
                        "char" => typeof(char),
                        "list" => typeof(List<object>),
                        "array" => typeof(object[]),
                        "dictionary" => typeof(Dictionary<object, object>),
                        _ => typeof(object)
                    };

                    // 确保leftType不为null
                    leftType ??= typeof(object);

                    // 处理基本类型到字符串的转换
                    if (targetType == typeof(string))
                    {
                        // 所有类型转换为字符串
                        if (leftType == typeof(int))
                        {
                            // int转string
                            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToString", [typeof(int)])!);
                        }
                        else if (leftType == typeof(double))
                        {
                            // double转string
                            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToString", [typeof(double)])!);
                        }
                        else if (leftType == typeof(bool))
                        {
                            // bool转string
                            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToString", [typeof(bool)])!);
                        }
                        else if (leftType == typeof(object))
                        {
                            // object转string
                            ilGenerator.Emit(OpCodes.Callvirt, typeof(object).GetMethod("ToString")!);
                        }
                        // 如果已经是string类型，不需要转换
                    }
                    // 处理数值类型之间的转换
                    else if (leftType == typeof(int))
                    {
                        if (targetType == typeof(double))
                        {
                            // 整数转双精度浮点数
                            ilGenerator.Emit(OpCodes.Conv_R8);
                        }
                        else if (targetType == typeof(bool))
                        {
                            // 整数转布尔值：非零即真
                            ilGenerator.Emit(OpCodes.Ldc_I4_0);
                            ilGenerator.Emit(OpCodes.Cgt);
                        }
                    }
                    else if (leftType == typeof(double))
                    {
                        if (targetType == typeof(int))
                        {
                            // 双精度浮点数转整数
                            ilGenerator.Emit(OpCodes.Conv_I4);
                        }
                    }
                    else if (leftType == typeof(bool))
                    {
                        if (targetType == typeof(int))
                        {
                            // 布尔值转整数：true->1, false->0
                            // 布尔值在栈上是1(true)或0(false)，直接转换为int即可
                        }
                    }
                    // 其他情况：如果类型不同但都是值类型，尝试使用Convert类转换
                    else if (leftType.IsValueType && targetType.IsValueType && leftType != targetType)
                    {
                        var convertMethod = typeof(Convert).GetMethod($"To{targetType.Name}", [leftType]);
                        if (convertMethod != null)
                        {
                            ilGenerator.Emit(OpCodes.Call, convertMethod);
                        }
                    }
                    // 其他情况：值类型装箱，引用类型拆箱
                    else if (leftType.IsValueType && !targetType.IsValueType)
                    {
                        // 值类型到引用类型转换，装箱
                        ilGenerator.Emit(OpCodes.Box, leftType);
                    }
                    else if (!leftType.IsValueType && targetType.IsValueType)
                    {
                        // 引用类型到值类型转换，拆箱
                        ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
                    }

                    return targetType;
                }
                else
                {
                    // 非LangId类型，返回object类型
                    Left!.LoadIlValue(ilGenerator, local);
                    return typeof(object);
                }
            case LangTokenType.Dot:
                if (local.InClassEnv != null && Left is LangId { IdName: "this" })
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    if (Right is not LangId rightId) return local.InClassEnv;

                    // 优先从 FieldVar 中查找字段（支持 TypeBuilder）
                    if (local.FieldVar.TryGetValue(rightId.IdName, out var fieldInfo))
                    {
                        ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
                        return fieldInfo.FieldType;
                    }

                    // 如果 FieldVar 中没有，尝试从类型中获取（仅适用于已创建的类型）
                    if (local.InClassEnv is not TypeBuilder)
                    {
                        var field = local.InClassEnv.GetField(rightId.IdName);
                        if (field != null)
                        {
                            ilGenerator.Emit(OpCodes.Ldfld, field);
                            return field.FieldType;
                        }

                        var p = local.InClassEnv.GetProperty(rightId.IdName);
                        if (p != null && p.GetGetMethod() != null)
                        {
                            ilGenerator.Emit(OpCodes.Call, p.GetGetMethod()!);
                            return p.PropertyType;
                        }
                    }

                    // 如果没有找到字段或属性，返回typeof(object)
                    return typeof(object);
                }

                if (Right is Instance instance)
                {
                    Left!.LoadIlValue(ilGenerator, local);
                    var types = new List<Type>();
                    foreach (var instanceId in instance.Ids)
                    {
                        instanceId.LoadIlValue(ilGenerator, local);
                        var idType = instanceId.OutputType(local);
                        if (idType!.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, idType);
                        }

                        types.Add(instanceId.OutputType(local)!);
                    }

                    // 尝试查找精确匹配的方法
                    var m = leftType!.GetMethod(instance.Id.IdName, [.. types]);

                    // 如果没有找到精确匹配，尝试查找参数数量匹配的方法
                    if (m == null)
                    {
                        m = leftType.GetMethods()
                            .FirstOrDefault(method =>
                                method.Name == instance.Id.IdName &&
                                method.GetParameters().Length == instance.Ids.Count);
                    }

                    if (m == null)
                    {
                        // 方法未找到，抛出异常
                        throw new InvalidOperationError(this, $"方法 '{instance.Id.IdName}' 未找到",
                            $"无法在类型 '{leftType.Name}' 中找到方法 '{instance.Id.IdName}'，参数类型为: {string.Join(", ", types.Select(t => t.Name))}");
                    }

                    // 对于实例方法使用 Callvirt，对于静态方法使用 Call
                    if (m.IsStatic)
                    {
                        ilGenerator.Emit(OpCodes.Call, m);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Callvirt, m);
                    }
                    return m.ReturnType;
                }

                if (Right is LangId id)
                {
                    Left!.LoadIlValue(ilGenerator, local);
                    var field = leftType!.GetField(id.IdName);
                    if (field == null)
                    {
                        var p = leftType.GetProperty(id.IdName);
                        if (p == null)
                        {
                            throw new InvalidOperationError(this, $"类型 {leftType.Name} 没有属性 {id.IdName}");
                        }

                        var getMethod = p.GetGetMethod();
                        if (getMethod == null)
                        {
                            throw new InvalidOperationError(this, $"属性 {id.IdName} 没有公开的 getter 方法");
                        }

                        ilGenerator.Emit(OpCodes.Call, getMethod);
                        return p.PropertyType;
                    }

                    ilGenerator.Emit(OpCodes.Ldfld, field);
                    return field.FieldType;
                }

                // 处理索引访问: left[right]，例如 array[0], list[1], dict["key"]
                if (rightType != null)
                {
                    Left!.LoadIlValue(ilGenerator, local);
                    Right!.LoadIlValue(ilGenerator, local);

                    // 处理不同类型的索引访问
                    if (leftType == typeof(object[]))
                    {
                        // 数组索引访问
                        ilGenerator.Emit(OpCodes.Ldelem_Ref);
                        return typeof(object);
                    }
                    else if (leftType == typeof(List<object>))
                    {
                        // List<T>索引访问，调用索引器的getter方法
                        var indexer = typeof(List<object>).GetProperty("Item")!;
                        ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
                        return typeof(object);
                    }
                    else if (leftType == typeof(Dictionary<object, object>))
                    {
                        // Dictionary<TKey, TValue>索引访问，调用索引器的getter方法
                        var indexer = typeof(Dictionary<object, object>).GetProperty("Item")!;
                        ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
                        return typeof(object);
                    }
                    else if (leftType == typeof(string))
                    {
                        // 字符串索引访问
                        var indexer = typeof(string).GetProperty("Chars")!;
                        ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
                        return typeof(char);
                    }

                    // 默认情况，尝试装箱并调用索引器
                    ilGenerator.Emit(OpCodes.Box, rightType);
                    var defaultIndexer = leftType!.GetProperty("Item")!;
                    ilGenerator.Emit(OpCodes.Callvirt, defaultIndexer.GetGetMethod()!);
                    return typeof(object);
                }

                return typeof(void);
            default:
                throw new InvalidOperationError(this, $"不支持的二元运算符: {Opera}");
        }
    }
}