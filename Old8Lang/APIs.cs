using System.Diagnostics;
using System.Text;
using System.Text.Json;
using ValueType = Old8Lang.AST.Expression.ValueType;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.Extensions.DependencyModel;
using Old8Lang.AST.Statement;
using Old8Lang.CslyParser;

namespace Old8Lang;

public class APIs
{
    #region OtherFunc

    public static string ListToString<T>(List<T> a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Count; i++)
        {
            builder.Append(a[i]+(i == a.Count-1 ? "" : " "));
        }
        return builder.ToString();
    }
    public static string ArrayToString<T>(T[] a)
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < a.Length; i++)
        {
            var b = i == 0 && a.Length == 1 || i == a.Length-1 ? "" : ",";
            builder.Append(a[i]+b);
        }
        return builder.ToString();
    }
    public static List<object> ListToObjects(List<ValueType> a)
    {
        if (a == null)
            return new List<object>();
        if (a.Count == 0)
            return new List<object>();
        if (a[0] == null)
            return new List<object>();


        return a.Select(x => x.GetValue()).ToList();
    }

    #endregion

    #region ReadFileOrDir

    public static string FromFile(string filename) => File.ReadAllText(filename,Encoding.UTF8);

    public static string FromDirectory(string DirectoryName)
    {
        var builder = new StringBuilder();
        builder.Append(FromFile(DirectoryName+"/"+"init.ws"));
        return builder.ToString();
    }

    #endregion

    public static (VariateManager Manager,List<string> Error,string Time) CslyUsing(string path,bool isdir)
    {
        var a = new Interpreter(path,isdir);
        a.ParserRun(true);
        return (a.GetVariateManager(),a.GetError(),a.GetTime());
    }

    public static void RunTask(BlockStatement statement)
    {
        string             code           = statement.ToCode();
        CSharpCodeProvider codeProvider   = new CSharpCodeProvider();
        CompilerParameters compParameters = new CompilerParameters();
        // Compile the code
        CompilerResults res = codeProvider.CompileAssemblyFromSource(compParameters,code);
        // Create a new instance of the class 'MyClass'　　　　// 有命名空间的，需要命名空间.类名
        object myClass = res.CompiledAssembly.CreateInstance("Project");
        myClass.GetType().GetMethod("Main").Invoke(myClass,null);
    }

    public static LangInfo ChangeBasicInfo(string import,string ver,string uri = "https://downland.old8lang.com")
    {
        LangInfo langInfo = new LangInfo() { LibInfos = Read_JSON().LibInfos,ImportPath = import,Ver = ver,Url = uri };
        string   jsonString = JsonSerializer.Serialize(langInfo);
        File.WriteAllText(BasicInfo.JSONPath,jsonString);
        return langInfo;
    }
    public static LangInfo Read_JSON()
    {
        var jsonString = FromFile(BasicInfo.JSONPath);
        return JsonSerializer.Deserialize<LangInfo>(jsonString);
    }
    public static bool ImportInstall(string context)
    {
        //以后再说
        return false;
    }
}