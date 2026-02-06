using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetClassInfo 函数 - 获取类的完整信息（合并了 GetClassName, GetClassMethods, GetClassFields）
/// </summary>
public sealed class GetClassInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetClassInfo"];
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

        // 获取所有方法名
        var methods = anyValue.Metadata.MethodTable.GetAllMethods()
            .Select(m => new StringLangValue(m.MethodName))
            .Cast<LangValueType>()
            .ToList();

        // 获取所有字段名
        var fields = anyValue.Metadata.FieldTable.GetAllFields()
            .Select(f => new StringLangValue(f.FieldName))
            .Cast<LangValueType>()
            .ToList();

        // 构建完整的类信息字典
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("className"), new StringLangValue(anyValue.ClassId.IdName)]),
            new TupleLangValue([new StringLangValue("methods"), new ListLangValue(methods)]),
            new TupleLangValue([new StringLangValue("fields"), new ListLangValue(fields)]),
            new TupleLangValue([new StringLangValue("isInterface"), new BoolLangValue(anyValue.Metadata.IsInterface)]),
            new TupleLangValue([new StringLangValue("isAbstract"), new BoolLangValue(anyValue.Metadata.IsAbstract)]),
            new TupleLangValue([new StringLangValue("isMixin"), new BoolLangValue(anyValue.Metadata.IsMixin)]),
            new TupleLangValue([
                new StringLangValue("baseClass"),
                anyValue.Metadata.ParentClassName is not null
                    ? new StringLangValue(anyValue.Metadata.ParentClassName)
                    : new NullLangValue()
            ]),
            new TupleLangValue([
                new StringLangValue("interfaces"),
                new ListLangValue(
                    anyValue.Metadata.InterfaceNames
                        .Select(name => new StringLangValue(name))
                        .Cast<LangValueType>()
                        .ToList()
                )
            ])
        };

        return new DictionaryLangValue(tuples);
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载对象参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetClassInfo(object)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.GetClassInfo));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];

        if (obj is BytecodeObjectInstance instance)
        {
            var vm = VMContext.CurrentVM;
            if (vm == null)
            {
                throw new InvalidOperationException("无法获取当前虚拟机实例");
            }

            var classMetadata = VMReflectionHelper.GetClassMetadataFromInstance(vm, instance);
            if (classMetadata == null)
            {
                throw new InvalidOperationException($"找不到类 {instance.ClassName} 的元数据");
            }

            var methodNames = VMReflectionHelper.GetAllMethodNames(classMetadata);
            var fieldNames = VMReflectionHelper.GetAllFieldNames(classMetadata);

            // 构建字典
            var dict = new Dictionary<object, object?>
            {
                ["className"] = instance.ClassName,
                ["methods"] = methodNames.Cast<object?>().ToList(),
                ["fields"] = fieldNames.Cast<object?>().ToList(),
                ["isInterface"] = classMetadata.IsInterface,
                ["isAbstract"] = classMetadata.IsAbstract,
                ["baseClass"] = classMetadata.BaseClassName,
                ["interfaces"] = classMetadata.InterfaceNames.Cast<object?>().ToList()
            };

            return dict;
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}
