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
        GenerateILValue(ilGenerator, local);
        var valueLocal = ilGenerator.DeclareLocal(typeof(int));
        ilGenerator.Emit(OpCodes.Stloc, valueLocal.LocalIndex);
        local.AddLocalVar(idName, valueLocal);
    }

    private void GenerateBinaryOp(ILGenerator ilGenerator, LocalManager local, Type valueType, string methodName)
    {
        left!.GenerateILValue(ilGenerator, local);
        right.GenerateILValue(ilGenerator, local);

        switch (methodName)
        {
            case "Plus":
                ilGenerator.Emit(OpCodes.Add);
                return;
            case "Minus":
                ilGenerator.Emit(OpCodes.Sub);
                return;
            case "Times":
                ilGenerator.Emit(OpCodes.Mul);
                return;
            case "Divide":
                ilGenerator.Emit(OpCodes.Div);
                return;
        }
    }

    private void GenerateComparisonOp(ILGenerator ilGenerator, LocalManager local, string methodName)
    {
        left!.GenerateILValue(ilGenerator, local);
        right.GenerateILValue(ilGenerator, local);

        ilGenerator.Emit(OpCodes.Callvirt, typeof(ValueType).GetMethod(methodName)!);
        ilGenerator.Emit(OpCodes.Newobj, typeof(BoolValue).GetConstructor([typeof(bool)])!);
    }

    private void GenerateConcatOp(ILGenerator ilGenerator, LocalManager local)
    {
        left!.GenerateILValue(ilGenerator, local);
        right.GenerateILValue(ilGenerator, local);

        ilGenerator.Emit(OpCodes.Callvirt, typeof(ValueType).GetMethod("DotToIL", [typeof(ValueType)])!);
    }

    private void GenerateLogicalOp(ILGenerator ilGenerator, LocalManager local, string methodName)
    {
        left!.GenerateILValue(ilGenerator, local);
        right.GenerateILValue(ilGenerator, local);

        ilGenerator.Emit(OpCodes.Callvirt, typeof(BoolValue).GetMethod(methodName)!);
        ilGenerator.Emit(OpCodes.Newobj, typeof(BoolValue).GetConstructor([typeof(bool)])!);
    }

    public override void GenerateILValue(ILGenerator ilGenerator, LocalManager local)
    {
        if (left == null)
        {
            // 处理单目运算符
            switch (opera)
            {
                case OldTokenGeneric.NOT:
                    right.GenerateILValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Callvirt, typeof(BoolValue).GetProperty("Value")!.GetGetMethod()!);
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Ceq);
                    ilGenerator.Emit(OpCodes.Newobj, typeof(BoolValue).GetConstructor([typeof(bool)])!);
                    break;
                case OldTokenGeneric.MINUS:
                    right.GenerateILValue(ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Call, typeof(IntValue).GetProperty("Value")!.GetGetMethod()!);
                    ilGenerator.Emit(OpCodes.Neg);
                    ilGenerator.Emit(OpCodes.Newobj, typeof(IntValue).GetConstructor([typeof(int)])!);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported unary operator: {opera}");
            }
        }
        else
        {
            // 处理双目运算符
            switch (opera)
            {
                case OldTokenGeneric.PLUS:
                    GenerateBinaryOp(ilGenerator, local, typeof(IntValue), "Plus");
                    break;
                case OldTokenGeneric.MINUS:
                    GenerateBinaryOp(ilGenerator, local, typeof(IntValue), "Minus");
                    break;
                case OldTokenGeneric.TIMES:
                    GenerateBinaryOp(ilGenerator, local, typeof(IntValue), "Times");
                    break;
                case OldTokenGeneric.DIVIDE:
                    GenerateBinaryOp(ilGenerator, local, typeof(IntValue), "Divide");
                    break;
                case OldTokenGeneric.GREATER:
                    GenerateComparisonOp(ilGenerator, local, "Greater");
                    break;
                case OldTokenGeneric.LESSER:
                    GenerateComparisonOp(ilGenerator, local, "Less");
                    break;
                case OldTokenGeneric.EQUALS:
                    GenerateComparisonOp(ilGenerator, local, "Equal");
                    break;
                case OldTokenGeneric.DIFFERENT:
                    GenerateComparisonOp(ilGenerator, local, "Different");
                    break;
                case OldTokenGeneric.CONCAT:
                    GenerateConcatOp(ilGenerator, local);
                    break;
                case OldTokenGeneric.AND:
                    GenerateLogicalOp(ilGenerator, local, "And");
                    break;
                case OldTokenGeneric.OR:
                    GenerateLogicalOp(ilGenerator, local, "Or");
                    break;
                case OldTokenGeneric.XOR:
                    GenerateLogicalOp(ilGenerator, local, "Xor");
                    break;
                case OldTokenGeneric.LESS_EQUAL:
                    GenerateComparisonOp(ilGenerator, local, "LessEqual");
                    break;
                case OldTokenGeneric.GREATER_EQUAL:
                    GenerateComparisonOp(ilGenerator, local, "GreaterEqual");
                    break;
                default:
                    throw new NotSupportedException($"Unsupported binary operator: {opera}");
            }
        }
    }
}