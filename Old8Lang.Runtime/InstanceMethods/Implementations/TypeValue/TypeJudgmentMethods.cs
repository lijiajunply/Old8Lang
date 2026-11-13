using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.TypeValue;

/// <summary>
/// Type.IsInterface 方法 - 判断是否为接口类型
/// </summary>
public class TypeIsInterfaceMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsInterface"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        return new BoolLangValue(typeValue.IsInterface(manager));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ldsfld, typeof(LocalManager).GetField("Interpreter")!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(LangInterpreter).GetProperty("Manager")!.GetMethod!);
        var helperMethod = typeof(TypeIsInterfaceMethod).GetMethod(nameof(IsInterfaceHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static BoolLangValue IsInterfaceHelper(TypeLangValue typeValue, VariateManager manager)
    {
        return new BoolLangValue(typeValue.IsInterface(manager));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var typeName = Old8Lang.Runtime.VMReflectionHelper.ResolveTypeName(instance);
        if (string.IsNullOrEmpty(typeName)) return false;
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null) return false;
        var classMetadata = Old8Lang.Runtime.VMReflectionHelper.GetClassMetadata(vm, typeName);
        return classMetadata?.IsInterface ?? false;
    }
}

/// <summary>
/// Type.IsPrimitive 方法 - 判断是否为基本类型
/// </summary>
public class TypeIsPrimitiveMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsPrimitive"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        return new BoolLangValue(typeValue.IsPrimitive());
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        var helperMethod = typeof(TypeIsPrimitiveMethod).GetMethod(nameof(IsPrimitiveHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static BoolLangValue IsPrimitiveHelper(TypeLangValue typeValue)
    {
        return new BoolLangValue(typeValue.IsPrimitive());
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        return Old8Lang.Runtime.VMReflectionHelper.IsPrimitiveTypeName(
            Old8Lang.Runtime.VMReflectionHelper.ResolveTypeName(instance));
    }
}

/// <summary>
/// Type.IsGeneric 方法 - 判断是否为泛型类型
/// </summary>
public class TypeIsGenericMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsGeneric"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        return new BoolLangValue(typeValue.IsGeneric(manager));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ldsfld, typeof(LocalManager).GetField("Interpreter")!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(LangInterpreter).GetProperty("Manager")!.GetMethod!);
        var helperMethod = typeof(TypeIsGenericMethod).GetMethod(nameof(IsGenericHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static BoolLangValue IsGenericHelper(TypeLangValue typeValue, VariateManager manager)
    {
        return new BoolLangValue(typeValue.IsGeneric(manager));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null) return false;

        // 虚拟机只编译泛型类的特化版本（如 Box$int），类元数据上留有类型参数映射
        var classMetadata = Old8Lang.Runtime.VMReflectionHelper.GetClassMetadataFromTypeValue(vm, instance);
        return Old8Lang.Runtime.VMReflectionHelper.IsGenericType(classMetadata);
    }
}

/// <summary>
/// Type.IsAssignableFrom 方法 - 检查类型兼容性
/// </summary>
public class TypeIsAssignableFromMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsAssignableFrom"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        var arg = parameters[0].Run(manager);

        if (arg is not TypeLangValue otherType)
        {
            throw new Error.InvalidOperationError(position, "IsAssignableFrom 参数必须是 TypeLangValue");
        }

        return new BoolLangValue(typeValue.IsAssignableFrom(otherType, manager));
    }

    protected override void GenerateIlInternal(LangExpression instance, List<LangExpression> parameters,
        ILGenerator ilGenerator, LocalManager local, SourcePosition position)
    {
        instance.LoadIlValue(ilGenerator, local);
        parameters[0].LoadIlValue(ilGenerator, local);
        ilGenerator.Emit(OpCodes.Ldsfld, typeof(LocalManager).GetField("Interpreter")!);
        ilGenerator.Emit(OpCodes.Callvirt, typeof(LangInterpreter).GetProperty("Manager")!.GetMethod!);
        var helperMethod = typeof(TypeIsAssignableFromMethod).GetMethod(nameof(IsAssignableFromHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    public static BoolLangValue IsAssignableFromHelper(TypeLangValue typeValue, TypeLangValue otherType, VariateManager manager)
    {
        return new BoolLangValue(typeValue.IsAssignableFrom(otherType, manager));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null) return false;

        var targetTypeName = Old8Lang.Runtime.VMReflectionHelper.ResolveTypeName(instance);
        var sourceTypeName = Old8Lang.Runtime.VMReflectionHelper.ResolveTypeName(
            arguments.Length > 0 ? arguments[0] : null);

        if (string.IsNullOrEmpty(targetTypeName) || string.IsNullOrEmpty(sourceTypeName))
        {
            return false;
        }

        return Old8Lang.Runtime.VMReflectionHelper.IsAssignableFrom(vm, targetTypeName, sourceTypeName);
    }
}
