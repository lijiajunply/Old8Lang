using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class NativeStatement : OldStatement
{
    private readonly string DllName;

    private readonly string ClassName;

    private readonly string? MethodName;

    private string? NativeName { get; set; }

    private readonly string? Name;
    private readonly FuncValue? FuncValue;

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

    public override void Run(VariateManager Manager)
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

        Manager.AddClassAndFunc(new NativeAnyValue(DllName, ClassName, path).Run(Manager));
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotImplementedException();
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"[import {DllName} {ClassName} {MethodName} {NativeName}]\n{FuncValue}";
}