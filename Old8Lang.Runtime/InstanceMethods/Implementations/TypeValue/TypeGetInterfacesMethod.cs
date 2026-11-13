using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.TypeValue;

/// <summary>
/// Type.GetInterfaces 方法 - 获取实现的接口列表
/// </summary>
public class TypeGetInterfacesMethod : BaseInstanceMethod
{
    public override string[] Names => ["GetInterfaces"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        var interfaces = typeValue.GetInterfaces(manager);

        return new ListLangValue(interfaces.Cast<LangValueType>().ToList());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        // 加载 TypeLangValue 实例
        instance.LoadIlValue(ilGenerator, local);

        // 加载 VariateManager
        ilGenerator.Emit(OpCodes.Ldsfld, typeof(LocalManager).GetField("Interpreter")!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(LangInterpreter).GetProperty("Manager")!.GetMethod!);

        // 调用辅助方法
        var helperMethod = typeof(TypeGetInterfacesMethod).GetMethod(nameof(GetInterfacesHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取接口列表
    /// </summary>
    public static ListLangValue GetInterfacesHelper(TypeLangValue typeValue, VariateManager manager)
    {
        var interfaces = typeValue.GetInterfaces(manager);
        return new ListLangValue(interfaces.Cast<LangValueType>().ToList());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var typeName = Old8Lang.Runtime.VMReflectionHelper.ResolveTypeName(instance);
        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException(
                $"GetInterfaces 需要类型值，实际得到 {(instance is null ? "null" : instance.GetType().Name)}");
        }

        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            return new List<object?>();
        }

        var classMetadata = Old8Lang.Runtime.VMReflectionHelper.GetClassMetadata(vm, typeName);
        if (classMetadata == null)
        {
            return new List<object?>();
        }

        // 与解释器一致：返回类型值列表而不是裸类型名
        return classMetadata.InterfaceNames
            .Select(name => (object?)new Old8Lang.AST.Expression.Value.TypeLangValue(name))
            .ToList();
    }
}
