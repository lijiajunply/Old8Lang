using Old8Lang.AST.Visitor;
using System.Reflection;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using System.Collections.Concurrent;

namespace Old8Lang.AST.Expression.Intermediates;

/// <summary>
/// NativeAnyLangValue 的执行/编译辅助实现（原位于 AST 目录，阶段 1 迁出）。
/// </summary>
public partial class NativeAnyLangValue
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
                try
                {
                    // 如果有实例对象，访问实例成员；否则访问静态成员
                    var value = prop.GetValue(InstanceObj);
                    return ObjToValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationError(this, $"访问属性 '{id.IdName}' 失败: {ex.Message}");
                }
            }

            // 尝试访问字段
            if (member is FieldInfo field)
            {
                try
                {
                    // 如果有实例对象，访问实例成员；否则访问静态成员
                    var value = field.GetValue(InstanceObj);
                    return ObjToValue(value);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationError(this, $"访问字段 '{id.IdName}' 失败: {ex.Message}");
                }
            }

            throw new AttributeError(this, id.IdName, ClassName);
        }

        if (dotExpression is Instance instance)
        {
            // 使用缓存获取方法
            var member = GetCachedMember(instance.Id.IdName);
            if (member is not MethodInfo method)
                throw new AttributeError(this, instance.Id.IdName, ClassName);

            try
            {
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
                var arguments = orderedArgs.Select(arg => arg.Accept(new InterpreterVisitor(manager))).ToList();

                // 将 Old8Lang 值转换为 .NET 对象
                var nativeArguments = arguments.Select(ValueToObj).ToArray();

                // 使用委托缓存优化方法调用
                var result = MethodInvokerCache.Invoke(method, InstanceObj, nativeArguments);

                // 将结果转换为 Old8Lang 值
                return ObjToValue(result);
            }
            catch (TargetInvocationException ex)
            {
                throw new InvalidOperationError(this, $"调用方法 '{instance.Id.IdName}' 失败: {ex.InnerException?.Message ?? ex.Message}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationError(this, $"调用方法 '{instance.Id.IdName}' 失败: {ex.Message}");
            }
        }

        throw new InvalidOperationError(this, "不支持的点操作表达式类型");
    }
}
