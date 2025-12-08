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
    public string? FileName { get; set; }

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
        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            VariateName.Add(id.IdName);
            Values.Add(langValueType);
            Count++;
            return;
        }

        //reset
        var count = VariateName.IndexOf(id.IdName);
        Values[count] = langValueType;
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
        var count = VariateName.IndexOf(id.IdName);
        return count != -1 ? Values[count] : GetAny(id);
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
            throw new DuplicateNameError(new SourcePosition(), name, "变量");
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
            FileName = FileName,
            Interpreter = Interpreter
        };
        return newManager;
    }

    public VariateManager NewManger()
    {
        // 创建新管理器方法实现
        return new VariateManager
        {
            LangInfo = LangInfo,
            Path = Path,
            FileName = FileName,
            Interpreter = Interpreter
        };
    }
}