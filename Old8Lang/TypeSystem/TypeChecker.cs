using System.Collections.Concurrent;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 全局类型检查器，提供统一的类型验证和转换功能
/// 支持类型假注、多态家族和类名称注解
/// </summary>
public static class TypeChecker
{
    /// <summary>
    /// 类型假注管理器实例
    /// </summary>
    private static TypeAnnotationManager? _annotationManager;

    /// <summary>
    /// 跟踪 const 变量的集合
    /// </summary>
    private static readonly HashSet<string> ConstVariables = [];

    /// <summary>
    /// 初始化类型检查器
    /// </summary>
    /// <param name="globalManager">全局变量管理器</param>
    public static void Initialize(VariateManager globalManager)
    {
        _annotationManager = new TypeAnnotationManager(globalManager);
    }

    /// <summary>
    /// 获取类型假注管理器
    /// </summary>
    public static TypeAnnotationManager GetAnnotationManager()
    {
        if (_annotationManager is null)
            throw new InvalidOperationException("TypeChecker not initialized. Call Initialize() first.");
        return _annotationManager;
    }

    /// <summary>
    /// 验证函数调用时的参数类型匹配
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="argumentValues">计算后的参数值列表</param>
    /// <param name="parameters">函数参数定义列表</param>
    /// <param name="typeArgumentMapping">泛型类型参数映射（可选）</param>
    /// <exception cref="TypeError">当参数类型不匹配时抛出</exception>
    public static void ValidateParameterTypes(
        List<IOldLangTree> argumentExpressions,
        List<LangValueType> argumentValues,
        List<LangId> parameters,
        Dictionary<string, ITypeInfo>? typeArgumentMapping = null)
    {
        for (int i = 0; i < Math.Min(argumentValues.Count, parameters.Count); i++)
        {
            var parameter = parameters[i];
            var argumentValue = argumentValues[i];

            // 只对有类型注解的参数进行类型检查
            if (!string.IsNullOrEmpty(parameter.AssumptionType))
            {
                var expectedType = parameter.AssumptionType;

                // 如果是泛型类型参数，尝试解析为实际类型
                if (typeArgumentMapping is not null && typeArgumentMapping.TryGetValue(expectedType, out var value))
                {
                    expectedType = value.Name;
                }

                var actualType = GetLangValueType(argumentValue);

                // 使用 IsPolymorphicCompatible 检查类型兼容性（支持联合类型和交叉类型）
                // 参数顺序：actualType 在前，expectedType 在后
                if (!IsPolymorphicCompatible(actualType, expectedType))
                {
                    throw new TypeError(
                        argumentExpressions[i],
                        expectedType,
                        actualType,
                        $"参数 '{parameter.IdName}' 类型不匹配"
                    );
                }
            }
        }
    }

    /// <summary>
    /// 获取 Old8Lang 值的类型名称
    /// </summary>
    /// <param name="value">Old8Lang 值</param>
    /// <returns>类型名称</returns>
    public static string GetLangValueType(LangValueType value)
    {
        return value switch
        {
            EnumLangValue enumValue => enumValue.EnumTypeName,
            IntLangValue => "int",
            DoubleLangValue => "double",
            StringLangValue => "string",
            BoolLangValue => "bool",
            CharLangValue => "char",
            ArrayLangValue array => array.ElementType is not null ? $"array<{array.ElementType}>" : "array",
            ListLangValue list => list.ElementType is not null ? $"list<{list.ElementType}>" : "list",
            DictionaryLangValue dict => (dict.KeyType is not null && dict.ValueType is not null)
                ? $"dict<{dict.KeyType}, {dict.ValueType}>"
                : "dict",
            FuncLangValue => "function",
            AsyncFuncLangValue => "async_func",
            GeneratorLangValue => "generator",
            AsyncGeneratorLangValue => "async_generator",
            AnyLangValue any => any.ClassId.IdName,
            NullLangValue => "null",
            TupleLangValue => "tuple",
            VoidLangValue => "void",
            TaskLangValue => "task",
            TaskClassLangValue => "task_static",
            ThreadLangValue => "thread",
            TypeTemplate => "class",
            TypeLangValue => "type",
            ListComprehension => "list",
            StringTemplateValue => "string",
            ErrorLangValue => "error",
            _ => "object"
        };
    }

    /// <summary>
    /// 检查类型是否兼容
    /// </summary>
    /// <param name="expectedType">期望类型</param>
    /// <param name="actualType">实际类型</param>
    /// <returns>是否兼容</returns>
    public static bool IsTypeCompatible(string expectedType, string actualType)
    {
        // 处理 const 类型假注
        if (expectedType.StartsWith("const ", StringComparison.OrdinalIgnoreCase))
        {
            // const 类型假注的格式：const <基础类型>
            var constInnerType = expectedType.Substring(6).Trim(); // 移除 "const " 前缀
            return IsTypeCompatible(constInnerType, actualType);
        }

        // 处理单独的 const 修饰符
        if (expectedType.Equals("const", StringComparison.OrdinalIgnoreCase))
        {
            // const 可以接受任何类型
            return true;
        }

        // 完全匹配
        if (expectedType == actualType) return true;

        // any 类型可以匹配任何类型
        if (expectedType == "any") return true;

        // object 类型可以匹配任何类型（类似于 any）
        if (expectedType == "object") return true;

        // null 可以赋值给任何引用类型
        if (actualType == "null" && expectedType != "int" && expectedType != "double" && expectedType != "bool" &&
            expectedType != "char") return true;

        // 数值类型的兼容性（int 可以隐式转换为 double，但反之不行）
        if (expectedType == "double" && actualType == "int") return true; // int 可以隐式转换为 double

        // 泛型集合类型兼容性（解释器模式下）
        // 允许非泛型集合（如 "list"）匹配泛型集合类型注解（如 "list<int>"）
        if (IsGenericCollectionCompatible(expectedType, actualType))
        {
            return true;
        }

        // 严格类型检查：不允许字符串到数字的自动转换
        // 这是测试期望的行为

        // 检查接口实现兼容性
        // 只在明确知道这是一个类型检查的上下文中才进行接口检查
        if (_annotationManager is not null && expectedType != actualType &&
            IsInterfaceImplementation(expectedType, actualType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查泛型集合类型兼容性
    /// </summary>
    /// <param name="expectedType">期望类型（如 "list&lt;int&gt;"）</param>
    /// <param name="actualType">实际类型（如 "list" 或 "list&lt;int&gt;"）</param>
    /// <returns>是否兼容</returns>
    private static bool IsGenericCollectionCompatible(string expectedType, string actualType)
    {
        // 提取基础类型名（不含泛型参数）
        var expectedBase = GetBaseTypeName(expectedType);
        var actualBase = GetBaseTypeName(actualType);

        // 如果基础类型不匹配，不兼容
        if (expectedBase != actualBase)
        {
            return false;
        }

        // 如果期望类型不是泛型（如 "list"），而实际类型是泛型（如 "list<int>"），也允许
        if (!IsGenericType(expectedType) && IsGenericType(actualType))
        {
            return true;
        }

        // 如果期望类型是泛型（如 "list<int>"），而实际类型不是泛型（如 "list"）
        // 在解释器模式下允许（因为解释器模式不强制类型检查）
        if (IsGenericType(expectedType) && !IsGenericType(actualType))
        {
            // 只检查基础类型是否匹配，已在上面检查过
            return true;
        }

        // 如果两者都是泛型，需要完全匹配（在 IsTypeCompatible 的完全匹配中处理）
        return false;
    }

    /// <summary>
    /// 获取类型的基础名称（不含泛型参数）
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>基础类型名称</returns>
    private static string GetBaseTypeName(string typeName)
    {
        var genericStart = typeName.IndexOf('<');
        return genericStart > 0 ? typeName.Substring(0, genericStart) : typeName;
    }

    /// <summary>
    /// 检查是否为泛型类型
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <returns>是否为泛型类型</returns>
    private static bool IsGenericType(string typeName)
    {
        return typeName.Contains('<') && typeName.Contains('>');
    }

    /// <summary>
    /// 检查一个类是否实现了指定的接口
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <param name="className">类名称</param>
    /// <returns>是否实现该接口</returns>
    private static bool IsInterfaceImplementation(string interfaceName, string className)
    {
        if (_annotationManager is null)
            return false;

        try
        {
            // 获取接口和类的类型模板
            var interfaceType = _annotationManager.GetGlobalManager().GetAny(new LangId(interfaceName)) as TypeTemplate;
            var classType = _annotationManager.GetGlobalManager().GetAny(new LangId(className)) as TypeTemplate;

            // 确保接口确实是一个接口，类确实是一个类
            if (interfaceType is null || !interfaceType.IsInterface || classType is null || classType.IsInterface)
                return false;

            // 检查类是否直接实现了该接口
            if (classType.ImplementsNames.Contains(interfaceName))
                return true;

            // 递归检查父类是否实现了该接口
            if (!string.IsNullOrEmpty(classType.ParentClassName))
            {
                if (IsInterfaceImplementation(interfaceName, classType.ParentClassName))
                    return true;
            }

            // 递归检查接口继承（如果接口继承其他接口）
            foreach (var parentInterfaceName in interfaceType.ImplementsNames)
            {
                if (IsInterfaceImplementation(parentInterfaceName, className))
                    return true;
            }

            return false;
        }
        catch (Exception)
        {
            // 如果发生任何异常，则不认为实现了接口
            return false;
        }
    }

    /// <summary>
    /// 验证变量赋值的类型兼容性
    /// </summary>
    /// <param name="expectedType">期望的类型注解（可以为空）</param>
    /// <param name="actualValue">实际的值</param>
    /// <param name="node">AST节点，用于错误报告</param>
    /// <param name="variableName">变量名</param>
    /// <param name="isInitialAssignment">是否为首次赋值（声明时赋值）</param>
    /// <exception cref="TypeError">当类型不匹配时抛出</exception>
    public static void ValidateVariableAssignment(
        string? expectedType,
        LangValueType actualValue,
        IOldLangTree node,
        string variableName,
        bool isInitialAssignment = false)
    {
        if (string.IsNullOrEmpty(expectedType)) return; // 没有类型注解，跳过检查

        var actualType = GetLangValueType(actualValue);
        var isConstDeclaration = expectedType.StartsWith("const ", StringComparison.OrdinalIgnoreCase) ||
                                 expectedType.Equals("const", StringComparison.OrdinalIgnoreCase);

        // 检查 const 变量规则
        if (isConstDeclaration)
        {
            if (!isInitialAssignment)
            {
                throw new TypeError(
                    node,
                    expectedType,
                    actualType,
                    $"const 变量 '{variableName}' 只能初始化赋值，不能修改"
                );
            }

            // 首次声明 const 变量，将其加入 const 集合
            ConstVariables.Add(variableName);
        }
        else
        {
            // 非 const 声明，检查是否在修改 const 变量
            if (!isInitialAssignment && ConstVariables.Contains(variableName))
            {
                throw new TypeError(
                    node,
                    expectedType,
                    actualType,
                    $"不能修改 const 变量 '{variableName}'"
                );
            }
        }

        // 对于 const 变量，跳过进一步的类型兼容性检查
        if (isConstDeclaration)
        {
            return;
        }

        // 首先尝试多态兼容性检查，然后回退到基本兼容性检查
        if (!IsPolymorphicCompatible(actualType, expectedType))
        {
            throw new TypeError(
                node,
                expectedType,
                actualType,
                $"变量 '{variableName}' 赋值类型不匹配"
            );
        }
    }

    /// <summary>
    /// 检查变量是否为 const
    /// </summary>
    /// <param name="variableName">变量名</param>
    /// <returns>是否为 const 变量</returns>
    public static bool IsConstVariable(string variableName)
    {
        return ConstVariables.Contains(variableName);
    }

    /// <summary>
    /// 清除 const 变量记录（用于新作用域等）
    /// </summary>
    public static void ClearConstVariables()
    {
        ConstVariables.Clear();
    }

    /// <summary>
    /// 验证返回值的类型兼容性，并在需要时进行类型转换
    /// </summary>
    /// <param name="expectedReturnType">期望的返回类型（可以为空）</param>
    /// <param name="actualReturnValue">实际的返回值（可能被转换）</param>
    /// <param name="returnStatement">return 语句节点，用于错误报告</param>
    /// <param name="functionName">函数名</param>
    /// <param name="typeArgumentMapping">泛型类型参数映射（可选）</param>
    /// <returns>转换后的返回值（如果需要转换）或原始值</returns>
    /// <exception cref="TypeError">当类型不匹配且无法转换时抛出</exception>
    public static LangValueType ValidateAndConvertReturnType(
        string? expectedReturnType,
        LangValueType actualReturnValue,
        IOldLangTree returnStatement,
        string functionName,
        Dictionary<string, ITypeInfo>? typeArgumentMapping = null)
    {
        if (string.IsNullOrEmpty(expectedReturnType)) return actualReturnValue; // 没有返回类型注解，跳过检查

        // 如果是泛型类型参数，尝试解析为实际类型
        if (typeArgumentMapping is not null && typeArgumentMapping.TryGetValue(expectedReturnType, out var value))
        {
            expectedReturnType = value.Name;
        }

        var actualType = GetLangValueType(actualReturnValue);

        // 使用 IsPolymorphicCompatible 检查类型兼容性（支持联合类型和交叉类型）
        // 参数顺序：actualType 在前，expectedReturnType 在后
        if (IsPolymorphicCompatible(actualType, expectedReturnType))
        {
            return actualReturnValue; // 类型兼容，无需转换
        }

        // 尝试类型转换
        var convertedValue = TryConvertType(actualReturnValue, expectedReturnType);
        if (convertedValue is not null)
        {
            return convertedValue; // 转换成功
        }

        // 无法转换，抛出类型错误
        throw new TypeError(
            returnStatement,
            expectedReturnType,
            actualType,
            $"函数 '{functionName}' 返回值类型不匹配"
        );
    }

    /// <summary>
    /// 验证返回值的类型兼容性（保持向后兼容的旧方法）
    /// </summary>
    /// <param name="expectedReturnType">期望的返回类型（可以为空）</param>
    /// <param name="actualReturnValue">实际的返回值</param>
    /// <param name="returnStatement">return 语句节点，用于错误报告</param>
    /// <param name="functionName">函数名</param>
    /// <exception cref="TypeError">当类型不匹配时抛出</exception>
    public static void ValidateReturnType(
        string? expectedReturnType,
        LangValueType actualReturnValue,
        IOldLangTree returnStatement,
        string functionName)
    {
        // 调用新方法但忽略返回值（仅用于类型检查）
        ValidateAndConvertReturnType(expectedReturnType, actualReturnValue, returnStatement, functionName);
    }

    /// <summary>
    /// 尝试将值转换为目标类型
    /// </summary>
    /// <param name="value">要转换的值</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的值，如果无法转换则返回 null</returns>
    private static LangValueType? TryConvertType(LangValueType value, string targetType)
    {
        try
        {
            // 转换为 string 类型
            if (targetType.Equals("string", StringComparison.OrdinalIgnoreCase))
            {
                return StringLangValue.Create(value.ToString());
            }

            // 转换为 int 类型
            if (targetType.Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                if (value is StringLangValue strValue && int.TryParse(strValue.Value, out var intVal))
                {
                    return IntLangValue.Create(intVal);
                }

                if (value is DoubleLangValue dblValue)
                {
                    return IntLangValue.Create((int)dblValue.Value);
                }
            }

            // 转换为 double 类型
            if (targetType.Equals("double", StringComparison.OrdinalIgnoreCase))
            {
                if (value is IntLangValue intValue)
                {
                    return DoubleLangValue.Create(intValue.Value);
                }

                if (value is StringLangValue strValue && double.TryParse(strValue.Value, out var dblVal))
                {
                    return DoubleLangValue.Create(dblVal);
                }
            }

            // 转换为 bool 类型
            if (targetType.Equals("bool", StringComparison.OrdinalIgnoreCase))
            {
                if (value is StringLangValue strValue && bool.TryParse(strValue.Value, out var boolVal))
                {
                    return BoolLangValue.Create(boolVal);
                }

                if (value is IntLangValue intValue)
                {
                    return BoolLangValue.Create(intValue.Value != 0);
                }
            }

            return null; // 无法转换
        }
        catch
        {
            return null; // 转换失败
        }
    }

    /// <summary>
    /// 注册类类型到类型假注系统
    /// </summary>
    /// <param name="className">类名称</param>
    /// <param name="baseClassName">基类名称（可选）</param>
    /// <param name="interfaceNames">实现的接口列表（可选）</param>
    public static void RegisterClassType(string className, string? baseClassName = null,
        List<string>? interfaceNames = null)
    {
        if (_annotationManager is null)
            throw new InvalidOperationException("TypeChecker not initialized. Call Initialize() first.");

        _annotationManager.RegisterClassType(className, baseClassName, interfaceNames);
    }

    /// <summary>
    /// 注册接口类型到类型假注系统
    /// </summary>
    /// <param name="interfaceName">接口名称</param>
    /// <param name="parentInterfaceNames">父接口列表（可选）</param>
    public static void RegisterInterfaceType(string interfaceName, List<string>? parentInterfaceNames = null)
    {
        if (_annotationManager is null)
            throw new InvalidOperationException("TypeChecker not initialized. Call Initialize() first.");

        _annotationManager.RegisterInterfaceType(interfaceName, parentInterfaceNames);
    }

    /// <summary>
    /// 注册枚举类型到类型假注系统
    /// </summary>
    /// <param name="enumName">枚举名称</param>
    /// <param name="members">枚举成员名称列表</param>
    public static void RegisterEnumType(string enumName, List<string> members)
    {
        if (_annotationManager is null)
            throw new InvalidOperationException("TypeChecker not initialized. Call Initialize() first.");

        _annotationManager.RegisterEnumType(enumName, members);
    }

    /// <summary>
    /// 检查类型是否兼容（支持多态）
    /// </summary>
    /// <param name="sourceTypeName">源类型名称</param>
    /// <param name="targetTypeName">目标类型名称</param>
    /// <returns>是否兼容</returns>
    public static bool IsPolymorphicCompatible(string sourceTypeName, string targetTypeName)
    {
        // 如果目标类型涉及 const，使用基本兼容性检查
        if (targetTypeName.StartsWith("const ", StringComparison.OrdinalIgnoreCase) ||
            targetTypeName.Equals("const", StringComparison.OrdinalIgnoreCase))
        {
            return IsTypeCompatible(sourceTypeName, targetTypeName);
        }

        return _annotationManager?.IsTypeCompatible(sourceTypeName, targetTypeName) ??
               IsTypeCompatible(sourceTypeName, targetTypeName);
    }

    /// <summary>
    /// 获取类型的所有成员
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>成员字典</returns>
    public static ConcurrentDictionary<string, LangValueType> GetTypeMembers(string typeName, VariateManager manager)
    {
        if (_annotationManager is null)
            return new ConcurrentDictionary<string, LangValueType>();

        return _annotationManager.GetTypeMembers(typeName, manager);
    }
}