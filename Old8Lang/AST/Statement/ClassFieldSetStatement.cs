using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
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
    
    public override T Accept<T>(IVisitor<T> visitor) => visitor.Visit(this);

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
            
            // 解析泛型类型注解，如 "list<int>" 或 "array<string>"
            var isGeneric = expectedType.Contains('<') && expectedType.EndsWith('>');
            string baseExpectedType;
            string genericArg = "";
            
            if (isGeneric)
            {
                // 提取泛型类型名称
                var genericIndex = expectedType.IndexOf('<');
                baseExpectedType = expectedType[..genericIndex].Trim();
                genericArg = expectedType[(genericIndex + 1)..^1].Trim();
            }
            else
            {
                baseExpectedType = expectedType;
            }

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

            // 检查基础类型是否匹配
            if (typeMap.TryGetValue(baseExpectedType, out var allowedTypes))
            {
                if (!allowedTypes.Contains(actualType))
                {
                    throw new TypeError(Id, expectedType, actualType);
                }
            }
            
            // 如果是泛型类型，检查元素类型是否匹配
            if (isGeneric && (baseExpectedType == "list" || baseExpectedType == "array" || baseExpectedType == "dictionary"))
            {
                // 对于列表和数组，检查元素类型
                if (result is ListLangValue listValue)
                {
                    // 检查列表中的所有元素类型是否匹配泛型参数
                    foreach (var item in listValue.Values)
                    {
                        var itemType = item.TypeToString().ToLower();
                        if (itemType != genericArg)
                        {
                            throw new TypeError(Id, expectedType, actualType, $"列表元素类型不匹配：期望 {genericArg}，实际 {itemType}");
                        }
                    }
                }
                else if (result is ArrayLangValue arrayValue)
                {
                    // 检查数组中的所有元素类型是否匹配泛型参数
                    foreach (var item in arrayValue.GetItems())
                    {
                        var itemType = item.TypeToString().ToLower();
                        if (itemType != genericArg)
                        {
                            throw new TypeError(Id, expectedType, actualType, $"数组元素类型不匹配：期望 {genericArg}，实际 {itemType}");
                        }
                    }
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