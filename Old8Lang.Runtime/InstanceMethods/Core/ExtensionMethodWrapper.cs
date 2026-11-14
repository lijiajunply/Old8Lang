using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

using Old8Lang.AST.Visitor;

namespace Old8Lang.InstanceMethods.Core;

/// <summary>
/// 扩展方法包装器，将 Old8Lang 函数包装为实例方法
/// </summary>
public class ExtensionMethodWrapper(
    Type targetType,
    FuncLangValue function,
    VariateManager manager,
    List<GenericParameter>? targetGenericParameters = null)
    : IInstanceMethod, IIlNativeValueInstanceMethod
{
    /// <summary>
    /// 声明扩展方法时的变量管理器（解释器预执行阶段）。
    /// </summary>
    /// <remarks>
    /// IL 模式下扩展方法体仍然由解释器执行，需要一个能访问全局函数/全局变量的管理器；
    /// 这里复用声明阶段的管理器（与解释器模式使用调用方 manager 的做法一致）。
    /// </remarks>
    private VariateManager DeclarationManager { get; } = manager;

    private IReadOnlyList<GenericParameter> TargetGenericParameters { get; } =
        targetGenericParameters ?? [];

    public string[] Names => [function.Id!.IdName];

    public Type TargetType => targetType;

    public string[]? ParameterNames
    {
        get
        {
            // 所有参数都是用户定义的参数（不包括隐式的 this）
            return function.Ids!.Select(id => id.IdName).ToArray();
        }
    }

    public int MinParameterCount => function.Ids!.Count;

    public int MaxParameterCount => function.Ids!.Count;

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
        var expectedParamCount = function.Ids!.Count;
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
        var methodSignature = $"{instance.GetType().Name}.{function.Id!.IdName}";

        // 【修复】进入扩展方法，启用递归检测
        manager.EnterExtensionMethod(methodSignature);

        try
        {
            ValidateTargetGenericConstraints(instance);

            // 创建新的作用域
            manager.AddChildren();
            var originalReturnType = manager.CurrentFunctionReturnType;
            var originalTypeArgumentMapping = manager.CurrentFunctionTypeArgumentMapping;

            try
            {
                manager.CurrentFunctionReturnType = function.Id?.AssumptionType;
                manager.CurrentFunctionTypeArgumentMapping = GetTargetGenericTypeMapping(instance);

                // 绑定 this 关键字到实例
                manager.Set(new LangId("this"), instance);

                // 绑定用户定义的参数
                for (int i = 0; i < parameters.Count; i++)
                {
                    var paramName = function.Ids![i].IdName;
                    var paramValue = parameters[i].Accept(RuntimeVisitors.CreateInterpreter(manager));
                    manager.Set(new LangId(paramName), paramValue);
                }

                // 执行函数体
                function.BlockStatement.Accept(RuntimeVisitors.CreateInterpreter(manager));

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
                manager.CurrentFunctionReturnType = originalReturnType;
                manager.CurrentFunctionTypeArgumentMapping = originalTypeArgumentMapping;
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

        // 保留原生引用到运行期 helper；由 helper 统一包装，才能在接收者、参数和嵌套集合之间
        // 保持别名关系，并把所有原地修改同步回原列表。
        EmitLoadBoxed(instance, ilGenerator, local);

        ilGenerator.Emit(OpCodes.Ldc_I4, parameters.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));

        for (var i = 0; i < parameters.Count; i++)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldc_I4, i);
            EmitLoadBoxed(parameters[i], ilGenerator, local);
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        var helperMethod = typeof(ExtensionMethodWrapper).GetMethod(nameof(InvokeNativeExtension),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// IL 模式的运行期 helper：按解释器语义执行扩展方法体。
    /// </summary>
    public static LangValueType InvokeExtension(ExtensionMethodWrapper wrapper, LangValueType instance,
        LangValueType[] arguments)
    {
        return wrapper.ExecuteWithValues(instance, arguments, wrapper.DeclarationManager);
    }

    public static object? InvokeNativeExtension(ExtensionMethodWrapper wrapper, object? instance,
        object?[] arguments)
    {
        var bridge = new NativeExtensionBridge();
        var wrapped = bridge.Wrap(instance);
        var wrappedArguments = arguments.Select(bridge.Wrap).ToArray();
        try
        {
            wrapper.ValidateTargetGenericConstraints(wrapped);
            var result = wrapper.ExecuteWithValues(wrapped, wrappedArguments, wrapper.DeclarationManager);
            return bridge.Unwrap(result);
        }
        finally
        {
            bridge.Synchronize();
        }
    }

    private static void EmitLoadBoxed(LangExpression expression, ILGenerator ilGenerator, LocalManager local)
    {
        expression.Accept(new CompilerVisitor(ilGenerator, local));

        if (expression is FuncLangValue or LambdaExpression)
        {
            return;
        }

        Type? expressionType;
        try
        {
            expressionType = expression.Accept(new TypeInferenceVisitor(local));
        }
        catch
        {
            expressionType = null;
        }

        if (expressionType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, expressionType);
        }
    }

    /// <summary>
    /// 用已求值的参数执行扩展方法（供 IL 模式使用，不依赖参数表达式）。
    /// </summary>
    private LangValueType ExecuteWithValues(LangValueType instance, LangValueType[] arguments,
        VariateManager manager)
    {
        var methodSignature = $"{instance.GetType().Name}.{function.Id!.IdName}";

        manager.EnterExtensionMethod(methodSignature);

        try
        {
            manager.AddChildren();
            var originalReturnType = manager.CurrentFunctionReturnType;
            var originalTypeArgumentMapping = manager.CurrentFunctionTypeArgumentMapping;

            try
            {
                manager.CurrentFunctionReturnType = function.Id?.AssumptionType;
                manager.CurrentFunctionTypeArgumentMapping = GetTargetGenericTypeMapping(instance);

                manager.Set(new LangId("this"), instance);

                for (var i = 0; i < arguments.Length && i < function.Ids!.Count; i++)
                {
                    manager.Set(new LangId(function.Ids![i].IdName), arguments[i]);
                }

                function.BlockStatement.Accept(RuntimeVisitors.CreateInterpreter(manager));

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
                manager.CurrentFunctionReturnType = originalReturnType;
                manager.CurrentFunctionTypeArgumentMapping = originalTypeArgumentMapping;
                manager.RemoveChildren();
            }
        }
        finally
        {
            manager.ExitExtensionMethod();
        }
    }

    private void ValidateTargetGenericConstraints(LangValueType instance)
    {
        if (TargetGenericParameters.Count == 0)
        {
            return;
        }

        var typeArguments = GetTargetGenericArgumentTypes(instance);
        for (var index = 0; index < TargetGenericParameters.Count; index++)
        {
            if (!typeArguments.TryGetValue(TargetGenericParameters[index].Name, out var actualType))
            {
                // 空集合不携带足够的运行期元素类型信息；保留动态语言对空集合的可用性。
                continue;
            }

            var parameter = TargetGenericParameters[index];
            foreach (var candidate in GetTargetElementTypes(instance, index).DefaultIfEmpty(actualType))
            {
                var candidateArguments = new Dictionary<string, Type>(typeArguments, StringComparer.Ordinal)
                {
                    [parameter.Name] = candidate
                };
                var validation = GenericConstraintValidator.ValidateWithDotNetType(
                    parameter, candidate, candidateArguments);
                if (!validation.IsValid)
                    throw new TypeError(function.Position, validation.ErrorMessage ?? "泛型目标类型不满足约束");
            }
        }
    }

    private static IEnumerable<Type> GetTargetElementTypes(LangValueType instance, int parameterIndex)
    {
        if (parameterIndex == 0 && instance is ListLangValue list)
            return list.Values.Select(GetRuntimeType);
        if (parameterIndex == 0 && instance is ArrayLangValue array)
            return array.GetItems().Select(GetRuntimeType);
        if (parameterIndex is 0 or 1 && instance is DictionaryLangValue dictionary)
            return dictionary.Tuples.Select(tuple => GetRuntimeType(tuple.Get(parameterIndex)));
        return [];
    }

    private Dictionary<string, Type> GetTargetGenericArgumentTypes(LangValueType instance)
    {
        var result = new Dictionary<string, Type>(StringComparer.Ordinal);
        if (instance is ListLangValue list && TargetGenericParameters.Count >= 1 && list.Values.Count > 0)
        {
            result[TargetGenericParameters[0].Name] = GetRuntimeType(list.Values[0]);
        }
        else if (instance is ArrayLangValue array && TargetGenericParameters.Count >= 1)
        {
            var firstValue = array.GetItems().FirstOrDefault();
            if (firstValue is not null)
            {
                result[TargetGenericParameters[0].Name] = GetRuntimeType(firstValue);
            }
        }
        else if (instance is DictionaryLangValue dictionary && dictionary.Tuples.Count > 0)
        {
            if (TargetGenericParameters.Count >= 1)
            {
                result[TargetGenericParameters[0].Name] = GetRuntimeType(dictionary.Tuples[0].Get(0));
            }

            if (TargetGenericParameters.Count >= 2)
            {
                result[TargetGenericParameters[1].Name] = GetRuntimeType(dictionary.Tuples[0].Get(1));
            }
        }

        return result;
    }

    private static Type GetRuntimeType(LangValueType value)
    {
        // 推断集合类型只需要类别，不应递归展开值；集合可能引用自身。
        return value switch
        {
            ArrayLangValue => typeof(object[]),
            ListLangValue => typeof(List<object>),
            DictionaryLangValue => typeof(Dictionary<object, object>),
            TupleLangValue => typeof(ValueTuple),
            _ => value.GetValue()?.GetType() ?? value.GetType()
        };
    }

    private Dictionary<string, ITypeInfo>? GetTargetGenericTypeMapping(LangValueType instance)
    {
        if (TargetGenericParameters.Count == 0)
        {
            return null;
        }

        LangValueType[] values = instance switch
        {
            ListLangValue list when list.Values.Count > 0 => [list.Values[0]],
            ArrayLangValue array when array.GetItems().FirstOrDefault() is { } first => [first],
            DictionaryLangValue dictionary when dictionary.Tuples.Count > 0 && TargetGenericParameters.Count >= 2 =>
                [dictionary.Tuples[0].Get(0), dictionary.Tuples[0].Get(1)],
            _ => []
        };

        var mapping = new Dictionary<string, ITypeInfo>(StringComparer.Ordinal);
        for (var index = 0; index < Math.Min(values.Length, TargetGenericParameters.Count); index++)
        {
            mapping[TargetGenericParameters[index].Name] =
                new PrimitiveTypeInfo(TypeChecker.GetLangValueType(values[index]));
        }

        return mapping;
    }

    /// <summary>
    /// 在一次 IL 扩展调用中保持原生集合与语言值之间的一对一映射。
    /// </summary>
    /// <remarks>
    /// 不能直接调用 <see cref="IlValueBridge.Wrap"/>：它会为每个出现位置创建新的列表包装器，
    /// 从而让 <c>this</c>、参数和嵌套集合失去别名关系。同步时反向复用映射中的原生对象，
    /// 以保证返回接收者及所有既有别名仍然指向同一集合。
    /// </remarks>
    private sealed class NativeExtensionBridge
    {
        private readonly Dictionary<object, LangValueType> _wrappedByNative =
            new(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<LangValueType, object> _nativeByWrapped =
            new(ReferenceEqualityComparer.Instance);

        public LangValueType Wrap(object? value)
        {
            if (value is null || value is LangValueType || value.GetType().IsValueType || value is string || value is Delegate || value is Task)
            {
                return IlValueBridge.Wrap(value);
            }

            if (_wrappedByNative.TryGetValue(value, out var existing))
            {
                return existing;
            }

            if (value is Array array)
            {
                if (array.Rank != 1 || array.GetLowerBound(0) != 0)
                    throw new InvalidOperationException("扩展方法暂不支持多维数组或非零起始索引数组");
                var wrappedArray = new ArrayLangValue(new LangValueType[array.Length]);
                Remember(value, wrappedArray);
                for (var index = 0; index < array.Length; index++)
                    wrappedArray.RunResult[index] = Wrap(array.GetValue(index));
                return wrappedArray;
            }

            if (value is IList list)
            {
                var wrappedList = new ListLangValue(new List<LangValueType>());
                Remember(value, wrappedList);
                foreach (var item in list)
                {
                    wrappedList.Values.Add(Wrap(item));
                }

                return wrappedList;
            }

            return IlValueBridge.Wrap(value);
        }

        public object? Unwrap(LangValueType value)
        {
            return _nativeByWrapped.TryGetValue(value, out var native)
                ? native
                : IlValueBridge.Unwrap(value);
        }

        public void Synchronize()
        {
            var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
            foreach (var (native, wrapped) in _wrappedByNative)
            {
                Synchronize(native, wrapped, visited);
            }
        }

        private void Remember(object native, LangValueType wrapped)
        {
            _wrappedByNative[native] = wrapped;
            _nativeByWrapped[wrapped] = native;
        }

        private void Synchronize(object native, LangValueType wrapped, HashSet<object> visited)
        {
            if (!visited.Add(native))
            {
                return;
            }

            switch (native)
            {
                case Array array when wrapped is ArrayLangValue wrappedArray:
                    SynchronizeArray(array, wrappedArray, visited);
                    break;
                case IList { IsReadOnly: false, IsFixedSize: false } list when wrapped is ListLangValue wrappedList:
                    SynchronizeList(list, wrappedList, visited);
                    break;
            }
        }

        private void SynchronizeArray(Array array, ArrayLangValue wrappedArray, HashSet<object> visited)
        {
            var values = wrappedArray.GetItems().ToArray();
            if (array.Length != values.Length)
            {
                throw new InvalidOperationException("扩展方法不能改变数组长度");
            }

            for (var index = 0; index < array.Length; index++)
            {
                SynchronizeNested(values[index], visited);
                array.SetValue(Unwrap(values[index]), index);
            }
        }

        private void SynchronizeList(IList list, ListLangValue wrappedList, HashSet<object> visited)
        {
            var values = wrappedList.Values.ToArray();
            foreach (var value in values)
            {
                SynchronizeNested(value, visited);
            }

            list.Clear();
            foreach (var value in values)
            {
                list.Add(Unwrap(value));
            }
        }

        private void SynchronizeNested(LangValueType value, HashSet<object> visited)
        {
            if (_nativeByWrapped.TryGetValue(value, out var native))
            {
                Synchronize(native, value, visited);
            }
        }
    }

    public Type GetReturnType(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        // 直接返回 this 的扩展方法保持接收者静态类型，允许继续链式调用。
        // 运行期 bridge 也会把同一个原生接收者对象返回，类型和身份在两层都一致。
        if (function.BlockStatement.OtherStatements
            .OfType<ReturnStatement>()
            .Any(statement => statement.Expression is LangId { IdName: "this" }))
        {
            return instanceType;
        }

        if (function.Id?.AssumptionType.StartsWith("list", StringComparison.OrdinalIgnoreCase) == true &&
            TargetType == typeof(ListLangValue))
        {
            return instanceType;
        }

        // 其他返回值仍是动态类型（IL 模式下结果已转回原生表示）。
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
            for (int i = 0; i < arguments.Length && i < function.Ids!.Count; i++)
            {
                var paramName = function.Ids![i].IdName;
                var paramValue = ConvertToLangValue(arguments[i]);
                tempManager.Set(new LangId(paramName), paramValue);
            }

            // 执行函数体
            function.BlockStatement.Accept(RuntimeVisitors.CreateInterpreter(tempManager));

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
