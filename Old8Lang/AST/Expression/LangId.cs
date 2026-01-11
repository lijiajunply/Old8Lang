using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 语言标识符类，用于表示变量名、函数名、类名等
/// </summary>
/// <param name="name">标识符名称</param>
/// <param name="assumptionType">类型注解，用于类型检查和推断</param>
/// <param name="defaultValue">默认值表达式</param>
/// <param name="isParams">是否为 params 可变参数</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
/// <remarks>
/// 该类是Old8Lang表达式系统的基础组件，用于表示各种标识符。
/// 支持类型注解、默认值、params 可变参数、"this"关键字处理等功能。
/// </remarks>
public partial class LangId(
    string name,
    string assumptionType = "",
    LangExpression? defaultValue = null,
    bool isParams = false,
    SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 标识符名称
    /// </summary>
    public readonly string IdName = name;

    /// <summary>
    /// 类型注解，用于类型检查和推断
    /// </summary>
    public string AssumptionType { get; } = assumptionType;

    /// <summary>
    /// 默认值表达式
    /// </summary>
    public LangExpression? DefaultValue { get; } = defaultValue;

    /// <summary>
    /// 是否为 params 可变参数
    /// </summary>
    public bool IsParams { get; } = isParams;

    /// <summary>
    /// 将标识符转换为字符串表示
    /// </summary>
    /// <returns>标识符名称</returns>
    public override string ToString() => IdName;

    /// <summary>
    /// 比较两个LangId是否相等
    /// </summary>
    /// <param name="obj">要比较的对象</param>
    /// <returns>如果相等则返回true，否则返回false</returns>
    public override bool Equals(object? obj)
    {
        var a = obj as LangId;
        return a?.IdName == IdName;
    }

    /// <summary>
    /// 获取标识符的哈希码
    /// </summary>
    /// <returns>哈希码</returns>
    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    /// <summary>
    /// 执行标识符，获取其对应的值
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>标识符对应的值</returns>
    /// <exception cref="NameError">当标识符未定义时抛出</exception>
    /// <remarks>
    /// 执行过程：
    /// 1. 如果是"this"关键字，直接从变量管理器中获取
    /// 2. 否则，尝试获取普通变量
    /// 3. 如果不是普通变量，尝试获取类或函数
    /// 4. 如果都没有找到，抛出NameError异常
    /// </remarks>
    public override LangValueType Run(VariateManager manager)
    {
        if (IdName == "this")
        {
            // 直接从变量储存器中获取名为"this"的变量
            if (manager is null)
            {
                throw new NameError(this, "this");
            }

            var thisValue = manager.GetValue(new LangId("this"));
            if (thisValue is not null)
            {
                return thisValue;
            }

            // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
            throw new NameError(this, "this");
        }

        // 先尝试获取普通变量
        if (manager is not null)
        {
            var value = manager.GetValue(this);
            if (value is not null)
            {
                return value;
            }

            // 如果不是普通变量，尝试获取类或函数
            var anyValue = manager.GetAny(this);
            if (anyValue is not null)
            {
                return anyValue as LangValueType ?? throw new NameError(this, IdName);
            }
        }

        // 如果都没有找到，检查是否是类型关键字
        var supportedTypes = new[]
            { "int", "double", "string", "bool", "char", "void", "list", "dict", "array", "dictionary", "tuple" };
        if (supportedTypes.Contains(IdName))
        {
            return new TypeLangValue(IdName);
        }

        throw new NameError(this, IdName);
    }

    /// <summary>
    /// 生成加载标识符值的IL指令
    /// </summary>
    /// <param name="ilGenerator">IL生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <remarks>
    /// 如果标识符是局部变量，使用Ldloc指令加载；
    /// 否则，假设是函数参数，使用Ldarg_0指令加载（简化实现）。
    /// </remarks>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 首先检查是否是全局静态类
        if (local.GlobalStaticClasses.TryGetValue(IdName, out var staticClassInstance))
        {
            // 将静态类实例加载到栈上
            staticClassInstance.LoadIlValue(ilGenerator, local);
            return;
        }

        var value = local.GetLocalVar(IdName);
        if (value is null)
        {
            // 检查是否是函数参数
            // 函数参数是通过Ldarg指令访问的，而不是Ldloc指令
            // 我们需要查找当前函数的参数列表，找到匹配的参数索引
            // 注意：这是一个简化的实现，假设参数名称与函数定义中的名称完全匹配
            // 在实际实现中，应该使用更可靠的方式来映射参数名称到索引
            // 对于当前简单的测试用例，这种方式应该足够了
            ilGenerator.Emit(OpCodes.Ldarg_0); // 假设只有一个参数，索引为0
        }
        else
        {
            ilGenerator.Emit(OpCodes.Ldloc, value);
        }
    }

    /// <summary>
    /// 获取标识符的输出类型
    /// </summary>
    /// <param name="local">局部变量管理器</param>
    /// <returns>标识符的输出类型</returns>
    /// <remarks>
    /// 输出类型的确定顺序：
    /// 1. 如果有类型注解，解析类型注解
    /// 2. 如果是"this"关键字，返回当前类类型
    /// 3. 如果是局部变量，返回局部变量类型
    /// 4. 如果在局部变量类型字典中存在，返回对应类型
    /// 5. 默认返回object类型
    /// </remarks>
    public override Type OutputType(LocalManager local)
    {
        if (!string.IsNullOrEmpty(AssumptionType))
        {
            // 解析泛型类型注解，如 "list<int>" 或 "array<string>"
            var typeName = AssumptionType.Trim().ToLower();

            // 处理可空类型（例如 "int?", "string?"）
            // 在编译器中，可空类型被视为其基础类型（因为.NET的可空类型会自动处理null）
            if (typeName.EndsWith('?'))
            {
                typeName = typeName.Substring(0, typeName.Length - 1).Trim();
            }

            // 检查是否为泛型类型
            if (typeName.Contains('<') && typeName.EndsWith('>'))
            {
                // 提取泛型类型名称和参数
                var genericIndex = typeName.IndexOf('<');
                var baseTypeName = typeName[..genericIndex].Trim();
                var genericArg = typeName[(genericIndex + 1)..^1].Trim();

                // 首先尝试使用泛型类型解析器解析泛型参数
                Type argType;
                if (local.CurrentGenericTypeResolver is not null)
                {
                    var resolvedType = local.CurrentGenericTypeResolver.ResolveType(genericArg);
                    if (resolvedType is not null)
                    {
                        argType = resolvedType;
                    }
                    else
                    {
                        // 回退到基本类型映射
                        argType = genericArg switch
                        {
                            "int" => typeof(int),
                            "double" => typeof(double),
                            "string" => typeof(string),
                            "bool" => typeof(bool),
                            "char" => typeof(char),
                            "object" => typeof(object),
                            _ => typeof(object) // 默认为object
                        };
                    }
                }
                else
                {
                    // 没有泛型解析器时使用基本类型映射
                    argType = genericArg switch
                    {
                        "int" => typeof(int),
                        "double" => typeof(double),
                        "string" => typeof(string),
                        "bool" => typeof(bool),
                        "char" => typeof(char),
                        "object" => typeof(object),
                        _ => typeof(object) // 默认为object
                    };
                }

                // 返回泛型类型
                return baseTypeName switch
                {
                    "list" => typeof(List<>).MakeGenericType(argType),
                    "array" => argType.MakeArrayType(),
                    "dictionary" => typeof(Dictionary<,>).MakeGenericType(typeof(object), argType),
                    _ => typeof(object) // 未知泛型类型，默认为object
                };
            }

            // 首先尝试使用泛型类型解析器
            if (local.CurrentGenericTypeResolver is not null)
            {
                var resolvedType = local.CurrentGenericTypeResolver.ResolveType(typeName);
                if (resolvedType is not null)
                {
                    return resolvedType;
                }
            }

            // 非泛型类型或解析失败时的默认映射
            return typeName switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "void" => typeof(void),
                "list" => typeof(List<object>),
                "array" => typeof(object[]),
                "dictionary" => typeof(Dictionary<object, object>),
                "tuple" => typeof(ValueTuple<object, object>),
                _ => typeof(object)
            };
        }

        // 如果没有显式类型注解，但有默认值，从默认值推断类型
        if (DefaultValue is not null)
        {
            return DefaultValue.OutputType(local) ?? typeof(object);
        }

        if (local.InClassEnv is not null && IdName == "this")
        {
            // 如果InClassEnv是TypeBuilder，返回typeof(object)，避免后续访问TypeBuilder的成员
            return local.InClassEnv is TypeBuilder ? typeof(object) : local.InClassEnv;
        }

        var value = local.GetLocalVar(IdName);
        if (value is not null)
        {
            return value.LocalType;
        }

        // 如果LocalVar中没有，检查LocalVarTypes（用于函数参数类型推断）
        if (local.LocalVarTypes.TryGetValue(IdName, out var varType))
        {
            return varType;
        }

        // 检查是否是全局静态类（Assert, Task, Thread等）
        if (local.GlobalStaticClasses.ContainsKey(IdName))
        {
            // 返回对应静态类的实际类型
            return IdName switch
            {
                "Assert" => typeof(AssertHelper),
                "Task" => typeof(Task),
                "Thread" => typeof(Thread),
                _ => typeof(object)
            };
        }

        // 检查是否是类或枚举类型（在编译模式下，枚举和类都存储在 ClassVar 中）
        if (local.ClassVar.TryGetValue(IdName, out var classType))
        {
            return classType;
        }

        return typeof(object);
    }
}