using System.Reflection.Emit;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression;

public class LangId(string name, string assumptionType = "", OldExpr? defaultValue = null, SourcePosition position = default) : OldExpr(position)
{
    public readonly string IdName = name;
    public override string ToString() => IdName;
    public string AssumptionType { get; } = assumptionType;
    public OldExpr? DefaultValue { get; } = defaultValue;

    public override bool Equals(object? obj)
    {
        var a = obj as LangId;
        return a?.IdName == IdName;
    }

    public override int GetHashCode()
    {
        return IdName.GetHashCode();
    }

    public override LangValueType Run(LangParser.VariateManager manager) 
    {
        if (IdName == "this")
        {
            // 直接从变量储存器中获取名为"this"的变量
            var thisValue = manager.GetValue(new LangId("this"));
            if (thisValue != null)
            {
                return thisValue;
            }
            
            // 如果没有找到，抛出NameError异常，因为this关键字只能在类的方法中使用
            throw new NameError(this, "this");
        }
        
        // 先尝试获取普通变量
        var value = manager.GetValue(this);
        if (value != null)
        {
            return value;
        }
        
        // 如果不是普通变量，尝试获取类或函数
        var anyValue = manager.GetAny(this);
        if (anyValue != null)
        {
            return anyValue as LangValueType ?? throw new NameError(this, IdName);
        }
        
        // 如果都没有找到，抛出NameError异常
        throw new NameError(this, IdName);
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        var value = local.GetLocalVar(IdName);
        if (value is null) return;
        ilGenerator.Emit(OpCodes.Ldloc, value.LocalIndex);
    }

    public override Type OutputType(LocalManager local)
    {
        if (!string.IsNullOrEmpty(AssumptionType))
        {
            // 解析泛型类型注解，如 "list<int>" 或 "array<string>"
            var typeName = AssumptionType.Trim().ToLower();
            
            // 检查是否为泛型类型
            if (typeName.Contains('<') && typeName.EndsWith('>'))
            {
                // 提取泛型类型名称和参数
                var genericIndex = typeName.IndexOf('<');
                var baseTypeName = typeName[..genericIndex].Trim();
                var genericArg = typeName[(genericIndex + 1)..^1].Trim();
                
                // 解析泛型参数类型
                var argType = genericArg switch
                {
                    "int" => typeof(int),
                    "double" => typeof(double),
                    "string" => typeof(string),
                    "bool" => typeof(bool),
                    "char" => typeof(char),
                    "object" => typeof(object),
                    _ => typeof(object) // 默认为object
                };
                
                // 返回泛型类型
                return baseTypeName switch
                {
                    "list" => typeof(List<>).MakeGenericType(argType),
                    "array" => argType.MakeArrayType(),
                    "dictionary" => typeof(Dictionary<,>).MakeGenericType(typeof(object), argType),
                    _ => typeof(object) // 未知泛型类型，默认为object
                };
            }
            
            // 非泛型类型
            return typeName switch
            {
                "int" => typeof(int),
                "double" => typeof(double),
                "string" => typeof(string),
                "bool" => typeof(bool),
                "char" => typeof(char),
                "void" => typeof(void),
                "list" => typeof(List<object>),
                "array" => typeof(object[]),
                "dictionary" => typeof(Dictionary<object, object>),
                "tuple" => typeof(ValueTuple<object, object>),
                _ => typeof(object)
            };
        }

        if (local.InClassEnv != null && IdName == "this")
        {
            return local.InClassEnv;
        }

        var value = local.GetLocalVar(IdName);
        return value?.LocalType ?? typeof(object);
    }
}