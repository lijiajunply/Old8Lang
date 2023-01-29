using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class NativeStatement : OldStatement
{
    private string DLL_NAME { get; set; }

    private string CLASS_NAME { get; set; }

    private string METHOD_NAME { get; set; }

    private string NATIVE_NAME { get; set; }

    private FuncValue FuncValue { get; set; }

    public NativeStatement(string dllName,string className,string methodName,string? nativeName)
    {
        DLL_NAME    = dllName;
        CLASS_NAME  = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = nativeName;
    }
    public NativeStatement(string dllName,string className,string methodName,string? nativeName,FuncInit a)
    {
        DLL_NAME    = dllName;
        CLASS_NAME  = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = nativeName;
        FuncValue        = a.FuncValue;
    }
    public NativeStatement(string dllName,string className)
    {
        DLL_NAME   = dllName;
        CLASS_NAME = className;
    }

    public override void Run(ref VariateManager Manager)
    {
        DLL_NAME = DLL_NAME.Split(@"""")[1];
        var path       = $"{Path.GetDirectoryName(Manager.Path)}/dll/{DLL_NAME}.dll"; // filepath/dll/dllname
        var assembly   = Assembly.LoadFile(path);
        var type       = assembly.GetType($"{DLL_NAME}.{CLASS_NAME}");
        if (METHOD_NAME != null)
        {
            var methodInfo = type.GetMethod(METHOD_NAME);
            if (NATIVE_NAME is "")
                NATIVE_NAME = METHOD_NAME;
            var func = new FuncValue(NATIVE_NAME,methodInfo,FuncValue);
            Manager.AddClassAndFunc(func);
            return;
        }
        Manager.AddClassAndFunc(new NativeAnyValue(DLL_NAME,CLASS_NAME,path).Run(ref Manager));
    }

    public override string ToString() => $"[import {DLL_NAME} {CLASS_NAME} {METHOD_NAME} {NATIVE_NAME}]\n{FuncValue}";
}