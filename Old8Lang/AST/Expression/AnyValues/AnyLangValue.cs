using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// 类实例（重新设计版本）
/// 职责明确：只存储实例数据和指向类型元数据的引用
/// </summary>
public partial class AnyLangValue : LangValueType
{
    /// <summary>
    /// 类型元数据引用（指向 ClassMetadata）
    /// 所有实例共享同一份元数据，节省内存
    /// </summary>
    public ClassMetadata Metadata { get; }

    /// <summary>
    /// 实例数据存储（仅存储字段值）
    /// key: 字段名
    /// value: 字段值
    /// </summary>
    public Dictionary<string, LangValueType> InstanceData { get; }

    /// <summary>
    /// 类名标识（用于显示和错误报告）
    /// </summary>
    public LangId ClassId { get; }

    /// <summary>
    /// 实例作用域（仅用于方法执行期间的临时存储）
    /// 职责：提供方法执行时的 this 上下文和参数空间
    /// 不与 InstanceData 同步（避免复杂的同步逻辑）
    /// </summary>
    public VariateManager InstanceScope { get; }

    /// <summary>
    /// 记录在当前方法执行期间通过SetField修改过的字段
    /// 用于避免SyncFieldsFromExecutionScope覆盖这些字段
    /// </summary>
    public readonly HashSet<string> FieldsModifiedBySetField = new();

    /// <summary>
    /// 泛型类型参数映射（用于泛型类实例）
    /// 例如：Box&lt;int&gt; 时为 {"T": IntTypeInfo}
    /// </summary>
    public Dictionary<string, TypeSystem.ITypeInfo>? TypeArgumentMapping { get; set; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="classId">类名标识</param>
    /// <param name="metadata">类型元数据</param>
    /// <param name="position">源码位置</param>
    public AnyLangValue(
        LangId classId,
        ClassMetadata metadata,
        SourcePosition position = default)
        : base(position)
    {
        ClassId = classId;
        Metadata = metadata;
        InstanceData = new Dictionary<string, LangValueType>();
        InstanceScope = new VariateManager { IsClass = true };
    }

    // ===== V2 核心实现 =====


    /// <summary>
    /// 初始化实例字段
    /// 根据 ClassMetadata 中的字段定义表初始化所有字段
    /// </summary>
    /// <param name="manager">变量管理器（用于执行字段初始化表达式）</param>
    public void InitializeFields(VariateManager manager)
    {
        // 遍历所有字段定义，执行初始化表达式
        foreach (var fieldDef in Metadata.FieldTable.GetAllFields())
        {
            // 跳过静态字段（静态字段存储在 ClassMetadata.StaticMembers 中）
            if (fieldDef.IsStatic)
                continue;

            // 执行初始化表达式，获取初始值
            var initialValue = fieldDef.InitialValueExpression.Run(manager);

            // 存储到实例数据中
            InstanceData[fieldDef.FieldName] = initialValue;
        }

        // 设置解释器
        InstanceScope.Interpreter = manager.Interpreter;

        // 将全局类型定义添加到实例作用域
        InstanceScope.AddImportInfoRange(manager.ImportInfos);
    }

    /// <summary>
    /// 成员访问（Dot 操作符）
    /// 支持：
    /// 1. 字段访问：obj.field
    /// 2. 方法调用：obj.method()
    /// 3. 索引访问：obj.list[0]
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        return dotExpression switch
        {
            // 1. 字段或方法访问：obj.name
            LangId id => AccessMemberById(id, manager),

            // 2. 方法调用：obj.method(args)
            Instance instance => CallMethod(instance, manager),

            // 3. 索引访问：obj.list[index]
            LangListItem listItem => AccessCollectionItem(listItem, manager),

            // 4. 其他表达式（不常见）
            _ => throw new InvalidOperationError(this, $"不支持的成员访问表达式: {dotExpression.GetType().Name}")
        };
    }

    /// <summary>
    /// 通过标识符访问成员（字段或方法）
    /// </summary>
    private LangValueType AccessMemberById(LangId id, VariateManager manager)
    {
        var memberName = id.IdName;
        bool isInternalAccess = CheckInternalAccess(manager);

        // 1. 优先查找字段
        if (InstanceData.TryGetValue(memberName, out var fieldValue))
        {
            // 检查字段访问权限
            var fieldDef = Metadata.FieldTable.LookupField(memberName);
            if (fieldDef != null && !fieldDef.IsAccessibleFrom(isInternalAccess))
            {
                throw new AttributeError(this, memberName, ClassId.IdName);
            }

            return fieldValue;
        }

        // 2. 查找方法（从 MethodTable）
        var methods = Metadata.MethodTable.LookupMethod(memberName);
        if (methods is { Count: > 0 })
        {
            var method = methods[0]; // 取第一个（如果有重载，在调用时再解析）

            // 检查方法访问权限
            if (!method.IsAccessibleFrom(isInternalAccess))
            {
                throw new AttributeError(this, memberName, ClassId.IdName);
            }

            // 返回方法（不执行，等待调用）
            return method.Implementation;
        }

        // 3. 找不到成员，抛出异常
        throw new AttributeError(this, memberName, ClassId.IdName);
    }

    /// <summary>
    /// 调用方法
    /// </summary>
    private LangValueType CallMethod(Instance instance, VariateManager manager)
    {
        var methodName = instance.Id.IdName;
        bool isInternalAccess = CheckInternalAccess(manager);

        // 从 MethodTable 查找方法
        var methods = Metadata.MethodTable.LookupMethod(methodName);
        if (methods == null || methods.Count == 0)
        {
            // 如果找不到方法，检查是否是嵌套类的实例化
            // 这支持 Level1().Level2() 这样的语法
            var nestedClassInstance = TryInstantiateNestedClass(instance, manager);
            if (nestedClassInstance != null)
            {
                return nestedClassInstance;
            }

            throw new AttributeError(this, methodName, ClassId.IdName);
        }

        // 检查访问权限
        var selectedMethod = methods[0];
        if (!selectedMethod.IsAccessibleFrom(isInternalAccess))
        {
            throw new AttributeError(this, methodName, ClassId.IdName);
        }

        // 如果有多个重载，进行重载解析
        if (methods.Count > 1)
        {
            selectedMethod = ResolveMethodOverload(methods, instance.Ids, manager);
        }

        // 执行方法
        return ExecuteMethod(selectedMethod, instance.Ids, manager);
    }

    /// <summary>
    /// 尝试实例化嵌套类
    /// 支持 Level1().Level2() 这样的语法
    /// </summary>
    /// <param name="instance">实例调用表达式</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>如果找到嵌套类则返回新实例，否则返回 null</returns>
    private LangValueType? TryInstantiateNestedClass(Instance instance, VariateManager manager)
    {
        var className = instance.Id.IdName;

        // 检查是否只是实例化调用（没有参数）
        if (instance.Ids.Count > 0)
        {
            return null; // 嵌套类实例化不支持参数
        }

        // 尝试从全局管理器中查找嵌套类
        // 因为嵌套类在注册时会被添加到全局作用域
        var nestedClassTemplate = manager.GetAny(new LangId(className));
        if (nestedClassTemplate is TypeTemplate typeTemplate)
        {
            // 创建嵌套类实例
            var nestedInstance = typeTemplate.CreateInstanceV2(manager);
            nestedInstance.Init(manager.Interpreter);
            return nestedInstance;
        }

        return null;
    }

    /// <summary>
    /// 执行方法
    /// </summary>
    private LangValueType ExecuteMethod(LangMethodInfo methodInfo, List<LangExpression> arguments,
        VariateManager manager)
    {
        var parameterValues = arguments.Select(arg => arg.Run(manager)).ToList();

        // 然后创建参数值表达式（直接返回这些值，不需要再次求值）
        var parameterValueExpressions = parameterValues.Select<LangValueType, LangExpression>(val => val).ToList();

        // 为方法执行创建一个混合作用域（与 CallInit 保持一致）：
        // 1. 基于外部 manager（可以访问外部变量，用于访问类型定义）
        // 2. 添加实例字段和 this（可以在方法中访问实例成员）
        var executionManager = manager.NewManger();

        // 1. 设置 this 指针
        executionManager.Set(new LangId("this"), this);

        // 2. 将所有实例字段添加到执行作用域
        //    （方法内部可以直接访问字段，不需要 this.field）
        foreach (var (fieldName, fieldValue) in InstanceData)
        {
            executionManager.Set(new LangId(fieldName), fieldValue);
        }

        // 3. 将类的所有方法添加到执行作用域
        //    （方法内部可以直接调用其他方法，不需要 this.method()）
        foreach (var method in Metadata.MethodTable.GetAllMethods())
        {
            // 只添加非静态方法到实例方法作用域
            if (!method.IsStatic)
            {
                executionManager.Set(new LangId(method.MethodName), method.Implementation);
            }
        }

        // 4. 将类型信息添加到作用域（从 InstanceScope 继承）
        executionManager.AddImportInfoRange(InstanceScope.ImportInfos);

        // 5. 设置函数上下文标志
        executionManager.IsFunc = true;

        // 5.1 如果这是泛型类的方法，将类型参数映射设置到执行管理器中
        //     这样函数在验证参数类型和返回值类型时可以正确解析泛型类型参数
        if (TypeArgumentMapping != null)
        {
            executionManager.CurrentFunctionTypeArgumentMapping = TypeArgumentMapping;
        }

        // 6. 执行方法，传入已经求值的参数表达式
        var funcValue = methodInfo.Implementation;
        var result = funcValue.Run(executionManager, parameterValueExpressions);

        // 7. 恢复函数上下文标志
        executionManager.IsFunc = false;

        // 8. 同步字段修改
        //    方法执行完成后，将执行作用域中修改的字段值同步回实例数据
        SyncFieldsFromExecutionScope(executionManager);

        // 注意：不在这里清空 _fieldsModifiedBySetField
        // 让调用者（如 CallInit）来管理

        return result;
    }

    /// <summary>
    /// 从执行作用域同步字段修改
    /// 只同步那些通过直接赋值（field &lt;- value）而非 SetField（this.field &lt;- value）修改的字段
    /// </summary>
    private void SyncFieldsFromExecutionScope(VariateManager executionManager)
    {
        // 遍历所有实例字段，检查是否被修改
        foreach (var fieldName in InstanceData.Keys.ToList())
        {
            // 跳过已经通过SetField修改过的字段
            if (FieldsModifiedBySetField.Contains(fieldName))
            {
                continue;
            }

            try
            {
                var updatedValue = executionManager.GetValue(new LangId(fieldName));
                if (updatedValue != null)
                {
                    // 直接同步值，不需要比较
                    InstanceData[fieldName] = updatedValue;
                }
            }
            catch
            {
                // 如果字段在执行作用域中不存在，跳过
            }
        }
    }

    private LangValueType AccessCollectionItem(LangListItem listItem, VariateManager manager)
    {
        var collectionName = listItem.ListId.IdName;
        bool isInternalAccess = CheckInternalAccess(manager);

        // 查找集合字段
        if (!InstanceData.TryGetValue(collectionName, out var collectionValue))
        {
            throw new AttributeError(this, collectionName, ClassId.IdName);
        }

        // 检查访问权限
        var fieldDef = Metadata.FieldTable.LookupField(collectionName);
        if (fieldDef != null && !fieldDef.IsAccessibleFrom(isInternalAccess))
        {
            throw new AttributeError(this, collectionName, ClassId.IdName);
        }

        // 计算索引值
        var indexValue = listItem.Key.Run(manager);

        // 根据集合类型进行索引访问
        return collectionValue switch
        {
            ListLangValue list when indexValue is IntLangValue intIndex => list.Get(intIndex),
            ArrayLangValue array when indexValue is IntLangValue intIndex => array.Get(intIndex),
            DictionaryLangValue dict => dict.Get(indexValue),
            StringLangValue str when indexValue is IntLangValue intIndex => str.Get(intIndex),
            _ => throw new InvalidOperationError(listItem, $"不支持的集合类型: {collectionValue.GetType().Name}")
        };
    }

    /// <summary>
    /// 方法重载解析
    /// 根据参数数量和类型选择最匹配的方法
    /// </summary>
    private LangMethodInfo ResolveMethodOverload(List<LangMethodInfo> overloads, List<LangExpression> arguments,
        VariateManager manager)
    {
        var argCount = arguments.Count;

        // 1. 参数数量精确匹配
        var exactMatches = overloads.Where(m => m.ParameterCount == argCount).ToList();
        if (exactMatches.Count == 1)
        {
            return exactMatches[0];
        }

        // 2. 类型匹配（如果有类型注解）
        if (exactMatches.Count > 1)
        {
            return ResolveByTypeMatching(exactMatches, arguments, manager);
        }

        // 3. 兼容性匹配（允许参数数量不完全匹配，如默认参数）
        var compatibleMatches = overloads.Where(m => CanHandleArguments(m, arguments)).ToList();
        if (compatibleMatches.Count > 0)
        {
            // 在兼容的方法中，选择参数数量最接近的
            return compatibleMatches
                .OrderBy(m => Math.Abs(m.ParameterCount - argCount))
                .ThenBy(m => m.ParameterCount)
                .First();
        }

        // 4. 找不到匹配的重载
        throw new ArgumentError(Position, $"函数 '{overloads[0].MethodName}' 没有找到匹配 {argCount} 个参数的重载版本");
    }

    /// <summary>
    /// 通过类型匹配选择最佳重载
    /// </summary>
    private LangMethodInfo ResolveByTypeMatching(List<LangMethodInfo> candidates, List<LangExpression> arguments,
        VariateManager manager)
    {
        // 计算每个候选的匹配得分
        var scoredCandidates = new List<(LangMethodInfo method, int score)>();

        foreach (var candidate in candidates)
        {
            int score = 0;
            var funcValue = candidate.Implementation;

            if (funcValue.Ids != null)
            {
                for (int i = 0; i < arguments.Count && i < funcValue.Ids.Count; i++)
                {
                    var paramType = funcValue.Ids[i].AssumptionType;
                    if (string.IsNullOrEmpty(paramType))
                    {
                        score += 1; // 无类型注解，得分较低
                    }
                    else
                    {
                        // 运行参数表达式获取实际类型
                        var argValue = arguments[i].Run(manager);
                        string actualTypeName = argValue.GetType().Name;
                        if (actualTypeName.Equals(paramType, StringComparison.OrdinalIgnoreCase))
                        {
                            score += 3; // 精确匹配
                        }
                        else if (IsCompatibleType(actualTypeName, paramType))
                        {
                            score += 2; // 兼容匹配
                        }
                        else
                        {
                            score += 1; // 可转换
                        }
                    }
                }
            }

            scoredCandidates.Add((candidate, score));
        }

        // 返回得分最高的候选
        var bestMatch = scoredCandidates.OrderByDescending(x => x.score).FirstOrDefault();
        return bestMatch.method ?? candidates[0];
    }

    /// <summary>
    /// 检查方法是否能处理给定的参数
    /// </summary>
    private bool CanHandleArguments(LangMethodInfo method, List<LangExpression> arguments)
    {
        var expectedParams = method.ParameterCount;
        var actualParams = arguments.Count;

        if (actualParams > expectedParams)
        {
            return false;
        }

        // 如果实际参数数量小于期望参数数量，检查缺失的参数是否都有默认值
        if (actualParams < expectedParams)
        {
            var func = method.Implementation;
            if (func.Ids == null) return true;
            for (int i = actualParams; i < expectedParams; i++)
            {
                var parameter = func.Ids[i];
                if (parameter.DefaultValue == null)
                {
                    return false; // 缺失的参数没有默认值
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 检查类型是否兼容
    /// </summary>
    private bool IsCompatibleType(string actualType, string expectedType)
    {
        return actualType.Contains(expectedType, StringComparison.InvariantCultureIgnoreCase) ||
               expectedType.Contains(actualType, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// 检查调用上下文是否在类内部
    /// </summary>
    private bool CheckInternalAccess(VariateManager manager)
    {
        // 检查 this 指针是否指向当前实例
        try
        {
            var thisValue = manager.GetValue(new LangId("this"));
            if (ReferenceEquals(thisValue, this))
            {
                return true;
            }
        }
        catch
        {
            // manager 中没有 this
        }

        // 检查 InstanceScope 中的 this
        try
        {
            var thisValue = InstanceScope.GetValue(new LangId("this"));
            if (ReferenceEquals(thisValue, this))
            {
                return true;
            }
        }
        catch
        {
            // InstanceScope 中没有 this
        }

        return false;
    }

    /// <summary>
    /// 初始化实例（设置解释器）
    /// 兼容 V1 接口
    /// </summary>
    /// <param name="interpreter">解释器实例</param>
    public void Init(LangInterpreter interpreter)
    {
        InstanceScope.Interpreter = interpreter;
    }

    /// <summary>
    /// 获取 init 构造函数方法（支持重载）
    /// </summary>
    /// <param name="arguments">构造函数参数</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>匹配的 init 方法，如果不存在则返回 null</returns>
    private FuncLangValue? GetInitMethod(List<LangExpression> arguments, VariateManager manager)
    {
        // 首先查找名为 "init" 的方法
        var methods = Metadata.MethodTable.LookupMethod("init");

        if (methods == null || methods.Count == 0)
            return null;

        // 如果只有一个 init 方法，直接返回
        if (methods.Count == 1)
            return methods[0].Implementation;

        // 如果有多个 init 方法，进行重载解析
        var selectedMethod = ResolveMethodOverload(methods, arguments, manager);
        return selectedMethod.Implementation;
    }

    /// <summary>
    /// 调用 init 构造函数
    /// </summary>
    /// <param name="arguments">构造函数参数</param>
    /// <param name="manager">变量管理器（外部作用域，用于计算参数）</param>
    public void CallInit(List<LangExpression> arguments, VariateManager manager)
    {
        // 清空SetField修改标记（开始新的方法执行）
        FieldsModifiedBySetField.Clear();

        var initMethod = GetInitMethod(arguments, manager);
        if (initMethod == null)
        {
            return;
        }

        var parameterValues = arguments.Select(arg => arg.Run(manager)).ToList();

        // 然后创建参数值表达式（直接返回这些值，不需要再次求值）
        var parameterValueExpressions = parameterValues.Select<LangValueType, LangExpression>(val => val).ToList();

        // 为 init 方法执行创建一个混合作用域：
        // 1. 基于外部 manager（可以访问外部变量，用于访问类型定义）
        // 2. 添加实例字段和 this（可以在 init 中访问实例成员）
        var initManager = manager.NewManger();

        // 1. 设置 this 指针
        initManager.Set(new LangId("this"), this);

        // 2. 创建基础作用域用于字段
        initManager.AddChildren();

        // 3. 将所有实例字段添加到基础作用域
        //    这样参数可以在更高优先级的作用域中覆盖字段
        var fieldScopeIndex = initManager.Scopes.Count - 1;
        foreach (var (fieldName, fieldValue) in InstanceData)
        {
            // 直接在字段作用域中设置字段
            initManager.Scopes[fieldScopeIndex][fieldName] = fieldValue;
        }

        // 3.5 将类的所有方法添加到基础作用域
        //     这样 init 方法内部可以直接调用其他方法，包括从 mixin 继承的方法
        foreach (var method in Metadata.MethodTable.GetAllMethods())
        {
            // 只添加非静态方法到实例方法作用域
            if (!method.IsStatic)
            {
                initManager.Scopes[fieldScopeIndex][method.MethodName] = method.Implementation;
            }
        }

        // 4. 将类型信息添加到字段作用域（从 InstanceScope 继承）
        initManager.AddImportInfoRange(InstanceScope.ImportInfos);

        // 5. 设置函数上下文标志
        initManager.IsFunc = true;

        // 6. 执行 init 方法，传入已经求值的参数表达式
        //    由于参数已经是值对象，FuncLangValue.Run 只需要直接使用它们
        initMethod.Run(initManager, parameterValueExpressions);

        // 7. 恢复函数上下文标志
        initManager.IsFunc = false;

        // 8. 同步字段修改
        //    init 方法执行完成后，将执行作用域中修改的字段值同步回实例数据
        SyncFieldsFromExecutionScope(initManager);

        // 9. 清空SetField修改标记（方法执行结束）
        FieldsModifiedBySetField.Clear();
    }

    /// <summary>
    /// 设置字段值（用于赋值语句）
    /// </summary>
    public void SetField(string fieldName, LangValueType value, VariateManager manager)
    {
        bool isInternalAccess = CheckInternalAccess(manager);

        // 检查字段是否存在
        var fieldDef = Metadata.FieldTable.LookupField(fieldName);
        if (fieldDef == null)
        {
            throw new AttributeError(this, fieldName, ClassId.IdName);
        }

        // 检查访问权限
        if (!fieldDef.IsAccessibleFrom(isInternalAccess))
        {
            throw new AttributeError(this, fieldName, ClassId.IdName);
        }

        // 更新字段值
        InstanceData[fieldName] = value;

        // 记录这个字段已经通过SetField修改过
        FieldsModifiedBySetField.Add(fieldName);

        // 同时更新执行作用域（让 SyncFieldsFromExecutionScope 能够读取到更新后的值）
        manager.Set(new LangId(fieldName), value);

        // 也更新 InstanceScope（如果方法执行期间修改字段）
        InstanceScope.Set(new LangId(fieldName), value);
    }

    /// <summary>
    /// 获取字段值
    /// </summary>
    public LangValueType GetField(string fieldName, VariateManager manager)
    {
        bool isInternalAccess = CheckInternalAccess(manager);

        // 查找字段
        if (!InstanceData.TryGetValue(fieldName, out var fieldValue))
        {
            throw new AttributeError(this, fieldName, ClassId.IdName);
        }

        // 检查访问权限
        var fieldDef = Metadata.FieldTable.LookupField(fieldName);
        if (fieldDef != null && !fieldDef.IsAccessibleFrom(isInternalAccess))
        {
            throw new AttributeError(this, fieldName, ClassId.IdName);
        }

        return fieldValue;
    }

    /// <summary>
    /// 运行表达式（通常用于构造函数）
    /// </summary>
    public sealed override LangValueType Run(VariateManager manager)
    {
        // 在 V2 版本中，Run 方法不再负责初始化字段
        // 初始化由 InitializeFields 方法完成
        return this;
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not TypeLangValue type)
            throw new TypeError(this, "Type", otherLangValueType.GetType().Name);

        var targetTypeInfo = manager.GetAny(new LangId(type.Value ?? ""));
        if (targetTypeInfo is not TypeTemplate targetTypeTemplate)
        {
            throw new TypeError(this, "Type", otherLangValueType.GetType().Name);
        }

        // 检查类型是否兼容
        if (targetTypeTemplate.Metadata != null &&
            !Metadata.IsAssignableTo(targetTypeTemplate.Metadata, manager))
        {
            throw new TypeError(this, type.Value ?? "", ClassId.IdName,
                $"类型 '{ClassId.IdName}' 不能转换为 '{type.Value}'");
        }


        // 对于接口类型转换，返回当前实例但更新类型标识
        // 保持原始的方法实现和字段数据不变
        if (targetTypeTemplate.Metadata == null)
        {
            // 如果元数据为空，很可能是接口类型
            // 创建一个临时的接口元数据进行兼容性检查
            var tempInterfaceMetadata = new ClassMetadata(
                className: type.Value ?? "",
                isInterface: true
            );

            // 检查类型是否兼容
            if (!Metadata.IsAssignableTo(tempInterfaceMetadata, manager))
            {
                // 不兼容，转换失败，抛出TypeError（会被Operation.cs捕获并返回null）
                throw new TypeError(this, type.Value ?? "", ClassId.IdName,
                    $"类型 '{ClassId.IdName}' 不能转换为 '{type.Value}'");
            }


            return this;
        }

        if (targetTypeTemplate.Metadata.IsInterface)
        {
            // 不创建新实例，而是修改当前实例的类型信息
            // 这样可以完全保持原始的方法实现和数据

            return this;
        }
        else
        {
            // 普通类类型转换，创建目标类型的实例并复制字段
            var targetInstance = targetTypeTemplate.CreateInstanceV2(manager);
            foreach (var (fieldName, fieldValue) in InstanceData)
            {
                try
                {
                    targetInstance.SetField(fieldName, fieldValue, manager);
                }
                catch
                {
                    // 如果目标类型没有这个字段，跳过
                }
            }

            return targetInstance;
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        var fields = InstanceData.ToList();
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            builder.Append($"{(i == 0 ? "" : ",")}\"{field.Key}\":{field.Value}");
        }

        builder.Append('}');
        return builder.ToString();
    }

    public override string ToDisplayString()
    {
        return $"Class {ClassId}";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // IL 代码生成（编译模式）
        var dictType = typeof(Dictionary<string, object>);
        var constructor = dictType.GetConstructor(Type.EmptyTypes)!;

        ilGenerator.Emit(OpCodes.Newobj, constructor);

        foreach (var (fieldName, fieldValue) in InstanceData)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldstr, fieldName);

            fieldValue.LoadIlValue(ilGenerator, local);

            var valueType = fieldValue.OutputType(local);
            if (valueType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }

            var addMethod = dictType.GetMethod("Add", [typeof(string), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        }
    }

    public override Type? OutputType(LocalManager local)
    {
        return local.ClassVar.GetValueOrDefault(ClassId.IdName);
    }
}