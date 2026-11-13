using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.AnyValues;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 类型值
/// </summary>
public partial class TypeLangValue : LangValueType
{
    private readonly LangExpression? _expression;
    public string? Value { get; private set; }

    /// <summary>
    /// 类型模板引用（懒加载）
    /// </summary>
    private TypeTemplate? _typeTemplate;

    /// <summary>
    /// 类型元数据引用（懒加载）
    /// </summary>
    private ClassMetadata? _metadata;

    public TypeLangValue(LangExpression expression) => _expression = expression;
    public TypeLangValue(string value) => Value = value;

    /// <summary>
    /// 从 TypeTemplate 创建 TypeLangValue
    /// </summary>
    public TypeLangValue(TypeTemplate template)
    {
        _typeTemplate = template;
        Value = template.ClassName;
    }

    /// <summary>
    /// 从 ClassMetadata 创建 TypeLangValue
    /// </summary>
    public TypeLangValue(ClassMetadata metadata)
    {
        _metadata = metadata;
        Value = metadata.ClassName;
    }

    public override LangValueType Run(VariateManager manager)
    {
        var result = _expression?.Run(manager);
        if (result is null) throw new InvalidOperationError(this, "类型表达式求值失败");
        Value = result.TypeToString();
        return this;
    }

    /// <summary>
    /// 懒加载获取 TypeTemplate
    /// </summary>
    public TypeTemplate? GetTypeTemplate(VariateManager manager)
    {
        if (_typeTemplate is not null)
            return _typeTemplate;

        if (Value is null)
            return null;

        _typeTemplate = TypeTemplate.FindType(Value);
        return _typeTemplate;
    }

    /// <summary>
    /// 懒加载获取 ClassMetadata
    /// </summary>
    public ClassMetadata? GetMetadata(VariateManager manager)
    {
        if (_metadata is not null)
            return _metadata;

        var template = GetTypeTemplate(manager);
        if (template is null)
            return null;

        _metadata = template.BuildMetadata(manager);
        return _metadata;
    }

    // ==================== 反射 API 方法 ====================

    /// <summary>
    /// 获取所有方法名列表
    /// </summary>
    public List<string> GetMethodNames(VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        if (metadata is null)
            throw new InvalidOperationError(this, $"无法获取类型 {Value} 的元数据");

        return metadata.MethodTable.GetAllMethodNames();
    }

    /// <summary>
    /// 获取所有字段名列表
    /// </summary>
    public List<string> GetFieldNames(VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        if (metadata is null)
            throw new InvalidOperationError(this, $"无法获取类型 {Value} 的元数据");

        return metadata.FieldTable.GetAllFieldNames();
    }

    /// <summary>
    /// 获取指定方法信息
    /// </summary>
    public LangMethodInfo? GetMethod(string methodName, VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        if (metadata is null)
            throw new InvalidOperationError(this, $"无法获取类型 {Value} 的元数据");

        return metadata.MethodTable.FindMethod(methodName);
    }

    /// <summary>
    /// 获取指定字段信息
    /// </summary>
    public FieldDefinition? GetField(string fieldName, VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        if (metadata is null)
            throw new InvalidOperationError(this, $"无法获取类型 {Value} 的元数据");

        return metadata.FieldTable.FindField(fieldName);
    }

    /// <summary>
    /// 创建类型实例
    /// </summary>
    public AnyLangValue CreateInstance(VariateManager manager, List<LangExpression>? args = null)
    {
        var template = GetTypeTemplate(manager);
        if (template is null)
            throw new InvalidOperationError(this, $"无法找到类型 {Value}");

        var instance = template.CreateInstance(manager);

        // 如果提供了参数，调用 init 方法
        if (args is not null && args.Count > 0)
        {
            var initInstance = new Instance(new LangId("init"), args);
            instance.Dot(initInstance, manager);
        }
        else
        {
            instance.Init(manager.Interpreter);
        }

        return instance;
    }

    /// <summary>
    /// 检查类型兼容性（是否可以从 other 类型赋值）
    /// </summary>
    public bool IsAssignableFrom(TypeLangValue other, VariateManager manager)
    {
        var thisMetadata = GetMetadata(manager);
        var otherMetadata = other.GetMetadata(manager);

        if (thisMetadata is null || otherMetadata is null)
            return false;

        return otherMetadata.IsAssignableTo(thisMetadata, manager);
    }

    /// <summary>
    /// 获取父类型
    /// </summary>
    public TypeLangValue? GetBaseType(VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        if (metadata is null || metadata.ParentClassName is null)
            return null;

        return new TypeLangValue(metadata.ParentClassName);
    }

    /// <summary>
    /// 获取实现的接口列表
    /// </summary>
    public List<TypeLangValue> GetInterfaces(VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        if (metadata is null)
            return [];

        return metadata.InterfaceNames
            .Select(name => new TypeLangValue(name))
            .ToList();
    }

    // ==================== 类型属性 ====================

    /// <summary>
    /// 语言内建类型：规范化名字 → 可接受的写法（语言关键字与 <see cref="LangValueType.TypeToString"/> 的写法）
    /// </summary>
    /// <remarks>
    /// <c>GetType</c> 原先只认用户类（走 <c>TypeTemplate</c> 注册表），
    /// 于是 <c>GetType("int")</c> 报「找不到类型: int」，解释器与虚拟机还不一致。
    /// 这张表让内建类型也能取到类型值，<c>IsPrimitive()</c> 才有意义。
    /// </remarks>
    private static readonly Dictionary<string, string> BuiltinTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["int"] = "Int",
        ["integer"] = "Int",
        ["double"] = "Double",
        ["float"] = "Double",
        ["bool"] = "Bool",
        ["boolean"] = "Bool",
        ["char"] = "Char",
        ["string"] = "String",
        ["null"] = "Null",
        ["array"] = "Array",
        ["list"] = "List",
        ["dict"] = "Dictionary",
        ["dictionary"] = "Dictionary",
        ["tuple"] = "Tuple"
    };

    /// <summary>
    /// 把内建类型名解析成规范名（大小写不敏感，同时接受语言关键字写法）
    /// </summary>
    /// <param name="name">用户书写的类型名，如 <c>int</c> / <c>Int</c> / <c>dict</c></param>
    /// <param name="canonicalName">规范化后的类型名，如 <c>Int</c> / <c>Dictionary</c></param>
    /// <returns>是内建类型时返回 true</returns>
    public static bool TryResolveBuiltinTypeName(string? name, out string canonicalName)
    {
        canonicalName = "";
        return name is not null && BuiltinTypeNames.TryGetValue(name, out canonicalName!);
    }

    /// <summary>
    /// 是否为类类型
    /// </summary>
    public bool IsClass(VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        return metadata is not null && !metadata.IsInterface && !metadata.IsMixin;
    }

    /// <summary>
    /// 是否为接口类型
    /// </summary>
    public bool IsInterface(VariateManager manager)
    {
        var metadata = GetMetadata(manager);
        return metadata?.IsInterface ?? false;
    }

    /// <summary>
    /// 是否为基本类型名
    /// </summary>
    /// <remarks>
    /// 集合类型名（<c>Array</c> / <c>List</c> / <c>Dictionary</c> / <c>Tuple</c>）不算基本类型。
    /// <c>Float</c> 是历史遗留写法，等价于 <c>Double</c>。
    /// </remarks>
    public static bool IsPrimitiveTypeName(string? typeName)
    {
        return typeName is "Int" or "Double" or "Float" or "String" or "Bool" or "Char" or "Null";
    }

    /// <summary>
    /// 是否为基本类型
    /// </summary>
    public bool IsPrimitive() => IsPrimitiveTypeName(Value);

    /// <summary>
    /// 是否为泛型类型
    /// </summary>
    public bool IsGeneric(VariateManager manager)
    {
        var template = GetTypeTemplate(manager);
        return template?.IsGeneric ?? false;
    }

    public override string ToString() => Value ?? "";
    public override object GetValue() => Value ?? "";

    /// <summary>
    /// 类型值的相等比较。
    /// </summary>
    /// <remarks>
    /// 原先没有重写，默认实现恒为 <c>false</c>，于是 <c>GetType("A") == GetType("A")</c> 与
    /// <c>typeof(e) == "ZeroDivisionError"</c> 这类写法都不成立。这里按类型名比较：
    /// 与另一个类型值比，或与类型名字符串比。
    /// </remarks>
    public override bool Equal(LangValueType? otherValueType)
    {
        return otherValueType switch
        {
            TypeLangValue other => string.Equals(Value, other.Value, StringComparison.Ordinal),
            StringLangValue otherString => string.Equals(Value, otherString.Value, StringComparison.Ordinal),
            _ => false
        };
    }

    /// <summary>
    /// 支持属性访问（如 typeValue.Value）
    /// </summary>
    public override LangValueType Dot(LangExpression right, VariateManager manager)
    {
        if (right is LangId id)
        {
            return id.IdName switch
            {
                "Value" => new StringLangValue(Value ?? ""),
                _ => throw new AttributeError(this, id.IdName, "TypeLangValue")
            };
        }

        // 所有方法调用都通过 InstanceMethods 系统处理
        return base.Dot(right, manager);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 在 IL 模式下，创建 TypeLangValue 对象
        ilGenerator.Emit(OpCodes.Ldstr, Value ?? "");
        ilGenerator.Emit(OpCodes.Newobj, typeof(TypeLangValue).GetConstructor([typeof(string)])!);
    }

    public override Type OutputType(LocalManager local)
    {
        // TypeLangValue 表示类型信息，输出为 TypeLangValue 类型
        return typeof(TypeLangValue);
    }
}