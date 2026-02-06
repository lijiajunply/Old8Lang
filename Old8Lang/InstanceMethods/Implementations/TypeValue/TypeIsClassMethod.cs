using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;

namespace Old8Lang.InstanceMethods.Implementations.TypeValue;

/// <summary>
/// Type.IsClass 方法 - 判断是否为类类型
/// </summary>
public class TypeIsClassMethod : BaseInstanceMethod
{
    public override string[] Names => ["IsClass"];
    public override Type TargetType => typeof(TypeLangValue);
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(LangValueType instance, List<LangExpression> parameters,
        VariateManager manager, SourcePosition position)
    {
        var typeValue = (TypeLangValue)instance;
        return new BoolLangValue(typeValue.IsClass(manager));
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
        var helperMethod = typeof(TypeIsClassMethod).GetMethod(nameof(IsClassHelper),
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        ilGenerator.Emit(OpCodes.Call, helperMethod!);
    }

    /// <summary>
    /// 辅助方法：判断是否为类
    /// </summary>
    public static BoolLangValue IsClassHelper(TypeLangValue typeValue, VariateManager manager)
    {
        return new BoolLangValue(typeValue.IsClass(manager));
    }

    protected override Type GetReturnTypeInternal(Type instanceType, List<LangExpression> parameters, LocalManager local)
    {
        return typeof(BoolLangValue);
    }

    protected override object ExecuteInVMInternal(object? instance, object?[] arguments)
    {
        if (instance is not string typeName)
        {
            throw new ArgumentException("VM 模式下 TypeLangValue 应该是字符串类型");
        }

        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            return false;
        }

        var classMetadata = Old8Lang.Runtime.VMReflectionHelper.GetClassMetadata(vm, typeName);
        if (classMetadata == null)
        {
            return false;
        }

        return !classMetadata.IsInterface && !classMetadata.IsMixin;
    }
}
