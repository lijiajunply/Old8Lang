using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 原生语句类，用于处理Old8Lang中的native导入语句
/// 支持导入原生DLL、类和方法
/// </summary>
public partial class NativeStatement : OldStatement
{
    /// <summary>
    /// DLL名称
    /// </summary>
    private readonly string DllName;

    /// <summary>
    /// 类名称
    /// </summary>
    private readonly string ClassName;

    /// <summary>
    /// 方法名称（可选）
    /// </summary>
    private readonly string? MethodName;

    /// <summary>
    /// 原生方法名称别名（可选）
    /// </summary>
    private string? NativeName { get; set; }

    /// <summary>
    /// 导入名称（可选）
    /// </summary>
    private readonly string? Name;

    /// <summary>
    /// 函数值（可选）
    /// </summary>
    private readonly FuncLangValue? FuncValue;

    /// <summary>
    /// 是否导入所有方法 (*)
    /// </summary>
    private readonly bool ImportAll;

    /// <summary>
    /// 选择性导入的方法列表
    /// </summary>
    private readonly List<string>? MethodList;

    /// <summary>
    /// 类导入别名
    /// </summary>
    private readonly string? ClassAlias;

    /// <summary>
    /// 构造函数：导入单个方法
    /// </summary>
    /// <param name="dllName">DLL名称</param>
    /// <param name="className">类名称</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="nativeName">原生方法别名</param>
    public NativeStatement(string dllName, string className, string methodName, string nativeName)
    {
        DllName = dllName;
        ClassName = className;
        MethodName = methodName;
        NativeName = nativeName;
        ImportAll = false;
        MethodList = null;
    }

    /// <summary>
    /// 构造函数：导入单个方法并指定函数值
    /// </summary>
    /// <param name="dllName">DLL名称</param>
    /// <param name="className">类名称</param>
    /// <param name="methodName">方法名称</param>
    /// <param name="nativeName">原生方法别名</param>
    /// <param name="a">函数初始化对象</param>
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

    /// <summary>
    /// 构造函数：导入类
    /// </summary>
    /// <param name="dllName">DLL名称</param>
    /// <param name="className">类名称</param>
    /// <param name="name">导入名称</param>
    public NativeStatement(string dllName, string className, string name = "")
    {
        DllName = dllName;
        ClassName = className;
        Name = name;
        ImportAll = false;
        MethodList = null;
    }

    /// <summary>
    /// 构造函数：批量导入所有方法
    /// </summary>
    /// <param name="dllName">DLL名称</param>
    /// <param name="className">类名称</param>
    /// <param name="importAll">是否导入所有方法</param>
    public NativeStatement(string dllName, string className, bool importAll)
    {
        DllName = dllName;
        ClassName = className;
        ImportAll = importAll;
        MethodList = null;
    }

    /// <summary>
    /// 构造函数：选择性导入多个方法
    /// </summary>
    /// <param name="dllName">DLL名称</param>
    /// <param name="className">类名称</param>
    /// <param name="methodList">要导入的方法列表</param>
    public NativeStatement(string dllName, string className, List<string> methodList)
    {
        DllName = dllName;
        ClassName = className;
        MethodList = methodList;
        ImportAll = false;
    }

    /// <summary>
    /// 构造函数：带别名的类导入
    /// </summary>
    /// <param name="dllName">DLL名称</param>
    /// <param name="className">类名称</param>
    /// <param name="classAlias">类别名</param>
    /// <param name="isAliasImport">是否为别名导入</param>
    public NativeStatement(string dllName, string className, string classAlias, bool isAliasImport)
    {
        DllName = dllName;
        ClassName = className;
        ClassAlias = classAlias;
        ImportAll = false;
        MethodList = null;
    }

    /// <summary>
    /// 在解释模式下执行原生导入
    /// </summary>
    /// <param name="manager">变量管理器，用于管理导入的原生元素</param>
    /// <exception cref="Error.ImportError">当导入失败时抛出</exception>
    /// <exception cref="TypeError">当找不到指定类型时抛出</exception>
    /// <exception cref="InvalidOperationError">当找不到指定方法时抛出</exception>
    public override void Run(VariateManager manager)
    {
        // 使用 DllPathResolver 查找 DLL 路径
        string path;
        try
        {
            path = DllPathResolver.ResolveDllPath(
                DllName,
                null, // 不再使用 importPath
                manager.Path);
        }
        catch (FileNotFoundException ex)
        {
            // 包装异常，添加源代码位置信息
            throw new ImportError(Position, $"导入原生库失败：\n{ex.Message}");
        }

        // 加载程序集并获取类型
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(path);
        }
        catch (Exception ex)
        {
            throw new ImportError(Position, $"加载 DLL 文件失败：{path}\n错误：{ex.Message}");
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
            if (methodInfo is null) throw new InvalidOperationError(this, $"找不到方法 {MethodName} 在 {ClassName} 类中");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            var func = new FuncLangValue(NativeName, methodInfo, FuncValue);
            manager.AddClassAndFunc(func);
            return;
        }

        // 处理批量导入所有方法：native "DllName" ClassName *
        if (ImportAll)
        {
            var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (methods is null || methods.Length == 0)
            {
                throw new InvalidOperationError(this, $"类 {ClassName} 中没有找到公共静态方法");
            }

            foreach (var method in methods)
            {
                // 过滤掉 Object 基类的方法
                if (method.DeclaringType == typeof(object))
                    continue;

                var func = new FuncLangValue(method.Name, method);
                manager.AddClassAndFunc(func);
            }

            return;
        }

        // 处理选择性导入多个方法：native "DllName" ClassName { Method1, Method2 }
        if (MethodList is { Count: > 0 })
        {
            foreach (var methodName in MethodList)
            {
                var methodInfo = type?.GetMethod(methodName);
                if (methodInfo is null)
                {
                    throw new InvalidOperationError(this, $"找不到方法 {methodName} 在 {ClassName} 类中");
                }

                var func = new FuncLangValue(methodName, methodInfo);
                manager.AddClassAndFunc(func);
            }

            return;
        }

        // 处理类导入（支持别名）：native "DllName" ClassName 或 native "DllName" ClassName as Alias
        var registerName = !string.IsNullOrEmpty(ClassAlias) ? ClassAlias : ClassName;
        var nativeClass = new NativeAnyLangValue(DllName, ClassName, path, registerName);
        var importInfo = (ImportInfo)nativeClass.Run(manager);
        manager.AddClassAndFunc(importInfo);
    }

    /// <summary>
    /// 在编译模式下生成原生导入的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器</param>
    /// <exception cref="Error.ImportError">当导入失败时抛出</exception>
    /// <exception cref="TypeError">当找不到指定类型时抛出</exception>
    /// <exception cref="InvalidOperationError">当找不到指定方法时抛出</exception>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 使用 DllPathResolver 查找 DLL 路径
        string path;
        try
        {
            path = DllPathResolver.ResolveDllPath(
                DllName,
                null, // 不再使用 importPath
                local.FilePath);
        }
        catch (FileNotFoundException ex)
        {
            throw new ImportError(new SourcePosition(0, 0), $"编译模式：导入原生库失败：\n{ex.Message}");
        }

        // 加载程序集并获取类型
        Assembly assembly;
        try
        {
            assembly = Assembly.LoadFile(path);
        }
        catch (Exception ex)
        {
            throw new ImportError(new SourcePosition(0, 0), $"编译模式：加载 DLL 文件失败：{path}\n错误：{ex.Message}");
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
            if (methodInfo is null) throw new InvalidOperationError(this, $"找不到方法 {MethodName} 在 {ClassName} 类中");
            if (string.IsNullOrEmpty(NativeName))
                NativeName = MethodName;
            local.DelegateVar.Add(NativeName, methodInfo);
            return;
        }

        // 处理批量导入所有方法：native "DllName" ClassName *
        if (ImportAll)
        {
            var methods = type?.GetMethods(BindingFlags.Public | BindingFlags.Static);
            if (methods is null || methods.Length == 0)
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

        // 处理选择性导入多个方法：native "DllName" ClassName { Method1, Method2 }
        if (MethodList is { Count: > 0 })
        {
            foreach (var methodName in MethodList)
            {
                var methodInfo = type?.GetMethod(methodName);
                if (methodInfo is null)
                {
                    throw new InvalidOperationError(this, $"找不到方法 {methodName} 在 {ClassName} 类中");
                }

                local.DelegateVar.Add(methodName, methodInfo);
            }
        }
    }

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为NativeStatement是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为NativeStatement是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将原生语句转换为字符串表示
    /// </summary>
    /// <returns>原生语句的字符串表示</returns>
    public override string ToString()
    {
        if (!string.IsNullOrEmpty(Name))
        {
            return $"import extern {DllName}.{ClassName} as {Name}";
        }

        if (!string.IsNullOrEmpty(MethodName))
        {
            var funcName = string.IsNullOrEmpty(NativeName) ? MethodName : NativeName;
            return $"import extern {DllName}.{ClassName}.{MethodName} as {funcName}\n{FuncValue}";
        }

        // 批量导入所有方法
        if (ImportAll)
        {
            return $"extern \"{DllName}\" {ClassName} *";
        }

        // 选择性导入多个方法
        if (MethodList is { Count: > 0 })
        {
            var methods = string.Join(", ", MethodList);
            return $"extern \"{DllName}\" {ClassName} {{ {methods} }}";
        }

        // 类导入（可能带别名）
        if (!string.IsNullOrEmpty(ClassAlias))
        {
            return $"extern \"{DllName}\" {ClassName} as {ClassAlias}";
        }

        return $"import extern {DllName}.{ClassName}";
    }
}