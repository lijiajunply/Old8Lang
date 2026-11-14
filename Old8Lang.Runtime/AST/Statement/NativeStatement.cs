using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 原生语句类，用于处理Old8Lang中的native导入语句
/// 支持导入原生DLL、类和方法
/// </summary>
public partial class NativeStatement : OldStatement, IBlockDeclaration
{
    /// <summary>
    /// DLL名称
    /// </summary>
    public readonly string DllName;

    /// <summary>
    /// 类名称
    /// </summary>
    public readonly string ClassName;

    /// <summary>
    /// 方法名称（可选）
    /// </summary>
    public readonly string? MethodName;

    /// <summary>
    /// 原生方法名称别名（可选）
    /// </summary>
    public string? NativeName { get; set; }

    /// <summary>
    /// 导入名称（可选）
    /// </summary>
    public readonly string? Name;

    /// <summary>
    /// 函数值（可选）
    /// </summary>
    public readonly FuncLangValue? FuncValue;

    /// <summary>
    /// 是否导入所有方法 (*)
    /// </summary>
    public readonly bool ImportAll;

    /// <summary>
    /// 选择性导入的方法列表
    /// </summary>
    public readonly List<string>? MethodList;

    /// <summary>
    /// 类导入别名
    /// </summary>
    public readonly string? ClassAlias;

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
        FuncValue = a.GetFuncValue();
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