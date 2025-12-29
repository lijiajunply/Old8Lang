using Old8Lang.AST.Visitor;
using System.Reflection;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 原生映射 适用于有构造函数的类
/// 也可以直接包装已有的 .NET 对象实例
/// </summary>
public class NativeAnyLangValue : ImportInfo
{
    private Type? ClassType { get; set; }
    public readonly string ClassName;

    // 注册名称（用于别名功能，如果为null则使用ClassName）
    public readonly string RegisterName;

    private readonly string? DllName;
    private readonly string? Path;

    private ConstructorInfo? Constructor { get; set; }
    private object? InstanceObj { get; set; }

    private VariateManager Manager = new();

    /// <summary>
    /// 从 DLL 导入类型的构造函数
    /// </summary>
    public NativeAnyLangValue(string dllName, string className, string path, string? registerName = null)
    {
        DllName = dllName;
        ClassName = className;
        Path = path;
        RegisterName = registerName ?? className;
    }

    /// <summary>
    /// 直接包装已有对象实例的构造函数
    /// </summary>
    public NativeAnyLangValue(object nativeObject, SourcePosition position = default) : base(position)
    {
        InstanceObj = nativeObject ?? throw new ArgumentNullException(nameof(nativeObject));
        ClassType = nativeObject.GetType();
        ClassName = ClassType.Name;
        RegisterName = ClassName;
        DllName = null;
        Path = null;
    }

    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            // 尝试访问属性
            var prop = ClassType?.GetProperty(id.IdName);
            if (prop != null)
            {
                try
                {
                    // 如果有实例对象，访问实例成员；否则访问静态成员
                    var value = prop.GetValue(InstanceObj);
                    return ObjToValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationError(this, $"访问属性 '{id.IdName}' 失败: {ex.Message}");
                }
            }

            // 尝试访问字段
            var field = ClassType?.GetField(id.IdName);
            if (field != null)
            {
                try
                {
                    // 如果有实例对象，访问实例成员；否则访问静态成员
                    var value = field.GetValue(InstanceObj);
                    return ObjToValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationError(this, $"访问字段 '{id.IdName}' 失败: {ex.Message}");
                }
            }

            throw new AttributeError(this, id.IdName, ClassName);
        }

        if (dotExpression is Instance instance)
        {
            var method = ClassType?.GetMethod(instance.Id.IdName);
            if (method == null)
                throw new AttributeError(this, instance.Id.IdName, ClassName);

            try
            {
                // 计算参数值
                var arguments = instance.Ids.Select(arg => arg.Run(manager)).ToList();

                // 将 Old8Lang 值转换为 .NET 对象
                var nativeArguments = arguments.Select(ValueToObj).ToArray();

                // 直接调用方法，避免 FuncLangValue 的参数计数bug
                var result = method.Invoke(InstanceObj, nativeArguments);

                // 将结果转换为 Old8Lang 值
                return ObjToValue(result);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationError(this, $"调用方法 '{instance.Id.IdName}' 失败: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(this, $"调用方法 '{instance.Id.IdName}' 失败: {ex.Message}");
            }
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }

    public override LangValueType Run(VariateManager manager)
    {
        // 如果已经有实例对象（通过第二个构造函数创建），则直接返回
        if (InstanceObj != null && ClassType != null)
        {
            Manager = manager.Clone();
            return this;
        }

        // 否则从 DLL 加载类型
        if (Path == null || DllName == null)
        {
            throw new InvalidOperationError(this, "无法加载类型：缺少 DLL 路径或名称");
        }

        var assembly = Assembly.LoadFile(Path);
        ClassType = assembly.GetType($"{DllName}.{ClassName}")!;
        var constructors = ClassType.GetConstructors();
        if (constructors is { Length: > 0 })
            Constructor = constructors[0];
        Manager = manager.Clone();
        return this;
    }

    public void New(object[] pa)
    {
        InstanceObj = Constructor != null ? Constructor.Invoke(pa) : Activator.CreateInstance(ClassType!)!;
    }

    /// <summary>
    /// 获取包装的 .NET 对象
    /// </summary>
    public object? GetNativeObject() => InstanceObj;

    /// <summary>
    /// 获取对象的类型信息
    /// </summary>
    public Type? GetNativeType() => ClassType;

    /// <summary>
    /// 类型转换为字符串
    /// </summary>
    public override string TypeToString()
    {
        return InstanceObj != null
            ? $"NativeObject<{ClassName}>"
            : $"NativeType<{ClassName}>";
    }

    /// <summary>
    /// 显示字符串
    /// </summary>
    public override string ToDisplayString()
    {
        return InstanceObj != null
            ? $"NativeObject({InstanceObj})"
            : $"NativeType({ClassName})";
    }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString()
    {
        return InstanceObj?.ToString() ?? ClassName;
    }

    /// <summary>
    /// 获取值的实际.NET对象
    /// </summary>
    public override object GetValue()
    {
        return InstanceObj ?? new object();
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is NativeAnyLangValue other)
        {
            // 如果都有实例对象，比较实例
            if (InstanceObj != null && other.InstanceObj != null)
            {
                return InstanceObj.Equals(other.InstanceObj);
            }
            // 如果都是类型引用，比较类型
            if (InstanceObj == null && other.InstanceObj == null)
            {
                return ClassName == other.ClassName;
            }
        }
        return false;
    }

    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("NativeAnyLangValue 暂不支持 Visitor 模式访问");
    }
}