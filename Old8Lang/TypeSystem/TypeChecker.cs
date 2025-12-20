using System.Collections.Concurrent;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
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
    private static readonly HashSet<string> _constVariables = new();

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
        if (_annotationManager == null)
            throw new InvalidOperationException("TypeChecker not initialized. Call Initialize() first.");
        return _annotationManager;
    }

    /// <summary>
    /// 验证函数调用时的参数类型匹配
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="argumentValues">计算后的参数值列表</param>
    /// <param name="parameters">函数参数定义列表</param>
    /// <exception cref="TypeError">当参数类型不匹配时抛出</exception>
    public static void ValidateParameterTypes(
        List<IOldLangTree> argumentExpressions,
        List<LangValueType> argumentValues,
        List<LangId> parameters)
    {
        for (int i = 0; i < Math.Min(argumentValues.Count, parameters.Count); i++)
        {
            var parameter = parameters[i];
            var argumentValue = argumentValues[i];

            // 只对有类型注解的参数进行类型检查
            if (!string.IsNullOrEmpty(parameter.AssumptionType))
            {
                var expectedType = parameter.AssumptionType;
                var actualType = GetLangValueType(argumentValue);

                if (!IsTypeCompatible(expectedType, actualType))
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
            IntLangValue => "int",
            DoubleLangValue => "double",
            StringLangValue => "string",
            BoolLangValue => "bool",
            CharLangValue => "char",
            ArrayLangValue => "array",
            ListLangValue => "list",
            DictionaryLangValue => "dict",
            FuncLangValue => "function",
            AsyncFuncLangValue => "async_func",
            GeneratorLangValue => "generator",
            AsyncGeneratorLangValue => "async_generator",
            AnyLangValue any => any.Id.IdName,
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

        // null 可以赋值给任何引用类型
        if (actualType == "null" && expectedType != "int" && expectedType != "double" && expectedType != "bool" &&
            expectedType != "char") return true;

        // 数值类型的兼容性（int 可以隐式转换为 double，但反之不行）
        if (expectedType == "double" && actualType == "int") return true; // int 可以隐式转换为 double

        // 严格类型检查：不允许字符串到数字的自动转换
        // 这是测试期望的行为

        // TODO: 可以添加更多的类型兼容性规则，比如子类型关系等

        return false;
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
            _constVariables.Add(variableName);
        }
        else
        {
            // 非 const 声明，检查是否在修改 const 变量
            if (!isInitialAssignment && _constVariables.Contains(variableName))
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
        return _constVariables.Contains(variableName);
    }

    /// <summary>
    /// 清除 const 变量记录（用于新作用域等）
    /// </summary>
    public static void ClearConstVariables()
    {
        _constVariables.Clear();
    }

    /// <summary>
    /// 验证返回值的类型兼容性
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
        if (string.IsNullOrEmpty(expectedReturnType)) return; // 没有返回类型注解，跳过检查

        var actualType = GetLangValueType(actualReturnValue);

        if (!IsTypeCompatible(expectedReturnType, actualType))
        {
            throw new TypeError(
                returnStatement,
                expectedReturnType,
                actualType,
                $"函数 '{functionName}' 返回值类型不匹配"
            );
        }
    }

    /// <summary>
    /// 注册类类型到类型假注系统
    /// </summary>
    /// <param name="className">类名称</param>
    /// <param name="baseClassName">基类名称（可选）</param>
    public static void RegisterClassType(string className, string? baseClassName = null)
    {
        if (_annotationManager == null)
            throw new InvalidOperationException("TypeChecker not initialized. Call Initialize() first.");

        _annotationManager.RegisterClassType(className, baseClassName);
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

        if (_annotationManager == null)
            return IsTypeCompatible(sourceTypeName, targetTypeName); // 回退到基本兼容性检查

        return _annotationManager.IsTypeCompatible(sourceTypeName, targetTypeName);
    }

    /// <summary>
    /// 获取类型的所有成员
    /// </summary>
    /// <param name="typeName">类型名称</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>成员字典</returns>
    public static ConcurrentDictionary<string, LangValueType> GetTypeMembers(string typeName, VariateManager manager)
    {
        if (_annotationManager == null)
            return new ConcurrentDictionary<string, LangValueType>();

        return _annotationManager.GetTypeMembers(typeName, manager);
    }
}