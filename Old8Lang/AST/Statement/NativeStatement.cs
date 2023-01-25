using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.OldLandParser;

namespace Old8Lang.AST.Statement;

public class NativeStatement : OldStatement
{
    public string DLL_NAME { get; set; }
    public string CLASS_NAME { get; set; }
    public string METHOD_NAME { get; set; }
    public string NATIVE_NAME { get; set; }

    public OldFunc Func { get; set; }

    public NativeStatement(string dllName, string className, string methodName , string NativeName)
    {
        DLL_NAME = dllName;
        CLASS_NAME = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = NativeName;
    }
    public NativeStatement(string dllName,string className,string methodName,string NativeName,FuncInit a)
    {
        DLL_NAME    = dllName;
        CLASS_NAME  = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = NativeName;
        Func        = a.Func;
    }

    public override void Run(ref VariateManager Manager)
    {
        string path = $"{APIs.Path}/dll/{DLL_NAME}"; // filepath/dll/dllname
        Assembly assembly = Assembly.LoadFile(path);
        Type type = assembly.GetType(CLASS_NAME);
        MethodInfo methodInfo = type.GetMethod(METHOD_NAME);
        //PropertyInfo propertyInfo = type.GetProperty(METHOD_NAME);
        //var a = methodInfo.Invoke()
        if (NATIVE_NAME is null)
            NATIVE_NAME = METHOD_NAME;
        var func = new OldFunc(NATIVE_NAME, methodInfo,Func);
        Manager.AddClassAndFunc(new OldID(NATIVE_NAME), func);
    }

    public override string ToString() => $"[import {DLL_NAME} {CLASS_NAME} {METHOD_NAME} {NATIVE_NAME}]\n{Func}";
}