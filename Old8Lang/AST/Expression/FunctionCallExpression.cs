using Old8Lang.Error;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using System.Reflection.Emit;
using Old8Lang.Interpreter;
using System.Reflection;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 函数调用表达式，用于处理 expression(arguments) 语法
/// 例如：cla[0]("World"), obj.method()(args)
/// 支持命名参数：func(a: 1, b: 2) 或混合使用：func(1, b: 2)
/// </summary>
public partial class FunctionCallExpression : LangExpression
{
    /// <summary>
    /// 要调用的函数表达式
    /// </summary>
    public readonly LangExpression FunctionExpression;

    /// <summary>
    /// 函数调用参数列表（位置参数）
    /// </summary>
    public readonly List<LangExpression> Arguments;

    /// <summary>
    /// 命名参数列表
    /// </summary>
    public readonly List<NamedArgument> NamedArguments;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="functionExpression">要调用的函数表达式</param>
    /// <param name="arguments">函数调用参数列表</param>
    /// <param name="position">位置信息</param>
    public FunctionCallExpression(LangExpression functionExpression, List<LangExpression> arguments,
        SourcePosition position = default)
        : base(position)
    {
        FunctionExpression = functionExpression;
        Arguments = arguments;
        NamedArguments = new List<NamedArgument>();
    }

    /// <summary>
    /// 构造函数（支持命名参数）
    /// </summary>
    /// <param name="functionExpression">要调用的函数表达式</param>
    /// <param name="arguments">位置参数列表</param>
    /// <param name="namedArguments">命名参数列表</param>
    /// <param name="position">位置信息</param>
    public FunctionCallExpression(LangExpression functionExpression, List<LangExpression> arguments,
        List<NamedArgument> namedArguments, SourcePosition position = default)
        : base(position)
    {
        FunctionExpression = functionExpression;
        Arguments = arguments;
        NamedArguments = namedArguments;
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 1. 运行函数表达式获取函数对象
        var functionValue = FunctionExpression.Run(manager);

        // 2. 检查获取到的是否为函数
        if (functionValue is not FuncLangValue func)
        {
            throw new InvalidOperationError(this, $"表达式 '{FunctionExpression}' 的结果不是函数，无法调用");
        }

        // 3. 如果是泛型函数且未实例化，尝试自动推断类型参数
        if (func.IsGeneric && func.TypeArgumentMapping is null)
        {
            // 尝试从参数推断类型
            if (manager.Interpreter is null)
            {
                throw new InvalidOperationError(this, "无法执行泛型类型推断：解释器未初始化");
            }

            var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;
            var inference = new TypeSystem.GenericTypeInference(typeAnnotationManager);
            var inferredTypes = inference.InferFunctionTypeArguments(func, Arguments, manager, Position);

            if (inferredTypes is not null)
            {
                // 使用推断出的类型实例化泛型函数
                var instantiatedFunc = func.InstantiateGeneric(inferredTypes, typeAnnotationManager);

                // 调用实例化后的函数（需要处理命名参数）
                return instantiatedFunc.Run(manager, Arguments, NamedArguments, Position);
            }
            else
            {
                // 无法推断类型，抛出错误
                throw new InvalidOperationError(this,
                    $"无法推断泛型函数 '{func.Id?.IdName}' 的类型参数，请使用显式类型参数调用：{func.Id?.IdName}<类型>(...)");
            }
        }

        // 4. 调用函数，传入位置参数和命名参数
        return func.Run(manager, Arguments, NamedArguments, Position);
    }

    public override string ToString()
    {
        var args = new List<string>();
        args.AddRange(Arguments.Select(arg => arg.ToString()));
        args.AddRange(NamedArguments.Select(na => na.ToString()));
        var argsStr = string.Join(", ", args);
        return $"{FunctionExpression}({argsStr})";
    }

    public override Type OutputType(LocalManager local)
    {
        // 1. 尝试分析函数表达式的类型
        var funcType = FunctionExpression.OutputType(local);
        
        // 2. 如果是委托类型（包括 Action/Func），获取 Invoke 方法的返回类型
        if (typeof(Delegate).IsAssignableFrom(funcType))
        {
            var invokeMethod = funcType.GetMethod("Invoke");
            if (invokeMethod != null)
            {
                return invokeMethod.ReturnType;
            }
        }
        
        // 3. 如果是 LangId，尝试从 DelegateVar 中查找（针对直接函数调用）
        if (FunctionExpression is LangId funcId)
        {
            // 尝试构建委托键查找
            // 这里有一个循环依赖问题：我们需要参数类型来构建键，但参数类型推断可能依赖于函数返回类型？
            // 通常参数类型是独立的。
            try 
            {
                var paramTypes = Arguments.Select(arg => arg.OutputType(local) ?? typeof(object)).ToArray();
                var paramTypeNames = string.Join("_", paramTypes.Select(t => t.Name));
                var delegateKey = $"{funcId.IdName}${paramTypeNames}";
                
                if (local.DelegateVar.TryGetValue(delegateKey, out var method))
                {
                    if (method is DynamicMethod dm) return dm.ReturnType;
                    if (method is MethodInfo mi) return mi.ReturnType;
                }
            }
            catch
            {
                // 忽略错误，回退到 object
            }
        }

        // return typeof(object);
        
        // 如果无法推断，返回 object
        // 这是一个妥协，允许编译继续，但在运行时可能会有问题（除非我们处理了 object）
        // 如果是 Async 函数，通常返回 Task<object>
        if (FunctionExpression is LangId id && (id.IdName.StartsWith("Async") || id.IdName.EndsWith("Async")))
        {
            return typeof(Task<object>);
        }
        
        return typeof(object);
        
        // Debug:
        // throw new InvalidOperationError(this, $"无法推断函数调用返回类型。函数表达式类型: {funcType.FullName}, IsDelegate: {typeof(Delegate).IsAssignableFrom(funcType)}");
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 1. 优先尝试直接方法调用（针对 LangId 且在 DelegateVar 中存在的情况）
        if (FunctionExpression is LangId functionId)
        {
            var funcName = functionId.IdName;
            var paramTypes = Arguments.Select(arg => arg.OutputType(local) ?? typeof(object)).ToArray();
            var paramTypeNames = string.Join("_", paramTypes.Select(t => t.Name));
            var delegateKey = $"{funcName}${paramTypeNames}";
            
            if (local.DelegateVar.TryGetValue(delegateKey, out var method))
            {
                foreach (var arg in Arguments)
                {
                    arg.LoadIlValue(ilGenerator, local);
                }
                ilGenerator.Emit(OpCodes.Call, method);
                return;
            }
        }

        // 2. 通用委托调用逻辑
        // 加载函数表达式（期望是一个委托实例）
        FunctionExpression.LoadIlValue(ilGenerator, local);
        var funcType = FunctionExpression.OutputType(local);
        
        if (typeof(Delegate).IsAssignableFrom(funcType))
        {
            var invokeMethod = funcType.GetMethod("Invoke");
            if (invokeMethod != null)
            {
                // 加载参数
                foreach (var arg in Arguments)
                {
                    arg.LoadIlValue(ilGenerator, local);
                }
                
                // 调用委托的 Invoke 方法
                ilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
                return;
            }
        }

        // 如果是复杂的表达式函数调用且无法解析为委托，抛出异常
        throw new InvalidOperationError(this, $"无法调用类型为 {funcType} 的表达式");
    }
}