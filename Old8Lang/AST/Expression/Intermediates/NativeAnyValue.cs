using System.Reflection;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 适用于有构造函数的类
/// </summary>
public class NativeAnyValue(string dllName, string className, string path) : ValueType
{
    private Type? ClassType { get; set; }
    public readonly string ClassName = className;
    private ConstructorInfo? Constructor { get; set; }
    private object? InstanceObj { get; set; }

    private VariateManager manager = new();

    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID id)
        {
            var prop = ClassType?.GetProperty(id.IdName);
            if (prop is null)
            {
                var fie = ClassType?.GetField(id.IdName);
                return fie is null ? new VoidValue() : ObjToValue(fie.GetValue(null)!);
            }

            return ObjToValue(prop.GetValue(null)!);
        }

        if (dotExpr is Instance instance)
        {
            var method = ClassType?.GetMethod(instance.Id.IdName);
            if (method == null) return new VoidValue();
            var func = new FuncValue(instance.Id.IdName, method);
            return func.Run(ref manager, instance.Ids, InstanceObj);
        }

        return new VoidValue();
    }

    public override ValueType Run(ref VariateManager Manager)
    {
        var assembly = Assembly.LoadFile(path);
        ClassType = assembly.GetType($"{dllName}.{ClassName}")!;
        if (ClassType?.GetConstructors() is not null)
            Constructor = ClassType.GetConstructors()[0];
        manager = Manager.Clone();
        return this;
    }

    public void New(object[] pa)
    {
        InstanceObj = Constructor != null ? Constructor.Invoke(pa) : Activator.CreateInstance(ClassType!)!;
    }
}