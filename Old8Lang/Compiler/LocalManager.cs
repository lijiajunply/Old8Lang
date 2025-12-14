using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Error;
using Old8Lang.LangParser;

namespace Old8Lang.Compiler;

/// <summary>
/// 局部变量管理器，用于在编译过程中管理局部变量、委托、类和字段
/// </summary>
/// <remarks>
/// 该类是编译器生成IL代码时的重要组件，主要负责：
/// - 管理局部变量的创建、访问和移除
/// - 管理委托、类和字段的信息
/// - 提供类型兼容性验证
/// - 支持break和continue标签的管理
/// - 提供调试信息记录和错误报告功能
/// - 支持状态克隆和恢复
/// </remarks>
public class LocalManager
{
    /// <summary>
    /// 局部变量字典，键为变量名，值为LocalBuilder实例
    /// </summary>
    public readonly Dictionary<string, LocalBuilder> LocalVar = [];
    
    /// <summary>
    /// 委托方法字典，键为委托名，值为MethodInfo实例
    /// </summary>
    public readonly Dictionary<string, MethodInfo> DelegateVar = [];
    
    /// <summary>
    /// 类类型字典，键为类名，值为Type实例
    /// </summary>
    public readonly Dictionary<string, Type> ClassVar = [];
    
    /// <summary>
    /// 字段信息字典，键为字段名，值为FieldInfo实例
    /// </summary>
    public readonly Dictionary<string, FieldInfo> FieldVar = [];

    /// <summary>
    /// 局部变量类型字典，键为变量名，值为Type实例
    /// </summary>
    public readonly Dictionary<string, Type> LocalVarTypes = [];

    /// <summary>
    /// 存储函数的参数列表信息（用于支持默认参数）
    /// </summary>
    public readonly Dictionary<string, List<LangId>> FuncParameters = [];
    
    /// <summary>
    /// 当前所在的类环境类型
    /// </summary>
    public Type? InClassEnv { get; init; }
    
    /// <summary>
    /// 当前源代码文件路径
    /// </summary>
    public string FilePath { get; set; } = "";
    
    /// <summary>
    /// 关联的解释器实例
    /// </summary>
    public LangInterpreter? Interpreter { get; init; }

    /// <summary>
    /// break语句的目标标签
    /// </summary>
    public Label? BreakLabel { get; set; }
    
    /// <summary>
    /// continue语句的目标标签
    /// </summary>
    public Label? ContinueLabel { get; set; }

    /// <summary>
    /// 标记是否在finally块中生成IL代码
    /// </summary>
    public bool IsInFinallyBlock { get; set; }

    /// <summary>
    /// 记录调试信息
    /// </summary>
    /// <param name="message">调试信息内容</param>
    /// <param name="position">源代码位置</param>
    public void LogDebug(string message, SourcePosition position)
    {
        Console.WriteLine($"[DEBUG] {FilePath}:{position.Line}:{position.Column} - {message}");
    }

    /// <summary>
    /// 报告编译错误
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="position">源代码位置</param>
    /// <exception cref="CompilerException">编译异常</exception>
    public void ReportError(string message, SourcePosition position)
    {
        var errorMessage = $"{FilePath}:{position.Line}:{position.Column} - {message}";
        throw new CompilerException(errorMessage, position);
    }

    /// <summary>
    /// 验证类型兼容性
    /// </summary>
    /// <param name="expected">预期类型</param>
    /// <param name="actual">实际类型</param>
    /// <param name="position">源代码位置</param>
    /// <returns>如果类型兼容返回true，否则返回false并报告错误</returns>
    public bool ValidateType(Type? expected, Type? actual, SourcePosition position)
    {
        if (expected == null || actual == null)
            return false;

        if (expected == actual || expected.IsAssignableFrom(actual))
            return true;

        ReportError($"类型不兼容: 预期 {expected.Name}, 实际 {actual.Name}", position);
        return false;
    }

    /// <summary>
    /// 创建一个新的LocalManager实例，复制当前实例的FilePath和Interpreter属性
    /// </summary>
    /// <returns>新的LocalManager实例</returns>
    public LocalManager New()
    {
        return new LocalManager() { FilePath = FilePath, Interpreter = Interpreter };
    }

    /// <summary>
    /// 克隆当前LocalManager实例
    /// </summary>
    /// <returns>克隆后的LocalManager实例</returns>
    /// <remarks>
    /// 克隆过程会复制所有字典和属性，但不会复制InClassEnv，因为它是init-only属性
    /// </remarks>
    public LocalManager Clone()
    {
        var cloned = new LocalManager
        {
            FilePath = FilePath,
            Interpreter = Interpreter,
            InClassEnv = InClassEnv,
            BreakLabel = BreakLabel,
            ContinueLabel = ContinueLabel
        };

        // 克隆局部变量
        foreach (var (name, local) in LocalVar)
        {
            cloned.LocalVar[name] = local;
        }

        // 克隆委托变量
        foreach (var (name, method) in DelegateVar)
        {
            cloned.DelegateVar[name] = method;
        }

        // 克隆类变量
        foreach (var (name, type) in ClassVar)
        {
            cloned.ClassVar[name] = type;
        }

        // 克隆字段变量
        foreach (var (name, field) in FieldVar)
        {
            cloned.FieldVar[name] = field;
        }

        // 克隆函数参数信息
        foreach (var (name, @params) in FuncParameters)
        {
            cloned.FuncParameters[name] = @params;
        }

        return cloned;
    }

    /// <summary>
    /// 从克隆实例中恢复当前LocalManager的状态
    /// </summary>
    /// <param name="cloned">克隆的LocalManager实例</param>
    /// <remarks>
    /// 恢复过程会替换当前实例的所有字典和属性，但不会修改InClassEnv，因为它是init-only属性
    /// </remarks>
    public void Restore(LocalManager cloned)
    {
        // 清空当前局部变量
        LocalVar.Clear();

        // 恢复局部变量
        foreach (var (name, local) in cloned.LocalVar)
        {
            LocalVar[name] = local;
        }

        // 清空当前委托变量
        DelegateVar.Clear();

        // 恢复委托变量
        foreach (var (name, method) in cloned.DelegateVar)
        {
            DelegateVar[name] = method;
        }

        // 清空当前类变量
        ClassVar.Clear();

        // 恢复类变量
        foreach (var (name, type) in cloned.ClassVar)
        {
            ClassVar[name] = type;
        }

        // 清空当前字段变量
        FieldVar.Clear();

        // 恢复字段变量
        foreach (var (name, field) in cloned.FieldVar)
        {
            FieldVar[name] = field;
        }

        // 恢复其他属性（注意：InClassEnv是init-only属性，不能修改）
        FilePath = cloned.FilePath;
        BreakLabel = cloned.BreakLabel;
        ContinueLabel = cloned.ContinueLabel;
    }

    /// <summary>
    /// 获取指定名称的局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <returns>LocalBuilder实例，如果变量不存在则返回null</returns>
    public LocalBuilder? GetLocalVar(string name)
    {
        return LocalVar.GetValueOrDefault(name);
    }

    /// <summary>
    /// 添加局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="index">LocalBuilder实例</param>
    public void AddLocalVar(string name, LocalBuilder index)
    {
        LocalVar[name] = index;
    }

    /// <summary>
    /// 移除指定名称的局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    public void RemoveLocalVar(string name)
    {
        LocalVar.Remove(name);
    }

    /// <summary>
    /// 检查是否存在指定名称的局部变量
    /// </summary>
    /// <param name="name">变量名</param>
    /// <returns>如果变量存在则返回true，否则返回false</returns>
    public bool IsHasVar(string name) => LocalVar.ContainsKey(name);

    /// <summary>
    /// 获取局部变量的数量
    /// </summary>
    /// <returns>局部变量的数量</returns>
    public int GetCount() => LocalVar.Count;
}