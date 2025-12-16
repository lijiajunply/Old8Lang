using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 异步函数声明语句
/// 表示 async func 定义
/// </summary>
public class AsyncFuncInit : OldStatement
{
    public readonly AsyncFuncLangValue AsyncFuncValue;

    /// <summary>
    /// 判断是否为 Lambda 表达式
    /// </summary>
    public bool IsLambda => AsyncFuncValue.Id == null;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncFuncInit(AsyncFuncLangValue funcValue, SourcePosition position = default)
        : base(position)
    {
        AsyncFuncValue = funcValue;
    }

    /// <summary>
    /// 解释执行：注册异步函数到变量管理器
    /// </summary>
    public override void Run(VariateManager manager)
    {
        // 检查函数重复声明
        if (AsyncFuncValue.Id != null)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is AsyncFuncLangValue func &&
                func.Id?.IdName == AsyncFuncValue.Id.IdName &&
                func.Ids?.Count == AsyncFuncValue.Ids?.Count);

            if (existingFunc != null)
            {
                throw new DuplicateNameError(
                    this,
                    AsyncFuncValue.Id.IdName,
                    "异步函数"
                );
            }
        }

        // 添加到导入信息列表
        manager.AddClassAndFunc(AsyncFuncValue);
    }

    /// <summary>
    /// 生成 IL 代码（编译器模式）
    /// </summary>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取方法的名称
        var methodName = AsyncFuncValue.Id!.IdName;
        
        // 使用参数的类型注解来确定参数类型
        var parameterTypes = AsyncFuncValue.Ids!.Select(item => item.OutputType(local)).ToArray();
        
        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };
        
        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < AsyncFuncValue.Ids!.Count; i++)
        {
            var id = AsyncFuncValue.Ids[i];
            var paramType = parameterTypes[i];
            // 存储参数类型，用于后续推断返回类型
            funcLocal.LocalVarTypes[id.IdName] = paramType;
        }
        
        // 异步函数返回Task<object>
        var returnType = typeof(Task<object>);
        
        // 定义新的动态方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes,
            true
        );
        
        // 获取动态方法的IL生成器
        var methodIl = dynamicMethod.GetILGenerator();
        
        // 清空funcLocal，重新添加参数（这次使用真正的LocalBuilder）
        funcLocal.LocalVar.Clear();
        
        // 处理参数
        for (var i = 0; i < AsyncFuncValue.Ids!.Count; i++)
        {
            var id = AsyncFuncValue.Ids[i];
            // 使用实际的参数类型声明局部变量
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            // 加载参数并存储到局部变量
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }
        
        // 生成异步方法体
        GenerateAsyncMethodBody(methodIl, funcLocal);
        
        // 将方法添加到本地变量管理器
        // 使用函数名+参数类型作为键，支持更准确的函数重载
        var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
        var delegateKey = $"{methodName}${paramTypeNames}";
        local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
        
        // 同时存储函数的参数列表信息
        if (AsyncFuncValue.Ids != null)
        {
            local.FuncParameters.TryAdd(delegateKey, AsyncFuncValue.Ids);
        }
    }
    
    /// <summary>
    /// 生成异步方法体的IL代码
    /// </summary>
    private void GenerateAsyncMethodBody(ILGenerator ilGenerator, LocalManager local)
    {
        // 生成异步方法体
        AsyncFuncValue.BlockStatement.GenerateIl(ilGenerator, local);
        
        // 返回一个已完成的Task
        ilGenerator.Emit(OpCodes.Ldnull);
        // 获取Task.FromResult<T>泛型方法
        var fromResultMethod = typeof(Task).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == "FromResult" && m.IsGenericMethod);
        // 指定T为object
        fromResultMethod = fromResultMethod.MakeGenericMethod(typeof(object));
        ilGenerator.Emit(OpCodes.Call, fromResultMethod);
        ilGenerator.Emit(OpCodes.Ret);
    }
    
    /// <summary>
    /// 实现MoveNext方法
    /// </summary>
    private void ImplementMoveNext(
        ILGenerator ilGenerator,
        Type stateMachineType,
        FieldInfo stateField,
        FieldInfo builderField,
        FieldInfo awaiterField,
        LocalManager local
    )
    {
        // 简化实现：这个方法暂时不会被调用
        ilGenerator.Emit(OpCodes.Ret);
    }

    /// <summary>
    /// AST 子节点访问（无子节点）
    /// </summary>
    public override OldStatement? this[int index] => null;

    /// <summary>
    /// AST 子节点数量
    /// </summary>
    public override int Count => 0;
}
