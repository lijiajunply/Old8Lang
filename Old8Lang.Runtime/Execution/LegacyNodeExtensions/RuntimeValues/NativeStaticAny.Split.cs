using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using System.Collections.Concurrent;
using System.Reflection;
using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// NativeStaticAny 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class NativeStaticAny
{
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            // 使用缓存获取成员
            var member = GetCachedMember(id.IdName);

            // 尝试访问属性
            if (member is PropertyInfo prop)
            {
                return ObjToValue(prop.GetValue(null)!);
            }

            // 尝试访问字段
            if (member is FieldInfo field)
            {
                return ObjToValue(field.GetValue(null)!);
            }

            throw new AttributeError(this, id.IdName, ClassName);
        }

        if (dotExpression is Instance instance)
        {
            // 使用缓存获取方法
            var member = GetCachedMember(instance.Id.IdName);
            if (member is not MethodInfo method)
                throw new AttributeError(this, instance.Id.IdName, ClassName);

            // 处理命名参数：将命名参数重新排序为位置参数
            List<LangExpression> orderedArgs;
            if (instance.NamedArgs is { Count: > 0 })
            {
                orderedArgs = ReorderArgumentsWithNamedParameters(
                    method,
                    instance.Ids,
                    instance.NamedArgs,
                    instance.Position);
            }
            else
            {
                orderedArgs = instance.Ids;
            }

            // 计算参数值
            var arguments = orderedArgs.Select(arg => arg.Accept(RuntimeVisitors.CreateInterpreter(manager))).ToList();
            var a = Apis.ListToObjects(arguments.OfType<LangValueType>().ToList()).ToArray();

            // 使用委托缓存优化静态方法调用
            var invoke = MethodInvokerCache.Invoke(method, null, a);
            return ObjToValue(invoke!);
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }
}
