using Old8Lang.LangParser;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class SetStatement : OldStatement
{
    public readonly LangId? Id;
    public readonly OldExpr? LeftExpr;
    public readonly OldExpr Value;

    public SetStatement(LangId id, OldExpr value, SourcePosition position = default) : base(position)
    {
        Id = id;
        LeftExpr = null;
        Value = value;
    }

    public SetStatement(OldExpr leftExpr, OldExpr value, SourcePosition position = default) : base(position)
    {
        Id = null;
        LeftExpr = leftExpr;
        Value = value;
    }

    public override void Run(VariateManager manager)
    {
        var result = Value.Run(manager);

        // 如果有类型注解，进行类型检查
        if (Id != null && !string.IsNullOrEmpty(Id.AssumptionType))
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

        // 处理成员访问赋值：this.name <- value, person.name <- value
        if (LeftExpr is Operation operation)
        {
            // 检查是否是 CONCAT 操作（成员访问）
            if (operation.Opera == OperationType.CONCAT)
            {
                // 处理 this.member <- value 形式的赋值
                if (operation is { Left: LangId { IdName: "this" }, Right: LangId memberName })
                {
                    // 查找当前实例
                    if (manager.GetValue(new LangId("this")) is AnyLangValue anyValue)
                    {
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyValue.Result[memberName.IdName] = result;
                        // 同时更新VariateManager中的值，确保后续访问能获取到最新值
                        anyValue.Manager.Set(new LangId(memberName.IdName), result);
                        // 同时更新当前manager中的值，确保在同一个方法中后续访问能获取到最新值
                        manager.Set(new LangId(memberName.IdName), result);
                        return;
                    }

                    // 如果没有找到，可能是在init方法中，此时需要检查manager.IsFunc标志
                    if (manager.IsFunc)
                    {
                        // 在init方法中，当前实例应该是manager.AnyInfo中的第一个AnyLangValue
                        anyValue = manager.GetValue(new LangId("this")) as AnyLangValue ??
                                   throw new NameError(this, "this");
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyValue.Result[memberName.IdName] = result;
                        // 同时更新VariateManager中的值，确保后续访问能获取到最新值
                        anyValue.Manager.Set(new LangId(memberName.IdName), result);
                        // 同时更新当前manager中的值，确保在同一个方法中后续访问能获取到最新值
                        manager.Set(new LangId(memberName.IdName), result);
                        return;
                    }
                }
                // 处理普通对象成员访问：person.name <- value
                else if (operation is { Left: { } leftExpr, Right: LangId memberNameObj })
                {
                    // 获取左侧对象的值
                    var leftValue = leftExpr.Run(manager);
                    if (leftValue is AnyLangValue anyObj)
                    {
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyObj.Result[memberNameObj.IdName] = result;
                        // 同时更新对象的管理器中的值
                        anyObj.Manager.Set(new LangId(memberNameObj.IdName), result);
                        return;
                    }
                }
            }
        }
        // 处理索引访问赋值：array[index] <- value, list[index] <- value, dict[key] <- value
        else if (LeftExpr is LangListItem listItem)
        {
            // 获取集合对象
            var collectionValue = manager.GetValue(listItem.ListId);
            // 获取索引或键
            var indexValue = listItem.Key.Run(manager);

            // 检查集合是否是ILangList类型，如果是则调用其Set方法
            if (collectionValue is ILangList listCollection)
            {
                listCollection.Set(indexValue, result);
                return;
            }
        }

        // 处理普通变量赋值：name <- value
        if (Id != null && !string.IsNullOrEmpty(Id.IdName))
        {
            manager.Set(Id, result);
        }
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        if (Id != null && !string.IsNullOrEmpty(Id.IdName))
        {
            Value.SetValueToIl(ilGenerator, local, Id.IdName);
        }
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;

    public override string ToString() => LeftExpr != null ? $"{LeftExpr} <- {Value}" : $"{Id} <- {Value}";
}