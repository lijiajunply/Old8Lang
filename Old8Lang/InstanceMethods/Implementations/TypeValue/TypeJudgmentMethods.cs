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
        if (instance is not string typeName) return false;
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
        if (instance is not string typeName) return false;
        return typeName switch
        {
            "Int" or "Float" or "String" or "Bool" or "Null" => true,
            _ => false
        };
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
        // VM 模式下暂不支持泛型检查
        return false;
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
        // VM 模式下暂不支持类型兼容性检查
        return false;
    }
}
