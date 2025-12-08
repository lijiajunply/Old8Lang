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
        return "";
    }

    public override string ToString() => $"{Left}{OperaToString()}{Right}";
    public Type? Type { get; set; }
    public OldExpr? Left { get; set; } = left;
    public OldExpr? Right { get; set; } = right;
    public OperationType Opera { get; set; } = opera;

    public override LangValueType Run(LangParser.VariateManager manager)
    {
        // not right
        if (Left == null && Opera == OperationType.NOT)
            return new BoolLangValue(!(Right?.Run(manager) as BoolLangValue)!.Value);
        if (Left == null && Opera == OperationType.MINUS)
        {
            var rightValue = Right?.Run(manager);
            if (rightValue is IntLangValue intValue)
                return new IntLangValue(-intValue.Value);
            if (rightValue is DoubleLangValue doubleValue)
                return new DoubleLangValue(-doubleValue.Value);
            throw new InvalidOperationError(this, "一元负号运算符只支持整数和浮点数");
        }

        // this.id => dot_value
        if (Opera == OperationType.CONCAT && Left is LangId { IdName: "this" } && Right != null)
        {
            // 处理this关键字，获取当前类实例
            // 运行Left表达式，获取this关键字的值
            var thisValue = Left.Run(manager);
            if (thisValue is AnyLangValue anyValue)
            {
                // 直接调用当前实例的Dot方法，处理成员访问，传递外部管理器
                return anyValue.Dot(Right);
            }

            // 如果没有找到，抛出错误
            throw new NameError(Left, "this");
        }

        // 处理逻辑AND操作 - 短路求值
        if (Opera == OperationType.AND)
        {
            var leftValue = Left?.Run(manager);
            if (leftValue is BoolLangValue boolLeft)
            {
                if (!boolLeft.Value)
                {
                    // 短路求值：左操作数为false，直接返回false，不执行右操作数
                    return new BoolLangValue(false);
                }
                // 左操作数为true，继续执行右操作数
                var rightValue = Right?.Run(manager);
                if (rightValue is BoolLangValue boolRight)
                {
                    return new BoolLangValue(boolLeft.Value && boolRight.Value);
                }
            }
        }

        // 处理逻辑OR操作 - 短路求值
        if (Opera == OperationType.OR)
        {
            var leftValue = Left?.Run(manager);
            if (leftValue is BoolLangValue boolLeft)
            {
                if (boolLeft.Value)
                {
                    // 短路求值：左操作数为true，直接返回true，不执行右操作数
                    return new BoolLangValue(true);
                }
                // 左操作数为false，继续执行右操作数
                var rightValue = Right?.Run(manager);
                if (rightValue is BoolLangValue boolRight)
                {
                    return new BoolLangValue(boolLeft.Value || boolRight.Value);
                }
            }
        }

        // 处理其他情况
        var l = Left?.Run(manager);
        var r = Right;

        // id.id => dot_value
        if (l is AnyLangValue any && Opera == OperationType.CONCAT)
        {
            if (Right is Instance r1)
            {
                // 对于成员访问，先运行所有参数表达式，获取它们的值，使用外部管理器
                var evaluatedArgs = new List<OldExpr>();
                foreach (var arg in r1.Ids)
                {
                    // 运行参数表达式，使用外部管理器，这样可以访问外部变量
                    var argValue = arg.Run(manager);
                    // 将计算结果包装为LangValueType，以便后续使用
                    evaluatedArgs.Add(argValue);
                }

                // 创建一个新的Instance，使用已经计算好的参数值
                var newInstance = new Instance(r1.Id, evaluatedArgs, r1.Position);

                // 调用Dot方法，传递已经计算好的参数
                return any.Dot(newInstance);
            }

            if (Right != null)
            {
                return any.Dot(Right);
            }
        }

        if (l is ListLangValue && Opera == OperationType.CONCAT)
        {
            // 先尝试将 r 作为 Instance 处理
            Instance r1;

            if (r is Instance instance)
            {
                r1 = instance;
            }
            else if (r != null)
            {
                // 如果 r 不是 Instance，先运行它，获取实际值
                var rValue = r.Run(manager);
                if (rValue is Instance rInstance)
                {
                    r1 = rInstance;
                }
                else
                {
                    // 如果运行结果不是 Instance，直接使用 r 作为操作数
                    return l.Dot(r);
                }
            }
            else
            {
                throw new InvalidOperationError(this, "列表操作需要右侧操作数");
            }

            // 处理实例的参数，运行每个参数
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is NativeStaticAny && Opera == OperationType.CONCAT)
        {
            // 先运行 r，获取实际值
            var rValue = r?.Run(manager);

            if (rValue is not Instance r1) throw new InvalidOperationError(this, "原生静态类型操作需要实例");

            // 处理实例的参数，运行每个参数
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        if (l is not AnyLangValue && Opera == OperationType.CONCAT)
        {
            if (l is null || r is null)
                throw new InvalidOperationError(this, "连接运算符左右操作数均不能为空");

            // 先运行 r，获取实际值
            var rValue = r.Run(manager);

            if (rValue is not Instance r1)
            {
                // 如果不是实例，直接使用 r
                return l.Dot(r);
            }

            // 处理实例的参数，运行每个参数
            List<OldExpr> values = [];
            values.AddRange(r1.Ids.Select(id => id.Run(manager)));

            var newInstance = new Instance(r1.Id, values);
            return l.Dot(newInstance);
        }

        // r get value
        r = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");
        // (right)
        if (Right is LangId oldId && l is not AnyLangValue)
            r = manager.GetValue(oldId);
        if (Right is Operation)
            r = Right.Run(manager);

        // left xor right
        if (l is BoolLangValue && r is BoolLangValue value && Opera == OperationType.XOR)
            return new BoolLangValue(!l.Equal(value));


        // == , < , > 
        if (l is not null && r != null! && Opera == OperationType.EQUALS)
            return new BoolLangValue(l.Equal(r as LangValueType ?? throw new InvalidOperationError(this, "无效的右操作数类型")));
        if (l is not null && r is not null && Opera == OperationType.LESSER)
            return new BoolLangValue(l.Less(r as LangValueType));
        if (l is not null && r is not null && Opera == OperationType.GREATER)
            return new BoolLangValue(l.Greater(r as LangValueType));
        if (l is not null && r is not null && Opera == OperationType.DIFFERENT)
            return new BoolLangValue(!l.Equal(r as LangValueType));
        if (l is not null && r is not null && Opera == OperationType.LESS_EQUAL)
            return new BoolLangValue(l.LessEqual(r as LangValueType));
        if (l is not null && r is not null && Opera == OperationType.GREATER_EQUAL)
            return new BoolLangValue(l.GreaterEqual(r as LangValueType));

        // r (+-*/%) l
        if (l is not null && r is not null)
        {
            if (r is not LangValueType r1) throw new InvalidOperationError(this, "右操作数必须是ValueType类型");
            switch (Opera)
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