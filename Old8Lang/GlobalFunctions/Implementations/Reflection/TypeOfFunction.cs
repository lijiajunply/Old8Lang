using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// TypeOf 函数 - 获取对象的类型（语法糖）
/// </summary>
public sealed class TypeOfFunction : BaseGlobalFunction
{
    public override string[] Names => ["TypeOf"];
    public override string[] ParameterNames => ["obj"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        var className = anyValue.ClassId.IdName;
        var template = TypeTemplate.FindType(className);

        if (template is null)
        {
            throw new InvalidOperationError(position, $"找不到类型: {className}");
        }

        return new TypeLangValue(template);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.TypeOf(object)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.TypeOf));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(TypeLangValue);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];

        if (obj is BytecodeObjectInstance instance)
        {
            return new TypeLangValue(instance.ClassName);
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}
