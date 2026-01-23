using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetField 函数 - 动态获取字段值
/// </summary>
public sealed class GetFieldFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetField"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var fieldName = ((StringLangValue)results[1]).Value;

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        return anyValue.ReflectionGetField(fieldName);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载字段名参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetField(object, string)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.GetField));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];
        var fieldName = (string)arguments[1]!;

        if (obj is BytecodeObjectInstance instance)
        {
            if (instance.Fields.TryGetValue(fieldName, out var value))
            {
                return value;
            }

            // 如果实例字段中没有，检查静态字段
            var vm = VMContext.CurrentVM;
            if (vm != null)
            {
                var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
                if (classMetadata != null && classMetadata.StaticFieldValues.TryGetValue(fieldName, out var staticValue))
                {
                    return staticValue;
                }
            }

            throw new InvalidOperationException($"找不到字段 {fieldName}");
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}

/// <summary>
/// SetField 函数 - 动态设置字段值
/// </summary>
public sealed class SetFieldFunction : BaseGlobalFunction
{
    public override string[] Names => ["SetField"];
    public override int MinParameterCount => 3;
    public override int MaxParameterCount => 3;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var fieldName = ((StringLangValue)results[1]).Value;
        var value = results[2];

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        anyValue.ReflectionSetField(fieldName, value);
        return new VoidLangValue();
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载字段名参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 加载值参数
        parameters[2].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.SetField(object, string, object)
        var method = typeof(ReflectionHelper).GetMethod(nameof(ReflectionHelper.SetField));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(void);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];
        var fieldName = (string)arguments[1]!;
        var value = arguments[2];

        if (obj is BytecodeObjectInstance instance)
        {
            // 先尝试设置实例字段
            if (instance.Fields.ContainsKey(fieldName))
            {
                instance.Fields[fieldName] = value;
                return null;
            }

            // 如果实例字段中没有，尝试设置静态字段
            var vm = VMContext.CurrentVM;
            if (vm != null)
            {
                var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
                if (classMetadata != null && classMetadata.StaticFieldValues.ContainsKey(fieldName))
                {
                    classMetadata.StaticFieldValues[fieldName] = value;
                    return null;
                }
            }

            throw new InvalidOperationException($"找不到字段 {fieldName}");
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}
