using System.Reflection;
using Old8Lang.AST.Expression.Value;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class NativeStatement : OldStatement
{
    private string DllName { get; }

    private string ClassName { get; }

    private string? MethodName { get; }

    private string? NativeName { get; set; }

    private string? Name { get; }
    private FuncValue? FuncValue { get; }

    public NativeStatement(string dllName, string className, string methodName, string nativeName)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
    }

    public NativeStatement(string dllName, string className, string methodName, string nativeName, FuncInit a)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
        FuncValue = a.FuncValue;
    }

    public NativeStatement(string dllName, string className, string name = "")
    {
        DllName = dllName;
        ClassName = className;
        Name = name;
    }

    public override void Run(ref VariateManager Manager)
    {
        var path = $"{Path.GetDirectoryName(Manager.Path)}/dll/{DllName}.dll"; // filepath/dll/dllname
        var assembly = Assembly.LoadFile(path);
        var type = assembly.GetType($"{DllName}.{ClassName}");
        if (!string.IsNullOrEmpty(Name))
        {
            type = assembly.GetType($"{Name}.{ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{Name}.{ClassName}");
                if (type is null)
                    throw new TypeError(this, this);
            }

            Manager.AddClassAndFunc(new NativeStaticAny(ClassName, type));
            return;
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var methodInfo = type?.GetMethod(MethodName);
            if (methodInfo == null) throw new Exception($"Not Have Method in {ToString()}");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            var func = new FuncValue(NativeName, methodInfo, FuncValue);
            Manager.AddClassAndFunc(func);
            return;
        }

        Manager.AddClassAndFunc(new NativeAnyValue(DllName, ClassName, path).Run(ref Manager));
    }

    public override string ToString() => $"[import {DllName} {ClassName} {MethodName} {NativeName}]\n{FuncValue}";
}