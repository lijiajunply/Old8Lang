using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// Super表达式类，用于表示对父类成员的调用
/// </summary>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类用于处理super关键字，支持：
/// - super.init(params) - 调用父类构造函数
/// - super.method(params) - 调用父类方法
/// - super.property - 访问父类属性
/// </remarks>
public class SuperExpression(SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 在当前上下文中解析super表达式
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>父类实例或成员的值</returns>
    /// <exception cref="InvalidOperationError">当super不在类实例中调用时抛出</exception>
    public override LangValueType Run(VariateManager manager)
    {
        // 获取当前实例（this）
        var currentInstance = manager.GetCurrentInstance();

        if (currentInstance == null)
        {
            throw new InvalidOperationError(Position, "super只能在类实例方法中使用");
        }

        // 返回一个特殊的SuperProxy对象，用于延迟解析父类成员访问
        return new SuperProxy(currentInstance, manager, Position);
    }

    public override string ToString()
    {
        return "super";
    }
}

/// <summary>
/// Super代理类，用于延迟解析父类成员访问
/// </summary>
public class SuperProxy(AnyLangValue currentInstance, VariateManager manager, SourcePosition position = default)
    : LangValueType(position)
{
    /// <summary>
    /// 处理 super.ParentClassName(args) 形式的父类构造函数调用
    /// 或 super.method(args) 形式的父类方法调用
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        return dotExpression switch
        {
            // super.ParentClassName(...) - 父类构造函数调用
            Instance instance => CallParentConstructor(instance, manager),

            // super.methodName - 父类方法访问
            LangId id => AccessParentMember(id, manager),

            _ => throw new InvalidOperationError(this, $"不支持的super表达式: {dotExpression.GetType().Name}")
        };
    }

    /// <summary>
    /// 调用父类构造函数
    /// </summary>
    private LangValueType CallParentConstructor(Instance instance, VariateManager manager)
    {
        var parentClassName = instance.Id.IdName;

        // 获取父类元数据
        var parentMetadata = GetParentMetadata(parentClassName, manager);
        if (parentMetadata == null)
        {
            throw new InvalidOperationError(this, $"找不到父类 '{parentClassName}'");
        }

        // 查找父类的构造函数（init 或与类名相同的方法）
        var initMethods = parentMetadata.MethodTable.LookupMethod("init");
        if (initMethods == null || initMethods.Count == 0)
        {
            initMethods = parentMetadata.MethodTable.LookupMethod(parentClassName);
        }

        if (initMethods == null || initMethods.Count == 0)
        {
            // 如果父类没有构造函数，返回 null（不返回 VoidLangValue）
            return new NullLangValue();
        }

        // 选择匹配的构造函数
        var initMethod = SelectMethod(initMethods, instance.Ids, manager);

        // 在当前实例的上下文中执行父类构造函数
        return ExecuteParentMethod(initMethod, instance.Ids, manager);
    }

    /// <summary>
    /// 访问父类成员
    /// </summary>
    private LangValueType AccessParentMember(LangId id, VariateManager manager)
    {
        var memberName = id.IdName;
        var parentMetadata = GetDirectParentMetadata();

        if (parentMetadata == null)
        {
            throw new InvalidOperationError(this, "当前类没有父类");
        }

        // 查找父类方法
        var methods = parentMetadata.MethodTable.LookupMethod(memberName);
        if (methods != null && methods.Count > 0)
        {
            // 返回第一个方法（如果有重载,在调用时解析）
            return methods[0].Implementation;
        }

        // 查找父类字段
        if (currentInstance.InstanceData.TryGetValue(memberName, out var fieldValue))
        {
            var fieldDef = parentMetadata.FieldTable.LookupField(memberName);
            if (fieldDef != null && fieldDef.OriginClassName == parentMetadata.ClassName)
            {
                return fieldValue;
            }
        }

        throw new AttributeError(this, memberName, parentMetadata.ClassName);
    }

    /// <summary>
    /// 获取指定名称的父类元数据
    /// </summary>
    private ClassMetadata? GetParentMetadata(string parentClassName, VariateManager manager)
    {
        // 从管理器中获取父类类型模板
        var parentType = manager.GetAny(new LangId(parentClassName));
        if (parentType is TypeTemplate typeTemplate)
        {
            return typeTemplate.BuildMetadata(manager);
        }
        return null;
    }

    /// <summary>
    /// 获取直接父类的元数据
    /// </summary>
    private ClassMetadata? GetDirectParentMetadata()
    {
        var parentClassName = currentInstance.Metadata.ParentClassName;
        if (string.IsNullOrEmpty(parentClassName))
        {
            return null;
        }

        return GetParentMetadata(parentClassName, manager);
    }

    /// <summary>
    /// 选择匹配的方法（处理重载）
    /// </summary>
    private LangMethodInfo SelectMethod(List<LangMethodInfo> methods, List<LangExpression> arguments, VariateManager manager)
    {
        if (methods.Count == 1)
        {
            return methods[0];
        }

        // 简单的重载解析：根据参数数量选择
        var argCount = arguments.Count;
        var exactMatch = methods.FirstOrDefault(m => m.ParameterCount == argCount);

        return exactMatch ?? methods[0];
    }

    /// <summary>
    /// 执行父类方法
    /// </summary>
    private LangValueType ExecuteParentMethod(LangMethodInfo methodInfo, List<LangExpression> arguments, VariateManager manager)
    {
        // 计算参数值（在调用者作用域中）
        var parameterValues = arguments.Select(arg => arg.Run(manager)).ToList();
        var parameterValueExpressions = parameterValues.Select<LangValueType, LangExpression>(val => val).ToList();

        // 创建方法执行作用域(基于调用者作用域,能访问外部变量和类型定义)
        var executionManager = manager.NewManger();

        // 1. 设置 this 指针为当前实例
        executionManager.Set(new LangId("this"), currentInstance);

        // 2. 创建基础作用域用于字段(使用与CallInit相同的模式)
        executionManager.AddChildren();

        // 3. 将所有实例字段添加到基础作用域
        var fieldScopeIndex = executionManager.Scopes.Count - 1;
        foreach (var (fieldName, fieldValue) in currentInstance.InstanceData)
        {
            // 直接在字段作用域中设置字段
            executionManager.Scopes[fieldScopeIndex][fieldName] = fieldValue;
        }

        // 4. 将类的所有方法添加到基础作用域
        foreach (var method in currentInstance.Metadata.MethodTable.GetAllMethods())
        {
            if (!method.IsStatic)
            {
                executionManager.Scopes[fieldScopeIndex][method.MethodName] = method.Implementation;
            }
        }

        // 5. 添加类型信息
        executionManager.AddImportInfoRange(currentInstance.InstanceScope.ImportInfos);

        // 6. 设置函数上下文
        executionManager.IsFunc = true;

        // 7. 执行方法
        var funcValue = methodInfo.Implementation;
        var result = funcValue.Run(executionManager, parameterValueExpressions);

        // 8. 恢复函数上下文
        executionManager.IsFunc = false;

        // 9. 同步字段修改回实例数据(使用与CallInit相同的逻辑)
        foreach (var fieldName in currentInstance.InstanceData.Keys.ToList())
        {
            // 跳过已经通过SetField修改过的字段
            if (currentInstance._fieldsModifiedBySetField.Contains(fieldName))
            {
                continue;
            }

            try
            {
                var updatedValue = executionManager.GetValue(new LangId(fieldName));
                if (updatedValue != null)
                {
                    currentInstance.InstanceData[fieldName] = updatedValue;
                }
            }
            catch
            {
                // 如果字段在执行作用域中不存在，跳过
            }
        }

        return result;
    }

    public override string ToString()
    {
        return "super";
    }
}