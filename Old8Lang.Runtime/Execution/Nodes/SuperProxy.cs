using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression;

/// <summary>
/// SuperProxy 运行时实现（阶段 1 从 AST 迁出）。
/// </summary>
/// <summary>
/// Super代理类，用于延迟解析父类成员访问
/// </summary>
public partial class SuperProxy(AnyLangValue currentInstance, VariateManager variateManager, SourcePosition position = default)
    : LangValueType(position)
{
    /// <summary>
    /// 处理 super.ParentClassName(args) 形式的父类构造函数调用
    /// 或 super.method(args) 形式的父类方法调用
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is Instance instance)
        {
            // 只有 super.init(...) / super.父类名(...) 才是父类构造函数调用。
            // 其它方法名必须按普通父类方法调用处理，否则 super.speak() 会被误当成构造函数
            // （表现为去调用父类的 init，参数个数不符就报「缺少实参」，或者静默返回 null）。
            var methodName = instance.Id.IdName;
            var parentMetadata = GetDirectParentMetadata();

            if (parentMetadata is not null &&
                (methodName == "init" || methodName == parentMetadata.ClassName))
            {
                return CallParentConstructor(instance, manager);
            }

            return CallParentMethod(instance, manager);
        }

        // super.methodName - 父类方法/字段访问
        if (dotExpression is LangId id)
        {
            return AccessParentMember(id);
        }

        throw new InvalidOperationError(this, $"不支持的super表达式: {dotExpression.GetType().Name}");
    }

    /// <summary>
    /// 调用父类的普通方法：super.method(args)。
    /// </summary>
    internal LangValueType CallParentMethod(Instance instance, VariateManager manager)
    {
        var parentMetadata = GetDirectParentMetadata()
                             ?? throw new InvalidOperationError(this, "当前类没有父类");

        var methods = parentMetadata.MethodTable.LookupMethod(instance.Id.IdName);
        if (methods is null || methods.Count == 0)
        {
            throw new AttributeError(this, instance.Id.IdName, parentMetadata.ClassName);
        }

        var method = SelectMethod(methods, instance.Ids);
        return ExecuteParentMethod(method, parentMetadata, instance.Ids, manager);
    }

    /// <summary>
    /// 调用父类构造函数
    /// </summary>
    internal LangValueType CallParentConstructor(Instance instance, VariateManager manager)
    {
        // 获取父类元数据（使用当前执行类的直接父类）
        var parentMetadata = GetDirectParentMetadata();
        if (parentMetadata is null)
        {
            throw new InvalidOperationError(this, "当前类没有父类");
        }

        // 查找父类的构造函数（init 或与类名相同的方法）
        var initMethods = parentMetadata.MethodTable.LookupMethod("init");
        if (initMethods is null || initMethods.Count == 0)
        {
            initMethods = parentMetadata.MethodTable.LookupMethod(parentMetadata.ClassName);
        }

        if (initMethods is null || initMethods.Count == 0)
        {
            // 如果父类没有构造函数，返回 null（不返回 VoidLangValue）
            return new NullLangValue();
        }

        // 选择匹配的构造函数
        var initMethod = SelectMethod(initMethods, instance.Ids);

        // 在当前实例的上下文中执行父类构造函数
        return ExecuteParentMethod(initMethod, parentMetadata, instance.Ids, manager);
    }

    /// <summary>
    /// 访问父类成员
    /// </summary>
    internal LangValueType AccessParentMember(LangId id)
    {
        var memberName = id.IdName;
        var parentMetadata = GetDirectParentMetadata();

        if (parentMetadata is null)
        {
            throw new InvalidOperationError(this, "当前类没有父类");
        }

        // 查找父类方法
        var methods = parentMetadata.MethodTable.LookupMethod(memberName);
        if (methods is { Count: > 0 })
        {
            // 返回第一个方法（如果有重载,在调用时解析）
            return methods[0].Implementation;
        }

        // 查找父类字段
        if (currentInstance.InstanceData.TryGetValue(memberName, out var fieldValue))
        {
            var fieldDef = parentMetadata.FieldTable.LookupField(memberName);
            if (fieldDef is not null && fieldDef.OriginClassName == parentMetadata.ClassName)
            {
                return fieldValue;
            }
        }

        throw new AttributeError(this, memberName, parentMetadata.ClassName);
    }

    /// <summary>
    /// 获取指定名称的父类元数据
    /// </summary>
    internal ClassMetadata? GetParentMetadata(string parentClassName, VariateManager manager)
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
    /// 获取直接父类的元数据（沿当前正在执行方法所属类向上解析，避免三层及以上继承无限递归）
    /// </summary>
    internal ClassMetadata? GetDirectParentMetadata()
    {
        var executingClassName = variateManager.CurrentExecutingClassName;
        string? parentClassName;

        if (!string.IsNullOrEmpty(executingClassName) &&
            !string.Equals(executingClassName, currentInstance.Metadata.ClassName, StringComparison.Ordinal))
        {
            var executingMetadata = GetParentMetadata(executingClassName, variateManager);
            parentClassName = executingMetadata?.ParentClassName;
        }
        else
        {
            parentClassName = currentInstance.Metadata.ParentClassName;
        }

        if (string.IsNullOrEmpty(parentClassName))
        {
            return null;
        }

        return GetParentMetadata(parentClassName, variateManager);
    }

    /// <summary>
    /// 选择匹配的方法（处理重载）
    /// </summary>
    internal LangMethodInfo SelectMethod(List<LangMethodInfo> methods, List<LangExpression> arguments)
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
    internal LangValueType ExecuteParentMethod(
        LangMethodInfo methodInfo,
        ClassMetadata parentMetadata,
        List<LangExpression> arguments,
        VariateManager manager)
    {
        // 计算参数值（在调用者作用域中）
        var parameterValues = arguments.Select(arg => arg.Accept(RuntimeVisitors.CreateInterpreter(manager))).ToList();
        var parameterValueExpressions = parameterValues.Select<LangValueType, LangExpression>(val => val).ToList();

        // 创建方法执行作用域(基于调用者作用域,能访问外部变量和类型定义)
        var executionManager = manager.NewManger();
        // 将当前执行类推进到被调用方法所属的父类，使父类内部再调 super 时继续向上解析祖类
        executionManager.CurrentExecutingClassName = methodInfo.OriginClassName ?? parentMetadata.ClassName;

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
            if (currentInstance.FieldsModifiedBySetField.Contains(fieldName))
            {
                continue;
            }

            try
            {
                var updatedValue = executionManager.GetValue(new LangId(fieldName));
                if (updatedValue is not null)
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
