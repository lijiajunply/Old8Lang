using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class NativeStatement : OldStatement
{

    private readonly string DllName;

    private readonly string ClassName;

    private readonly string? MethodName;

    private string? NativeName { get; set; }

    private readonly string? Name;
    private readonly FuncLangValue? FuncValue;

    // 批量导入相关字段
    private readonly bool ImportAll;  // 是否导入所有方法 (*)
    private readonly List<string>? MethodList;  // 选择性导入的方法列表

    // 类导入别名（用于 [import "DllName" ClassName as Alias]）
    private readonly string? ClassAlias;

    public NativeStatement(string dllName, string className, string methodName, string nativeName)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
        ImportAll = false;
        MethodList = null;
    }

    public NativeStatement(string dllName, string className, string methodName, string nativeName, FuncInit a)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
        FuncValue = a.FuncLangValue;
        ImportAll = false;
        MethodList = null;
    }

    public NativeStatement(string dllName, string className, string name = "")
    {
        DllName = dllName;
        ClassName = className;
        Name = name;
        ImportAll = false;
        MethodList = null;
    }

    // 新增：批量导入所有方法的构造函数
    public NativeStatement(string dllName, string className, bool importAll)
    {
        DllName = dllName;
        ClassName = className;
        ImportAll = importAll;
        MethodList = null;
    }

    // 新增：选择性导入多个方法的构造函数
    public NativeStatement(string dllName, string className, List<string> methodList)
    {
        DllName = dllName;
        ClassName = className;
        MethodList = methodList;
        ImportAll = false;
    }

    // 新增：带别名的类导入构造函数 ([import "DllName" ClassName as Alias])
    public NativeStatement(string dllName, string className, string classAlias, bool isAliasImport)
    {
        DllName = dllName;
        ClassName = className;
        ClassAlias = classAlias;
        ImportAll = false;
        MethodList = null;
    }

    public override void Run(VariateManager manager)
    {
        // 使用 DllPathResolver 查找 DLL 路径
        string path;
        try
        {
            path = DllPathResolver.ResolveDllPath(
                DllName,
                manager.LangInfo?.ImportPath,
                manager.Path);
        }
        catch (FileNotFoundException ex)
        {
            // 包装异常，添加源代码位置信息
            throw new Error.ImportError(Position, $"导入原生库失败：\n{ex.Message}");
        }

        // 加载程序集并获取类型
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(path);
        }
        catch (Exception ex)
        {
            throw new Error.ImportError(Position, $"加载 DLL 文件失败：{path}\n错误：{ex.Message}");
        }

        var type = assembly.GetType($"{DllName}.{ClassName}");

        if (!string.IsNullOrEmpty(Name))
        {
            type = assembly.GetType($"{Name}.{ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{Name}.{ClassName}");
                if (type is null)
                    throw new TypeError(this, $"找不到类型 {Name}.{ClassName}");
            }

            manager.AddClassAndFunc(new NativeStaticAny(ClassName, type));
            return;
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var methodInfo = type?.GetMethod(MethodName);
            if (methodInfo == null) throw new InvalidOperationError(this, $"找不到方法 {MethodName} 在 {ClassName} 类中");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            var func = new FuncLangValue(NativeName, methodInfo, FuncValue);
            manager.AddClassAndFunc(func);
            return;
        }

        // 处理批量导入所有方法：[import "DllName" ClassName *]
        if (ImportAll)
        {
            var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (methods == null || methods.Length == 0)
            {
                throw new InvalidOperationError(this, $"类 {ClassName} 中没有找到公共静态方法");
            }

            foreach (var method in methods)
            {
                // 过滤掉 Object 基类的方法
                if (method.DeclaringType == typeof(object))
                    continue;

                var func = new FuncLangValue(method.Name, method, null);
                manager.AddClassAndFunc(func);
            }
            return;
        }

        // 处理选择性导入多个方法：[import "DllName" ClassName { Method1, Method2 }]
        if (MethodList != null && MethodList.Count > 0)
        {
            foreach (var methodName in MethodList)
            {
                var methodInfo = type?.GetMethod(methodName);
                if (methodInfo == null)
                {
                    throw new InvalidOperationError(this, $"找不到方法 {methodName} 在 {ClassName} 类中");
                }

                var func = new FuncLangValue(methodName, methodInfo, null);
                manager.AddClassAndFunc(func);
            }
            return;
        }

        // 处理类导入（支持别名）：[import "DllName" ClassName] 或 [import "DllName" ClassName as Alias]
        var registerName = !string.IsNullOrEmpty(ClassAlias) ? ClassAlias : ClassName;
        var nativeClass = new NativeAnyLangValue(DllName, ClassName, path, registerName);
        var importInfo = (ImportInfo)nativeClass.Run(manager);
        manager.AddClassAndFunc(importInfo);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 使用 DllPathResolver 查找 DLL 路径
        string path;
        try
        {
            // 对于编译模式，尝试从 Apis.ReadJson 获取导入路径
            var langInfo = Apis.ReadJson();
            path = DllPathResolver.ResolveDllPath(
                DllName,
                langInfo.ImportPath,
                local.FilePath);
        }
        catch (FileNotFoundException ex)
        {
            throw new Error.ImportError(new SourcePosition(0, 0), $"编译模式：导入原生库失败：\n{ex.Message}");
        }

        // 加载程序集并获取类型
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(path);
        }
        catch (Exception ex)
        {
            throw new Error.ImportError(new SourcePosition(0, 0), $"编译模式：加载 DLL 文件失败：{path}\n错误：{ex.Message}");
        }

        var type = assembly.GetType($"{DllName}.{ClassName}");
        if (!string.IsNullOrEmpty(Name))
        {
            type = assembly.GetType($"{Name}.{ClassName}");
            if (type is null)
            {
                type = Type.GetType($"{Name}.{ClassName}");
                if (type is null)
                    throw new TypeError(this, $"找不到类型 {Name}.{ClassName}");
            }

            local.ClassVar.Add(ClassName, type);
            return;
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var methodInfo = type?.GetMethod(MethodName);
            if (methodInfo == null) throw new InvalidOperationError(this, $"找不到方法 {MethodName} 在 {ClassName} 类中");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            local.DelegateVar.Add(NativeName, methodInfo);
            return;
        }

        // 处理批量导入所有方法：[import "DllName" ClassName *]
        if (ImportAll)
        {
            var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (methods == null || methods.Length == 0)
            {
                throw new InvalidOperationError(this, $"类 {ClassName} 中没有找到公共静态方法");
            }

            foreach (var method in methods)
            {
                // 过滤掉 Object 基类的方法
                if (method.DeclaringType == typeof(object))
                    continue;

                local.DelegateVar.Add(method.Name, method);
            }
            return;
        }

        // 处理选择性导入多个方法：[import "DllName" ClassName { Method1, Method2 }]
        if (MethodList != null && MethodList.Count > 0)
        {
            foreach (var methodName in MethodList)
            {
                var methodInfo = type?.GetMethod(methodName);
                if (methodInfo == null)
                {
                    throw new InvalidOperationError(this, $"找不到方法 {methodName} 在 {ClassName} 类中");
                }

                local.DelegateVar.Add(methodName, methodInfo);
            }
        }
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return $"import native {DllName}.{ClassName} as {Name}";
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var funcName = string.IsNullOrEmpty(NativeName) ? MethodName : NativeName;
            return $"import native {DllName}.{ClassName}.{MethodName} as {funcName}\n{FuncValue}";
        }

        // 批量导入所有方法
        if (ImportAll)
        {
            return $"[import \"{DllName}\" {ClassName} *]";
        }

        // 选择性导入多个方法
        if (MethodList != null && MethodList.Count > 0)
        {
            var methods = string.Join(", ", MethodList);
            return $"[import \"{DllName}\" {ClassName} {{ {methods} }}]";
        }

        // 类导入（可能带别名）
        if (!string.IsNullOrEmpty(ClassAlias))
        {
            return $"[import \"{DllName}\" {ClassName} as {ClassAlias}]";
        }

        return $"import native {DllName}.{ClassName}";
    }
}