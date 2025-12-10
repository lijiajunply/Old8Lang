using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression;

public class Operation(OldExpr? left, OperationType opera, OldExpr? right, SourcePosition position = default)
    : OldExpr(position)
{
    private string OperaToString()
    {
        if (Opera == OperationType.PLUS)
            return "+";
        if (Opera == OperationType.MINUS)
            return "-";
        if (Opera == OperationType.TIMES)
            return "*";
        if (Opera == OperationType.DIVIDE)
            return "/";
        if (Opera == OperationType.GREATER)
            return ">";
        if (Opera == OperationType.LESSER)
            return "<";
        if (Opera == OperationType.EQUALS)
            return "==";
        if (Opera == OperationType.DIFFERENT)
            return "!=";
        if (Opera == OperationType.CONCAT)
            return ".";
        if (Opera == OperationType.MODULO)
            return "%";
        if (Opera == OperationType.LESS_EQUAL)
            return "<=";
        if (Opera == OperationType.GREATER_EQUAL)
            return ">=";
        if (Opera == OperationType.XOR)
            return "^";
        if (Opera == OperationType.AND)
            return "&&";
        if (Opera == OperationType.OR)
            return "||";
        if (Opera == OperationType.AS)
            return "as";
        return "";
    }

    public override string ToString() => $"{Left}{OperaToString()}{Right}";
    public Type? Type { get; set; }
    public OldExpr? Left { get; set; } = left;
    public OldExpr? Right { get; set; } = right;
    public OperationType Opera { get; set; } = opera;

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        // 处理一元运算符
        if (Left == null)
        {
            if (Opera == OperationType.NOT)
            {
                var rightValue = Right?.Run(manager) as BoolLangValue ??
                                 throw new InvalidOperationError(this, "NOT运算符只支持布尔类型");
                return new BoolLangValue(!rightValue.Value);
            }

            if (Opera == OperationType.MINUS)
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
        if (Opera == OperationType.CONCAT && Left is LangId { IdName: "this" } && Right != null)
        {
            var thisValue = Left.Run(manager);
            if (thisValue is AnyLangValue anyValue)
            {
                return anyValue.Dot(Right);
            }

            throw new NameError(Left, "this");
        }

        // 处理逻辑AND操作 - 短路求值
        if (Opera == OperationType.AND)
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
        if (Opera == OperationType.OR)
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
        if (Opera == OperationType.CONCAT)
        {
            var dotLeftResult = Left.Run(manager);
            if (dotLeftResult is AnyLangValue any)
            {
                if (Right is Instance r1)
                {
                    var newInstance = new Instance(r1.Id, r1.Ids, r1.Position);
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
                    var newInstance = new Instance(instance.Id, instance.Ids);
                    return list.Dot(newInstance);
                }

                if (Right != null)
                {
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

        if (Opera == OperationType.AS)
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
            Opera == OperationType.XOR)
        {
            return new BoolLangValue(!boolLeft.Equal(boolRight));
        }

        // == , < , > 
        if (leftResult != null! && rightResult != null!)
        {
            switch (Opera)
            {
                case OperationType.EQUALS:
                    return new BoolLangValue(leftResult.Equal(rightResult));
                case OperationType.LESSER:
                    return new BoolLangValue(leftResult.Less(rightResult));
                case OperationType.GREATER:
                    return new BoolLangValue(leftResult.Greater(rightResult));
                case OperationType.DIFFERENT:
                    return new BoolLangValue(!leftResult.Equal(rightResult));
                case OperationType.LESS_EQUAL:
                    return new BoolLangValue(leftResult.LessEqual(rightResult));
                case OperationType.GREATER_EQUAL:
                    return new BoolLangValue(leftResult.GreaterEqual(rightResult));
                // r (+-*/%) l
                case OperationType.PLUS:
                    return leftResult.Plus(rightResult);
                case OperationType.MINUS:
                    return leftResult.Minus(rightResult);
                case OperationType.TIMES:
                    return leftResult.Times(rightResult);
                case OperationType.DIVIDE:
                    return leftResult.Divide(rightResult);
                case OperationType.MODULO:
                    return leftResult.Mod(rightResult);
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
        return leftType == typeof(object) ? rightType : leftType;
    }

    private Type OutputType(ILGenerator ilGenerator, LocalManager local)
    {
        var leftType = Left?.OutputType(local);
        var rightType = Right?.OutputType(local);

        if (Left == null)
        {
            // 处理单目运算符
            switch (Opera)
            {
                case OperationType.NOT:
                    Right?.LoadIlValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
                    ilGenerator.Emit(OpCodes.Xor); // 进行异或运算
                    return typeof(bool);
                case OperationType.MINUS:
                    Right?.LoadIlValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Neg);
                    return rightType ?? throw new InvalidOperationError(this, "右操作数不能为空");

                default:
                    throw new InvalidOperationError(this, $"不支持的一元运算符: {Opera}");
            }
        }

        switch (Opera)
        {
            case OperationType.PLUS:
                if (leftType == typeof(string) || rightType == typeof(string))
                {
                    // 处理字符串连接 - 需要调用string.Concat方法
                    Left?.LoadIlValue(ilGenerator, local);
                    Right?.LoadIlValue(ilGenerator, local);
                    // 确保两个操作数都是字符串类型，或者进行转换
                    if (leftType != typeof(string))
                    {
                        ilGenerator.Emit(OpCodes.Box, leftType!);
                    }
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
                    ilGenerator.Emit(OpCodes.Add);
                    return typeof(double);
                }
                // 整数加法
                ilGenerator.Emit(OpCodes.Add);
                return typeof(int);
            case OperationType.MINUS:
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
            case OperationType.TIMES:
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
                    ilGenerator.Emit(OpCodes.Mul);
                    return typeof(double);
                }
                else if (leftType == typeof(int) && rightType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Mul);
                    return typeof(int);
                }
                else
                {
                    // 对于其他类型，尝试转换为double类型
                    // 对于object类型，先转换为double
                    if (leftType == typeof(object))
                    {
                        // 调用Convert.ToDouble(object)
                        ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToDouble", [typeof(object)])!);
                    }
                    else if (leftType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }
                    
                    if (rightType == typeof(object))
                    {
                        // 调用Convert.ToDouble(object)
                        ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToDouble", [typeof(object)])!);
                    }
                    else if (rightType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }
                    
                    ilGenerator.Emit(OpCodes.Mul);
                    return typeof(double);
                }
            case OperationType.DIVIDE:
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
            case OperationType.MODULO:
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
            case OperationType.GREATER:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                return typeof(bool);
            case OperationType.LESSER:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                return typeof(bool);
            case OperationType.EQUALS:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                return typeof(bool);
            case OperationType.DIFFERENT:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.AND:
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
                // 如果右操作数为true，跳转到endLabel
                ilGenerator.Emit(OpCodes.Brtrue, endLabel);
                // 左操作数为false的情况
                ilGenerator.MarkLabel(falseLabel);
                ilGenerator.Emit(OpCodes.Ldc_I4_0); // 加载false
                ilGenerator.Emit(OpCodes.Br, endLabel);
                // 结果为true的情况
                ilGenerator.MarkLabel(endLabel);
                return typeof(bool);
            }
            case OperationType.OR:
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
                // 如果右操作数为false，跳转到endLabel
                ilGenerator.Emit(OpCodes.Brfalse, endLabel);
                // 左操作数为true的情况
                ilGenerator.MarkLabel(trueLabel);
                ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载true
                ilGenerator.Emit(OpCodes.Br, endLabel);
                // 结果为false的情况
                ilGenerator.MarkLabel(endLabel);
                return typeof(bool);
            }
            case OperationType.XOR:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.LESS_EQUAL:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.GREATER_EQUAL:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.AS:
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
                    
                    // 生成类型转换指令
                    if (leftType == typeof(string))
                    {
                        // 字符串到其他类型的转换
                        if (targetType == typeof(int))
                        {
                            // 字符串转整数
                            ilGenerator.Emit(OpCodes.Call, typeof(int).GetMethod("Parse", [typeof(string)])!);
                        }
                        else if (targetType == typeof(double))
                        {
                            // 字符串转双精度浮点数
                            ilGenerator.Emit(OpCodes.Call, typeof(double).GetMethod("Parse", [typeof(string)])!);
                        }
                        else if (targetType == typeof(bool))
                        {
                            // 字符串转布尔值
                            ilGenerator.Emit(OpCodes.Call, typeof(bool).GetMethod("Parse", [typeof(string)])!);
                        }
                        else
                        {
                            // 对于其他类型，直接返回字符串
                            // 无需转换
                        }
                    }
                    else if (targetType == typeof(string))
                    {
                        // 其他类型到字符串的转换
                        if (leftType != null)
                        {
                            // 调用ToString方法
                            ilGenerator.Emit(OpCodes.Call, leftType.GetMethod("ToString", Type.EmptyTypes)!);
                        }
                    }
                    else if (leftType == typeof(int) && targetType == typeof(double))
                    {
                        // 整数转双精度浮点数
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }
                    else if (leftType == typeof(double) && targetType == typeof(int))
                    {
                        // 双精度浮点数转整数
                        ilGenerator.Emit(OpCodes.Conv_I4);
                    }
                    else if (leftType == typeof(int) && targetType == typeof(bool))
                    {
                        // 整数转布尔值
                        ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        ilGenerator.Emit(OpCodes.Cgt);
                    }
                    else if (leftType == typeof(bool) && targetType == typeof(int))
                    {
                        // 布尔值转整数
                        ilGenerator.Emit(OpCodes.Ldc_I4_0);
                        ilGenerator.Emit(OpCodes.Ceq);
                    }
                    else if (leftType != null && leftType.IsValueType && targetType.IsValueType)
                    {
                        // 其他值类型转换，使用Convert类
                        var convertMethod = typeof(Convert).GetMethod($"To{targetType.Name}", [leftType]);
                        if (convertMethod != null)
                        {
                            ilGenerator.Emit(OpCodes.Call, convertMethod);
                        }
                        else
                        {
                            // 装箱后再转换
                            ilGenerator.Emit(OpCodes.Box, leftType);
                            var objectConvertMethod = typeof(Convert).GetMethod($"To{targetType.Name}", [typeof(object)]);
                            if (objectConvertMethod != null)
                            {
                                ilGenerator.Emit(OpCodes.Call, objectConvertMethod);
                            }
                        }
                    }
                    else if (leftType != null && leftType.IsValueType)
                    {
                        // 值类型到引用类型转换，装箱
                        ilGenerator.Emit(OpCodes.Box, leftType);
                    }
                    else if (targetType.IsValueType)
                    {
                        // 引用类型到值类型转换，拆箱
                        ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
                    }
                    
                    return targetType;
                }
                return typeof(object);
            case OperationType.CONCAT:
                if (local.InClassEnv != null && Left is LangId { IdName: "this" })
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    if (Right is not LangId rightId) return local.InClassEnv;
                    
                    // 检查local.InClassEnv是否是TypeBuilder
                    if (local.InClassEnv is TypeBuilder typeBuilder)
                    {
                        // 如果是TypeBuilder，我们不能在类型创建之前访问它的字段或属性
                        // 直接返回typeof(object)，这是一个安全的默认值
                        // 实际的类型信息会在类型创建后通过其他方式处理
                        return typeof(object);
                    }
                    
                    // 正常处理，local.InClassEnv是一个已经创建好的类型
                    var field = local.InClassEnv.GetField(rightId.IdName);
                    if (field == null)
                    {
                        var p = local.InClassEnv.GetProperty(rightId.IdName);
                        if (p != null && p.GetGetMethod() != null)
                        {
                            ilGenerator.Emit(OpCodes.Call, p.GetGetMethod()!);
                            return p.PropertyType;
                        }
                        // 如果没有找到属性或属性没有getter，返回typeof(object)
                        return typeof(object);
                    }

                    ilGenerator.Emit(OpCodes.Ldfld, field);
                    return field.FieldType;
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
                    ilGenerator.Emit(OpCodes.Call, m);
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

                return typeof(void);
            default:
                throw new InvalidOperationError(this, $"不支持的二元运算符: {Opera}");
        }
    }
}