using System.Reflection;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 适用于有构造函数的类
/// </summary>
public class NativeAnyValue : ValueType
{
    private Type            ClassType      { get; set; }
    private string          DllName        { get; set; }
    public string          ClassName      { get; set; }
    private string          Path           { get; set; }
    private ConstructorInfo Constructor    { get; set; }
    private object          InstanceObj    { get; set; }

    private VariateManager manager;

    public NativeAnyValue(string dllName,string className,string path)
    {
        DllName   = dllName;
        ClassName = className;
        Path      = path;
    }
    public override ValueType Dot(OldExpr dotExpr)
    {
        if (dotExpr is OldID id)
        {
            var prop = ClassType.GetProperty(id.IdName);
            if (prop is null)
            {
                var fie = ClassType.GetField(id.IdName);
                if (fie is null)
                    return null;
                return ObjToValue(fie.GetValue(null));
            }
            return ObjToValue(prop.GetValue(null));
        }
        if (dotExpr is Instance instance)
        {
            var method = ClassType.GetMethod(instance.Id.IdName);
            var func   = new FuncValue(instance.Id.IdName,method);
            return func.Run(ref manager,instance.Ids,InstanceObj);
        }
        return null;
    }
    public override ValueType Run(ref VariateManager Manager)
    {
        var assembly = Assembly.LoadFile(Path);
        ClassType   = assembly.GetType($"{DllName}.{ClassName}");
        if (ClassType.GetConstructors() is not null)
            Constructor = ClassType.GetConstructors()[0];
        manager     = Manager.Clone();
        return this;
    }
    public void New(object[] pa)
    {
        InstanceObj = Constructor is not null ? Constructor.Invoke(pa) : Activator.CreateInstance(ClassType)!;
    }
    
}