using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 实例，a(b,c)，支持命名参数
/// </summary>
public partial class Instance : LangValueType
{
    public readonly List<LangExpression> Ids;
    public readonly LangId Id;
    public readonly List<NamedArgument> NamedArgs;

    public Instance(LangId langId, List<LangExpression> ids, SourcePosition position = default)
        : base(position)
    {
        Id = langId;
        Ids = ids;
        NamedArgs = [];
    }

    public Instance(LangId langId, List<LangExpression> ids, List<NamedArgument> namedArgs,
        SourcePosition position = default)
        : base(position)
    {
        Id = langId;
        Ids = ids;
        NamedArgs = namedArgs;
    }

    public LangValueType FromClassToResult(LangValueType baseLangValue, VariateManager? manager = null)
    {
        // 1. 首先尝试通过实例方法注册器执行（新系统）
        if (TryExecuteInstanceMethod(baseLangValue, manager ?? new VariateManager(), out var instanceMethodResult))
        {
            return instanceMethodResult!;
        }

        // 2. 如果新系统没有找到方法，使用旧的反射系统（向后兼容）
        var type = baseLangValue.GetType();
        MethodInfo? m = null;

        // 设置执行上下文，以便扩展方法可以访问当前的 VariateManager
        if (manager is not null)
        {
            ValueFunctions.ExecutionContext.SetCurrentManager(manager);
        }

        // Old8Lang 类型的实例方法已经通过 InstanceMethods 系统处理
        // 这里不再需要旧的 ValueFunctions 回退逻辑

        // 如果没有找到扩展方法，尝试在类型本身上查找
        if (m is null)
        {
            type = baseLangValue.GetType();
            // 根据参数数量查找正确的重载
            var allInstanceMethods = type.GetMethods().Where(x => x.Name == Id.IdName).ToArray();
            if (allInstanceMethods is { Length: > 0 })
            {
                // 对于实例方法，预期参数数量 = 传入参数数量
                var expectedParamCount = Ids.Count;
                m = allInstanceMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount) ??
                    allInstanceMethods[0];
            }
        }

        // 如果找不到方法，抛出异常
        if (m is null)
        {
            throw new AttributeError(baseLangValue, Id.IdName, baseLangValue.GetType().Name);
        }

        var os = new List<object>();

        // 检查方法是否需要参数
        var parameters = m?.GetParameters() ?? [];

        // 对于静态方法（扩展方法），第一个参数是 baseLangValue
        if (m?.IsStatic == true && parameters.Length > 0)
        {
            os.Add(baseLangValue);
        }

        // 只添加与方法参数数量匹配的参数
        for (int i = 0; i < Ids.Count && os.Count < parameters.Length; i++)
        {
            // 对于实例方法，第一个参数已经是实例本身，所以跳过
            if (m?.IsStatic == false && i == 0 && parameters.Length > 0)
            {
                continue;
            }

            // 运行表达式获取参数值
            // 如果参数已经是 LangValueType，则直接使用；否则调用 Run
            // 对于 TaskLangValue 和 ThreadLangValue，优先使用 ExternalManager
            LangValueType argValue;
            if (Ids[i] is LangValueType langValue)
            {
                argValue = langValue;
            }
            else if (baseLangValue is TaskLangValue { ExternalManager: not null } taskLangValue)
            {
                argValue = Ids[i].Accept(new InterpreterVisitor(taskLangValue.ExternalManager));
            }
            else if (baseLangValue is ThreadLangValue { ExternalManager: not null } threadLangValue)
            {
                argValue = Ids[i].Accept(new InterpreterVisitor(threadLangValue.ExternalManager));
            }
            else if (baseLangValue is FuncLangValue funcLangValue)
            {
                argValue = funcLangValue;
            }
            else
            {
                // 使用传入的 manager 参数来运行表达式，而不是 null
                argValue = Ids[i].Accept(new InterpreterVisitor(manager ?? new VariateManager()));
            }

            os.Add(argValue);
        }

        // 补充缺失的参数，包括可选参数的默认值和 SourcePosition 参数
        if (os.Count < parameters.Length)
        {
            for (int i = os.Count; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType == typeof(SourcePosition))
                {
                    // 使用 Instance 的位置信息
                    os.Add(Position);
                }
                else if (parameters[i].IsOptional || parameters[i].HasDefaultValue)
                {
                    // 使用可选参数的默认值
                    os.Add(parameters[i].DefaultValue!);
                }
            }
        }

        // 对于静态方法，实例参数为 null；对于实例方法，实例参数为 baseLangValue
        object? invokeInstance = m?.IsStatic == false ? baseLangValue : null;

        try
        {
            var r = m?.Invoke(invokeInstance, [.. os]);
            if (r is LangValueType v) return v;
            return ObjToValue(r!);
        }
        finally
        {
            // 清理执行上下文
            ValueFunctions.ExecutionContext.ClearCurrentManager();
        }
    }

    public override string ToString()
    {
        return $"{Id}({string.Join(", ", Ids)})";
    }

    /// <summary>
    /// 生成局部 lambda 委托的动态调用。闭包委托在创建时才绑定外层局部变量，
    /// 因此不能像具名函数一样直接使用静态 DynamicMethod 调用。
    /// </summary>
    internal bool TryEmitLocalDelegateCall(ILGenerator ilGenerator, LocalManager local)
    {
        if (NamedArgs.Count != 0 || local.GetLocalVar(Id.IdName) is not { LocalType: var type } lambdaLocal ||
            !typeof(Delegate).IsAssignableFrom(type))
        {
            return false;
        }

        var returnType = local.LocalDelegateReturnTypes.GetValueOrDefault(Id.IdName, typeof(object));

        ilGenerator.Emit(OpCodes.Ldloc, lambdaLocal);
        ilGenerator.Emit(OpCodes.Ldc_I4, Ids.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));
        for (var i = 0; i < Ids.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i);
            var argument = Ids[i];
            argument.Accept(new CompilerVisitor(ilGenerator, local));
            var argumentType = argument.Accept(new TypeInferenceVisitor(local));
            if (argumentType?.IsValueType == true)
            {
                ilGenerator.Emit(OpCodes.Box, argumentType);
            }

            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        ilGenerator.Emit(OpCodes.Callvirt,
            typeof(Delegate).GetMethod(nameof(Delegate.DynamicInvoke), [typeof(object[])])!);
        if (returnType == typeof(void))
        {
            ilGenerator.Emit(OpCodes.Pop);
        }
        else if (returnType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, returnType);
        }
        else if (returnType != typeof(object))
        {
            ilGenerator.Emit(OpCodes.Castclass, returnType);
        }

        return true;
    }

    /// <summary>
    /// 加载参数并进行必要的类型转换
    /// </summary>
    internal void LoadParameterWithConversion(ILGenerator ilGenerator, LocalManager local, LangExpression id,
        Type? idType, Type paramType)
    {
        // 使用LocalManager的ValidateType方法验证参数类型
        if (idType is not null)
        {
            // 执行类型验证，确保参数类型与方法期望的类型兼容
            local.ValidateType(paramType, idType, id.Position);
        }

        // 处理必要的类型转换
        if (idType is not null && paramType != idType)
        {
            if (paramType == typeof(object) && idType.IsValueType)
            {
                // 从值类型转换为object，需要装箱
                ilGenerator.Emit(OpCodes.Box, idType);
            }
            else if (paramType == typeof(int) && idType == typeof(object))
            {
                // 从object转换为int
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            }
            else if (paramType == typeof(double) && idType == typeof(object))
            {
                // 从object转换为double
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(double));
            }
            else if (paramType == typeof(int) && idType == typeof(double))
            {
                // 从double转换为int
                ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
            }
            else if (paramType == typeof(double) && idType == typeof(int))
            {
                // 从int转换为double
                ilGenerator.Emit(OpCodes.Conv_R8);
            }
            else if (paramType == typeof(string) && idType != typeof(string))
            {
                // 从其他类型转换为string
                ilGenerator.Emit(OpCodes.Call, idType.GetMethod("ToString", Type.EmptyTypes)!);
            }
        }
    }
}
