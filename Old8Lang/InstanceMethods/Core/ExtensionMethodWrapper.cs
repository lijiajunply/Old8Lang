using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
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
    /// <summary>
    /// 声明扩展方法时的变量管理器（解释器预执行阶段）。
    /// </summary>
    /// <remarks>
    /// IL 模式下扩展方法体仍然由解释器执行，需要一个能访问全局函数/全局变量的管理器；
    /// 这里复用声明阶段的管理器（与解释器模式使用调用方 manager 的做法一致）。
    /// </remarks>
    private VariateManager DeclarationManager { get; } = manager;

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

    /// <summary>
    /// 标记为扩展方法
    /// </summary>
    public bool IsExtensionMethod => true;

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
        // 【修复】生成方法签名用于递归检测
        var methodSignature = $"{instance.GetType().Name}.{function.Id.IdName}";

        // 【修复】进入扩展方法，启用递归检测
        manager.EnterExtensionMethod(methodSignature);

        try
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
                    var returnValue = manager.Result;
                    manager.ClearReturn();
                    return returnValue;
                }

                // 如果没有显式返回，返回 null
                return new NullLangValue();
            }
            finally
            {
                manager.RemoveChildren();
            }
        }
        finally
        {
            // 【修复】退出扩展方法，清理递归检测状态
            manager.ExitExtensionMethod();
        }
    }

    /// <summary>
    /// IL 模式：把接收者与参数转成 Old8Lang 值，交给运行期 helper 用解释器执行扩展方法体。
    /// </summary>
    /// <remarks>
    /// 扩展方法声明在编译前会由解释器预执行（<c>Compiler.Compile</c> 里的
    /// <c>statement.ExecuteModule</c>）注册进实例方法表，因此这里只需要把
    /// 「注册好的包装器」带进生成代码，再按 <see cref="Execute"/> 的语义执行函数体。
    /// </remarks>
    public void GenerateIl(
        LangExpression instance,
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 包装器本身作为编译期常量传入
        var wrapperId = RuntimeConstantRegistry.Register(this);
        ilGenerator.Emit(OpCodes.Ldc_I4, wrapperId);
        ilGenerator.Emit(OpCodes.Call,
            typeof(RuntimeConstantRegistry).GetMethod(nameof(RuntimeConstantRegistry.Get))!);
        ilGenerator.Emit(OpCodes.Castclass, typeof(ExtensionMethodWrapper));

        // 接收者与参数：原生值 -> Old8Lang 值
        IlValueBridge.EmitLoadWrapped(instance, ilGenerator, local);

        ilGenerator.Emit(OpCodes.Ldc_I4, parameters.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(LangValueType));

        for (var i = 0; i < parameters.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i);
            IlValueBridge.EmitLoadWrapped(parameters[i], ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        var helperMethod = typeof(ExtensionMethodWrapper).GetMethod(nameof(InvokeExtension),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);

        // 结果转回 IL 模式的原生表示，避免后续按 LangValueType 处理导致跨模式输出差异
        ilGenerator.Emit(OpCodes.Call, typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.Unwrap))!);
    }

    /// <summary>
    /// IL 模式的运行期 helper：按解释器语义执行扩展方法体。
    /// </summary>
    public static LangValueType InvokeExtension(ExtensionMethodWrapper wrapper, LangValueType instance,
        LangValueType[] arguments)
    {
        return wrapper.ExecuteWithValues(instance, arguments, wrapper.DeclarationManager);
    }

    /// <summary>
    /// 用已求值的参数执行扩展方法（供 IL 模式使用，不依赖参数表达式）。
    /// </summary>
    private LangValueType ExecuteWithValues(LangValueType instance, LangValueType[] arguments,
        VariateManager manager)
    {
        var methodSignature = $"{instance.GetType().Name}.{function.Id.IdName}";

        manager.EnterExtensionMethod(methodSignature);

        try
        {
            manager.AddChildren();

            try
            {
                manager.Set(new LangId("this"), instance);

                for (var i = 0; i < arguments.Length && i < function.Ids.Count; i++)
                {
                    manager.Set(new LangId(function.Ids[i].IdName), arguments[i]);
                }

                function.BlockStatement.Run(manager);

                if (manager.IsReturn)
                {
                    var returnValue = manager.Result;
                    manager.ClearReturn();
                    return returnValue;
                }

                return new NullLangValue();
            }
            finally
            {
                manager.RemoveChildren();
            }
        }
        finally
        {
            manager.ExitExtensionMethod();
        }
    }

    public Type GetReturnType(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        // 返回动态类型（IL 模式下结果已转回原生表示）
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