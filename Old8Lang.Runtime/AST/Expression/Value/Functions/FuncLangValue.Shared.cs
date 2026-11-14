using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;
// ReSharper disable CheckNamespace

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 共享的返回类型推断与字符串表示
/// </summary>
public partial class FuncLangValue
{

    internal static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.Accept(new TypeInferenceVisitor(local));
                if (varType is not null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.Accept(new TypeInferenceVisitor(local))!;
            }

            if (item is null || item.Count == 0)
            {
                continue;
            }

            var innerType = GetItemType(item, local);
            if (innerType != typeof(void))
            {
                return innerType;
            }
        }

        return typeof(void);
    }


    public override string ToString()
    {
        if (Method is not null)
        {
            return $"{Method}";
        }

        var paramList = Ids is not null ? string.Join(", ", Ids) : string.Empty;
        return $"func {Id}({paramList}) \n {{ {BlockStatement} }}";
    }

}
