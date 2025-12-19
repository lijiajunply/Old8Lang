using Old8Lang.AST.Expression.Value;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.ModuleObjects;

/// <summary>
/// 简单模块对象，用于处理带别名的模块导入
/// </summary>
public class SimpleModuleObject(VariateManager manager, SourcePosition position = default) : LangValueType(position)
{
    /// <summary>
    /// 处理方法调用，将其转发到全局作用域
    /// </summary>
    public override LangValueType Dot(LangExpression dotExpression, VariateManager manager1)
    {
        if (dotExpression is Instance { Id: { } langId } instance)
        {
            // 获取函数名，处理大小写不敏感匹配
            var functionName = langId.IdName;

            // 1. 尝试从全局作用域中获取函数（大小写不敏感）
            var func = manager.GetValue(new LangId(functionName));
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                // 直接运行函数调用，返回结果
                if (func is FuncLangValue funcValue)
                {
                    return funcValue.Run(manager, instance.Ids);
                }
            }

            // 2. 如果没有找到函数，尝试从当前作用域中获取
            func = manager1.GetValue(new LangId(functionName));
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager1.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                // 直接运行函数调用，返回结果
                if (func is FuncLangValue funcValue)
                {
                    return funcValue.Run(manager1, instance.Ids);
                }
            }

            // 3. 如果还是没有找到，尝试从导入信息中获取
            foreach (var importInfo in manager.ImportInfos)
            {
                if (importInfo is FuncLangValue funcValue &&
                    (funcValue.Id?.IdName == functionName ||
                     funcValue.Id?.IdName == char.ToUpper(functionName[0]) + functionName.Substring(1)))
                {
                    // 直接运行函数调用，返回结果
                    return funcValue.Run(manager, instance.Ids);
                }
            }
        }
        else if (dotExpression is LangId simpleLangId)
        {
            // 获取函数名，处理大小写不敏感匹配
            var functionName = simpleLangId.IdName;

            // 1. 尝试从全局作用域中获取函数
            var func = manager.GetValue(simpleLangId);
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                return func;
            }

            // 2. 如果没有找到函数，尝试从当前作用域中获取
            func = manager1.GetValue(simpleLangId);
            if (func == null)
            {
                // 尝试大写开头的函数名
                var upperCaseName = char.ToUpper(functionName[0]) + functionName.Substring(1);
                func = manager1.GetValue(new LangId(upperCaseName));
            }

            if (func != null)
            {
                return func;
            }

            // 3. 如果还是没有找到，尝试从导入信息中获取
            foreach (var importInfo in manager.ImportInfos)
            {
                if (importInfo is FuncLangValue funcValue &&
                    (funcValue.Id?.IdName == functionName ||
                     funcValue.Id?.IdName == char.ToUpper(functionName[0]) + functionName.Substring(1)))
                {
                    return funcValue;
                }
            }
        }

        // 如果还是没有找到，调用父类的 Dot 方法（会报错）
        return base.Dot(dotExpression, manager1);
    }

    public override string ToString()
    {
        return "<module>";
    }
}