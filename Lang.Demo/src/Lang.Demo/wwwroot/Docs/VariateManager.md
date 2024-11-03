# VariateManager

一个语言最需要的就是一个变量储存器，所以我们这里写一个

## 基础字段

VariateManager需要一个Variates(List `<OldID>`，储存变量名)，VariateDirectValue(List `<int>` ，储存 变量名 指向 值列表 的列表)，Values（Dic `<int,OldValue>,值，其实用List就行了，当时没想到，后面会改`）

```csharp
public List<int> VariateDirectValue { get; set; } = new List<int>();
public List<OldID> Variates { get; set; } = new List<OldID>();
public Dictionary<int, OldValue> Values { get; set; } = new Dictionary<int, OldValue>();
```

然后初始化和实例化需要一个字典来存储

```csharp
public Dictionary<string, OldValue> ClassAndFuncInfo { get; set; } = new Dictionary<string, OldValue>();
```

为了支持for,if,while这些语句，我们还需要标记在这些语句中新添加的变量：

```csharp
public int Count { get; set; } = 0;
public List<int> ChildrenNum { get; set; } = new List<int>();
```

## 方法

首先我们需要一个方法来进行赋值：

```csharp
public ValueTuple<OldID, OldExpr> Set(OldID id, OldExpr value)
    {
        var a1 = GetValue(id);
        if (a1 is null)
        {
            //init
            if (value is OldValue)
            {
                var a =  value as OldValue;
                Variates.Add(id);
                Values.Add(Count,a);
                VariateDirectValue.Add(Count);
                Count++;
                return (id,a);
            }else if (value is OldID)
            {
                var a = value as OldID;
                return Direct(id, a);
            }
        }
        else
        {
            //reset
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            if (value is OldID)
            {
                var b = Variates.FindLastIndex(x => x.IdName == (value as OldID).IdName);
                Values[VariateDirectValue[a]] = Values[VariateDirectValue[b]];
            }
            else
            {
                Values[VariateDirectValue[a]] = value as OldValue;
            }
        }
        return (id, value);
    }
```

还需要一个方法来查看值:

```csharp
 public OldValue GetValue(OldID id)
    {
        try
        {
            var a = Variates.FindLastIndex(x => x.IdName == id.IdName);
            int b = VariateDirectValue[a];
            return Values[b];
        }
        catch (Exception e)
        {
            return null;
        }
    }
```

我们还需要标记那些未来要删掉的变量，以及删除变量的函数：

```csharp
public void AddChildren()
    {
        ChildrenNum.Add(Count+1);
    }

    public void RemoveChildren()
    {
        for (; Count >= ChildrenNum.Last(); Count--)
        {
            Variates.RemoveAt(Count - 1);
            VariateDirectValue.RemoveAt(Count - 1);
        }
        var a = ChildrenNum.Last();
        ChildrenNum.Remove(a);
    }
```

还有关于类和方法的函数：

```csharp
public void Init()
    {
        Values = new Dictionary<int, OldValue>();
        Variates = new List<OldID>();
        VariateDirectValue = new List<int>();
    }

    public void Init(Dictionary<OldID, OldValue> values)
    {
        int i = 0;
        foreach (var VARIABLE in values)
        {
            Variates.Add(VARIABLE.Key);
            VariateDirectValue.Add(i);
            Values.Add(i,VARIABLE.Value);
        }
    }

    public ValueTuple<OldID, OldValue> AddClassAndFunc(OldID id, OldValue value)
    {
        ClassAndFuncInfo.Add(id.IdName,value);
        return (id, value);
    }
```

现在我们应该差不多了。
