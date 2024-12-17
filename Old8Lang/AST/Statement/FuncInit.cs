using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.CslyParser;

namespace Old8Lang.AST.Statement;

public class FuncInit(FuncValue a) : OldStatement
{
    public readonly FuncValue FuncValue = a;

    public override void Run(VariateManager Manager)
    {
        Manager.AddClassAndFunc(FuncValue);
    }

    public override void GenerateIL(ILGenerator ilGenerator, LocalManager local)
    {
        // 获取方法的名称和参数类型
        var methodName = FuncValue.Id!.IdName;
        var parameterTypes = FuncValue.Ids!.Select(_ => typeof(object)).ToArray();

        // 假设 LocalManager 包含一个 AssemblyBuilder 和 ModuleBuilder 实例
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder =
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");

        // 定义一个新的类型
        var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);

        // 定义新的方法
        var methodBuilder = typeBuilder.DefineMethod(
            methodName,
            MethodAttributes.Public | MethodAttributes.Static,
            typeof(void),
            parameterTypes
        );

        // 创建方法的 IL 发射器
        var methodIL = methodBuilder.GetILGenerator();

        // 生成方法体的 IL 代码
        FuncValue.BlockStatement.GenerateIL(methodIL, local);

        // 返回
        methodIL.Emit(OpCodes.Ret);
        
        var dynamicType = typeBuilder.CreateType();
        
        // 获取方法信息
        var addMethod = dynamicType.GetMethod(methodName)!;

        local.DelegateVar.Add(methodName, addMethod);
    }


    public override string ToString() => FuncValue.ToString();
}