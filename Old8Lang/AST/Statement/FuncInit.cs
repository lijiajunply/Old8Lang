using Old8Lang.LangParser;
using System.Reflection;
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
        // 获取方法的名称和参数类型
        var methodName = FuncLangValue.Id!.IdName;
        if (FuncLangValue.Method != null)
        {
            local.DelegateVar.Add(methodName, FuncLangValue.Method);
            return;
        }
        var parameterTypes = FuncLangValue.Ids!.Select(item => item.OutputType(local)).ToArray();

        // 获取返回类型
        var returnType = GetItemType(FuncLangValue.BlockStatement, local);

        // 定义新的方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes,
            true
        );

        // 创建方法的 IL 发射器
        var methodIl = dynamicMethod.GetILGenerator();

        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        // 处理参数
        for (var i = 0; i < FuncLangValue.Ids!.Count; i++)
        {
            var id = FuncLangValue.Ids[i];
            var localVar = methodIl.DeclareLocal(parameterTypes[i]);
            funcLocal.AddLocalVar(id.IdName, localVar);
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar.LocalIndex);
        }

        // 生成方法体的 IL 代码
        FuncLangValue.BlockStatement.GenerateIl(methodIl, funcLocal);

        // 返回
        methodIl.Emit(OpCodes.Ret);

        // 将方法添加到本地变量管理器
        local.DelegateVar.Add(methodName, dynamicMethod);
    }

    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.OutputType(local);
            }

            if (item == null || item.Count == 0)
            {
                continue;
            }

            return GetItemType(item, local);
        }

        return typeof(void);
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