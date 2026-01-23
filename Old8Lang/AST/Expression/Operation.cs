using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
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
/// - 比较运算符：>, &lt;, ==, !=, &gt;=, >=
/// - 逻辑运算符：&&, ||, ^
/// - 点运算符：.
/// - 类型转换运算符：as
/// - 成员访问和方法调用
/// - 索引访问
/// - 一元运算符：!, -
/// </remarks>
public partial class Operation(
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
        if (Opera == LangTokenType.Is)
            return "is";
        if (Opera == LangTokenType.IsNot)
            return "is not";
        if (Opera == LangTokenType.In)
            return "in";
        if (Opera == LangTokenType.NullishCoalescing)
            return "??";
        if (Opera == LangTokenType.Not)
            return "not";
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
    public Type? Type { get; private set; }

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
        if (Left is null)
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
        if (Opera == LangTokenType.Dot && Left is LangId { IdName: "this" } && Right is not null)
        {
            var thisValue = Left.Run(manager);
            if (thisValue is AnyLangValue anyValue)
            {
                // V2 架构：直接调用 Dot，访问控制由内部实现
                return anyValue.Dot(Right, manager);
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
                    var newInstance = new Instance(r1.Id, ids, r1.NamedArgs, r1.Position);
                    return any.Dot(newInstance, manager);
                }

                if (Right is not null)
                {
                    return any.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is ListLangValue list)
            {
                if (Right is Instance instance)
                {
                    var ids = instance.Ids.Select(x => x.Run(manager)).OfType<LangExpression>().ToList();
                    var newInstance = new Instance(instance.Id, ids, instance.NamedArgs);
                    return list.Dot(newInstance, manager);
                }

                if (Right is not null)
                {
                    // 先尝试将 Right 作为属性或方法调用传递给 Dot 方法
                    // 这样可以处理像 .Count 这样的属性访问
                    try
                    {
                        return list.Dot(Right, manager);
                    }
                    catch (InvalidOperationError)
                    {
                        // 如果 Dot 方法无法处理，则尝试作为索引访问
                        var listIndexResult = Right.Run(manager);
                        if (listIndexResult is IntLangValue intValue)
                        {
                            return list.Get(intValue);
                        }
                        throw;
                    }
                }
            }
            else if (dotLeftResult is NativeStaticAny native)
            {
                if (Right is not Instance r1) throw new InvalidOperationError(this, "原生静态类型操作需要实例");
                var newInstance = new Instance(r1.Id, r1.Ids, r1.NamedArgs);
                return native.Dot(newInstance, manager);
            }
            // 处理静态成员访问：ClassName.staticMember
            else if (dotLeftResult is TypeTemplate typeTemplate)
            {
                if (Right is Instance r1)
                {
                    // 处理静态方法调用
                    var newInstance = new Instance(r1.Id, r1.Ids, r1.NamedArgs, r1.Position);
                    return typeTemplate.Dot(newInstance, manager);
                }

                if (Right is not null)
                {
                    // 处理静态成员访问
                    return typeTemplate.Dot(Right, manager);
                }
            }
            // 处理枚举成员访问：EnumName.MemberName
            else if (dotLeftResult is EnumTemplate enumTemplate)
            {
                if (Right is LangId memberId)
                {
                    // 访问枚举成员
                    return enumTemplate.GetMemberValue(memberId.IdName);
                }

                throw new InvalidOperationError(this, $"枚举 '{enumTemplate.EnumName}' 只支持成员访问");
            }
            else if (dotLeftResult is ArrayLangValue array)
            {
                // 处理数组方法调用
                if (Right is Instance instance)
                {
                    var ids = instance.Ids.Select(x => x.Run(manager)).OfType<LangExpression>().ToList();
                    var newInstance = new Instance(instance.Id, ids, instance.NamedArgs);
                    return array.Dot(newInstance, manager);
                }

                // 处理数组索引访问，需要先运行Right表达式
                if (Right is not null)
                {
                    var arrayIndexResult = Right.Run(manager);
                    if (arrayIndexResult is IntLangValue intValue)
                    {
                        return array.Get(intValue);
                    }

                    // 如果不是整数索引，则作为方法调用处理
                    return array.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is DictionaryLangValue dict)
            {
                // 处理字典方法调用
                if (Right is Instance instance)
                {
                    var ids = instance.Ids.Select(x => x.Run(manager)).OfType<LangExpression>().ToList();
                    var newInstance = new Instance(instance.Id, ids, instance.NamedArgs);
                    return dict.Dot(newInstance, manager);
                }

                // 处理字典属性访问和方法调用
                if (Right is not null)
                {
                    return dict.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is TaskClassLangValue taskClassValue)
            {
                if (Right is Instance instance)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    taskClassValue.ExternalManager = manager;
                    return taskClassValue.Dot(instance, manager);
                }

                if (Right is not null)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    taskClassValue.ExternalManager = manager;
                    return taskClassValue.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is TaskLangValue taskValue)
            {
                // 设置外部管理器，确保 Then 等方法能访问有效的 Interpreter
                taskValue.ExternalManager = manager;

                if (Right is Instance instance)
                {
                    // Retry 方法需要特殊处理，因为需要重新执行原始函数调用
                    if (instance.Id.IdName == "Retry")
                    {
                        if (instance.Ids.Count is < 1 or > 2)
                        {
                            throw new ArgumentError(instance.Position,
                                $"Retry 方法需要 1-2 个参数，实际提供了 {instance.Ids.Count} 个");
                        }

                        var retryCountValue = instance.Ids[0].Run(manager);
                        if (retryCountValue is not IntLangValue retryCount)
                        {
                            throw new TypeError(instance, "Retry 的第一个参数必须是整数");
                        }

                        var delayMs = 0;
                        if (instance.Ids.Count == 2)
                        {
                            var delayValue = instance.Ids[1].Run(manager);
                            if (delayValue is IntLangValue delayInt)
                            {
                                delayMs = delayInt.Value;
                            }
                            else
                            {
                                throw new TypeError(instance, "Retry 的第二个参数必须是整数");
                            }
                        }

                        // Retry 需要重新执行原始函数，而不是重试同一个 Task 对象
                        if (Left is not Instance funcCall)
                        {
                            throw new InvalidOperationError(instance, "Retry 只能用于异步函数调用（如 func().Retry(...)）");
                        }

                        // 使用 TaskRetryHelper 创建重试任务，实现代码抽离
                        return TaskRetryHelper.CreateRetryTask(funcCall, manager, retryCount.Value, delayMs,
                            instance.Position);
                    }

                    // 其他方法调用（如 Then），使用扩展方法或 Dot 处理
                    return taskValue.Dot(instance, manager);
                }

                if (Right is not null)
                {
                    return taskValue.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is ThreadLangValue threadValue)
            {
                // 设置外部管理器，确保 Then 等方法能访问有效的 Interpreter
                threadValue.ExternalManager = manager;

                if (Right is Instance instance)
                {
                    // Retry 方法需要特殊处理，因为需要重新执行原始函数调用
                    if (instance.Id.IdName == "Retry")
                    {
                        if (instance.Ids.Count is < 1 or > 2)
                        {
                            throw new ArgumentError(instance.Position,
                                $"Retry 方法需要 1-2 个参数，实际提供了 {instance.Ids.Count} 个");
                        }

                        var retryCountValue = instance.Ids[0].Run(manager);
                        if (retryCountValue is not IntLangValue retryCount)
                        {
                            throw new TypeError(instance, "Retry 的第一个参数必须是整数");
                        }

                        var delayMs = 0;
                        if (instance.Ids.Count == 2)
                        {
                            var delayValue = instance.Ids[1].Run(manager);
                            if (delayValue is IntLangValue delayInt)
                            {
                                delayMs = delayInt.Value;
                            }
                            else
                            {
                                throw new TypeError(instance, "Retry 的第二个参数必须是整数");
                            }
                        }

                        // Retry 需要重新执行原始函数，而不是重试同一个 Thread 对象
                        if (Left is not Instance funcCall)
                        {
                            throw new InvalidOperationError(instance, "Retry 只能用于函数调用（如 func().Retry(...)）");
                        }

                        // 使用 ThreadRetryHelper 创建重试线程，实现代码抽离
                        return ThreadRetryHelper.CreateRetryThread(funcCall, manager, retryCount.Value, delayMs,
                            instance.Position);
                    }

                    // 其他方法调用（如 Then），使用扩展方法或 Dot 处理
                    return threadValue.Dot(instance, manager);
                }

                if (Right is not null)
                {
                    return threadValue.Dot(Right, manager);
                }
            }
            else if (dotLeftResult is not null && Right is not null)
            {
                return dotLeftResult.Dot(Right, manager);
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
            // 处理安全类型转换操作：left as right
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
                        try
                        {
                            return leftResult.Converse(typeLangValue, manager);
                        }
                        catch (TypeError)
                        {
                            // 安全转换失败，返回 null
                            return NullLangValue.Instance;
                        }
                    }

                    typeName = rightAsResult.ToString();
                    break;
                }
            }

            // 创建或获取类型对象
            var type = new TypeLangValue(typeName);
            try
            {
                return leftResult.Converse(type, manager);
            }
            catch (TypeError)
            {
                // 安全转换失败，返回 null
                return NullLangValue.Instance;
            }
        }

        if (Opera == LangTokenType.Is)
        {
            // 处理类型检查操作：left is right
            // 右侧应该是一个类型标识符或类名
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
                    var rightIsResult = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");

                    if (rightIsResult is TypeLangValue typeLangValue)
                    {
                        typeName = typeLangValue.Value ?? typeLangValue.ToString();
                    }
                    else
                    {
                        typeName = rightIsResult.ToString();
                    }

                    break;
                }
            }

            // 检查左侧值是否是指定类型的实例
            return new BoolLangValue(CheckIsInstance(leftResult, typeName, manager));
        }

        if (Opera == LangTokenType.IsNot)
        {
            // 处理否定类型检查操作：left is not right
            // 右侧应该是一个类型标识符或类名
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
                    var rightIsNotResult = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");

                    if (rightIsNotResult is TypeLangValue typeLangValue)
                    {
                        typeName = typeLangValue.Value ?? typeLangValue.ToString();
                    }
                    else
                    {
                        typeName = rightIsNotResult.ToString();
                    }

                    break;
                }
            }

            // 检查左侧值是否不是指定类型的实例（取反）
            return new BoolLangValue(!CheckIsInstance(leftResult, typeName, manager));
        }

        var rightResult = Right?.Run(manager) ?? throw new InvalidOperationError(this, "右操作数不能为空");

        // 处理空值合并运算符 ??
        if (Opera == LangTokenType.NullishCoalescing)
        {
            // 如果左侧结果为null或NullLangValue，则返回右侧结果，否则返回左侧结果
            if (leftResult is NullLangValue or null)
            {
                return rightResult;
            }

            return leftResult;
        }

        // left xor right
        if (leftResult is BoolLangValue boolLeft && rightResult is BoolLangValue boolRight &&
            Opera == LangTokenType.Xor)
        {
            return BoolLangValue.Create(!boolLeft.Equal(boolRight), Position);
        }

        // == , < , >
        if (leftResult is not null && rightResult is not null)
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
        if (type is null) return;
        // 声明局部变量或使用已存在的
        var existingLocal = local.GetLocalVar(idName);
        if (existingLocal is not null)
        {
            if (existingLocal.LocalType != type)
            {
                // 类型不匹配，重新声明
                local.RemoveLocalVar(idName);
                var newLocal = ilGenerator.DeclareLocal(type);
                local.AddLocalVar(idName, newLocal);
                ilGenerator.Emit(OpCodes.Stloc, newLocal);
            }
            else
            {
                // 类型匹配，直接存储
                ilGenerator.Emit(OpCodes.Stloc, existingLocal);
            }
        }
        else
        {
            // 首次声明变量
            var newLocal = ilGenerator.DeclareLocal(type);
            local.AddLocalVar(idName, newLocal);
            ilGenerator.Emit(OpCodes.Stloc, newLocal);
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
        if (Type is not null) return Type;
        // 直接返回类型信息，不创建临时方法
        var leftType = Left?.OutputType(local);
        var rightType = Right?.OutputType(local);

        // 如果leftType是TypeBuilder，需要查找对应的已完成的类型
        if (leftType is TypeBuilder typeBuilder)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = typeBuilder.Name;
            leftType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        // 处理成员访问（Dot操作符）
        if (Opera == LangTokenType.Dot && Right is LangId rightId)
        {
            if (leftType is not null)
            {
                // 检查是否是枚举类型的成员访问
                if (leftType.IsEnum)
                {
                    // 枚举成员访问返回枚举类型本身
                    var enumField = leftType.GetField(rightId.IdName);
                    if (enumField is not null)
                    {
                        return leftType; // 返回枚举类型
                    }
                }

                // 尝试获取字段类型
                var field = leftType.GetField(rightId.IdName);
                if (field is not null)
                {
                    return field.FieldType;
                }

                // 尝试获取属性类型
                var property = leftType.GetProperty(rightId.IdName);
                if (property is not null)
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

            // 特殊处理Assert静态方法调用，返回void类型
            if (Left is LangId { IdName: "Assert" })
            {
                return typeof(void);
            }

            // 对于其他方法调用，尝试查找方法并返回其返回类型
            if (leftType is not null)
            {
                var paramTypes = instance.Ids.Select(id => id.OutputType(local) ?? typeof(object)).ToArray();
                var method = leftType.GetMethod(instance.Id.IdName, paramTypes);
                if (method is not null)
                {
                    return method.ReturnType;
                }
            }

            return typeof(object);
        }

        // 处理空值合并运算符 ??
        if (Opera == LangTokenType.NullishCoalescing)
        {
            // 空值合并运算符返回左操作数的类型，如果左操作数是null则返回右操作数的类型
            return leftType ?? rightType;
        }

        // 处理类型转换运算符 as
        if (Opera == LangTokenType.As && Right is LangId rightAsId)
        {
            // 返回目标类型
            var typeName = rightAsId.IdName;
            return typeName.ToLower() switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "list" => typeof(List<object>),
                "array" => leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>)
                    ? leftType.GetGenericArguments()[0].MakeArrayType()
                    : typeof(object[]),
                "dictionary" => typeof(Dictionary<object, object>),
                _ => typeof(object)
            };
        }

        // 处理类型检查运算符 is 和 is not
        if (Opera == LangTokenType.Is || Opera == LangTokenType.IsNot)
        {
            return typeof(bool);
        }

        // 处理比较运算符和逻辑运算符
        if (Opera == LangTokenType.GreaterThan || Opera == LangTokenType.LessThan || 
            Opera == LangTokenType.Equals || Opera == LangTokenType.NotEquals || 
            Opera == LangTokenType.GreaterThanEquals || Opera == LangTokenType.LessThanEquals ||
            Opera == LangTokenType.And || Opera == LangTokenType.Or || Opera == LangTokenType.Xor)
        {
            return typeof(bool);
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
            // 如果有浮点数，返回double
            if (leftType == typeof(double) || rightType == typeof(double))
            {
                return typeof(double);
            }
            // 否则返回int
            return typeof(int);
        }

        return leftType == typeof(object) ? rightType : leftType;
    }

    private Type OutputType(ILGenerator ilGenerator, LocalManager local)
    {
        var leftType = Left?.OutputType(local);
        var rightType = Right?.OutputType(local);

        // 如果leftType是TypeBuilder，需要查找对应的已完成的类型
        if (leftType is TypeBuilder typeBuilder)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = typeBuilder.Name;
            leftType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        // 如果rightType是TypeBuilder，需要查找对应的已完成的类型
        if (rightType is TypeBuilder rightTypeBuilder)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = rightTypeBuilder.Name;
            rightType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        if (Left is null)
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
                return NumericBinaryOpHelper.GenerateAddition(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Minus:
                return NumericBinaryOpHelper.GenerateSubtraction(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Star:
                return NumericBinaryOpHelper.GenerateMultiplication(Left, Right, ilGenerator, local, leftType, rightType, this);
            case LangTokenType.Slash:
                return NumericBinaryOpHelper.GenerateDivision(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Percent:
                return NumericBinaryOpHelper.GenerateModulo(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Caret:
                return NumericBinaryOpHelper.GeneratePower(Left, Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.GreaterThan:
                return ComparisonOpHelper.GenerateGreaterThan(Left, Right, ilGenerator, local);
            case LangTokenType.LessThan:
                return ComparisonOpHelper.GenerateLessThan(Left, Right, ilGenerator, local);
            case LangTokenType.Equals:
                return ComparisonOpHelper.GenerateEquals(Left, Right, ilGenerator, local);
            case LangTokenType.NotEquals:
                return ComparisonOpHelper.GenerateNotEquals(Left, Right, ilGenerator, local);
            case LangTokenType.And:
                return LogicalOpILHelper.GenerateAnd(Left!, Right!, ilGenerator, local);
            case LangTokenType.Or:
                return LogicalOpILHelper.GenerateOr(Left!, Right!, ilGenerator, local);
            case LangTokenType.Xor:
                return LogicalOpILHelper.GenerateXor(Left!, Right!, ilGenerator, local);
            case LangTokenType.LessThanEquals:
                return ComparisonOpHelper.GenerateLessThanEquals(Left, Right, ilGenerator, local);
            case LangTokenType.GreaterThanEquals:
                return ComparisonOpHelper.GenerateGreaterThanEquals(Left, Right, ilGenerator, local);
            case LangTokenType.In:
                return InOperatorHelper.GenerateInOperator(Left!, Right!, ilGenerator, local, rightType, this);
            case LangTokenType.As:
                return TypeCheckILHelper.GenerateAsOperator(Left!, Right!, ilGenerator, local, leftType, this);
            case LangTokenType.Is:
                return TypeCheckILHelper.GenerateIsOperator(Left!, Right!, ilGenerator, local, leftType);
            case LangTokenType.IsNot:
                return TypeCheckILHelper.GenerateIsNotOperator(Left!, Right!, ilGenerator, local, leftType);
            case LangTokenType.NullishCoalescing:
                return NullishCoalescingILHelper.GenerateNullishCoalescing(Left!, Right!, ilGenerator, local, leftType!);
            case LangTokenType.Dot:
                return DotOperatorILHelper.GenerateDotOperator(Left, Right, ilGenerator, local, leftType, rightType, this);
            default:
                throw new InvalidOperationError(this, $"不支持的二元运算符: {Opera}");
        }
    }

    /// <summary>
    /// 检查值是否是指定类型的实例
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <param name="typeName">类型名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>如果值是指定类型的实例则返回true,否则返回false</returns>
    private bool CheckIsInstance(LangValueType value, string typeName, VariateManager manager)
    {
        // 处理基本类型
        switch (typeName.ToLower())
        {
            case "int":
                return value is IntLangValue;
            case "double":
                return value is DoubleLangValue;
            case "string":
                return value is StringLangValue;
            case "bool":
                return value is BoolLangValue;
            case "char":
                return value is CharLangValue;
            case "list":
                return value is ListLangValue;
            case "array":
                return value is ArrayLangValue;
            case "dictionary":
                return value is DictionaryLangValue;
            case "null":
                return value is NullLangValue;
        }

        // 处理类和接口类型
        if (value is AnyLangValue anyValue)
        {
            // 检查是否是该类的实例(类名匹配)
            if (anyValue.ClassId.IdName == typeName)
            {
                return true;
            }

            // 检查是否实现了指定的接口或继承了指定的类
            // 通过元数据的 IsAssignableTo 方法进行检查
            try
            {
                var targetType = manager.GetAny(new LangId(typeName));
                if (targetType is TypeTemplate targetTypeTemplate)
                {
                    var targetMetadata = targetTypeTemplate.BuildMetadata(manager);
                    return anyValue.Metadata.IsAssignableTo(targetMetadata, manager);
                }
            }
            catch
            {
                // 类型不存在,返回false
                return false;
            }
        }

        return false;
    }
}