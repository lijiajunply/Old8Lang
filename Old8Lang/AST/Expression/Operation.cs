using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 操作表达式类，用于表示各种运算符和操作
/// </summary>
/// <param name="left">左操作数</param>
/// <param name="opera">运算符类型</param>
/// <param name="right">右操作数</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类是Old8Lang表达式系统的核心组件，用于表示各种运算符和操作，包括：
/// - 算术运算符：+, -, *, /, %, ^
/// - 比较运算符：>, <, ==, !=, <=, >=
/// - 逻辑运算符：&&, ||, ^
/// - 点运算符：.
/// - 类型转换运算符：as
/// - 成员访问和方法调用
/// - 索引访问
/// - 一元运算符：!, -
/// </remarks>
public class Operation(
    LangExpression? left,
    LangTokenType opera,
    LangExpression? right,
    SourcePosition position = default)
    : LangExpression(position)
{
    /// <summary>
    /// 将运算符类型转换为字符串表示
    /// </summary>
    /// <returns>运算符的字符串表示</returns>
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
        if (Opera == LangTokenType.In)
            return "in";
        return "";
    }

    /// <summary>
    /// 将操作转换为字符串表示
    /// </summary>
    /// <returns>操作的字符串表示</returns>
    public override string ToString() => $"{Left}{OperaToString()}{Right}";
    
    /// <summary>
    /// 操作的输出类型缓存
    /// </summary>
    private Type? Type { get; set; }
    
    /// <summary>
    /// 左操作数
    /// </summary>
    public LangExpression? Left { get; } = left;
    
    /// <summary>
    /// 右操作数
    /// </summary>
    public LangExpression? Right { get; } = right;
    
    /// <summary>
    /// 运算符类型
    /// </summary>
    public LangTokenType Opera { get; } = opera;

    public override LangValueType Run(VariateManager manager)
    {
        // 处理一元运算符
        if (Left == null)
        {
            if (Opera == LangTokenType.Exclamation)
            {
                var rightValue = Right?.Run(manager) as BoolLangValue ??
                                 throw new InvalidOperationError(this, "NOT运算符只支持布尔类型");
                return BoolLangValue.Create(!rightValue.Value, Position);
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
                // 设置外部管理器，确保访问控制能够正确识别这是内部访问
                anyValue.ExternalManager = manager;
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
                return BoolLangValue.Create(false, Position);
            }

            var rightValue = Right?.Run(manager) as BoolLangValue ??
                             throw new InvalidOperationError(this, "AND运算符只支持布尔类型");
            return BoolLangValue.Create(leftValue.Value && rightValue.Value, Position);
        }

        // 处理逻辑OR操作 - 短路求值
        if (Opera == LangTokenType.Or)
        {
            var leftValue = Left.Run(manager) as BoolLangValue ?? throw new InvalidOperationError(this, "OR运算符只支持布尔类型");
            if (leftValue.Value)
            {
                return BoolLangValue.Create(true, Position);
            }

            var rightValue = Right?.Run(manager) as BoolLangValue ??
                             throw new InvalidOperationError(this, "OR运算符只支持布尔类型");
            return BoolLangValue.Create(leftValue.Value || rightValue.Value, Position);
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
            else if (dotLeftResult is TaskClassLangValue taskClassValue)
            {
                if (Right is Instance instance)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    taskClassValue.ExternalManager = manager;
                    return taskClassValue.Dot(instance);
                }

                if (Right != null)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    taskClassValue.ExternalManager = manager;
                    return taskClassValue.Dot(Right);
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
            return BoolLangValue.Create(!boolLeft.Equal(boolRight), Position);
        }

        // == , < , >
        if (leftResult != null! && rightResult != null!)
        {
            switch (Opera)
            {
                case LangTokenType.Equals:
                    return BoolLangValue.Create(leftResult.Equal(rightResult), Position);
                case LangTokenType.LessThan:
                    return BoolLangValue.Create(leftResult.Less(rightResult), Position);
                case LangTokenType.GreaterThan:
                    return BoolLangValue.Create(leftResult.Greater(rightResult), Position);
                case LangTokenType.NotEquals:
                    return BoolLangValue.Create(!leftResult.Equal(rightResult), Position);
                case LangTokenType.LessThanEquals:
                    return BoolLangValue.Create(leftResult.LessEqual(rightResult), Position);
                case LangTokenType.GreaterThanEquals:
                    return BoolLangValue.Create(leftResult.GreaterEqual(rightResult), Position);
                case LangTokenType.In:
                    // 处理 in 操作符：检查左侧值是否存在于右侧集合中
                    if (rightResult is ILangList list)
                    {
                        return BoolLangValue.Create(list.In(leftResult), Position);
                    }

                    throw new InvalidOperationError(this, "in 操作符右侧必须是集合类型");
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


    /// <summary>
    /// 将操作结果保存到局部变量的IL生成方法
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器，用于生成IL代码</param>
    /// <param name="local">局部变量管理器，用于管理局部变量</param>
    /// <param name="idName">要保存的局部变量名称</param>
    /// <remarks>
    /// 该方法执行以下步骤：
    /// 1. 调用LoadIlValue生成计算操作结果的IL指令，将结果压入栈中
    /// 2. 获取操作结果的类型
    /// 3. 检查是否已存在同名局部变量
    ///    - 如果存在且类型匹配：直接将结果存储到该变量
    ///    - 如果存在但类型不匹配：删除旧变量，声明新变量并存储结果
    ///    - 如果不存在：声明新变量并存储结果
    /// </remarks>
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

    /// <summary>
    /// 生成将操作结果加载到栈上的IL指令
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器，用于生成IL代码</param>
    /// <param name="local">局部变量管理器，用于管理局部变量</param>
    /// <remarks>
    /// 该方法通过调用带有ILGenerator参数的OutputType方法来生成实际的IL指令，
    /// 并将结果类型存储在Type属性中以便后续使用。
    /// </remarks>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 调用带有ILGenerator参数的OutputType方法，该方法会生成实际的IL指令
        Type = OutputType(ilGenerator, local);
    }

    /// <summary>
    /// 获取操作的输出类型，用于编译时类型检查和IL生成
    /// </summary>
    /// <param name="local">局部变量管理器，用于管理局部变量</param>
    /// <returns>操作的输出类型，如果无法确定则返回null</returns>
    /// <remarks>
    /// 该方法主要用于编译时类型检查，不生成实际的IL指令。
    /// 它会根据操作符类型、左操作数类型和右操作数类型来确定操作的输出类型。
    /// 如果已经计算过类型，会直接返回缓存的Type属性值。
    /// </remarks>
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

        // 处理方法调用（Dot操作符 + Instance）
        if (Opera == LangTokenType.Dot && Right is Instance instance)
        {
            // 特殊处理ToStr()方法，返回string类型
            if (instance.Id.IdName == "ToStr" && instance.Ids.Count == 0)
            {
                return typeof(string);
            }

            // 对于其他方法调用，尝试查找方法并返回其返回类型
            if (leftType != null)
            {
                var paramTypes = instance.Ids.Select(id => id.OutputType(local) ?? typeof(object)).ToArray();
                var method = leftType.GetMethod(instance.Id.IdName, paramTypes);
                if (method != null)
                {
                    return method.ReturnType;
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
                    // 特殊处理：如果两个操作数都是object类型，使用Convert.ToInt32进行安全转换
                    if (leftType == typeof(object) && rightType == typeof(object))
                    {
                        // 在进入这个分支前，栈上有: [left_value, right_value]
                        // 先保存right到临时变量
                        var rightTemp = ilGenerator.DeclareLocal(typeof(object));
                        ilGenerator.Emit(OpCodes.Stloc, rightTemp);

                        // 现在栈上只有left_value，转换它为int
                        var toInt32Method = typeof(Convert).GetMethod("ToInt32", [typeof(object)])!;
                        ilGenerator.Emit(OpCodes.Call, toInt32Method);

                        // 现在加载right并转换
                        ilGenerator.Emit(OpCodes.Ldloc, rightTemp);
                        ilGenerator.Emit(OpCodes.Call, toInt32Method);

                        // 现在栈上是: [left_int, right_int]
                        ilGenerator.Emit(OpCodes.Mul);
                        return typeof(int);
                    }

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
                        // 对于引用类型（非object），拆箱为int
                        else if (leftType != typeof(object) && leftType is { IsValueType: false })
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
                        // 对于引用类型（非object），拆箱为int
                        else if (rightType != typeof(object) && rightType is { IsValueType: false })
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
            case LangTokenType.In:
            {
                // 处理 in 操作符：left in right
                // 加载左侧值
                Left!.LoadIlValue(ilGenerator, local);
                var leftInType = Left.OutputType(local);
                // 确保左侧值是object类型（装箱值类型）
                if (leftInType is { IsValueType: true })
                {
                    ilGenerator.Emit(OpCodes.Box, leftInType);
                }

                // 加载右侧集合
                Right!.LoadIlValue(ilGenerator, local);

                // 处理不同类型的集合
                if (rightType!.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // 调用 List<T>.Contains(T) 方法
                    // 注意：栈上当前顺序是 [value, list]，但实例方法期望 [list, value]
                    // 需要使用局部变量交换栈顺序

                    var listLocal = ilGenerator.DeclareLocal(rightType);
                    var valueLocal = ilGenerator.DeclareLocal(typeof(object));

                    // 保存list和value到局部变量
                    ilGenerator.Emit(OpCodes.Stloc, listLocal); // 保存list
                    ilGenerator.Emit(OpCodes.Stloc, valueLocal); // 保存value

                    // 按正确顺序重新加载：list, value
                    ilGenerator.Emit(OpCodes.Ldloc, listLocal);
                    ilGenerator.Emit(OpCodes.Ldloc, valueLocal);

                    var containsMethod = rightType.GetMethod("Contains", [rightType.GetGenericArguments()[0]])!;
                    ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
                }
                else if (rightType.IsArray)
                {
                    // 调用 Array.IndexOf(array, value) 方法，然后检查结果是否 >= 0
                    // 注意：栈上当前顺序是 [value, array]，但 Array.IndexOf 期望 [array, value]
                    // 需要使用局部变量交换栈顺序

                    var arrayLocal = ilGenerator.DeclareLocal(rightType);
                    var valueLocal = ilGenerator.DeclareLocal(typeof(object));

                    // 保存array和value到局部变量
                    ilGenerator.Emit(OpCodes.Stloc, arrayLocal); // 保存array
                    ilGenerator.Emit(OpCodes.Stloc, valueLocal); // 保存value

                    // 按正确顺序重新加载：array, value
                    ilGenerator.Emit(OpCodes.Ldloc, arrayLocal);
                    ilGenerator.Emit(OpCodes.Ldloc, valueLocal);

                    var indexOfMethod = typeof(Array).GetMethod("IndexOf", [typeof(Array), typeof(object)])!;
                    ilGenerator.Emit(OpCodes.Call, indexOfMethod);
                    ilGenerator.Emit(OpCodes.Ldc_I4_0);
                    ilGenerator.Emit(OpCodes.Clt);
                    ilGenerator.Emit(OpCodes.Ldc_I4_1);
                    ilGenerator.Emit(OpCodes.Xor);
                }
                else if (rightType.IsGenericType && rightType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    // 调用 Dictionary<TKey, TValue>.ContainsKey(TKey) 方法
                    // 注意：栈上当前顺序是 [value, dict]，但实例方法期望 [dict, value]
                    // 需要使用局部变量交换栈顺序

                    var dictLocal = ilGenerator.DeclareLocal(rightType);
                    var valueLocal = ilGenerator.DeclareLocal(typeof(object));

                    // 保存dict和value到局部变量
                    ilGenerator.Emit(OpCodes.Stloc, dictLocal); // 保存dict
                    ilGenerator.Emit(OpCodes.Stloc, valueLocal); // 保存value

                    // 按正确顺序重新加载：dict, value
                    ilGenerator.Emit(OpCodes.Ldloc, dictLocal);
                    ilGenerator.Emit(OpCodes.Ldloc, valueLocal);

                    var containsMethod = rightType.GetMethod("ContainsKey", [rightType.GetGenericArguments()[0]])!;
                    ilGenerator.Emit(OpCodes.Callvirt, containsMethod);
                }
                else
                {
                    // 默认情况，尝试调用 Contains 方法，参数类型为 object
                    var containsMethod = rightType.GetMethod("Contains", [typeof(object)]);
                    if (containsMethod != null)
                    {
                        // 非静态方法需要交换栈顺序
                        if (!containsMethod.IsStatic)
                        {
                            var objLocal = ilGenerator.DeclareLocal(rightType);
                            var valueLocal = ilGenerator.DeclareLocal(typeof(object));
                            ilGenerator.Emit(OpCodes.Stloc, objLocal);
                            ilGenerator.Emit(OpCodes.Stloc, valueLocal);
                            ilGenerator.Emit(OpCodes.Ldloc, objLocal);
                            ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
                        }

                        ilGenerator.Emit(containsMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, containsMethod);
                    }
                    else
                    {
                        // 尝试调用 Contains 方法，参数类型为左侧值的类型
                        var leftOperandType = Left?.OutputType(local);
                        containsMethod = rightType.GetMethod("Contains", [leftOperandType!]);
                        if (containsMethod != null)
                        {
                            // 非静态方法需要交换栈顺序
                            if (!containsMethod.IsStatic)
                            {
                                var objLocal = ilGenerator.DeclareLocal(rightType);
                                var valueLocal = ilGenerator.DeclareLocal(leftOperandType!);
                                ilGenerator.Emit(OpCodes.Stloc, objLocal);
                                ilGenerator.Emit(OpCodes.Stloc, valueLocal);
                                ilGenerator.Emit(OpCodes.Ldloc, objLocal);
                                ilGenerator.Emit(OpCodes.Ldloc, valueLocal);
                            }

                            ilGenerator.Emit(containsMethod.IsStatic ? OpCodes.Call : OpCodes.Callvirt, containsMethod);
                        }
                        else
                        {
                            // 对于字符串类型，特殊处理
                            if (rightType == typeof(string))
                            {
                                // 调用 string.Contains(string) 方法
                                // 注意：栈上当前顺序是 [value, string]，但实例方法期望 [string, value]
                                var strLocal = ilGenerator.DeclareLocal(typeof(string));
                                var valueLocal = ilGenerator.DeclareLocal(typeof(object));
                                ilGenerator.Emit(OpCodes.Stloc, strLocal);
                                ilGenerator.Emit(OpCodes.Stloc, valueLocal);
                                ilGenerator.Emit(OpCodes.Ldloc, strLocal);
                                ilGenerator.Emit(OpCodes.Ldloc, valueLocal);

                                var stringContainsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
                                ilGenerator.Emit(OpCodes.Callvirt, stringContainsMethod);
                            }
                            else
                            {
                                // 如果没有 Contains 方法，抛出异常
                                throw new InvalidOperationError(this, $"类型 {rightType.Name} 不支持 in 操作符");
                            }
                        }
                    }
                }

                return typeof(bool);
            }
            case LangTokenType.As:
            {
                // 处理类型转换操作：left as right
                // 右侧应该是一个类型标识符，如 int, double, string 等
                if (Right is LangId rightLangId)
                {
                    var typeName = rightLangId.IdName;
                    // 加载左侧值
                    Left!.LoadIlValue(ilGenerator, local);
                    // 根据类型名称生成转换指令
                    var targetType = typeName switch
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

                    // 统一处理类型转换
                    TypeConversion.GenerateTypeConversionIl(ilGenerator, leftType, targetType, this);

                    return targetType;
                }

                // 非LangId类型，返回object类型
                Left!.LoadIlValue(ilGenerator, local);
                return typeof(object);
            }
            case LangTokenType.Dot:
            {
                if (local.InClassEnv != null && Left is LangId { IdName: "this" })
                {
                    ilGenerator.Emit(OpCodes.Ldarg_0);
                    if (Right is not LangId rightId) return local.InClassEnv;

                    // 优先从 FieldVar 中查找字段（支持 TypeBuilder）
                    if (local.FieldVar.TryGetValue(rightId.IdName, out var fieldInfo))
                    {
                        // 找到了字段
                    }
                    // 如果 FieldVar 中没有，尝试从当前类型或父类中获取
                    else if (local.InClassEnv is TypeBuilder typeBuilder)
                    {
                        // 对于 TypeBuilder，尝试从基类中查找字段
                        var baseType = typeBuilder.BaseType;
                        while (baseType != null && baseType != typeof(object))
                        {
                            fieldInfo = baseType.GetField(rightId.IdName, BindingFlags.Public | BindingFlags.Instance);
                            if (fieldInfo != null) break;
                            baseType = baseType.BaseType;
                        }
                    }
                    else
                    {
                        // 对于已创建的类型，直接获取字段
                        fieldInfo = local.InClassEnv.GetField(rightId.IdName,
                            BindingFlags.Public | BindingFlags.Instance);
                    }

                    if (fieldInfo != null)
                    {
                        ilGenerator.Emit(OpCodes.Ldfld, fieldInfo);
                        return fieldInfo.FieldType;
                    }

                    // 尝试查找属性（仅适用于已创建的类型）
                    if (local.InClassEnv is not TypeBuilder)
                    {
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

                    // 特殊处理Old8Lang的ToStr()方法
                    // ToStr()是Old8Lang的扩展方法，在编译模式下将其映射到.NET的ToString()
                    if (instance.Id.IdName == "ToStr" && instance.Ids.Count == 0)
                    {
                        // 调用ToString()方法
                        var toStringMethod = typeof(object).GetMethod("ToString", Type.EmptyTypes)!;

                        // 如果左侧是值类型，需要先装箱
                        if (leftType!.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, leftType);
                        }

                        ilGenerator.Emit(OpCodes.Callvirt, toStringMethod);
                        return typeof(string);
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
                    ilGenerator.Emit(m.IsStatic ? OpCodes.Call : OpCodes.Callvirt, m);

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
                    else if (leftType == typeof(object))
                    {
                        // Object类型，可能是字典、列表或数组
                        // 策略：先存储到局部变量，然后依次尝试类型转换

                        // 栈上已经有: leftValue, rightValue
                        // 先保存rightValue
                        var rightLocal = ilGenerator.DeclareLocal(typeof(object));
                        if (rightType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, rightType);
                        }

                        ilGenerator.Emit(OpCodes.Stloc, rightLocal);

                        // leftValue仍在栈上，保存它
                        var leftLocal = ilGenerator.DeclareLocal(typeof(object));
                        ilGenerator.Emit(OpCodes.Stloc, leftLocal);

                        var endLabel = ilGenerator.DefineLabel();
                        var notDictLabel = ilGenerator.DefineLabel();
                        var notListLabel = ilGenerator.DefineLabel();

                        // 尝试Dictionary<object, object>
                        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
                        ilGenerator.Emit(OpCodes.Isinst, typeof(Dictionary<object, object>));
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Brfalse, notDictLabel);

                        // 是Dictionary
                        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
                        var dictIndexer = typeof(Dictionary<object, object>).GetProperty("Item")!;
                        ilGenerator.Emit(OpCodes.Callvirt, dictIndexer.GetGetMethod()!);
                        ilGenerator.Emit(OpCodes.Br, endLabel);

                        // 不是Dictionary，尝试List<object>
                        ilGenerator.MarkLabel(notDictLabel);
                        ilGenerator.Emit(OpCodes.Pop); // 弹出null
                        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
                        ilGenerator.Emit(OpCodes.Isinst, typeof(List<object>));
                        ilGenerator.Emit(OpCodes.Dup);
                        ilGenerator.Emit(OpCodes.Brfalse, notListLabel);

                        // 是List
                        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
                        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                        var listIndexer = typeof(List<object>).GetProperty("Item")!;
                        ilGenerator.Emit(OpCodes.Callvirt, listIndexer.GetGetMethod()!);
                        ilGenerator.Emit(OpCodes.Br, endLabel);

                        // 不是List，尝试object[]
                        ilGenerator.MarkLabel(notListLabel);
                        ilGenerator.Emit(OpCodes.Pop);
                        ilGenerator.Emit(OpCodes.Ldloc, leftLocal);
                        ilGenerator.Emit(OpCodes.Isinst, typeof(object[]));
                        ilGenerator.Emit(OpCodes.Ldloc, rightLocal);
                        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                        ilGenerator.Emit(OpCodes.Ldelem_Ref);

                        ilGenerator.MarkLabel(endLabel);
                        return typeof(object);
                    }

                    // 默认情况，尝试装箱并调用索引器
                    if (rightType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, rightType);
                    }

                    var defaultIndexer = leftType!.GetProperty("Item");
                    if (defaultIndexer == null) throw new InvalidOperationError(this, $"类型 '{leftType.Name}' 不支持索引访问");
                    ilGenerator.Emit(OpCodes.Callvirt, defaultIndexer.GetGetMethod()!);
                    return typeof(object);
                }

                return typeof(void);
            }
            default:
                throw new InvalidOperationError(this, $"不支持的二元运算符: {Opera}");
        }
    }
}