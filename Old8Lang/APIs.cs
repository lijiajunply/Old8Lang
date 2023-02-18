using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Old8Lang.OldLandParser;
using ValueType = Old8Lang.AST.Expression.ValueType;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
        a.ParserRun(!isdir);
        return (a.GetVariateManager(),a.GetError(),a.GetTime());
    }
    
    public static void CslyRun(string code,bool isdir)
    {
        var a = new Interpreter(code,isdir);
        var b = a.Build();
        var s = b.ToCode();
        var assembly = Compile(s, Assembly.Load(new AssemblyName("System.Runtime")), typeof(object).Assembly);
        var personType = assembly.GetType("Project");
        if (personType != null)
        {
            var method = personType.GetMethod("Main");
            var result = method.Invoke(null,null); // fan
        }
    }
    
    public static Assembly Compile(string text,params Assembly[] referencedAssemblies)
    {
        var       references        = referencedAssemblies.Select(it => MetadataReference.CreateFromFile(it.Location));
        var       options           = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var       assemblyName      = "_"+ Guid.NewGuid().ToString("D");
        var       syntaxTrees       = new SyntaxTree[] { CSharpSyntaxTree.ParseText(text) };
        var       compilation       = CSharpCompilation.Create(assemblyName,syntaxTrees,references,options);
        using var stream            = new MemoryStream();
        var       compilationResult = compilation.Emit(stream);
        if (compilationResult.Success)
        {
            stream.Seek(0,SeekOrigin.Begin);
            return Assembly.Load(stream.ToArray());
        }
        throw new InvalidOperationException("Compilation error");


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