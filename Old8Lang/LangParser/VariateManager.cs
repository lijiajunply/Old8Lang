using System.Diagnostics.CodeAnalysis;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;

namespace Old8Lang.LangParser;

public class VariateManager
{
    #region Lang

    public LangInfo? LangInfo { get; set; }
    public string Path { get; set; } = "";

    [NotNull] public LangInterpreter? Interpreter { get; set; }

    #endregion

    #region Variate

    // 作用域栈，每个作用域是一个 Dictionary
    // Scopes[0] 是全局作用域，Scopes[^1] 是当前作用域
    private List<Dictionary<string, LangValueType>> Scopes { get; } = [new()];

    public List<ImportInfo> ImportInfos { get; } = [];

    #endregion

    #region Return

    public bool IsReturn { get; set; }
    public LangValueType Result { get; set; } = new VoidLangValue();

    #endregion

    #region Block

    public bool IsFunc { get; set; }
    public bool IsClass { get; set; }

    // 递归深度限制
    private const int MaxRecursionDepth = 1000;

    public int RecursionDepth
    {
        get;
        set
        {
            if (value > MaxRecursionDepth)
            {
                throw new RecursionError(
                    new SourcePosition(0, 0),
                    MaxRecursionDepth
                );
            }

            field = value;
        }
    }

    #endregion

    public void Set(LangId id, LangValueType langValueType)
    {
        // 检查是否是函数调用中的参数设置
        // 如果是，直接添加到当前作用域，创建新的局部变量
        if (IsFunc)
        {
            Scopes[^1][id.IdName] = langValueType;
            return;
        }

        // 1. 先查找变量，从当前作用域向父作用域查找
        for (var i = Scopes.Count - 1; i >= 0; i--)
        {
            if (!Scopes[i].ContainsKey(id.IdName)) continue;
            Scopes[i][id.IdName] = langValueType;
            return;
        }

        // 3. 没有找到变量，在当前作用域中创建新变量
        Scopes[^1][id.IdName] = langValueType;
    }

    public void AddChildren()
    {
        // 创建新的作用域
        Scopes.Add(new Dictionary<string, LangValueType>());
    }

    public void RemoveChildren()
    {
        // 移除当前作用域
        if (Scopes.Count > 1)
        {
            Scopes.RemoveAt(Scopes.Count - 1);
        }
    }

    public LangValueType? GetValue(LangId id)
    {
        // 从当前作用域（栈顶）向全局作用域（栈底）查找
        for (var i = Scopes.Count - 1; i >= 0; i--)
        {
            if (Scopes[i].TryGetValue(id.IdName, out var value))
            {
                return value;
            }
        }

        // 如果还是没有找到，尝试查找导入的函数或类
        return GetAny(id);
    }

    /// <summary>
    /// 根据函数名和参数数量查找函数
    /// </summary>
    /// <param name="id">函数名</param>
    /// <param name="paramCount">参数数量</param>
    /// <returns>找到的函数或null</returns>
    public FuncLangValue? GetFunc(LangId id, int paramCount)
    {
        return ImportInfos.FirstOrDefault(x =>
            x is FuncLangValue func &&
            func.Id!.IdName == id.IdName &&
            func.Ids?.Count == paramCount) as FuncLangValue;
    }

    public ImportInfo? GetAny(LangId id)
    {
        return ImportInfos.FirstOrDefault(x =>
        {
            return x switch
            {
                FuncLangValue func => func.Id!.IdName == id.IdName,
                TypeTemplate template => template.ClassName == id.IdName,
                NativeAnyLangValue na => na.RegisterName == id.IdName, // 使用 RegisterName 而不是 ClassName
                NativeStaticAny staticAny => staticAny.ClassName == id.IdName,
                _ => false
            };
        });
    }

    public void AddClassAndFunc(ImportInfo langValue)
    {
        ImportInfos.Add(langValue);
    }

    private void AddVariate(string name, LangValueType langValueType)
    {
        // 检查当前作用域中是否已存在同名变量
        if (Scopes[^1].ContainsKey(name))
        {
            // 创建一个默认的SourcePosition，因为AddVariate方法没有位置信息
            // 在实际使用中，应该从调用处传递位置信息
            throw new DuplicateNameError(langValueType.Position, name, "变量");
        }

        Scopes[^1][name] = langValueType;
    }

    public void ClearReturn()
    {
        IsReturn = false;
        Result = new VoidLangValue();
    }

    public void Init(Dictionary<string, LangValueType> result)
    {
        // 初始化方法实现
        // 将结果添加到管理器中
        foreach (var item in result)
        {
            // 使用 AddVariate 方法添加变量
            AddVariate(item.Key, item.Value);
        }
    }

    public VariateManager Clone()
    {
        // 克隆方法实现
        var newManager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter,
            IsFunc = IsFunc,
            IsClass = IsClass,
            IsReturn = IsReturn,
            Result = Result
        };

        // 深拷贝作用域栈
        foreach (var scope in Scopes)
        {
            var newScope = new Dictionary<string, LangValueType>(scope);
            newManager.Scopes.Add(newScope);
        }

        // 移除初始化时的空作用域（因为构造函数已经创建了一个）
        if (newManager.Scopes.Count > 0 && Scopes.Count > 0)
        {
            newManager.Scopes.RemoveAt(0);
        }

        // 复制导入信息
        newManager.ImportInfos.AddRange(ImportInfos);

        return newManager;
    }

    public VariateManager NewManger()
    {
        var newManager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter,
            // 复制返回和函数状态
            IsReturn = IsReturn,
            Result = Result,
            IsFunc = IsFunc,
            IsClass = IsClass
        };

        // 深拷贝作用域栈
        foreach (var newScope in Scopes.Select(scope => new Dictionary<string, LangValueType>(scope)))
        {
            newManager.Scopes.Add(newScope);
        }

        // 移除初始化时的空作用域（因为构造函数已经创建了一个）
        if (newManager.Scopes.Count > 0 && Scopes.Count > 0)
        {
            newManager.Scopes.RemoveAt(0);
        }

        // 复制导入信息
        newManager.ImportInfos.AddRange(ImportInfos);

        return newManager;
    }
}