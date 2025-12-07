using System.Diagnostics.CodeAnalysis;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using ValueType = Old8Lang.AST.Expression.ValueType;

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
    private List<ValueType> Values { get; } = [];

    public List<ValueType> AnyInfo { get; private init; } = [];

    #endregion

    #region Return

    public bool IsReturn { get; set; }
    public ValueType Result { get; set; } = new VoidValue();

    #endregion

    #region Block

    private int Count { get; set; }
    public bool IsFunc { get; set; }

    private List<int> ChildrenNum { get; } = [];

    public bool IsClass { get; set; }

    #endregion

    public void Set(OldId id, ValueType valueType)
    {
        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            VariateName.Add(id.IdName);
            Values.Add(valueType);
            Count++;
            return;
        }

        //reset
        var count = VariateName.IndexOf(id.IdName);
        Values[count] = valueType;
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

    public ValueType? GetValue(OldId id)
    {
        var count = VariateName.IndexOf(id.IdName);
        return count != -1 ? Values[count] : GetAny(id);
    }

    public ValueType? GetAny(OldId id)
    {
        return AnyInfo.FirstOrDefault(x =>
        {
            return x switch
            {
                FuncValue func => func.Id!.IdName == id.IdName,
                AnyValue any => any.Id.IdName == id.IdName,
                NativeAnyValue na => na.ClassName == id.IdName,
                NativeStaticAny staticAny => staticAny.ClassName == id.IdName,
                _ => false
            };
        });
    }

    public void AddClassAndFunc(ValueType value)
    {
        AnyInfo.Add(value);
    }

    public void AddFunc(ValueType value)
    {
        AnyInfo.Add(value);
    }

    public void AddClass(ValueType value)
    {
        AnyInfo.Add(value);
    }

    public void AddVariate(string name, ValueType valueType)
    {
        if (VariateName.Contains(name))
        {
            // 创建一个默认的SourcePosition，因为AddVariate方法没有位置信息
            // 在实际使用中，应该从调用处传递位置信息
            throw new DuplicateNameError(new SourcePosition(), name, "变量");
        }
        VariateName.Add(name);
        Values.Add(valueType);
        Count++;
    }

    public void ClearReturn()
    {
        IsReturn = false;
        Result = new VoidValue();
    }
    
    public void Init(Dictionary<string, ValueType> result)
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