using System.Reflection;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Expression.Value;

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
        ValueType result = new ValueType();
        if (dotExpr is Instance instance)
        {
            var method = ClassType.GetMethod(instance.Id.IdName);
            var func   = new FuncValue(instance.Id.IdName,method);
            result = func.Run(ref manager,instance.Ids,InstanceObj);
        }
        return result;
    }
    public override ValueType Run(ref VariateManager Manager)
    {
        var assembly = Assembly.LoadFile(Path);
        ClassType   = assembly.GetType($"{DllName}.{ClassName}");
        Constructor = ClassType.GetConstructors()[0];
        manager     = Manager.Clone();
        return this;
    }
    public void New(object[] pa)
    {
        InstanceObj = Constructor.Invoke(pa);
    }
    
}