using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression;

public class Operation(OldExpr? left, OldTokenGeneric opera, OldExpr right) : OldExpr
{
    private string OperaToString()
    {
        if (opera == OldTokenGeneric.PLUS)
            return "+";
        if (opera == OldTokenGeneric.MINUS)
            return "-";
        if (opera == OldTokenGeneric.TIMES)
            return "*";
        if (opera == OldTokenGeneric.DIVIDE)
            return "/";
        if (opera == OldTokenGeneric.GREATER)
            return ">";
        if (opera == OldTokenGeneric.LESSER)
            return "<";
        if (opera == OldTokenGeneric.EQUALS)
            return "==";
        if (opera == OldTokenGeneric.DIFFERENT)
            return "!=";
        if (opera == OldTokenGeneric.CONCAT)
            return ".";
        return "";
    }

    public override string ToString() => $"{left} {OperaToString()} {right}";
    public Type? Type { get; set; }

    public override ValueType Run(VariateManager Manager)
    {
        // not right
        if (left == null && opera == OldTokenGeneric.NOT)
            return new BoolValue(!(right.Run(Manager) as BoolValue)!.Value);
        if (left == null && opera == OldTokenGeneric.MINUS)
            return new IntValue(-(right.Run(Manager) as IntValue)!.Value);

        var l = left?.Run(Manager);
        var r = right;

        // id.id => dot_value
        if (l is AnyValue any && opera == OldTokenGeneric.CONCAT)
        {
            if (r is Instance r1)
                return any.Dot(r1);
            return any.Dot(r);
        }

        if (l is ListValue && opera == OldTokenGeneric.CONCAT)
        {
            if (r is not Instance r1) return l.Dot(r);
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(Manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is NativeStaticAny && opera == OldTokenGeneric.CONCAT)
        {
            if (r is not Instance r1) return l.Dot(r);
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(Manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is not AnyValue && opera == OldTokenGeneric.CONCAT)
            return l?.Dot(r)!;

        // r get value
        r = right.Run(Manager);
        // (right)
        if (right is OldID oldId && l is not AnyValue)
            r = Manager.GetValue(oldId);
        if (right is Operation)
            r = right.Run(Manager);


        // left and right
        if (l is BoolValue b && r is BoolValue expr && opera == OldTokenGeneric.AND)
            return new BoolValue(b.Value && expr.Value);

        // left or right
        if (l is BoolValue b1 && r is BoolValue oldBool && opera == OldTokenGeneric.OR)
            return new BoolValue(b1.Value || oldBool.Value);

        // left xor right
        if (l is BoolValue && r is BoolValue value && opera == OldTokenGeneric.XOR)
            return new BoolValue(!l.Equal(value));


        // == , < , > 
        if (l is not null && r != null! && opera == OldTokenGeneric.EQUALS)
            return new BoolValue(l.Equal(r as ValueType ?? new VoidValue()));
        if (l is not null && r is not null && opera == OldTokenGeneric.LESSER)
            return new BoolValue(l.Less(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.GREATER)
            return new BoolValue(l.Greater(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.DIFFERENT)
            return new BoolValue(!l.Equal(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.LESS_EQUAL)
            return new BoolValue(l.LessEqual(r as ValueType));
        if (l is not null && r is not null && opera == OldTokenGeneric.GREATER_EQUAL)
            return new BoolValue(l.GreaterEqual(r as ValueType));

        // r (+-*/) l
        if (l is not null && r is not null)
        {
            if (r is not ValueType r1) return new VoidValue();
            switch (opera)
            {
                case OldTokenGeneric.PLUS:
                    return l.Plus(r1);
                case OldTokenGeneric.MINUS:
                    return l.Minus(r1);
                case OldTokenGeneric.TIMES:
                    return l.Times(r1);
                case OldTokenGeneric.DIVIDE:
                    return l.Divide(r1);
            }
        }

        return new VoidValue();
    }


    public override void SetValueToIL(ILGenerator ilGenerator, LocalManager local, string idName)
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

    public override void LoadILValue(ILGenerator ilGenerator, LocalManager local)
    {
        Type = OutputType(ilGenerator, local);
    }

    public override Type? OutputType(LocalManager local)
    {
        if (Type != null) return Type;
        var leftType = left?.OutputType(local);
        var rightType = right.OutputType(local);
        return leftType == typeof(object) ? rightType : leftType;
    }

    private Type OutputType(ILGenerator ilGenerator, LocalManager local)
    {
        var leftType = left?.OutputType(local);
        var rightType = right.OutputType(local);

        if (leftType == typeof(object))
        {
            return typeof(object);
        }

        if (left == null)
        {
            // 处理单目运算符
            switch (opera)
            {
                case OldTokenGeneric.NOT:
                    right.LoadILValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
                    ilGenerator.Emit(OpCodes.Xor); // 进行异或运算
                    return typeof(bool);
                case OldTokenGeneric.MINUS:
                    right.LoadILValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Neg);
                    return typeof(bool);
                default:
                    throw new NotSupportedException($"Unsupported unary operator: {opera}");
            }
        }

        switch (opera)
        {
            case OldTokenGeneric.PLUS:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Add);
                if (leftType == typeof(string) || rightType == typeof(string))
                    return typeof(string);

                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);

                return typeof(int);
            case OldTokenGeneric.MINUS:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Sub);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OldTokenGeneric.TIMES:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Mul);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OldTokenGeneric.DIVIDE:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Div);
                if (leftType == typeof(double) || rightType == typeof(double))
                    return typeof(double);
                return typeof(int);
            case OldTokenGeneric.GREATER:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                return typeof(bool);
            case OldTokenGeneric.LESSER:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                return typeof(bool);
            case OldTokenGeneric.EQUALS:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                return typeof(bool);
            case OldTokenGeneric.DIFFERENT:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Ceq);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OldTokenGeneric.AND:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.And);
                return typeof(bool);
            case OldTokenGeneric.OR:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Or);
                return typeof(bool);
            case OldTokenGeneric.XOR:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OldTokenGeneric.LESS_EQUAL:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Cgt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OldTokenGeneric.GREATER_EQUAL:
                left!.LoadILValue(ilGenerator, local);
                right.LoadILValue(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Clt);
                ilGenerator.Emit(OpCodes.Ldc_I4_1);
                ilGenerator.Emit(OpCodes.Xor);
                return typeof(bool);
            case OldTokenGeneric.CONCAT:
                if (local.InClassEnv != null && left is OldID { IdName: "this" })
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    if (right is not OldID rightId) return local.InClassEnv;
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
                    left!.LoadILValue(ilGenerator, local);
                    var types = new List<Type>();
                    foreach (var instanceId in instance.Ids)
                    {
                        instanceId.LoadILValue(ilGenerator, local);
                        types.Add(instanceId.OutputType(local)!);
                    }

                    var m = leftType!.GetMethod(instance.Id.IdName, types.ToArray())!;
                    ilGenerator.Emit(OpCodes.Call, m);
                    return m.ReturnType;
                }

                if (right is OldID id)
                {
                    left!.LoadILValue(ilGenerator, local);
                    var field = leftType!.GetField(id.IdName);
                    if (field == null)
                    {
                        var p = leftType.GetProperty(id.IdName);
                        ilGenerator.Emit(OpCodes.Call, p!.GetGetMethod()!);
                        return p.PropertyType;
                    }

                    ilGenerator.Emit(OpCodes.Ldfld, field);
                    return field.FieldType;
                }

                return typeof(void);
            default:
                throw new NotSupportedException($"Unsupported binary operator: {opera}");
        }
    }
}