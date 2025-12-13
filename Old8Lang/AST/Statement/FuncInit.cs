using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

public class FuncInit(FuncLangValue a, SourcePosition position = default) : OldStatement(position)
{
    public readonly FuncLangValue FuncLangValue = a;

    public override void Run(VariateManager manager)
    {
        // 检查函数是否已存在（只有当函数名和参数数量都相同时才视为重复）
        if (FuncLangValue.Id != null)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is FuncLangValue func &&
                func.Id?.IdName == FuncLangValue.Id.IdName &&
                func.Ids?.Count == FuncLangValue.Ids?.Count);

            if (existingFunc != null)
            {
                throw new DuplicateNameError(this, FuncLangValue.Id.IdName, "函数");
            }
        }

        manager.AddClassAndFunc(FuncLangValue);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取方法的名称
        var methodName = FuncLangValue.Id!.IdName;
        if (FuncLangValue.Method != null)
        {
            local.DelegateVar.Add(methodName, FuncLangValue.Method);
            return;
        }

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = FuncLangValue.Ids!.Select(item => item.OutputType(local)).ToArray();

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        // 这样可以避免函数内部的局部变量与外部的局部变量冲突
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < FuncLangValue.Ids!.Count; i++)
        {
            var id = FuncLangValue.Ids[i];
            var paramType = parameterTypes[i];
            // 创建一个临时的LocalBuilder来表示参数
            // 注意：这里我们不能使用真正的LocalBuilder，因为还没有创建ILGenerator
            // 所以我们使用一个占位符，稍后会替换
            funcLocal.LocalVarTypes[id.IdName] = paramType;
        }

        // 获取返回类型（现在funcLocal中已经有参数信息了）
        var returnType = GetItemType(FuncLangValue.BlockStatement, funcLocal);

        // 定义新的方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes,
            true
        );

        // 创建方法的 IL 发射器
        var methodIl = dynamicMethod.GetILGenerator();

        // 清空funcLocal，重新添加参数（这次使用真正的LocalBuilder）
        funcLocal.LocalVar.Clear();

        // 处理参数
        for (var i = 0; i < FuncLangValue.Ids!.Count; i++)
        {
            var id = FuncLangValue.Ids[i];
            // 使用实际的参数类型声明局部变量
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            // 加载参数并存储到局部变量
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar.LocalIndex);
        }

        // 生成方法体的 IL 代码
        FuncLangValue.BlockStatement.GenerateIl(methodIl, funcLocal);

        // 检查函数体的最后一个语句是否是 ReturnStatement
        var lastStatement = FuncLangValue.BlockStatement.Count > 0
            ? FuncLangValue.BlockStatement[^1]
            : null;

        // 只有当最后一个语句不是 ReturnStatement 时，才添加 Ret 指令
        if (lastStatement is not ReturnStatement)
        {
            // 对于 void 方法，添加 Ret 指令
            // 对于有返回值的方法，如果没有显式 return，这里会导致栈不平衡，但这是用户代码的问题
            methodIl.Emit(OpCodes.Ret);
        }

        // 将方法添加到本地变量管理器
        // 对于用户定义的函数，我们需要保留原始方法名以便调用
        // 对于重载函数，我们需要将所有重载都添加到字典中，使用"函数名$参数数量"作为键
        var delegateKey = methodName;
        if (FuncLangValue.Ids != null)
        {
            // 使用函数名+参数数量作为键，支持函数重载
            delegateKey = $"{methodName}${FuncLangValue.Ids.Count}";
        }
        local.DelegateVar.TryAdd(delegateKey, dynamicMethod);

        // 同时存储函数的参数列表信息，用于支持默认参数
        if (FuncLangValue.Ids != null)
        {
            local.FuncParameters.TryAdd(delegateKey, FuncLangValue.Ids);
        }
    }

    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.OutputType(local);
                if (varType != null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                // 确保返回类型不为null
                var returnType = returnStatement.OutputType(local);
                return returnType;
            }

            if (item == null || item.Count == 0)
            {
                continue;
            }

            var innerType = GetItemType(item, local);
            if (innerType != typeof(void))
            {
                return innerType;
            }
        }

        return typeof(void); // 默认返回void类型
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;


    public override string ToString()
    {
        var sb = new StringBuilder();
        var paramList = FuncLangValue.Ids != null ? string.Join(", ", FuncLangValue.Ids) : string.Empty;
        sb.AppendLine($"func {FuncLangValue.Id}({paramList})");
        sb.AppendLine($"{{ {FuncLangValue.BlockStatement} }}");
        return sb.ToString();
    }
}