using System.Reflection;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// 适用于有构造函数的类
/// </summary>
public class NativeAnyValue(string dllName, string className, string path) : ValueType
{
    private Type? ClassType { get; set; }
    public readonly string ClassName = className;
    private ConstructorInfo? Constructor { get; set; }
    private object? InstanceObj { get; set; }

    private VariateManager Manager = new();

    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldId id)
        {
            var prop = ClassType?.GetProperty(id.IdName);
            if (prop is null)
            {
                var fie = ClassType?.GetField(id.IdName);
                if (fie is null)
                    throw new AttributeError(this, id.IdName, ClassName);
                return ObjToValue(fie.GetValue(null)!);
            }

            return ObjToValue(prop.GetValue(null)!);
        }

        if (dotExpr is Instance instance)
        {
            var method = ClassType?.GetMethod(instance.Id.IdName);
            if (method == null)
                throw new AttributeError(this, instance.Id.IdName, ClassName);
            var func = new FuncValue(instance.Id.IdName, method);
            return func.Run(Manager, instance.Ids, InstanceObj);
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }

    public override ValueType Run(VariateManager manager)
    {
        var assembly = Assembly.LoadFile(path);
        ClassType = assembly.GetType($"{dllName}.{ClassName}")!;
        if (ClassType?.GetConstructors() is not null)
            Constructor = ClassType.GetConstructors()[0];
        Manager = manager.Clone();
        return this;
    }

    public void New(object[] pa)
    {
        InstanceObj = Constructor != null ? Constructor.Invoke(pa) : Activator.CreateInstance(ClassType!)!;
    }
}