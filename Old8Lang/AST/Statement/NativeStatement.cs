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

    public NativeStatement(string dllName,string className,string methodName,string NativeName)
    {
        DLL_NAME    = dllName;
        CLASS_NAME  = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = NativeName;
    }
    public NativeStatement(string dllName,string className,string methodName,string NativeName,FuncInit a)
    {
        DLL_NAME    = dllName;
        CLASS_NAME  = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = NativeName;
        FuncValue        = a.FuncValue;
    }

    public override void Run(ref VariateManager Manager)
    {
        var path       = $"{Manager.Path}/dll/{DLL_NAME}"; // filepath/dll/dllname
        var assembly   = Assembly.LoadFile(Manager.LangInfo.LangLibDllPath);
        var type       = assembly.GetType($"Old8LangLib.{CLASS_NAME}");
        var methodInfo = type.GetMethod(METHOD_NAME);
        if (NATIVE_NAME is "")
            NATIVE_NAME = METHOD_NAME;
        var func = new FuncValue(NATIVE_NAME,methodInfo,FuncValue);
        Manager.AddClassAndFunc(new OldID(NATIVE_NAME),func);
    }

    public override string ToString() => $"[import {DLL_NAME} {CLASS_NAME} {METHOD_NAME} {NATIVE_NAME}]\n{FuncValue}";
}