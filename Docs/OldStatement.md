# OldStatement

OldStatement处理各种语句，也是程序运行的主函数。

OldStatement包括：

```
ClassInit.cs （类的初始化）、
DirectStatement.cs （指向语句）、
ForStatement.cs （For语句）、
FuncInit.cs （函数初始化）、
If_Elif_ElseStatement.cs （if语句）、
ImportStatement.cs （引用语句）、
InstanceStatement.cs （实例化）、
NativeStatement.cs （原生函数）、
OldIf.cs （处理if语句的模块）、
SetStatement.cs （赋值语句）、
WhileStatement.cs （while语句）
```

## Set,Direct

### EBNF:

```
set: IDENTFIER SET[d] OldParser_expressions
set: OldParser_expressions DIS_SET[d] IDENTFIER
statement: IDENTFIER DIRECT[d] IDENTFIER
```

这两个类型主要是使用变量管理器的Set()和Direct()，所以我们可以这么写：

### Code:

#### Set:

```csharp
public class SetStatement : OldStatement
{
    public OldID Id { get; set; } 
    public OldExpr Value { get; set; }
    public List<OldID> Init_ID { get; set; }

    public SetStatement(OldID id, OldExpr value)
    {
        Id = id;
        Value = value;
    }

    public SetStatement(OldID id, OldExpr value, List<OldID> a)
    {
        Id = id;
        Value = value;
        Init_ID = a;
    }

    public override void Run(ref VariateManager Manager)
    {
        if (Value is BinaryOperation)
        {
            var a = Value.Run(ref Manager);
            var b = a as OldValue;
            Manager.Set(Id, b);
        }else if(Value is OldID)
        {
            var a = Manager.GetValue(Value as OldID);
            if (a is OldFunc)
            {
                if ((a as OldFunc).Return is not null)
                {
                    Value = Manager.GetValue(Value as OldID).Run(ref Manager);
                }
                else
                {
                    return;
                }
            }
            Manager.Set(Id, Value);
        }
        else
        {
            Manager.Set(Id, Value);
        }
    }

    public override string ToString() => $"setStatement : id = {Id} , expr = {Value} \n at the location : {Location}";
}
```

#### Direct:

```csharp
public class DirectStatement : OldStatement
{
    public OldID ID1 { get; set; }
    public OldID ID2 { get; set; }
    public DirectStatement(OldID id1, OldID id2)
    {
        ID1 = id1;
        ID2 = id2;
    }

    public override void Run(ref VariateManager Manager)
    {
        Manager.Direct(ID1, ID2);
    }
}
```

Set函数因为要进行expr的运算和各种判空，所以会比较复杂一点。

## For While

For和While循环在C#中我们统一使用While来写

### EBNF:

```csharp
statement: FOR[d] set DOUHAO[d] OldParser_expressions DOUHAO[d] statement block
statement: WHILE[d] OldParser_expressions block
```

### Code:

#### For:

```csharp
public SetStatement SetStatement { get; set;}
    public BinaryOperation Expr { get; set; }
    public OldStatement Statement { get; set; }
    public BlockStatement ForBlockStatement { get; set; }

    public ForStatement(SetStatement setStatement, BinaryOperation expr, OldStatement statement, BlockStatement blockStatement)
    {
        SetStatement = setStatement;
        Expr = expr;
        Statement = statement;
        ForBlockStatement = blockStatement;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool expr = false;
        Manager.AddChildren();
        SetStatement.Run(ref Manager);
        while (true)
        {
            var varexpr = Expr.Run(ref Manager);
            if (varexpr is OldBool)
            {
                expr = (varexpr as OldBool).Value;
            }
            if (expr)
            {
                ForBlockStatement.Run(ref Manager);
                Statement.Run(ref Manager);
            }
            else
            {
                Manager.RemoveChildren();
                return;
            }
        }
    }
```

#### While:

```csharp
public BinaryOperation Expr { get; set; }
    public BlockStatement BlockStatement { get; set; }

    public WhileStatement(BinaryOperation expr, BlockStatement blockStatement)
    {
        Expr = expr;
        BlockStatement = blockStatement;
    }

    public override void Run(ref VariateManager Manager)
    {
        bool expr = false;
        Manager.AddChildren();
        while (true)
        {
            var varbool = Expr.Run(ref Manager);
            if (varbool is OldBool)
            {
                expr = (bool)(Expr.Run(ref Manager) as OldBool).Value;
            }
            if (expr)
            {
                BlockStatement.Run(ref Manager);
            }
            else
            {
                Manager.RemoveChildren();
                return;
            }
        }
    
    }
```

## NativeMethod Import

### EBNF：

```csharp
statement: L_BRACKET[d] IMPORT[d] STRING IDENTFIER IDENTFIER IDENTFIER? R_BRACKET[d]
statement: IMPORT[d] IDENTFIER
```

这两个主要使用Assembly和APIs.cs来实现

### Code:

#### NativeMethod:

```csharp
public string DLL_NAME { get; set; }
    public string CLASS_NAME { get; set; }
    public string METHOD_NAME { get; set; }
    public string NATIVE_NAME { get; set; }

    public NativeStatement(string dllName, string className, string methodName , string NativeName)
    {
        DLL_NAME = dllName;
        CLASS_NAME = className;
        METHOD_NAME = methodName;
        NATIVE_NAME = NativeName;
    }

    public override void Run(ref VariateManager Manager)
    {
        string path = $"/dll/{DLL_NAME}";
        Assembly assembly = Assembly.LoadFile(path);
        Type type = assembly.GetType(CLASS_NAME);
        MethodInfo methodInfo = type.GetMethod(METHOD_NAME);
        //PropertyInfo propertyInfo = type.GetProperty(METHOD_NAME);
        //var a = methodInfo.Invoke()
        if (NATIVE_NAME is null)
            NATIVE_NAME = METHOD_NAME;
        var Func = new OldFunc(NATIVE_NAME, methodInfo);
        Manager.AddClassAndFunc(new OldID(NATIVE_NAME), Func);
    }
```

#### Import:

```csharp
public string ImportString { get; set; }

    public ImportStatement(string importString)
    {
        ImportString = importString;
    }

    public override void Run(ref VariateManager Manager)
    {
        //查找
        var path = APIs.ImportSearch(ImportString);
        //
        var a = APIs.CslyUsing(APIs.FromDirectory(path));
        //
        Manager = a.Manager;
        Manager.Init();
    }
```

到现在，我们已经完成了OldStatement的构建。
