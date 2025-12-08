using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// 函数 ，作为一种变量存在
/// </summary>
public class FuncLangValue : LangValueType
{
    public readonly LangId? Id;
    public readonly BlockStatement BlockStatement = new([]);

    public readonly List<LangId>? Ids;

    public readonly MethodInfo? Method;

    private readonly FuncLangValue? Func;

    public FuncLangValue(LangId? id, List<LangId> ids, BlockStatement blockStatement, SourcePosition position = default) :
        base(position)
    {
        Id = id;
        Ids = ids;
        BlockStatement = blockStatement;
    }

    public FuncLangValue(string idName, MethodInfo methodInfo, FuncLangValue? func = null,
        SourcePosition position = default) : base(position)
    {
        Id = new LangId(idName);
        Method = methodInfo;
        Func = func;
    }

    public override LangValueType Run(VariateManager manager) => this;

    public LangValueType Run(VariateManager variateManagerFunc, List<OldExpr> ids, object? obj = null)
    {
        if (Method != null)
        {
            // 检查参数数量是否匹配（Method的参数数量减去this参数）
            var expectedParams = Method.GetParameters().Length;
            if (obj != null) expectedParams--; // 如果有this参数，减去1
            var actualParams = ids.Count;
            if (expectedParams != actualParams)
            {
                throw new ArgumentError(Position,
                    $"方法 '{Method.Name}' 期望 {expectedParams} 个参数，但实际提供了 {actualParams} 个参数");
            }

            var values = ids.Select(expr => expr.Run(variateManagerFunc)).ToList();
            var a = Apis.ListToObjects(values).ToArray();
            var invoke = Method?.Invoke(obj, a);

            if (invoke is null)
                return new VoidLangValue();

            var manager = new VariateManager();
            manager.Init(new Dictionary<string, LangValueType> { { "base", ObjToValue(invoke) } });
            manager.IsClass = false;
            manager.Result = ObjToValue(invoke);
            Func?.Run(manager, ids);
            return manager.Result;
        }

        // 检查参数数量是否匹配
        if (Ids != null)
        {
            var expectedParams = Ids.Count;
            var actualParams = ids.Count;
            if (expectedParams != actualParams)
            {
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 期望 {expectedParams} 个参数，但实际提供了 {actualParams} 个参数");
            }
        }

        // 调用方法体
        variateManagerFunc.AddChildren();
        variateManagerFunc.IsFunc = true; // 设置为函数上下文
        if (Ids != null && Ids.Count != 0)
            for (var i = 0; i < Ids.Count; i++)
            {
                if (i < ids.Count)
                {
                    // 将实例化时的参数表达式结果赋值给方法参数
                    var paramValue = ids[i].Run(variateManagerFunc);
                    variateManagerFunc.Set(Ids[i], paramValue);
                }
            }
        
        // 运行方法体
        BlockStatement.Run(variateManagerFunc);
        
        // 恢复非函数上下文标志
        variateManagerFunc.IsFunc = false;
        
        // 移除子作用域，但是要注意，在init方法中使用this关键字设置的值已经被保存到实例中了
        // 所以这里移除子作用域不会影响实例的状态
        variateManagerFunc.RemoveChildren();
        
        return variateManagerFunc.Result;
    }

    public override Type OutputType(LocalManager local)
    {
        var idType = Id?.OutputType(local);
        if (idType != null && idType != typeof(object)) return idType;
        var a = GetItemType(BlockStatement, local);
        return a;
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

    public override string ToString()
    {
        if (Method != null)
        {
            return $"{Method}";
        }

        var paramList = Ids != null ? string.Join(", ", Ids) : string.Empty;
        return $"func {Id}({paramList}) \n {{ {BlockStatement} }}";
    }

    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 如果是.NET方法，直接加载方法引用
        if (Method != null)
        {
            // 对于实例方法，需要先加载对象实例到堆栈上
            // 这里假设Method已经是正确的委托类型
            return;
        }

        // 如果是Old8Lang函数，需要加载函数委托
        var funcMethod = local.DelegateVar!.GetValueOrDefault(Id?.IdName);
        if (funcMethod != null)
        {
            // 函数已经被编译为动态方法，直接调用
            ilGenerator.Emit(OpCodes.Ldsfld, funcMethod);
        }
    }

    public void LoadIl(MethodBuilder methodBuilder, LocalManager local)
    {
        //var funcLocal = new LocalManager();
        var parameterTypes = Ids!.Select(item => item.OutputType(local)).ToArray();

        // 创建方法的 IL 发射器
        var methodIl = methodBuilder.GetILGenerator();

        for (var i = 1; i <= Ids!.Count; i++)
        {
            var id = Ids[i - 1];
            var localVar = methodIl.DeclareLocal(parameterTypes[i - 1]);
            local.AddLocalVar(id.IdName, localVar);
            methodIl.Emit(OpCodes.Ldarg, i);

            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        local.DelegateVar.Add(Id!.IdName, methodBuilder);

        // 生成方法体的 IL 代码
        BlockStatement.GenerateIl(methodIl, local);

        // 返回
        methodIl.Emit(OpCodes.Ret);
    }
}