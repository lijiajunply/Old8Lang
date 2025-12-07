using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class SetStatement(OldId id, OldExpr value, SourcePosition position = default) : OldStatement(position)
{
    public readonly OldId Id = id;
    public readonly OldExpr Value = value;

    public override void Run(VariateManager manager)
    {
        var result = Value.Run(manager);
        
        // 如果有类型注解，进行类型检查
        if (!string.IsNullOrEmpty(Id.AssumptionType))
        {
            var expectedType = Id.AssumptionType.ToLower();
            var actualType = result.TypeToString().ToLower();
            
            // 建立类型匹配映射
            var typeMap = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>
            {
                { "int", new System.Collections.Generic.List<string>{ "int" } },
                { "double", new System.Collections.Generic.List<string>{ "double" } },
                { "string", new System.Collections.Generic.List<string>{ "string" } },
                { "bool", new System.Collections.Generic.List<string>{ "bool" } },
                { "char", new System.Collections.Generic.List<string>{ "char" } }
            };
            
            // 检查类型是否匹配
            if (typeMap.TryGetValue(expectedType, out var allowedTypes))
            {
                if (!allowedTypes.Contains(actualType))
                {
                    throw new TypeError(Id, expectedType, actualType);
                }
            }
        }
        
        manager.Set(Id, result);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        Value.SetValueToIl(ilGenerator, local,Id.IdName);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} <- {Value}";
}