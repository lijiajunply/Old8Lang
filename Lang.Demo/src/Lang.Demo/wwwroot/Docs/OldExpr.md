# OldExpr

OldExpr分为三种：OldValue(值)、OldID(记录变量)、BinaryOperation (多级运算)

## OldValue

OldValue记录值和二元运算函数，所以需要这些东西：

```csharp
public object Value { get; set; }
    public override bool Equals(object? value)
    {
        var a = value as OldValue;
        return Value.ToString() == a.Value.ToString();
    }
    public virtual OldValue PLUS(OldValue otherValue) => new OldValue();
    public virtual OldValue MINUS(OldValue otherValue) => new OldValue();
    public virtual OldValue TIMES(OldValue otherValue) => new OldValue();
    public virtual OldValue DIVIDE(OldValue otherValue) => new OldValue();
    public override string ToString() => Value.ToString();
    public virtual OldValue Dot(OldID DotID)
    {
        if (DotID.IdName == "toint")
        {
            return new OldInt((int)Value);
        }
        if (DotID.IdName == "tostring")
        {
            return new OldString((string)Value);
        }

        if (DotID.IdName == "tochar")
        {
            return new OldChar(((string)Value)[0]);
        }
        return null;
    }
    public virtual bool EQUAL(OldValue otherValue) => Value == otherValue.Value;
    public virtual bool LESS(OldValue otherValue) => false;
    public virtual bool GREATER(OldValue otherValue) => false;
```

OldValue下面有 OldAny（Class）、OldBool(Bool)、OldChar(Char)、OldDir(字典)、OldDouble(浮点数)、OldFunc(function)、OldInt(int)、OldList(列表)、OldString(string)

这些就不去讲了。

## OldID

OldID比较简单，就只要一个IDName就行：

```csharp
public string IdName { get; set; }
    public OldID(string name) => IdName = name;
    public override string ToString() => IdName;
    public override bool Equals(object? obj)
    {
        var a = obj as OldID;
        return a.IdName == IdName;
    }
```

## BinaryOperation

这个也还好，只需要传入left(OldExpr类型) oper(操作符) right(OldExpr类型)，然后再使用Run函数，来进行计算就行:

```csharp
public override OldExpr Run(ref VariateManager Manager)
    {
        var l = Left;
        var r = Right;
  
        // var/binary -> value (left)
        if (Left is OldID)
            l = Manager.GetValue(Left as OldID);
        if (Left is BinaryOperation)
            l = Left.Run(ref Manager);
  
        // id.id => dot_value
        if (l is OldValue && r is OldID && Oper == OldTokenGeneric.CONCAT)
            return (l as OldValue).Dot(r as OldID);

        // (right)
        if (Right is OldID && l is not OldAny)
            r = Manager.GetValue(Right as OldID);
        if (Right is BinaryOperation)
            r = Right.Run(ref Manager);

        // not right 
        if (r is OldBool && l == null && Oper == OldTokenGeneric.NOT)
            return new OldBool(!(bool)(r as OldBool).Value);
  
        // left and right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.AND)
            return new OldBool((bool)(l as OldBool).Value && (bool)(r as OldBool).Value);
  
        // left or right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.OR)
            return new OldBool((bool)(l as OldBool).Value || (bool)(r as OldBool).Value);
  
        // left xor right
        if (l is OldBool && r is OldBool && Oper == OldTokenGeneric.XOR)
            return new OldBool(!((bool)(l as OldBool).Value == (bool)(l as OldBool).Value));
  
        // - right
        if (l is null && r is OldInt && Oper == OldTokenGeneric.MINUS)
        {
            var r1 = r as OldInt;
            r1.Value = -(int)r1.Value;
            return r1;
        }
        if (r is OldDouble && l is null && Oper == OldTokenGeneric.MINUS)
        {
            var r1 = r as OldDouble;
            r1.Value = -(int)r1.Value;
            return r1;
        }
  
        // == , < , > 
        if (l is not null && r is not null && Oper == OldTokenGeneric.EQUALS)
            return new OldBool((l as OldValue).EQUAL(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.LESSER)
            return new OldBool((l as OldValue).LESS(r as OldValue));
        if (l is not null && r is not null && Oper == OldTokenGeneric.GREATER)
            return new OldBool((l as OldValue).GREATER(r as OldValue));
  
        // r (+-*/) l
        if (l is not null && r is not null && Oper != null)
        {
            var r1 = r as OldValue;
            var l1 = l as OldValue;
            switch (Oper)
            {
                case OldTokenGeneric.PLUS:
                    return l1.PLUS(r1);
                case OldTokenGeneric.MINUS:
                    return l1.MINUS(r1);
                case OldTokenGeneric.TIMES:
                    return l1.TIMES(r1);
                case OldTokenGeneric.DIVIDE:
                    return l1.DIVIDE(r1);
            }
        }
  
        return null;
    }
```

就这样，我们把OldExpr全部都写完了。
