using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetAllTypes 函数 - 获取所有已注册类型
/// </summary>
public sealed class GetAllTypesFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetAllTypes"];
    public override string[] ParameterNames => [];
    public override int MinParameterCount => 0;
    public override int MaxParameterCount => 0;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var typeNames = TypeTemplate.GetAllRegisteredTypes();
        var typeValues = typeNames
            .Select(name => new TypeLangValue(name))
            .Cast<LangValueType>()
            .ToList();

        return new ListLangValue(typeValues);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 调用 ReflectionHelper.GetAllTypes()
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.GetAllTypes));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(ListLangValue);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        // 在 VM 模式下，需要从 VMContext 获取当前 VM
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            return new List<string>();
        }

        var classNames = VMReflectionHelper.GetAllClassNames(vm);
        return classNames.Select(name => new TypeLangValue(name)).Cast<object>().ToList();
    }
}
