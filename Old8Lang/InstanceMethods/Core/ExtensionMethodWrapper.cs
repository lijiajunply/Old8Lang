using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 扩展方法包装器，将 Old8Lang 函数包装为实例方法
/// </summary>
public class ExtensionMethodWrapper : IInstanceMethod
{
    private readonly Type _targetType;
    private readonly FuncLangValue _function;
    private readonly VariateManager _capturedManager;

    public ExtensionMethodWrapper(Type targetType, FuncLangValue function, VariateManager manager)
    {
        _targetType = targetType;
        _function = function;
        _capturedManager = manager;

        // 扩展方法不需要显式的 this 参数，this 会自动绑定
    }

    public string[] Names => [_function.Id.IdName];

    public Type TargetType => _targetType;

    public string[]? ParameterNames
    {
        get
        {
            // 所有参数都是用户定义的参数（不包括隐式的 this）
            return _function.Ids.Select(id => id.IdName).ToArray();
        }
    }

    public int MinParameterCount => _function.Ids.Count;

    public int MaxParameterCount => _function.Ids.Count;

    public Type?[]? ParameterTypes => null; // 接受任意类型

    public Type? DeclaredReturnType => null; // 动态返回类型

    public string? Documentation => _function.DocComment?.Summary;

    public bool CanAccept(List<LangExpression> parameters, LocalManager? local)
    {
        // 检查参数数量（不包括隐式的 this 参数）
        var expectedParamCount = _function.Ids.Count;
        return parameters.Count == expectedParamCount;
    }

    public int CalculateMatchScore(List<LangExpression> parameters, LocalManager? local)
    {
        if (!CanAccept(parameters, local))
        {
            return -1;
        }

        // 简单匹配：参数数量正确即可
        return 100;
    }

    public LangValueType Execute(
        LangValueType instance,
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        // 创建新的作用域
        manager.AddChildren();

        try
        {
            // 绑定 this 关键字到实例
            manager.Set(new LangId("this"), instance);

            // 绑定用户定义的参数
            for (int i = 0; i < parameters.Count; i++)
            {
                var paramName = _function.Ids[i].IdName;
                var paramValue = parameters[i].Run(manager);
                manager.Set(new LangId(paramName), paramValue);
            }

            // 执行函数体
            _function.BlockStatement.Run(manager);

            // 检查是否有返回值
            if (manager.IsReturn)
            {
                return manager.Result;
            }

            // 如果没有显式返回，返回 null
            return new NullLangValue();
        }
        finally
        {
            manager.RemoveChildren();
        }
    }

    public void GenerateIl(
        LangExpression instance,
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        throw new NotImplementedException("扩展方法的编译模式支持尚未实现");
    }

    public Type GetReturnType(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        // 返回动态类型
        return typeof(object);
    }

    public object? ExecuteInVM(object? instance, object?[] arguments)
    {
        throw new NotImplementedException("扩展方法的 VM 模式支持尚未实现");
    }
}
