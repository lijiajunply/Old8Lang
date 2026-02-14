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
public class ExtensionMethodWrapper(Type targetType, FuncLangValue function, VariateManager manager)
    : IInstanceMethod
{
    public string[] Names => [function.Id.IdName];

    public Type TargetType => targetType;

    public string[]? ParameterNames
    {
        get
        {
            // 所有参数都是用户定义的参数（不包括隐式的 this）
            return function.Ids.Select(id => id.IdName).ToArray();
        }
    }

    public int MinParameterCount => function.Ids.Count;

    public int MaxParameterCount => function.Ids.Count;

    public Type?[]? ParameterTypes => null; // 接受任意类型

    public Type? DeclaredReturnType => null; // 动态返回类型

    public string? Documentation => function.DocComment?.Summary;

    public bool CanAccept(List<LangExpression> parameters, LocalManager? local)
    {
        // 检查参数数量（不包括隐式的 this 参数）
        var expectedParamCount = function.Ids.Count;
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
                var paramName = function.Ids[i].IdName;
                var paramValue = parameters[i].Run(manager);
                manager.Set(new LangId(paramName), paramValue);
            }

            // 执行函数体
            function.BlockStatement.Run(manager);

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
        // 在 VM 模式下执行扩展方法
        // 需要创建一个临时的 VariateManager 来执行函数体

        // 创建新的作用域
        var tempManager = new VariateManager();
        tempManager.AddChildren();

        try
        {
            // 绑定 this 关键字到实例
            tempManager.Set(new LangId("this"), ConvertToLangValue(instance));

            // 绑定用户定义的参数
            for (int i = 0; i < arguments.Length && i < function.Ids.Count; i++)
            {
                var paramName = function.Ids[i].IdName;
                var paramValue = ConvertToLangValue(arguments[i]);
                tempManager.Set(new LangId(paramName), paramValue);
            }

            // 执行函数体
            function.BlockStatement.Run(tempManager);

            // 检查是否有返回值
            if (tempManager.IsReturn)
            {
                return ConvertFromLangValue(tempManager.Result);
            }

            // 如果没有显式返回，返回 null
            return null;
        }
        finally
        {
            tempManager.RemoveChildren();
        }
    }

    /// <summary>
    /// 将 VM 对象转换为 LangValueType
    /// </summary>
    private static LangValueType ConvertToLangValue(object? value)
    {
        if (value == null)
            return new NullLangValue();

        if (value is LangValueType langValue)
            return langValue;

        // 基本类型转换
        return value switch
        {
            int i => new IntLangValue(i),
            long l => new IntLangValue((int)l),
            double d => new DoubleLangValue(d),
            bool b => new BoolLangValue(b),
            string s => new StringLangValue(s),
            char c => new CharLangValue(c),
            LangValueType lv => lv,
            _ => (LangValueType)value // 强制转换为 LangValueType
        };
    }

    /// <summary>
    /// 将 LangValueType 转换回 VM 对象
    /// </summary>
    private static object? ConvertFromLangValue(LangValueType value)
    {
        if (value is NullLangValue)
            return null;

        if (value is IntLangValue intVal)
            return intVal.Value;

        if (value is DoubleLangValue doubleVal)
            return doubleVal.Value;

        if (value is BoolLangValue boolVal)
            return boolVal.Value;

        if (value is CharLangValue charVal)
            return charVal.Value;

        if (value is StringLangValue str)
            return str.Value;

        // 其他类型保持原样
        return value;
    }
}