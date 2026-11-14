using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;

using Old8Lang.Concurrency;
using Old8Lang.ExternProviders;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.ProjectManagement;
using Old8Lang.Runtime;
using Old8Lang.TypeSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// InterpreterVisitor - Expression 节点的 Visit 方法实现
/// </summary>
public partial class InterpreterVisitor
{
    /// <summary>
    /// 访问 LangId 节点
    /// </summary>
    public LangValueType VisitLangId(LangId node)
    {
        // 先尝试获取普通变量
        if (manager is not null)
        {
            var value = manager.GetValue(node);
            if (value is not null)
            {
                return value;
            }

            // 如果不是普通变量，尝试获取类或函数
            var anyValue = manager.GetAny(node);
            if (anyValue is not null)
            {
                return anyValue as LangValueType ?? throw new NameError(node.Position, node.IdName);
            }
        }

        // 如果都没有找到，检查是否是类型关键字
        var supportedTypes = new[]
            { "int", "double", "string", "bool", "char", "void", "list", "dict", "array", "dictionary", "tuple" };
        if (supportedTypes.Contains(node.IdName))
        {
            return new TypeLangValue(node.IdName);
        }

        throw new NameError(node.Position, node.IdName);
    }

    /// <summary>
    /// 访问 Operation 节点
    /// </summary>
    public LangValueType VisitOperation(Operation node)
    {

        // 处理一元运算符
        if (node.Left is null)
        {
            if (node.Opera == LangTokenType.Exclamation)
            {
                var rightValue = node.Right?.Accept(new InterpreterVisitor(manager)) as BoolLangValue ??
                                 throw new InvalidOperationError(node, "NOT运算符只支持布尔类型");
                return BoolLangValue.Create(!rightValue.Value, node.Position);
            }

            if (node.Opera == LangTokenType.Minus)
            {
                var rightValue = node.Right?.Accept(new InterpreterVisitor(manager));
                if (rightValue is IntLangValue intValue)
                    return new IntLangValue(-intValue.Value);
                if (rightValue is DoubleLangValue doubleValue)
                    return new DoubleLangValue(-doubleValue.Value);
                throw new InvalidOperationError(node, "一元负号运算符只支持整数和浮点数");
            }

            throw new InvalidOperationError(node, "不支持的一元运算符");
        }

        // 处理this.id => dot_value
        if (node.Opera == LangTokenType.Dot && node.Left is LangId { IdName: "this" } && node.Right is not null)
        {
            var thisValue = node.Left.Accept(this);
            if (thisValue is AnyLangValue anyValue)
            {
                // V2 架构：直接调用 Dot，访问控制由内部实现
                return anyValue.Dot(node.Right, manager);
            }

            throw new NameError(node.Left, "this");
        }

        // 处理逻辑AND操作 - 短路求值
        if (node.Opera == LangTokenType.And)
        {
            var leftValue = node.Left.Accept(this) as BoolLangValue ??
                            throw new InvalidOperationError(node, "AND运算符只支持布尔类型");
            if (!leftValue.Value)
            {
                return BoolLangValue.Create(false, node.Position);
            }

            var rightValue = node.Right?.Accept(new InterpreterVisitor(manager)) as BoolLangValue ??
                             throw new InvalidOperationError(node, "AND运算符只支持布尔类型");
            return BoolLangValue.Create(leftValue.Value && rightValue.Value, node.Position);
        }

        // 处理逻辑OR操作 - 短路求值
        if (node.Opera == LangTokenType.Or)
        {
            var leftValue = node.Left.Accept(this) as BoolLangValue ?? throw new InvalidOperationError(node, "OR运算符只支持布尔类型");
            if (leftValue.Value)
            {
                return BoolLangValue.Create(true, node.Position);
            }

            var rightValue = node.Right?.Accept(new InterpreterVisitor(manager)) as BoolLangValue ??
                             throw new InvalidOperationError(node, "OR运算符只支持布尔类型");
            return BoolLangValue.Create(leftValue.Value || rightValue.Value, node.Position);
        }

        // 处理点操作
        if (node.Opera == LangTokenType.Dot)
        {
            var dotRight = CallSyntaxAdapter.Normalize(node.Right);
            var dotLeftResult = node.Left.Accept(this);
            if (dotLeftResult is AnyLangValue any)
            {
                if (dotRight is Instance r1)
                {
                    return any.Dot(r1, manager);
                }

                if (dotRight is not null)
                {
                    return any.Dot(dotRight, manager);
                }
            }
            else if (dotLeftResult is ListLangValue list)
            {
                if (dotRight is Instance instance)
                {
                    return list.Dot(instance, manager);
                }

                if (dotRight is not null)
                {
                    // 先尝试将 Right 作为属性或方法调用传递给 Dot 方法
                    // 这样可以处理像 .Count 这样的属性访问
                    try
                    {
                        return list.Dot(dotRight, manager);
                    }
                    catch (InvalidOperationError)
                    {
                        // 如果 Dot 方法无法处理，则尝试作为索引访问
                        var listIndexResult = dotRight.Accept(this);
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
                if (dotRight is not Instance r1) throw new InvalidOperationError(node, "原生静态类型操作需要实例");
                var newInstance = new Instance(r1.Id, r1.Ids, r1.NamedArgs);
                return native.Dot(newInstance, manager);
            }
            // 处理静态成员访问：ClassName.staticMember
            else if (dotLeftResult is TypeTemplate typeTemplate)
            {
                if (dotRight is Instance r1)
                {
                    // 处理静态方法调用
                    var newInstance = new Instance(r1.Id, r1.Ids, r1.NamedArgs, r1.Position);
                    return typeTemplate.Dot(newInstance, manager);
                }

                if (dotRight is not null)
                {
                    // 处理静态成员访问
                    return typeTemplate.Dot(dotRight, manager);
                }
            }
            // 处理枚举成员访问：EnumName.MemberName
            else if (dotLeftResult is EnumTemplate enumTemplate)
            {
                if (dotRight is LangId memberId)
                {
                    // 访问枚举成员
                    return enumTemplate.GetMemberValue(memberId.IdName);
                }

                throw new InvalidOperationError(node, $"枚举 '{enumTemplate.EnumName}' 只支持成员访问");
            }
            else if (dotLeftResult is ArrayLangValue array)
            {
                // 处理数组方法调用
                if (dotRight is Instance instance)
                {
                    return array.Dot(instance, manager);
                }

                // 处理数组索引访问，需要先运行Right表达式
                if (dotRight is not null)
                {
                    var arrayIndexResult = dotRight.Accept(this);
                    if (arrayIndexResult is IntLangValue intValue)
                    {
                        return array.Get(intValue);
                    }

                    // 如果不是整数索引，则作为方法调用处理
                    return array.Dot(dotRight, manager);
                }
            }
            else if (dotLeftResult is DictionaryLangValue dict)
            {
                // 处理字典方法调用
                if (dotRight is Instance instance)
                {
                    return dict.Dot(instance, manager);
                }

                // 处理字典属性访问和方法调用
                if (dotRight is not null)
                {
                    return dict.Dot(dotRight, manager);
                }
            }
            else if (dotLeftResult is TaskClassLangValue taskClassValue)
            {
                if (dotRight is Instance instance)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    taskClassValue.ExternalManager = manager;
                    return taskClassValue.Dot(instance, manager);
                }

                if (dotRight is not null)
                {
                    // 设置外部管理器，确保能访问最新的外部变量
                    taskClassValue.ExternalManager = manager;
                    return taskClassValue.Dot(dotRight, manager);
                }
            }
            else if (dotLeftResult is TaskLangValue taskValue)
            {
                // 设置外部管理器，确保 Then 等方法能访问有效的 Interpreter
                taskValue.ExternalManager = manager;

                if (dotRight is Instance instance)
                {
                    // Retry 方法需要特殊处理，因为需要重新执行原始函数调用
                    if (instance.Id.IdName == "Retry")
                    {
                        if (instance.Ids.Count is < 1 or > 2)
                        {
                            throw new ArgumentError(instance.Position,
                                $"Retry 方法需要 1-2 个参数，实际提供了 {instance.Ids.Count} 个");
                        }

                        var retryCountValue = instance.Ids[0].Accept(new InterpreterVisitor(manager));
                        if (retryCountValue is not IntLangValue retryCount)
                        {
                            throw new TypeError(instance, "Retry 的第一个参数必须是整数");
                        }

                        var delayMs = 0;
                        if (instance.Ids.Count == 2)
                        {
                            var delayValue = instance.Ids[1].Accept(new InterpreterVisitor(manager));
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
                        if (node.Left is not Instance funcCall)
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

                if (dotRight is not null)
                {
                    return taskValue.Dot(dotRight, manager);
                }
            }
            else if (dotLeftResult is ThreadLangValue threadValue)
            {
                // 设置外部管理器，确保 Then 等方法能访问有效的 Interpreter
                threadValue.ExternalManager = manager;

                if (dotRight is Instance instance)
                {
                    // Retry 方法需要特殊处理，因为需要重新执行原始函数调用
                    if (instance.Id.IdName == "Retry")
                    {
                        if (instance.Ids.Count is < 1 or > 2)
                        {
                            throw new ArgumentError(instance.Position,
                                $"Retry 方法需要 1-2 个参数，实际提供了 {instance.Ids.Count} 个");
                        }

                        var retryCountValue = instance.Ids[0].Accept(new InterpreterVisitor(manager));
                        if (retryCountValue is not IntLangValue retryCount)
                        {
                            throw new TypeError(instance, "Retry 的第一个参数必须是整数");
                        }

                        var delayMs = 0;
                        if (instance.Ids.Count == 2)
                        {
                            var delayValue = instance.Ids[1].Accept(new InterpreterVisitor(manager));
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
                        if (node.Left is not Instance funcCall)
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

                if (dotRight is not null)
                {
                    return threadValue.Dot(dotRight, manager);
                }
            }
            else if (dotLeftResult is not null && dotRight is not null)
            {
                return dotLeftResult.Dot(dotRight, manager);
                // throw new InvalidOperationError(node, $"类型 '{dotLeftResult.GetType().Name}' 不支持点操作");
            }
            else
            {
                throw new InvalidOperationError(node, "连接运算符左右操作数均不能为空");
            }
        }

        // 处理其他二元运算符，运行左右操作数
        // 注意：直接在当前作用域中运行两个操作数
        // Set方法已经被修复，只在当前作用域中设置变量，不会修改外部变量
        var leftResult = node.Left.Accept(this);

        if (node.Opera == LangTokenType.As)
        {
            // 处理安全类型转换操作：left as right
            // 右侧应该是一个类型标识符，如 int, double, string 等
            // 直接从 Right 表达式获取类型名称，而不是从运行结果
            string typeName;
            switch (node.Right)
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
                    var rightAsResult = node.Right?.Accept(new InterpreterVisitor(manager)) ?? throw new InvalidOperationError(node, "右操作数不能为空");

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

        if (node.Opera == LangTokenType.Is)
        {
            // 处理类型检查操作：left is right
            // 右侧应该是一个类型标识符或类名
            string typeName;
            switch (node.Right)
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
                    var rightIsResult = node.Right?.Accept(new InterpreterVisitor(manager)) ?? throw new InvalidOperationError(node, "右操作数不能为空");

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
            return new BoolLangValue(node.CheckIsInstance(leftResult, typeName, manager));
        }

        if (node.Opera == LangTokenType.IsNot)
        {
            // 处理否定类型检查操作：left is not right
            // 右侧应该是一个类型标识符或类名
            string typeName;
            switch (node.Right)
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
                    var rightIsNotResult = node.Right?.Accept(new InterpreterVisitor(manager)) ?? throw new InvalidOperationError(node, "右操作数不能为空");

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
            return new BoolLangValue(!node.CheckIsInstance(leftResult, typeName, manager));
        }

        var rightResult = node.Right?.Accept(new InterpreterVisitor(manager)) ?? throw new InvalidOperationError(node, "右操作数不能为空");

        // 处理空值合并运算符 ??
        if (node.Opera == LangTokenType.NullishCoalescing)
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
            node.Opera == LangTokenType.Xor)
        {
            return BoolLangValue.Create(!boolLeft.Equal(boolRight), node.Position);
        }

        // == , < , >
        if (leftResult is not null && rightResult is not null)
        {
            switch (node.Opera)
            {
                case LangTokenType.Equals:
                    return BoolLangValue.Create(leftResult.Equal(rightResult), node.Position);
                case LangTokenType.LessThan:
                    return BoolLangValue.Create(leftResult.Less(rightResult), node.Position);
                case LangTokenType.GreaterThan:
                    return BoolLangValue.Create(leftResult.Greater(rightResult), node.Position);
                case LangTokenType.NotEquals:
                    return BoolLangValue.Create(!leftResult.Equal(rightResult), node.Position);
                case LangTokenType.LessThanEquals:
                    return BoolLangValue.Create(leftResult.LessEqual(rightResult), node.Position);
                case LangTokenType.GreaterThanEquals:
                    return BoolLangValue.Create(leftResult.GreaterEqual(rightResult), node.Position);
                case LangTokenType.In:
                    // 处理 in 操作符：检查左侧值是否存在于右侧集合中
                    if (rightResult is ILangList list)
                    {
                        return BoolLangValue.Create(list.In(leftResult), node.Position);
                    }

                    throw new InvalidOperationError(node, "in 操作符右侧必须是集合类型");
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

        throw new InvalidOperationError(node, $"不支持的操作类型: {node.OperaToString()}");
    
    
    }

    /// <summary>
    /// 访问 FunctionCallExpression 节点
    /// </summary>
    public LangValueType VisitFunctionCallExpression(FunctionCallExpression node)
    {

        // 1. 运行函数表达式获取函数对象
        // 对于简单标识符调用，尝试使用函数引用缓存（仅缓存非闭包函数）
        LangValueType functionValue;
        if (node.FunctionExpression is LangId langId)
        {
            var name = langId.IdName;
            if (!manager.TryGetCachedFunction(name, out functionValue!))
            {
                functionValue = node.FunctionExpression.Accept(this);
                // 只缓存非闭包、非泛型的顶层函数（闭包每次调用外层函数时会重新创建）
                if (functionValue is FuncLangValue { IsGeneric: false, CapturedScope: null } topLevelFunc)
                {
                    manager.CacheFunctionReference(name, topLevelFunc);
                }
            }
        }
        else
        {
            functionValue = node.FunctionExpression.Accept(this);
        }

        // 2. 检查获取到的是否为函数
        if (functionValue is not FuncLangValue func)
        {
            throw new InvalidOperationError(node, $"表达式 '{node.FunctionExpression}' 的结果不是函数，无法调用");
        }

        // 3. 如果是泛型函数且未实例化，尝试自动推断类型参数
        if (func is { IsGeneric: true, TypeArgumentMapping: null })
        {
            // 尝试从参数推断类型
            if (manager.Interpreter is null)
            {
                throw new InvalidOperationError(node, "无法执行泛型类型推断：解释器未初始化");
            }

            var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
            var inference = new TypeSystem.GenericTypeInference(typeAnnotationManager);
            var inferredTypes = inference.InferFunctionTypeArguments(func, node.Arguments, manager, node.Position);

            if (inferredTypes is not null)
            {
                // 使用推断出的类型实例化泛型函数
                var instantiatedFunc = func.InstantiateGeneric(inferredTypes, typeAnnotationManager);

                // 调用实例化后的函数（需要处理命名参数）
                return instantiatedFunc.Run(manager, node.Arguments, node.NamedArguments, node.Position);
            }
            else
            {
                // 无法推断类型，抛出错误
                throw new InvalidOperationError(node,
                    $"无法推断泛型函数 '{func.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{func.Id?.IdName}<类型>(...)");
            }
        }

        // 4. 调用函数，传入位置参数和命名参数
        return func.Run(manager, node.Arguments, node.NamedArguments, node.Position);
    
    
    }

    /// <summary>
    /// 访问 TernaryExpression 节点
    /// </summary>
    public LangValueType VisitTernaryExpression(TernaryExpression node)
    {
        // 完整迁移自 TernaryExpression.Run()
        // 执行条件判断
        var condition = node.Condition.Accept(this);
        if (condition is not BoolLangValue boolValue)
        {
            throw new InvalidOperationError(node, "三元条件表达式的条件必须是Bool类型");
        }

        // 根据条件结果返回相应的表达式值
        return boolValue.Value
            ? node.TrueExpression.Accept(this)
            : node.FalseExpression.Accept(this);
    }

    /// <summary>
    /// 访问 AwaitExpression 节点
    /// </summary>
    public LangValueType VisitAwaitExpression(AwaitExpression node)
    {

        // 执行表达式，期望得到 TaskLangValue
        var result = node.Expression.Accept(this);

        if (result is not TaskLangValue taskValue)
        {
            throw new TypeError(
                node,
                $"await 只能用于 Task 类型，实际类型为 {result.TypeToString()}"
            );
        }

        // 统一走 AwaitAsync：已完成的任务会同步返回结果，未完成的任务会阻塞等待。
        // 这样成功、异常、取消三种情况都能正确处理。
        // 注意不要用“已完成就直接读 TaskLangValue.Result”的快速路径：
        // 任务可能在未被 await 的情况下于后台完成，那种写法会把已完成任务的结果当成 null。
        try
        {
            var awaitTask = taskValue.AwaitAsync();
            var taskResult = awaitTask.GetAwaiter().GetResult();
            return taskResult;
        }
        catch (AggregateException aggEx)
        {
            // 展开聚合异常，抛出内部异常
            throw aggEx.InnerException ?? aggEx;
        }
    
    
    }

    /// <summary>
    /// 访问 AsyncStreamExpression 节点
    /// </summary>
    public LangValueType VisitAsyncStreamExpression(AsyncStreamExpression node)
    {

        // 创建一个异步函数来包装 async { } 块
        // 将块语句包装为异步函数：async () -> { block }
        var asyncFunc = new AsyncFuncLangValue(
            id: null,
            ids: [], // 无参数
            blockStatement: node.Block,
            position: node.Position
        );

        // 运行异步函数以捕获闭包
        // 对于包含 yield 的异步函数，Run() 会返回 AsyncGeneratorLangValue
        var result = asyncFunc.Accept(this);

        // 如果返回的是 AsyncGeneratorLangValue，直接包装为 AsyncStreamLangValue
        if (result is AsyncGeneratorLangValue asyncGen)
        {
            return new AsyncStreamLangValue(asyncGen, node.Position);
        }

        // 如果不是生成器（不包含 yield），仍然包装为 AsyncStreamLangValue
        // 但需要传递 AsyncFuncLangValue
        if (result is AsyncFuncLangValue asyncFuncValue)
        {
            return new AsyncStreamLangValue(asyncFuncValue, node.Position);
        }

        // 不应该到达这里
        throw new InvalidOperationException(
            $"Unexpected result type from AsyncFuncLangValue.Run(): {result.GetType().Name}");
    
    
    }

    /// <summary>
    /// 访问 SuperExpression 节点
    /// </summary>
    public LangValueType VisitSuperExpression(SuperExpression node)
    {

        // 获取当前实例（node）
        var currentInstance = manager.GetCurrentInstance();

        if (currentInstance is null)
        {
            throw new InvalidOperationError(node.Position, "super只能在类实例方法中使用");
        }

        // 返回一个特殊的SuperProxy对象，用于延迟解析父类成员访问
        return new SuperProxy(currentInstance, manager, node.Position);
    
    
    }

    /// <summary>
    /// 访问 ThisExpression 节点
    /// </summary>
    public LangValueType VisitThisExpression(ThisExpression node)
    {
        // 直接从变量储存器中获取名为"this"的变量
        if (manager is null)
        {
            throw new NameError(node.Position, "this");
        }

        var thisValue = manager.GetValue(new LangId("this"));
        if (thisValue is not null)
        {
            return thisValue;
        }

        // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
        throw new NameError(node.Position, "this");
    }

    /// <summary>
    /// 访问 MatchExpression 节点
    /// </summary>
    public LangValueType VisitMatchExpression(MatchExpression node)
    {

        // 计算被匹配的值
        var matchValue = node.MatchValue.Accept(this);

        // 依次检查每个 case
        foreach (var matchCase in node.Cases)
        {
            // 检查是否匹配
            if (matchCase.IsMatch(matchValue, manager, out var boundValues))
            {
                // 如果有变量绑定，将值绑定到新的作用域
                if (boundValues is not null && boundValues.Count > 0)
                {
                    // 添加新的子作用域
                    manager.AddChildren();

                    // 直接在当前作用域中创建绑定变量，避免修改父作用域的同名变量
                    var currentScope = manager.Scopes[^1];
                    foreach (var (varName, varValue) in boundValues)
                    {
                        currentScope[varName] = varValue;
                    }
                }

                try
                {
                    // 执行对应的表达式并返回结果
                    return matchCase.ResultExpression.Accept(this);
                }
                finally
                {
                    // 清理变量绑定的作用域
                    if (boundValues is not null && boundValues.Count > 0)
                    {
                        manager.RemoveChildren();
                    }
                }
            }
        }

        // 如果没有任何 case 匹配，抛出错误
        throw new InvalidOperationError(node, $"Match 表达式没有匹配的分支，值为: {matchValue}");
    
    
    }
}
