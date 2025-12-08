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
        if (opera == OperationType.PLUS)
            return "+";
        if (opera == OperationType.MINUS)
            return "-";
        if (opera == OperationType.TIMES)
            return "*";
        if (opera == OperationType.DIVIDE)
            return "/";
        if (opera == OperationType.GREATER)
            return ">";
        if (opera == OperationType.LESSER)
            return "<";
        if (opera == OperationType.EQUALS)
            return "==";
        if (opera == OperationType.DIFFERENT)
            return "!=";
        if (opera == OperationType.CONCAT)
            return ".";
        return "";
    }

    public override string ToString() => $"{left}{OperaToString()}{right}";
    public Type? Type { get; set; }

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        // not right
        if (left == null && opera == OperationType.NOT)
            return new BoolLangValue(!(right?.Run(manager) as BoolLangValue)!.Value);
        if (left == null && opera == OperationType.MINUS)
        {
            var rightValue = right?.Run(manager);
            if (rightValue is IntLangValue intValue)
                return new IntLangValue(-intValue.Value);
            if (rightValue is DoubleLangValue doubleValue)
                return new DoubleLangValue(-doubleValue.Value);
            throw new InvalidOperationError(this, "一元负号运算符只支持整数和浮点数");
        }

        var l = left?.Run(manager);
        var r = right;

        // id.id => dot_value
        if (l is AnyLangValue any && opera == OperationType.CONCAT)
        {
            if (r is Instance r1)
                return any.Dot(r1);
            if (r != null) return any.Dot(r);
        }

        if (l is ListLangValue && opera == OperationType.CONCAT)
        {
            if (r is not Instance r1) throw new InvalidOperationError(this, "列表操作需要实例");
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is NativeStaticAny && opera == OperationType.CONCAT)
        {
            if (r is not Instance r1) throw new InvalidOperationError(this, "原生静态类型操作需要实例");
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is not AnyLangValue && opera == OperationType.CONCAT)
        {
            if (l is null || r is null)
                throw new InvalidOperationError(this, "连接运算符左右操作数均不能为空");
            return l.Dot(r);
        }

        // r get value
        r = right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");
        // (right)
        if (right is LangId oldId && l is not AnyLangValue)
            r = manager.GetValue(oldId);
        if (right is Operation)
            r = right.Run(manager);


        // left and right
        if (l is BoolLangValue b && r is BoolLangValue expr && opera == OperationType.AND)
            return new BoolLangValue(b.Value && expr.Value);

        // left or right
        if (l is BoolLangValue b1 && r is BoolLangValue oldBool && opera == OperationType.OR)
            return new BoolLangValue(b1.Value || oldBool.Value);

        // left xor right
        if (l is BoolLangValue && r is BoolLangValue value && opera == OperationType.XOR)
            return new BoolLangValue(!l.Equal(value));


        // == , < , > 
        if (l is not null && r != null! && opera == OperationType.EQUALS)
            return new BoolLangValue(l.Equal(r as LangValueType ?? throw new InvalidOperationError(this, "无效的右操作数类型")));
        if (l is not null && r is not null && opera == OperationType.LESSER)
            return new BoolLangValue(l.Less(r as LangValueType));
        if (l is not null && r is not null && opera == OperationType.GREATER)
            return new BoolLangValue(l.Greater(r as LangValueType));
        if (l is not null && r is not null && opera == OperationType.DIFFERENT)
            return new BoolLangValue(!l.Equal(r as LangValueType));
        if (l is not null && r is not null && opera == OperationType.LESS_EQUAL)
            return new BoolLangValue(l.LessEqual(r as LangValueType));
        if (l is not null && r is not null && opera == OperationType.GREATER_EQUAL)
            return new BoolLangValue(l.GreaterEqual(r as LangValueType));

        // r (+-*/%) l
        if (l is not null && r is not null)
        {
            if (r is not LangValueType r1) throw new InvalidOperationError(this, "右操作数必须是ValueType类型");
            switch (opera)
            {
                case OperationType.PLUS:
                    return l.Plus(r1);
                case OperationType.MINUS:
                    return l.Minus(r1);
                case OperationType.TIMES:
                    return l.Times(r1);
                case OperationType.DIVIDE:
                    return l.Divide(r1);
                case OperationType.MODULO:
                    return l.Mod(r1);
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
        var leftType = left?.OutputType(local);
        var rightType = right?.OutputType(local);
        return leftType == typeof(object) ? rightType : leftType;
    }

    private Type OutputType(ILGenerator ilGenerator, LocalManager local)
    {
        var leftType = left?.OutputType(local);
        var rightType = right?.OutputType(local);

        // if (leftType == typeof(object))
        // {
        //     return typeof(object);
        // }

        if (left == null)
        {
            // 处理单目运算符
            switch (opera)
            {
                case OperationType.NOT:
                    right?.LoadIlValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
                    ilGenerator.Emit(OpCodes.Xor); // 进行异或运算
                    return typeof(bool);
                case OperationType.MINUS:
                    right?.LoadIlValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Neg);
                    return rightType;
                default:
                    throw new InvalidOperationError(this, $"不支持的一元运算符: {opera}");
            }
        }

        switch (opera)
        {
            case OperationType.PLUS:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Add);
                if (leftType == typeof(string) || rightType == typeof(string))
                    return typeof(string);

                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);

                return typeof(int);
            case OperationType.MINUS:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Sub);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.TIMES:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Mul);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.DIVIDE:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Div);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.MODULO:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Rem);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OperationType.GREATER:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                return typeof(bool);
            case OperationType.LESSER:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                return typeof(bool);
            case OperationType.EQUALS:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                return typeof(bool);
            case OperationType.DIFFERENT:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.AND:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.And);
                return typeof(bool);
            case OperationType.OR:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Or);
                return typeof(bool);
            case OperationType.XOR:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.LESS_EQUAL:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.GREATER_EQUAL:
                left?.LoadIlValue(ilGenerator, local);
                right?.LoadIlValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OperationType.CONCAT:
                if (local.InClassEnv != null && left is LangId { IdName: "this" })
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    if (right is not LangId rightId) return local.InClassEnv;
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

                if (right is Instance instance)
                {
                    left!.LoadIlValue(ilGenerator, local);
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

                if (right is LangId id)
                {
                    left!.LoadIlValue(ilGenerator, local);
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
                throw new InvalidOperationError(this, $"不支持的二元运算符: {opera}");
        }
    }
}