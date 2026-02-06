using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetTypeInfo 函数 - 获取类型的详细信息
/// </summary>
public sealed class GetTypeInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetTypeInfo"];
    public override string[] ParameterNames => ["typeName"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var typeNameValue = results[0];

        if (typeNameValue is not StringLangValue stringValue)
        {
            throw new InvalidOperationError(position, "类型名必须是字符串");
        }

        var typeName = stringValue.Value;
        var template = TypeTemplate.FindType(typeName);

        if (template is null)
        {
            throw new InvalidOperationError(position, $"找不到类型: {typeName}");
        }

        var metadata = template.BuildMetadata(manager);

        // 构建类型信息字典
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(metadata.ClassName)]),
            new TupleLangValue([new StringLangValue("isInterface"), new BoolLangValue(metadata.IsInterface)]),
            new TupleLangValue([new StringLangValue("isAbstract"), new BoolLangValue(metadata.IsAbstract)]),
            new TupleLangValue([new StringLangValue("isMixin"), new BoolLangValue(metadata.IsMixin)]),
            new TupleLangValue([
                new StringLangValue("baseClass"),
                metadata.ParentClassName is not null
                    ? new StringLangValue(metadata.ParentClassName)
                    : new NullLangValue()
            ]),
            new TupleLangValue([
                new StringLangValue("interfaces"),
                new ListLangValue(
                    metadata.InterfaceNames
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([
                new StringLangValue("mixins"),
                new ListLangValue(
                    metadata.MixinNames
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([
                new StringLangValue("methods"),
                new ListLangValue(
                    metadata.MethodTable.GetAllMethodNames()
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([
                new StringLangValue("fields"),
                new ListLangValue(
                    metadata.FieldTable.GetAllFieldNames()
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ]),
            new TupleLangValue([new StringLangValue("isGeneric"), new BoolLangValue(template.IsGeneric)])
        };

        return new DictionaryLangValue(tuples);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载类型名参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetTypeInfo(string)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.GetTypeInfo));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(DictionaryLangValue);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var typeName = arguments[0] as string;

        if (string.IsNullOrEmpty(typeName))
        {
            throw new InvalidOperationException("类型名必须是字符串");
        }

        // 在 VM 模式下，使用 VMReflectionHelper
        var vm = Old8Lang.Bytecode.Core.VMContext.CurrentVM;
        if (vm == null)
        {
            return new Dictionary<string, object?>();
        }

        return VMReflectionHelper.GetTypeInfo(vm, typeName);
    }
}
