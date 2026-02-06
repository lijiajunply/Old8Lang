using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.TypeValue;

/// <summary>
/// Type.GetMethodNames 方法 - 获取类型的所有方法名列表
/// </summary>
public class TypeGetMethodNamesMethod : BaseInstanceMethod
{
    public override string[] Names => ["GetMethodNames"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        var methodNames = typeValue.GetMethodNames(manager);

        return new ListLangValue(
            methodNames.Select(name => new StringLangValue(name))
                .Cast<LangValueType>()
                .ToList()
        );
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载 TypeLangValue 实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载 VariateManager（需要从 LocalManager 获取）
        ilGenerator.Emit(OpCodes.Ldsfld, typeof(LocalManager).GetField("Interpreter")!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(LangInterpreter).GetProperty("Manager")!.GetMethod!);

        // 调用辅助方法
        var helperMethod = typeof(TypeGetMethodNamesMethod).GetMethod(nameof(GetMethodNamesHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取方法名列表
    /// </summary>
    public static ListLangValue GetMethodNamesHelper(TypeLangValue typeValue, VariateManager manager)
    {
        var methodNames = typeValue.GetMethodNames(manager);
        return new ListLangValue(
            methodNames.Select(name => new StringLangValue(name))
                .Cast<LangValueType>()
                .ToList()
        );
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not string typeName)
        {
            throw new ArgumentException("VM 模式下 TypeLangValue 应该是字符串类型");
        }

        // 在 VM 模式下，需要从 VMContext 获取类型信息
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            return new List<string>();
        }

        var classMetadata = Old8Lang.Runtime.VMReflectionHelper.GetClassMetadata(vm, typeName);
        if (classMetadata == null)
        {
            return new List<string>();
        }

        return Old8Lang.Runtime.VMReflectionHelper.GetAllMethodNames(classMetadata).Cast<object?>().ToList();
    }
}
