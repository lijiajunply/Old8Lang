using Old8Lang.Error;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Interpreter;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;

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
    /// 是否为基本类型
    /// </summary>
    public bool IsPrimitive()
    {
        if (Value is null) return false;

        return Value switch
        {
            "Int" or "Float" or "String" or "Bool" or "Null" => true,
            _ => false
        };
    }

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
        // 在编译模式下，创建 TypeLangValue 对象
        ilGenerator.Emit(OpCodes.Ldstr, Value ?? "");
        ilGenerator.Emit(OpCodes.Newobj, typeof(TypeLangValue).GetConstructor([typeof(string)])!);
    }

    public override Type OutputType(LocalManager local)
    {
        // TypeLangValue 表示类型信息，输出为 TypeLangValue 类型
        return typeof(TypeLangValue);
    }
}