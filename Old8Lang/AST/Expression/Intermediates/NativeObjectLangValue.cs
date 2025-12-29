using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Visitor;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 包装任意 .NET 对象的 Old8Lang 值类型
/// 用于在 Old8Lang 中使用 C# 自定义类实例
/// </summary>
public class NativeObjectLangValue : LangValueType
{
    /// <summary>
    /// 被包装的 .NET 对象
    /// </summary>
    public object NativeObject { get; }

    /// <summary>
    /// 对象的类型信息
    /// </summary>
    public Type NativeType { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="nativeObject">要包装的 .NET 对象</param>
    /// <param name="position">源代码位置</param>
    public NativeObjectLangValue(object nativeObject, SourcePosition position = default)
        : base(position)
    {
        NativeObject = nativeObject ?? throw new ArgumentNullException(nameof(nativeObject));
        NativeType = nativeObject.GetType();
    }

    /// <summary>
    /// 成员访问（Dot 操作符）
    /// 支持：
    /// 1. 属性访问：obj.Property
    /// 2. 字段访问：obj.Field
    /// 3. 方法调用：obj.Method()
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        return dotExpression switch
        {
            // 1. 属性或字段访问：obj.name
            LangId id => AccessMember(id),

            // 2. 方法调用：obj.method(args)
            Instance instance => CallMethod(instance, manager),

            // 3. 其他表达式（不常见）
            _ => throw new InvalidOperationError(this, $"不支持的成员访问表达式: {dotExpression.GetType().Name}")
        };
    }

    /// <summary>
    /// 访问属性或字段
    /// </summary>
    private LangValueType AccessMember(LangId id)
    {
        var memberName = id.IdName;

        // 1. 尝试访问属性
        var property = NativeType.GetProperty(memberName);
        if (property != null)
        {
            try
            {
                var value = property.GetValue(NativeObject);
                return ObjToValue(value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(this, $"访问属性 '{memberName}' 失败: {ex.Message}");
            }
        }

        // 2. 尝试访问字段
        var field = NativeType.GetField(memberName);
        if (field != null)
        {
            try
            {
                var value = field.GetValue(NativeObject);
                return ObjToValue(value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(this, $"访问字段 '{memberName}' 失败: {ex.Message}");
            }
        }

        // 3. 找不到成员
        throw new AttributeError(this, memberName, NativeType.Name);
    }

    /// <summary>
    /// 调用方法
    /// </summary>
    private LangValueType CallMethod(Instance instance, VariateManager manager)
    {
        var methodName = instance.Id.IdName;

        // 查找方法
        var method = NativeType.GetMethod(methodName);
        if (method == null)
        {
            throw new AttributeError(this, methodName, NativeType.Name);
        }

        try
        {
            // 计算参数值
            var arguments = instance.Ids.Select(arg => arg.Run(manager)).ToList();

            // 将 Old8Lang 值转换为 .NET 对象
            var nativeArguments = arguments.Select(ValueToObj).ToArray();

            // 调用方法
            var result = method.Invoke(NativeObject, nativeArguments);

            // 将结果转换为 Old8Lang 值
            return ObjToValue(result);
        }
        catch (TargetInvocationException ex)
        {
            // 展开反射调用异常
            throw new InvalidOperationError(this, $"调用方法 '{methodName}' 失败: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError(this, $"调用方法 '{methodName}' 失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 类型转换为字符串
    /// </summary>
    public override string TypeToString()
    {
        return $"NativeObject<{NativeType.Name}>";
    }

    /// <summary>
    /// 显示字符串
    /// </summary>
    public override string ToDisplayString()
    {
        return $"NativeObject({NativeObject})";
    }

    /// <summary>
    /// 字符串表示
    /// </summary>
    public override string ToString()
    {
        return NativeObject.ToString() ?? "";
    }

    /// <summary>
    /// 获取值的实际.NET对象
    /// </summary>
    public override object GetValue()
    {
        return NativeObject;
    }

    /// <summary>
    /// 相等比较
    /// </summary>
    public override bool Equal(LangValueType? otherValueType)
    {
        if (otherValueType is NativeObjectLangValue other)
        {
            return NativeObject.Equals(other.NativeObject);
        }

        return false;
    }

    /// <summary>
    /// 接受 Visitor
    /// </summary>
    public override TResult Accept<TResult>(IVisitor<TResult> visitor)
    {
        throw new NotSupportedException("NativeObjectLangValue 暂不支持 Visitor 模式访问");
    }

    /// <summary>
    /// 加载 IL 值（编译模式）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 在 IL 中，我们只能加载 null 或者尝试序列化对象
        // 对于运行时创建的 .NET 对象，无法直接在编译时生成 IL
        ilGenerator.Emit(OpCodes.Ldnull);
    }

    /// <summary>
    /// 输出类型（编译模式）
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        return NativeType;
    }
}
