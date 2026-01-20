using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.AnyValues;

/// <summary>
/// AnyLangValue 的反射扩展
/// 提供反射专用的公共方法，绕过访问权限检查
/// </summary>
public partial class AnyLangValue
{
    /// <summary>
    /// 反射：获取字段值（绕过访问权限检查）
    /// </summary>
    /// <param name="fieldName">字段名</param>
    /// <returns>字段值</returns>
    /// <exception cref="AttributeError">字段不存在时抛出</exception>
    public LangValueType ReflectionGetField(string fieldName)
    {
        if (!InstanceData.TryGetValue(fieldName, out var fieldValue))
        {
            throw new AttributeError(this, fieldName, ClassId.IdName);
        }
        return fieldValue;
    }

    /// <summary>
    /// 反射：设置字段值（绕过访问权限检查）
    /// </summary>
    /// <param name="fieldName">字段名</param>
    /// <param name="value">新值</param>
    /// <exception cref="AttributeError">字段不存在时抛出</exception>
    public void ReflectionSetField(string fieldName, LangValueType value)
    {
        var fieldDef = Metadata.FieldTable.LookupField(fieldName);
        if (fieldDef is null)
        {
            throw new AttributeError(this, fieldName, ClassId.IdName);
        }
        InstanceData[fieldName] = value;
    }

    /// <summary>
    /// 反射：调用方法（绕过访问权限检查）
    /// </summary>
    /// <param name="methodName">方法名</param>
    /// <param name="arguments">参数表达式列表</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>方法返回值</returns>
    /// <exception cref="AttributeError">方法不存在时抛出</exception>
    /// <exception cref="ArgumentError">参数数量不匹配时抛出</exception>
    public LangValueType ReflectionInvokeMethod(string methodName, List<LangExpression> arguments, VariateManager manager)
    {
        // 查找方法（不检查访问权限）
        var methods = Metadata.MethodTable.LookupMethod(methodName);
        if (methods is null || methods.Count == 0)
        {
            throw new AttributeError(this, methodName, ClassId.IdName);
        }

        // 根据参数数量匹配方法
        LangMethodInfo? matchedMethod = null;
        foreach (var method in methods)
        {
            if (method.ParameterCount == arguments.Count)
            {
                matchedMethod = method;
                break;
            }
        }

        if (matchedMethod is null)
        {
            throw new ArgumentError(Position,
                $"找不到匹配的方法 {methodName}，参数数量: {arguments.Count}");
        }

        // 调用 ExecuteMethod（需要将其改为 protected 或创建包装方法）
        return ExecuteMethod(matchedMethod, arguments, null, manager);
    }
}
