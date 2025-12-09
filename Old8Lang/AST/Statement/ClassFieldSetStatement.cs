using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 带有修饰符的类字段声明语句
/// </summary>
public class ClassFieldSetStatement : OldStatement
{
    public readonly ClassMemberId Id;
    public readonly OldExpr Value;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="id">类成员ID，包含修饰符信息</param>
    /// <param name="value">字段值</param>
    /// <param name="position">位置信息</param>
    public ClassFieldSetStatement(ClassMemberId id, OldExpr value, SourcePosition position = default) : base(position)
    {
        Id = id;
        Value = value;
    }

    public override void Run(VariateManager manager)
    {
        var result = Value.Run(manager);

        // 如果有类型注解，进行类型检查
        if (!string.IsNullOrEmpty(Id.AssumptionType))
        {
            var expectedType = Id.AssumptionType.ToLower();
            var actualType = result.TypeToString().ToLower();

            // 建立类型匹配映射
            var typeMap = new Dictionary<string, List<string>>
            {
                { "int", ["int"] },
                { "double", ["double"] },
                { "string", ["string"] },
                { "bool", ["bool"] },
                { "char", ["char"] },
                { "array", ["array"] },
                { "dictionary", ["dictionary"] },
                { "list", ["list"] },
                { "tuple", ["tuple"] },
                { "type", ["type"] },
                { "function", ["function"] }
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

        // 类成员声明直接存储在ClassMemberId中，由BlockStatement的ToAnyData和ToStaticData方法处理
        manager.Set(Id, result);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        Value.SetValueToIl(ilGenerator, local, Id.IdName);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => $"{Id} <- {Value}";
}