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
                var rightValue = Right?.Run(manager) as BoolLangValue ?? throw new InvalidOperationError(this, "NOT运算符只支持布尔类型");
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
            var leftValue = Left.Run(manager) as BoolLangValue ?? throw new InvalidOperationError(this, "AND运算符只支持布尔类型");
            if (!leftValue.Value)
            {
                return new BoolLangValue(false);
            }
            var rightValue = Right?.Run(manager) as BoolLangValue ?? throw new InvalidOperationError(this, "AND运算符只支持布尔类型");
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
            var rightValue = Right?.Run(manager) as BoolLangValue ?? throw new InvalidOperationError(this, "OR运算符只支持布尔类型");
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
                    return any.Dot(newInstance);
                }
                if (Right != null)
                {
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
            else if (dotLeftResult != null! && Right != null)
            {
                throw new InvalidOperationError(this, $"类型 '{dotLeftResult.GetType().Name}' 不支持点操作");
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
        var rightResult = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");

        // left xor right
        if (leftResult is BoolLangValue boolLeft && rightResult is BoolLangValue boolRight && Opera == OperationType.XOR)
            return new BoolLangValue(!boolLeft.Equal(boolRight));

        // == , < , > 
        if (leftResult != null! && rightResult != null)
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
        Type = OutputType(ilGenerator, local);
        var valueLocal = ilGenerator.DeclareLocal(Type);
        var b = local.GetLocalVar(idName);
        if (b != null)
        {
            if (b.LocalType != Type)
            {
                local.RemoveLocalVar(idName);
                local.AddLocalVar(idName, valueLocal);
                ilGenerator.Emit(OpCodes.Stloc, valueLocal.LocalIndex);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Stloc, b.LocalIndex);
            }

            return;
        }

        ilGenerator.Emit(OpCodes.Stloc, valueLocal.LocalIndex);
        local.AddLocalVar(idName, valueLocal);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
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

        // if (leftType == typeof(object))
        // {
        //     return typeof(object);
        // }

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
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Add);
                if (leftType == typeof(string) || rightType == typeof(string))
                    return typeof(string);

                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);

                return typeof(int);
            case OperationType.MINUS:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Sub);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.TIMES:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Mul);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.DIVIDE:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Div);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.MODULO:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Rem);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
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
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.And);
                return typeof(bool);
            case OperationType.OR:
                Left?.LoadIlValue(ilGenerator, local);
                Right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Or);
                return typeof(bool);
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
            case OperationType.CONCAT:
                if (local.InClassEnv != null && Left is LangId { IdName: "this" })
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    if (Right is not LangId rightId) return local.InClassEnv;
                    var field = local.InClassEnv.GetField(rightId.IdName);
                    if (field == null)
                    {
                        var p = local.InClassEnv.GetProperty(rightId.IdName);
                        ilGenerator.Emit(OpCodes.Call, p!.GetGetMethod()!);
                        return p.PropertyType;
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

                    var m = leftType!.GetMethod(instance.Id.IdName, [.. types])!;
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