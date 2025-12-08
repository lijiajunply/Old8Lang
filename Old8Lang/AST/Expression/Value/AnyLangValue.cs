using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

public class AnyLangValue : LangValueType
{
    public readonly Dictionary<LangId, OldExpr> Variates;
    public readonly Dictionary<string, LangValueType> Result = [];
    public readonly LangId Id;

    public readonly VariateManager Manager;

    public AnyLangValue(LangId id, Dictionary<LangId, OldExpr> variates, SourcePosition position = default) : base(position)
    {
        Variates = variates;
        Id = id;
        Manager = new VariateManager();
        Run(Manager);
        Manager.Init(Result);
        Manager.IsClass = true;
    }

    public AnyLangValue(Dictionary<LangId, OldExpr> variates, SourcePosition position = default) : base(position)
    {
        Variates = variates;
        Id = new LangId("JsonNative");
        Manager = new VariateManager();
        foreach (var variate in variates)
        {
            if (variate.Value is LangValueType valueType) Result.Add(variate.Key.IdName, valueType);
        }

        Manager.Init(Result);
        Manager.IsClass = true;
    }

    public sealed override LangValueType Run(VariateManager manager)
    {
        Manager.AnyInfo.AddRange(manager.AnyInfo.Where(x => x is not FuncLangValue).ToList());
        foreach (var variable in Variates.Keys)
            Result.Add(variable.IdName, Variates[variable].Run(manager));
        return this;
    }

    public override LangValueType Dot(OldExpr dotExpr)
    {
        switch (dotExpr)
        {
            case LangId id:
            {
                var a = Manager.GetValue(id);
                if (a == null) throw new AttributeError(this, id.IdName, Id.IdName);
                return a.Run(Manager);
            }
            case FuncLangValue func:
            {
                if (func.Id?.IdName == "GetType")
                    return new TypeLangValue(TypeToString());
                return func.Run(Manager);
            }
            default:
                return dotExpr.Run(Manager);
        }
    }

    public void Set(LangId id, LangValueType langValueType) => Manager.Set(id, langValueType);

    public override LangValueType Converse(LangValueType otherLangValueType, VariateManager manager)
    {
        if (otherLangValueType is not AnyLangValue typeAny) throw new TypeError(this, "AnyValue", otherLangValueType.GetType().Name);

        foreach (var a in Result)
        {
            typeAny.Set(new LangId(a.Key), a.Value);
        }

        return typeAny;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('{');
        for (var i = 0; i < Variates.Count; i++)
        {
            var variable = Variates.ElementAt(i);
            builder.Append($"{(i == 0 ? "" : ",")}\"{variable.Key}\":{variable.Value}");
        }

        builder.Append('}');
        return builder.ToString();
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 创建一个字典来存储AnyValue的属性
        var dictType = typeof(Dictionary<string, object>);
        var constructor = dictType.GetConstructor(Type.EmptyTypes)!;
        
        // 实例化字典
        ilGenerator.Emit(OpCodes.Newobj, constructor);
        
        // 遍历所有属性，将它们添加到字典中
        foreach (var variate in Variates)
        {
            // 复制字典引用到堆栈上
            ilGenerator.Emit(OpCodes.Dup);
            
            // 加载属性名
            ilGenerator.Emit(OpCodes.Ldstr, variate.Key.IdName);
            
            // 加载属性值
            variate.Value.LoadIlValue(ilGenerator, local);
            
            // 确保值是对象类型，如果是值类型则装箱
            var valueType = variate.Value.OutputType(local);
            if (valueType != null && valueType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }
            
            // 调用字典的Add方法
            var addMethod = dictType.GetMethod("Add", [typeof(string), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        }
    }

    public override Type? OutputType(LocalManager local)
    {
        return local.ClassVar.GetValueOrDefault(Id.IdName);
    }
}