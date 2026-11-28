using System.Collections;
using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
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
                var typeArgumentMapping = GetTargetGenericTypeMapping(instance);
                manager.CurrentFunctionTypeArgumentMapping = typeArgumentMapping;

                // 绑定 this 关键字到实例
                manager.Set(new LangId("this"), instance);

                // 绑定用户定义的参数（并校验泛型目标类型参数约束的形参类型）
                for (int i = 0; i < parameters.Count; i++)
                {
                    var paramId = function.Ids![i];
                    var paramValue = parameters[i].Accept(RuntimeVisitors.CreateInterpreter(manager));
                    ValidateParameterTypeIfAnnotated(paramId, paramValue, typeArgumentMapping, position);
                    manager.Set(new LangId(paramId.IdName), paramValue);
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
        expression.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));

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
                var typeArgumentMapping = GetTargetGenericTypeMapping(instance);
                manager.CurrentFunctionTypeArgumentMapping = typeArgumentMapping;

                manager.Set(new LangId("this"), instance);

                for (var i = 0; i < arguments.Length && i < function.Ids!.Count; i++)
                {
                    var paramId = function.Ids![i];
                    ValidateParameterTypeIfAnnotated(paramId, arguments[i], typeArgumentMapping, function.Position);
                    manager.Set(new LangId(paramId.IdName), arguments[i]);
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

    private void ValidateParameterTypeIfAnnotated(
        LangId paramId,
        LangValueType paramValue,
        Dictionary<string, ITypeInfo>? typeArgumentMapping,
        SourcePosition position)
    {
        if (string.IsNullOrWhiteSpace(paramId.AssumptionType) || typeArgumentMapping is null || typeArgumentMapping.Count == 0)
        {
            return;
        }

        var annotation = paramId.AssumptionType.Trim();
        var isNullable = annotation.EndsWith('?');
        var baseName = isNullable ? annotation[..^1].Trim() : annotation;
        if (!typeArgumentMapping.TryGetValue(baseName, out var mappedType))
        {
            return;
        }

        var resolved = isNullable ? $"{mappedType.Name}?" : mappedType.Name;

        var actualType = TypeChecker.GetLangValueType(paramValue);
        if (!TypeChecker.IsPolymorphicCompatible(actualType, resolved))
        {
            throw new TypeError(
                position,
                resolved,
                actualType,
                $"扩展方法 '{function.Id?.IdName}' 参数 '{paramId.IdName}' 类型不匹配");
        }
    }

    private void ValidateTargetGenericConstraints(LangValueType instance)
    {
        ValidateGenericConstraintsForInstance(TargetGenericParameters, instance, DeclarationManager, function.Position);
    }

    /// <summary>
    /// 校验泛型扩展方法的目标类型参数约束（供解释器、IL 与 VM 共享）。
    /// </summary>
    public static void ValidateGenericConstraintsForInstance(
        IReadOnlyList<GenericParameter> targetGenericParameters,
        LangValueType instance,
        VariateManager? manager,
        SourcePosition position)
    {
        if (targetGenericParameters.Count == 0)
        {
            return;
        }

        // 字典按条目逐对校验，保证多类型参数交叉约束（如 where K: V）检查所有键值对。
        if (instance is DictionaryLangValue dictionary)
        {
            var entries = GetDictionaryEntries(dictionary);
            if (entries.Count == 0)
            {
                ValidateEmptyCollectionDeclaredTypes(targetGenericParameters,
                    [dictionary.KeyType, dictionary.ValueType], manager, position);
                return;
            }

            foreach (var (key, value) in entries)
            {
                var pairValues = targetGenericParameters.Count >= 2
                    ? new[] { key, value }
                    : new[] { key };
                ValidateTypeArgumentTuple(targetGenericParameters, pairValues, manager, position);
            }

            return;
        }

        if (instance is ListLangValue list)
        {
            if (list.Values.Count == 0)
            {
                ValidateEmptyCollectionDeclaredTypes(targetGenericParameters, [list.ElementType], manager, position);
                return;
            }

            foreach (var item in list.Values)
            {
                ValidateTypeArgumentTuple(targetGenericParameters, [item], manager, position);
            }

            return;
        }

        if (instance is ArrayLangValue array)
        {
            var items = array.GetItems().ToArray();
            if (items.Length == 0)
            {
                ValidateEmptyCollectionDeclaredTypes(targetGenericParameters, [array.ElementType], manager, position);
                return;
            }

            foreach (var item in items)
            {
                ValidateTypeArgumentTuple(targetGenericParameters, [item], manager, position);
            }
        }
    }

    private static void ValidateEmptyCollectionDeclaredTypes(
        IReadOnlyList<GenericParameter> targetGenericParameters,
        string?[] declaredTypeNames,
        VariateManager? manager,
        SourcePosition position)
    {
        var typeInfoMapping = new Dictionary<string, ITypeInfo>(StringComparer.Ordinal);
        for (var i = 0; i < Math.Min(targetGenericParameters.Count, declaredTypeNames.Length); i++)
        {
            if (!string.IsNullOrWhiteSpace(declaredTypeNames[i]))
            {
                typeInfoMapping[targetGenericParameters[i].Name] = new PrimitiveTypeInfo(declaredTypeNames[i]!.Trim());
            }
        }

        if (typeInfoMapping.Count == 0)
        {
            return;
        }

        for (var i = 0; i < targetGenericParameters.Count; i++)
        {
            var parameter = targetGenericParameters[i];
            if (!typeInfoMapping.TryGetValue(parameter.Name, out var actualTypeInfo))
            {
                continue;
            }

            var validation = GenericConstraintValidator.Validate(parameter, actualTypeInfo, typeInfoMapping, manager);
            if (!validation.IsValid)
            {
                throw new TypeError(position, validation.ErrorMessage ?? "泛型目标类型不满足约束");
            }
        }
    }

    private static void ValidateTypeArgumentTuple(
        IReadOnlyList<GenericParameter> targetGenericParameters,
        LangValueType[] values,
        VariateManager? manager,
        SourcePosition position)
    {
        var count = Math.Min(targetGenericParameters.Count, values.Length);
        var typeInfoMapping = new Dictionary<string, ITypeInfo>(StringComparer.Ordinal);
        var dotNetMapping = new Dictionary<string, Type>(StringComparer.Ordinal);

        for (var i = 0; i < count; i++)
        {
            var paramName = targetGenericParameters[i].Name;
            typeInfoMapping[paramName] = new PrimitiveTypeInfo(GetLangTypeName(values[i]));
            dotNetMapping[paramName] = GetRuntimeType(values[i]);
        }

        for (var i = 0; i < count; i++)
        {
            var parameter = targetGenericParameters[i];
            if (parameter.StructuredConstraints is null || parameter.StructuredConstraints.Count == 0)
            {
                continue;
            }

            // Old8Lang 类实例使用语言层类型信息校验（支持 Old8Lang new()/继承/接口/跨参数约束）；
            // 原生 CLR 对象或 .NET 标量若语言层无法解析则回退到 .NET Type 校验。
            var actualValue = values[i];
            var useLangValidator = actualValue is AnyLangValue or IntLangValue or DoubleLangValue
                or BoolLangValue or CharLangValue or StringLangValue or ListLangValue
                or ArrayLangValue or DictionaryLangValue or TupleLangValue or NullLangValue;

            var validation = useLangValidator
                ? GenericConstraintValidator.Validate(parameter, typeInfoMapping[parameter.Name], typeInfoMapping, manager)
                : GenericConstraintValidator.ValidateWithDotNetType(parameter, dotNetMapping[parameter.Name], dotNetMapping);

            if (!validation.IsValid)
            {
                throw new TypeError(position, validation.ErrorMessage ?? "泛型目标类型不满足约束");
            }
        }
    }

    private static List<(LangValueType Key, LangValueType Value)> GetDictionaryEntries(DictionaryLangValue dictionary)
    {
        if (dictionary.Value.Count > 0)
        {
            return dictionary.Value;
        }

        if (dictionary.Tuples.Count > 0)
        {
            return dictionary.Tuples.Select(t => (t.Get(0), t.Get(1))).ToList();
        }

        return [];
    }

    private static string GetLangTypeName(LangValueType value)
    {
        if (value is NativeAnyLangValue nativeAny)
        {
            var obj = nativeAny.GetValue();
            if (obj is not null)
            {
                return obj.GetType().Name;
            }
        }

        return TypeChecker.GetLangValueType(value);
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

        var mapping = new Dictionary<string, ITypeInfo>(StringComparer.Ordinal);

        if (instance is ListLangValue list)
        {
            if (list.Values.Count > 0)
            {
                mapping[TargetGenericParameters[0].Name] = new PrimitiveTypeInfo(GetLangTypeName(list.Values[0]));
            }
            else if (!string.IsNullOrWhiteSpace(list.ElementType))
            {
                mapping[TargetGenericParameters[0].Name] = new PrimitiveTypeInfo(list.ElementType.Trim());
            }
        }
        else if (instance is ArrayLangValue array)
        {
            var first = array.GetItems().FirstOrDefault();
            if (first is not null)
            {
                mapping[TargetGenericParameters[0].Name] = new PrimitiveTypeInfo(GetLangTypeName(first));
            }
            else if (!string.IsNullOrWhiteSpace(array.ElementType))
            {
                mapping[TargetGenericParameters[0].Name] = new PrimitiveTypeInfo(array.ElementType.Trim());
            }
        }
        else if (instance is DictionaryLangValue dictionary)
        {
            var entries = GetDictionaryEntries(dictionary);
            if (entries.Count > 0)
            {
                if (TargetGenericParameters.Count >= 1)
                {
                    mapping[TargetGenericParameters[0].Name] = new PrimitiveTypeInfo(GetLangTypeName(entries[0].Key));
                }

                if (TargetGenericParameters.Count >= 2)
                {
                    mapping[TargetGenericParameters[1].Name] = new PrimitiveTypeInfo(GetLangTypeName(entries[0].Value));
                }
            }
            else
            {
                if (TargetGenericParameters.Count >= 1 && !string.IsNullOrWhiteSpace(dictionary.KeyType))
                {
                    mapping[TargetGenericParameters[0].Name] = new PrimitiveTypeInfo(dictionary.KeyType.Trim());
                }

                if (TargetGenericParameters.Count >= 2 && !string.IsNullOrWhiteSpace(dictionary.ValueType))
                {
                    mapping[TargetGenericParameters[1].Name] = new PrimitiveTypeInfo(dictionary.ValueType.Trim());
                }
            }
        }

        // 未绑定类型参数（如无类型注解的空集合）回退为 any，避免方法体内 `-> T` 报未解析参数名。
        foreach (var param in TargetGenericParameters)
        {
            mapping.TryAdd(param.Name, new PrimitiveTypeInfo("any"));
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

            if (value is IDictionary dictionary)
            {
                var wrappedDict = new DictionaryLangValue();
                Remember(value, wrappedDict);
                foreach (DictionaryEntry entry in dictionary)
                {
                    wrappedDict.Value.Add((Wrap(entry.Key), Wrap(entry.Value)));
                }

                return wrappedDict;
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
                case IDictionary { IsReadOnly: false, IsFixedSize: false } dictionary when wrapped is DictionaryLangValue wrappedDict:
                    SynchronizeDictionary(dictionary, wrappedDict, visited);
                    break;
            }
        }

        private void SynchronizeDictionary(IDictionary dictionary, DictionaryLangValue wrappedDict, HashSet<object> visited)
        {
            var entries = wrappedDict.Value.ToArray();
            foreach (var (key, val) in entries)
            {
                SynchronizeNested(key, visited);
                SynchronizeNested(val, visited);
            }

            dictionary.Clear();
            foreach (var (key, val) in entries)
            {
                var unwrappedKey = Unwrap(key);
                if (unwrappedKey is not null)
                {
                    dictionary[unwrappedKey] = Unwrap(val);
                }
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

        if ((function.Id?.AssumptionType.StartsWith("dict", StringComparison.OrdinalIgnoreCase) == true ||
             function.Id?.AssumptionType.StartsWith("dictionary", StringComparison.OrdinalIgnoreCase) == true) &&
            TargetType == typeof(DictionaryLangValue))
        {
            return instanceType;
        }

        if (function.Id?.AssumptionType.StartsWith("array", StringComparison.OrdinalIgnoreCase) == true &&
            TargetType == typeof(ArrayLangValue))
        {
            return instanceType;
        }

        // 其他返回值仍是动态类型（IL 模式下结果已转回原生表示）。
        return typeof(object);
    }

    public object? ExecuteInVM(object? instance, object?[] arguments)
    {
        return InvokeNativeExtension(this, instance, arguments);
    }
}
