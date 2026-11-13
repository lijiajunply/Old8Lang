using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.TypeValue;

/// <summary>
/// Type.GetBaseType 方法 - 获取父类型
/// </summary>
public class TypeGetBaseTypeMethod : BaseInstanceMethod
{
    public override string[] Names => ["GetBaseType"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        var baseType = typeValue.GetBaseType(manager);

        return baseType ?? (LangValueType)new NullLangValue();
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
        var helperMethod = typeof(TypeGetBaseTypeMethod).GetMethod(nameof(GetBaseTypeHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：获取父类型
    /// </summary>
    public static LangValueType GetBaseTypeHelper(TypeLangValue typeValue, VariateManager manager)
    {
        var baseType = typeValue.GetBaseType(manager);
        return baseType ?? (LangValueType)new NullLangValue();
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(LangValueType);
    }

    protected override object? ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var typeName = Old8Lang.Runtime.VMReflectionHelper.ResolveTypeName(instance);
        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException(
                $"GetBaseType 需要类型值，实际得到 {(instance is null ? "null" : instance.GetType().Name)}");
        }

        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            return null;
        }

        var classMetadata = Old8Lang.Runtime.VMReflectionHelper.GetClassMetadata(vm, typeName);
        if (classMetadata == null || string.IsNullOrEmpty(classMetadata.BaseClassName))
        {
            return null;
        }

        // 与解释器一致：返回类型值而不是裸类型名
        return new Old8Lang.AST.Expression.Value.TypeLangValue(classMetadata.BaseClassName);
    }
}
