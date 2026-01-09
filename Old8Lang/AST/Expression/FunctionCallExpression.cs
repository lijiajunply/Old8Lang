using Old8Lang.Error;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using System.Reflection.Emit;
using Old8Lang.Interpreter;

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
        // 编译器模式下的类型推断
        // 由于函数表达式可能很复杂，暂时返回 object 类型
        // 实际的编译器实现会更复杂，需要分析函数表达式的返回类型
        return typeof(object);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 编译器模式下的IL生成
        
        // 处理函数调用
        if (FunctionExpression is LangId functionId)
        {
            // 获取函数名称
            var funcName = functionId.IdName;
            
            // 构建委托键：函数名 + 参数类型签名
            var paramTypes = Arguments.Select(arg => arg.OutputType(local) ?? typeof(object)).ToArray();
            var paramTypeNames = string.Join("_", paramTypes.Select(t => t.Name));
            var delegateKey = $"{funcName}${paramTypeNames}";
            
            // 检查是否有对应的编译好的方法
            if (local.DelegateVar.TryGetValue(delegateKey, out var method))
            {
                // 加载所有参数到堆栈
                foreach (var arg in Arguments)
                {
                    arg.LoadIlValue(ilGenerator, local);
                }
                
                // 调用方法
                ilGenerator.Emit(OpCodes.Call, method);
                return;
            }
            
            // 尝试查找泛型函数实例
            var genericDelegateKey = $"{funcName}";
            if (local.DelegateVar.TryGetValue(genericDelegateKey, out var genericMethod))
            {
                // 对于泛型函数，需要特殊处理
                // 这里简化处理，直接调用
                foreach (var arg in Arguments)
                {
                    arg.LoadIlValue(ilGenerator, local);
                }
                
                ilGenerator.Emit(OpCodes.Call, genericMethod);
                return;
            }
        }

        // 如果是复杂的表达式函数调用，抛出异常
        throw new InvalidOperationError(this, "编译器模式下暂时不支持复杂的表达式函数调用，请使用简单的函数标识符调用");
    }
}