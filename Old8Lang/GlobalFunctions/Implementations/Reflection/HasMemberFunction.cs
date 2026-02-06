using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// HasMember 函数 - 检查对象是否有指定成员（合并了 HasMethod 和 HasField）
/// </summary>
public sealed class HasMemberFunction : BaseGlobalFunction
{
    public override string[] Names => ["HasMember"];
    public override string[] ParameterNames => ["obj", "memberName"];
    public override int MinParameterCount => 2;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var obj = results[0];
        var memberName = ((StringLangValue)results[1]).Value;

        if (obj is AnyLangValue anyValue)
        {
            // 检查是否有该方法或字段
            bool hasMethod = anyValue.Metadata.MethodTable.ContainsMethod(memberName);
            bool hasField = anyValue.Metadata.FieldTable.ContainsField(memberName);
            return new BoolLangValue(hasMethod || hasField);
        }

        return new BoolLangValue(false);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 加载成员名参数
        parameters[1].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.HasMember(object, string)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.HasMember));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(bool);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];
        var memberName = (string)arguments[1]!;

        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm != null)
            {
                var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
                if (classMetadata != null)
                {
                    bool hasMethod = VMReflectionHelper.HasMethod(classMetadata, memberName);
                    bool hasField = VMReflectionHelper.HasField(classMetadata, memberName);
                    return hasMethod || hasField;
                }
            }
        }

        return false;
    }
}
