using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;

namespace Old8Lang.AST.Statement;

public class FuncInit(FuncValue a, SourcePosition position = default) : OldStatement(position)
{
    public readonly FuncValue FuncValue = a;

    public override void Run(VariateManager manager)
    {
        manager.AddClassAndFunc(FuncValue);
    }

    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取方法的名称和参数类型
        var methodName = FuncValue.Id!.IdName;
        if (FuncValue.Method != null)
        {
            local.DelegateVar.Add(methodName, FuncValue.Method);
            return;
        }
        var parameterTypes = FuncValue.Ids!.Select(item => item.OutputType(local)).ToArray();

        // 假设 LocalManager 包含一个 AssemblyBuilder 和 ModuleBuilder 实例
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // 定义一个新的类型
        var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);

        var a = FuncValue.OutputType(local);
        
        // 定义新的方法
        var methodBuilder = typeBuilder.DefineMethod(
            methodName,
            MethodAttributes.Public | MethodAttributes.Static,
            a,
            parameterTypes
        );

        var funcLocal = new LocalManager();

        // 创建方法的 IL 发射器
        var methodIl = methodBuilder.GetILGenerator();

        for (var i = 0; i < FuncValue.Ids!.Count; i++)
        {
            var id = FuncValue.Ids[i];
            var localVar = methodIl.DeclareLocal(parameterTypes[i]);
            funcLocal.AddLocalVar(id.IdName, localVar);
            methodIl.Emit(OpCodes.Ldarg, i);

            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        funcLocal.DelegateVar.Add(methodName, methodBuilder);
        
        // 生成方法体的 IL 代码
        FuncValue.BlockStatement.GenerateIl(methodIl, funcLocal);

        // 返回
        methodIl.Emit(OpCodes.Ret);

        var dynamicType = typeBuilder.CreateType();

        // 获取方法信息
        var addMethod = dynamicType.GetMethod(methodName)!;

        local.DelegateVar.Add(methodName, addMethod);
    }

    public override OldStatement this[int index] => this;

    public override int Count => 0;


    public override string ToString() => FuncValue.ToString();
}