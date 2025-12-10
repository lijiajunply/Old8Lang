using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.LangParser;

namespace Old8Lang.Compiler;

public class LocalManager
{
    private readonly Dictionary<string, LocalBuilder> LocalVar = [];
    public readonly Dictionary<string, MethodInfo> DelegateVar = [];
    public readonly Dictionary<string, Type> ClassVar = [];
    public Type? InClassEnv { get; init; }
    public string FilePath { get; set; } = "";
    public IMiniInterpreter? Interpreter { get; init; }
    
    // break和continue标签
    public Label? BreakLabel { get; set; }
    public Label? ContinueLabel { get; set; }

    public LocalManager New()
    {
        return new LocalManager() { FilePath = FilePath, Interpreter = Interpreter };
    }

    /// <summary>
    /// 克隆当前LocalManager实例
    /// </summary>
    /// <returns>克隆后的LocalManager实例</returns>
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
        
        return cloned;
    }
    
    /// <summary>
    /// 从克隆实例中恢复当前LocalManager的状态
    /// </summary>
    /// <param name="cloned">克隆的LocalManager实例</param>
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
        
        // 恢复其他属性（注意：InClassEnv是init-only属性，不能修改）
        FilePath = cloned.FilePath;
        BreakLabel = cloned.BreakLabel;
        ContinueLabel = cloned.ContinueLabel;
    }

    public LocalBuilder? GetLocalVar(string name)
    {
        return LocalVar.GetValueOrDefault(name);
    }

    public void AddLocalVar(string name, LocalBuilder index)
    {
        LocalVar[name] = index;
    }

    public void RemoveLocalVar(string name)
    {
        LocalVar.Remove(name);
    }

    public bool IsHasVar(string name) => LocalVar.ContainsKey(name);

    public int GetCount() => LocalVar.Count;
}

public interface IMiniInterpreter
{
    public BlockStatement Build(string code);
    public AbsUseClass UseClass { get; set; }
    public bool IsCompileOptimization { get; set; }
}