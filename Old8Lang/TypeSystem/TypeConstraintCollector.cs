using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 类型约束收集器：遍历AST收集类型约束
/// </summary>
public class TypeConstraintCollector(TypeInferenceContext context, LocalManager localManager)
{
    /// <summary>
    /// 从函数声明收集约束
    /// </summary>
    public void CollectFromFunction(FuncInit funcInit)
    {
        if (funcInit.FuncLangValue.Ids == null)
            return;

        var funcName = funcInit.FuncLangValue.Id?.IdName ?? "anonymous";
        var parameters = funcInit.FuncLangValue.Ids;

        // 1. 收集参数约束
        for (int i = 0; i < parameters.Count; i++)
        {
            var param = parameters[i];
            var paramName = $"{funcName}$param${i}${param.IdName}";

            // 情况1：显式类型注解
            if (!string.IsNullOrEmpty(param.AssumptionType))
            {
                var paramType = ParseTypeAnnotation(param.AssumptionType);
                if (paramType != null)
                {
                    context.AddConstraint(new TypeConstraint(
                        TypeConstraintKind.Equality,
                        paramName,
                        paramType,
                        param.Position,
                        confidence: 1.0
                    ));
                }
            }
            // 情况2：默认值推断
            else if (param.DefaultValue != null)
            {
                var defaultType = param.DefaultValue.OutputType(localManager);
                if (defaultType != null && defaultType != typeof(object))
                {
                    context.AddConstraint(new TypeConstraint(
                        TypeConstraintKind.Equality,
                        paramName,
                        defaultType,
                        param.Position,
                        confidence: 0.9
                    ));
                }
            }
            // 情况3：无约束，等待从调用处推断
            else
            {
                context.AddConstraint(new TypeConstraint(
                    TypeConstraintKind.Equality,
                    paramName,
                    null, // 待推断
                    param.Position,
                    confidence: 0.0
                ));
            }
        }

        // 2. 收集返回类型约束
        var returnTypeName = $"{funcName}$return";

        // 显式返回类型注解
        if (!string.IsNullOrEmpty(funcInit.FuncLangValue.Id?.AssumptionType))
        {
            var returnType = ParseTypeAnnotation(funcInit.FuncLangValue.Id.AssumptionType);
            if (returnType != null)
            {
                context.AddConstraint(new TypeConstraint(
                    TypeConstraintKind.Equality,
                    returnTypeName,
                    returnType,
                    funcInit.Position,
                    confidence: 1.0
                ));
            }
        }
        else
        {
            // 从函数体推断返回类型
            CollectReturnConstraints(funcInit.FuncLangValue, returnTypeName);
        }
    }

    /// <summary>
    /// 从函数体收集返回类型约束
    /// </summary>
    private void CollectReturnConstraints(FuncLangValue funcValue, string returnTypeName)
    {
        var returnTypes = new List<Type>();
        CollectReturnTypes(funcValue, returnTypes);

        if (returnTypes.Count == 0)
        {
            // 无return语句，推断为void
            context.AddConstraint(new TypeConstraint(
                TypeConstraintKind.Return,
                returnTypeName,
                typeof(void),
                funcValue.Position,
                confidence: 1.0
            ));
        }
        else if (returnTypes.Count == 1)
        {
            // 单一返回类型
            context.AddConstraint(new TypeConstraint(
                TypeConstraintKind.Return,
                returnTypeName,
                returnTypes[0],
                funcValue.Position,
                confidence: 0.9
            ));
        }
        else
        {
            // 多个返回类型，尝试找到公共基类型
            var commonType = FindCommonType(returnTypes);
            context.AddConstraint(new TypeConstraint(
                TypeConstraintKind.Return,
                returnTypeName,
                commonType,
                funcValue.Position,
                confidence: 0.7
            ));
        }
    }

    /// <summary>
    /// 递归收集所有return语句的类型
    /// </summary>
    private void CollectReturnTypes(IOldLangTree? tree, List<Type> returnTypes)
    {
        if (tree is ReturnStatement returnStmt)
        {
            var returnType = returnStmt.OutputType(localManager);
            if (returnType != typeof(void))
            {
                returnTypes.Add(returnType);
            }
        }

        // 尝试遍历子节点（如果支持）
        if (tree is OldStatement stmt)
        {
            for (int i = 0; i < stmt.Count; i++)
            {
                CollectReturnTypes(stmt[i], returnTypes);
            }
        }
    }

    /// <summary>
    /// 从函数调用收集约束
    /// </summary>
    public void CollectFromFunctionCall(FunctionCallExpression callExpr, string targetFuncName)
    {
        if (callExpr.Arguments == null!)
            return;

        // 记录调用信息，用于反向推断参数类型
        var argTypes = new List<Type>();

        for (int i = 0; i < callExpr.Arguments.Count; i++)
        {
            var arg = callExpr.Arguments[i];
            var argType = arg.OutputType(localManager);

            if (argType != null)
            {
                argTypes.Add(argType);

                // 创建约束：函数的第i个参数应该兼容这个类型
                var paramName = $"{targetFuncName}$param${i}";
                context.AddConstraint(new TypeConstraint(
                    TypeConstraintKind.Call,
                    paramName,
                    argType,
                    arg.Position,
                    confidence: 0.8
                ));
            }
        }

        // 记录函数调用信息
        if (!context.FunctionCallInfo.ContainsKey(targetFuncName))
        {
            context.FunctionCallInfo[targetFuncName] = (argTypes, typeof(object));
        }
    }

    /// <summary>
    /// 从赋值语句收集约束
    /// </summary>
    public void CollectFromAssignment(SetStatement setStmt)
    {
        var varName = setStmt.Id?.IdName;
        var valueType = setStmt.Value.OutputType(localManager);

        if (valueType != null)
        {
            // 如果变量有显式类型注解，验证兼容性
            if (!string.IsNullOrEmpty(setStmt.Id?.AssumptionType))
            {
                var declaredType = ParseTypeAnnotation(setStmt.Id.AssumptionType);
                if (declaredType != null)
                {
                    context.AddConstraint(new TypeConstraint(
                        TypeConstraintKind.Equality,
                        varName ?? "",
                        declaredType,
                        setStmt.Position,
                        confidence: 1.0
                    ));
                }
            }
            else
            {
                // 从赋值推断变量类型
                context.AddConstraint(new TypeConstraint(
                    TypeConstraintKind.Assignment,
                    varName ?? "",
                    valueType,
                    setStmt.Position,
                    confidence: 0.85
                ));
            }
        }
    }

    /// <summary>
    /// 解析类型注解字符串
    /// </summary>
    private Type? ParseTypeAnnotation(string typeAnnotation)
    {
        if (string.IsNullOrEmpty(typeAnnotation))
            return null;

        try
        {
            return typeAnnotation.ToLower() switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "void" => typeof(void),
                "object" => typeof(object),
                _ when typeAnnotation.StartsWith("list<") => ParseGenericType(typeAnnotation, typeof(List<>)),
                _ when typeAnnotation.StartsWith("array<") => ParseArrayType(typeAnnotation),
                _ when typeAnnotation.StartsWith("dictionary<") => ParseGenericType(typeAnnotation,
                    typeof(Dictionary<,>)),
                _ => typeof(object)
            };
        }
        catch
        {
            return null;
        }
    }

    private Type? ParseGenericType(string typeAnnotation, Type genericTypeDefinition)
    {
        // 简化的泛型解析（完整实现应该使用TypeAnnotationManager）
        var startIndex = typeAnnotation.IndexOf('<');
        var endIndex = typeAnnotation.LastIndexOf('>');

        if (startIndex < 0 || endIndex < 0)
            return null;

        var innerType = typeAnnotation.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
        var elementType = ParseTypeAnnotation(innerType);

        if (elementType != null)
        {
            return genericTypeDefinition.MakeGenericType(elementType);
        }

        return null;
    }

    private Type? ParseArrayType(string typeAnnotation)
    {
        var startIndex = typeAnnotation.IndexOf('<');
        var endIndex = typeAnnotation.LastIndexOf('>');

        if (startIndex < 0 || endIndex < 0)
            return null;

        var innerType = typeAnnotation.Substring(startIndex + 1, endIndex - startIndex - 1).Trim();
        var elementType = ParseTypeAnnotation(innerType);

        return elementType?.MakeArrayType();
    }

    /// <summary>
    /// 查找多个类型的公共基类型
    /// </summary>
    private Type FindCommonType(List<Type> types)
    {
        if (types.Count == 0)
            return typeof(void);

        if (types.Count == 1)
            return types[0];

        // 检查是否所有类型相同
        if (types.All(t => t == types[0]))
            return types[0];

        // 检查数值类型兼容性
        var hasDouble = types.Any(t => t == typeof(double));
        var allNumeric = types.All(t => t == typeof(int) || t == typeof(double) || t == typeof(char));

        if (allNumeric && hasDouble)
            return typeof(double);

        if (allNumeric)
            return typeof(int);

        // 检查是否都是引用类型
        if (types.All(t => !t.IsValueType))
        {
            // 查找公共基类
            Type commonBase = types[0];
            foreach (var type in types.Skip(1))
            {
                commonBase = FindCommonBaseClass(commonBase, type);
                if (commonBase == typeof(object))
                    break;
            }

            return commonBase;
        }

        // 默认返回object
        return typeof(object);
    }

    private Type FindCommonBaseClass(Type? type1, Type type2)
    {
        if (type1 == null)
            return type2;

        if (type1 == type2)
            return type1;

        if (type1.IsAssignableFrom(type2))
            return type1;

        if (type2.IsAssignableFrom(type1))
            return type2;

        // 向上查找基类
        var current = type1.BaseType;
        while (current != null)
        {
            if (current.IsAssignableFrom(type2))
                return current;
            current = current.BaseType;
        }

        return typeof(object);
    }
}