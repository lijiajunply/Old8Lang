using System.Diagnostics.CodeAnalysis;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.LangParser;

public class VariateManager
{
    #region Lang

    public LangInfo? LangInfo { get; set; }
    public string Path { get; set; } = "";

    [NotNull] public IMiniInterpreter? Interpreter { get; set; }

    #endregion

    #region Variate

    //private Dictionary<string, ValueType> Variates { get; set; } = new();
    private List<string> VariateName { get; } = [];
    private List<LangValueType> Values { get; } = [];

    public List<ImportInfo> ImportInfos { get; } = [];

    #endregion

    #region Return

    public bool IsReturn { get; set; }
    public LangValueType Result { get; set; } = new VoidLangValue();

    #endregion

    #region Block

    private int Count { get; set; }
    public bool IsFunc { get; set; }

    private List<int> ChildrenNum { get; } = [];

    public bool IsClass { get; set; }

    #endregion

    public void Set(LangId id, LangValueType langValueType)
    {
        // 检查是否是函数调用中的参数设置
        // 如果是，直接添加到变量列表末尾，创建新的局部变量
        if (IsFunc)
        {
            VariateName.Add(id.IdName);
            Values.Add(langValueType);
            Count++;
            return;
        }

        // 1. 先查找变量，包括当前作用域和所有父作用域
        var findResult = FindVariable(id.IdName);

        if (findResult.Found)
        {
            // 2. 找到了变量，修改它的值
            Values[findResult.Index] = langValueType;
            return;
        }

        // 3. 没有找到变量，在当前作用域中创建新变量
        VariateName.Add(id.IdName);
        Values.Add(langValueType);
        Count++;
    }

    /// <summary>
    /// 查找变量，包括当前作用域和所有父作用域
    /// </summary>
    /// <param name="name">变量名</param>
    /// <returns>查找结果，包括是否找到和变量索引</returns>
    private (bool Found, int Index) FindVariable(string name)
    {
        // 获取当前作用域的起始索引
        var currentScopeStart = ChildrenNum.Count > 0 ? ChildrenNum[^1] : 0;

        // 1. 先在当前作用域中查找
        for (var i = Count - 1; i >= currentScopeStart; i--)
        {
            if (VariateName[i] == name)
            {
                return (true, i);
            }
        }

        // 2. 当前作用域中没有找到，递归查找父作用域
        if (ChildrenNum.Count > 0)
        {
            // 临时保存当前作用域状态
            var tempCount = Count;
            var tempChildrenNum = ChildrenNum[^1];

            // 移除当前子作用域，进入父作用域
            ChildrenNum.RemoveAt(ChildrenNum.Count - 1);
            Count = tempChildrenNum;

            try
            {
                // 递归查找父作用域
                return FindVariable(name);
            }
            finally
            {
                // 恢复当前作用域状态
                ChildrenNum.Add(tempChildrenNum);
                Count = tempCount;
            }
        }

        // 3. 所有作用域中都没有找到
        return (false, -1);
    }

    public void AddChildren()
    {
        ChildrenNum.Add(Count);
    }

    public void RemoveChildren()
    {
        var num = ChildrenNum[^1];

        while (Count > num)
        {
            Values.RemoveAt(Count - 1);
            VariateName.RemoveAt(Count - 1);
            Count--;
        }

        ChildrenNum.Remove(ChildrenNum[^1]);
    }

    public LangValueType? GetValue(LangId id)
    {
        // 获取当前作用域的起始索引
        var currentScopeStart = ChildrenNum.Count > 0 ? ChildrenNum[^1] : 0;

        // 从当前作用域的末尾向前查找，只查找当前作用域中的变量
        // 这样可以确保找到的是当前作用域中最新的变量，而不是父作用域中的变量
        for (var i = Count - 1; i >= currentScopeStart; i--)
        {
            if (VariateName[i] == id.IdName)
            {
                return Values[i];
            }
        }

        // 如果当前作用域中没有找到，尝试在父作用域中查找
        if (ChildrenNum.Count > 0)
        {
            // 临时移除当前作用域，递归查找父作用域
            var currentScopeEnd = Count;
            // var currentScopeSize = currentScopeEnd - currentScopeStart;

            // 临时调整状态
            ChildrenNum.RemoveAt(ChildrenNum.Count - 1);
            Count = currentScopeStart;

            try
            {
                // 递归查找父作用域
                return GetValue(id);
            }
            finally
            {
                // 恢复状态
                Count = currentScopeEnd;
                ChildrenNum.Add(currentScopeStart);
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
                NativeAnyLangValue na => na.ClassName == id.IdName,
                NativeStaticAny staticAny => staticAny.ClassName == id.IdName,
                _ => false
            };
        });
    }

    public void AddClassAndFunc(ImportInfo langValue)
    {
        ImportInfos.Add(langValue);
    }

    public void AddVariate(string name, LangValueType langValueType)
    {
        if (VariateName.Contains(name))
        {
            // 创建一个默认的SourcePosition，因为AddVariate方法没有位置信息
            // 在实际使用中，应该从调用处传递位置信息
            throw new DuplicateNameError(langValueType.Position, name, "变量");
        }

        VariateName.Add(name);
        Values.Add(langValueType);
        Count++;
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
            Interpreter = Interpreter
        };
        return newManager;
    }

    public VariateManager NewManger()
    {
        var newManager = new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            Interpreter = Interpreter,
            // 复制作用域信息
            Count = Count,
            // 复制返回和函数状态
            IsReturn = IsReturn,
            Result = Result,
            IsFunc = IsFunc,
            IsClass = IsClass
        };
        
        // 复制变量列表（使用 AddRange 因为属性是只读的）
        newManager.VariateName.AddRange(VariateName);
        newManager.Values.AddRange(Values);
        
        // 复制作用域信息
        newManager.ChildrenNum.AddRange(ChildrenNum);
        
        // 复制导入信息
        newManager.ImportInfos.AddRange(ImportInfos);
        
        return newManager;
    }
}