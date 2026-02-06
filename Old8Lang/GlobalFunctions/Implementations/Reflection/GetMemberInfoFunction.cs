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
/// GetMemberInfo 函数 - 获取成员详细信息（合并了 GetMethodInfo 和 GetFieldInfo）
/// 自动识别成员类型（方法或字段）并返回相应信息
/// </summary>
public sealed class GetMemberInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetMemberInfo"];
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

        if (obj is not AnyLangValue anyValue)
        {
            throw new InvalidOperationError(position, "对象不是类实例");
        }

        // 先尝试查找方法
        var methods = anyValue.Metadata.MethodTable.LookupMethod(memberName);
        if (methods is not null && methods.Count > 0)
        {
            // 如果有多个重载，返回第一个
            var method = methods[0];

            var methodTuples = new List<TupleLangValue>
            {
                new TupleLangValue([new StringLangValue("name"), new StringLangValue(method.MethodName)]),
                new TupleLangValue([new StringLangValue("type"), new StringLangValue("method")]),
                new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)]),
                new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!method.HasModifier(AccessModifierType.Private))]),
                new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(method.HasModifier(AccessModifierType.Private))]),
                new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(method.ParameterCount)]),
                new TupleLangValue([new StringLangValue("overloadCount"), new IntLangValue(methods.Count)])
            };
            return new DictionaryLangValue(methodTuples);
        }

        // 再尝试查找字段
        var field = anyValue.Metadata.FieldTable.LookupField(memberName);
        if (field is not null)
        {
            var fieldTuples = new List<TupleLangValue>
            {
                new TupleLangValue([new StringLangValue("name"), new StringLangValue(field.FieldName)]),
                new TupleLangValue([new StringLangValue("type"), new StringLangValue("field")]),
                new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(field.IsStatic)]),
                new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!field.HasModifier(AccessModifierType.Private))]),
                new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(field.HasModifier(AccessModifierType.Private))])
            };
            return new DictionaryLangValue(fieldTuples);
        }

        // 成员不存在
        throw new AttributeError(anyValue, memberName, anyValue.ClassId.IdName);
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

        // 调用 ReflectionHelper.GetMemberInfo(object, string)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.GetMemberInfo));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var obj = arguments[0];
        var memberName = (string)arguments[1]!;

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

            // 先尝试查找方法
            var method = VMReflectionHelper.FindMethod(classMetadata, memberName);
            if (method != null)
            {
                var methodTuples = VMReflectionHelper.CreateMethodInfoTuples(method);
                var dict = new Dictionary<object, object?>
                {
                    ["type"] = "method"
                };
                foreach (var (key, value) in methodTuples)
                {
                    dict[key] = value;
                }
                return dict;
            }

            // 再尝试查找字段
            var field = VMReflectionHelper.FindField(classMetadata, memberName);
            if (field != null)
            {
                var fieldTuples = VMReflectionHelper.CreateFieldInfoTuples(field);
                var dict = new Dictionary<object, object?>
                {
                    ["type"] = "field"
                };
                foreach (var (key, value) in fieldTuples)
                {
                    dict[key] = value;
                }
                return dict;
            }

            throw new InvalidOperationException($"找不到成员 {memberName}");
        }

        throw new InvalidOperationException("对象不是类实例");
    }
}
