using Jint;
using Jint.Native;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// JavaScript 函数包装类
/// 将 Jint 引擎中的 JavaScript 函数包装为 Old8Lang 函数
/// </summary>
public class JavaScriptFunctionLangValue : FuncLangValue
{
    private readonly Engine _engine;
    private readonly string _functionName;
    private readonly string? _returnType;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="name">函数名</param>
    /// <param name="engine">Jint JavaScript 引擎实例</param>
    /// <param name="functionName">JavaScript 函数名</param>
    /// <param name="parameters">参数列表</param>
    /// <param name="returnType">返回类型注解（可选）</param>
    public JavaScriptFunctionLangValue(
        string name,
        Engine engine,
        string functionName,
        List<LangId> parameters,
        string? returnType = null)
        : base(new LangId(name), parameters, new BlockStatement(new List<IOldLangTree>()), null, default, false)
    {
        _engine = engine;
        _functionName = functionName;
        _returnType = returnType;
    }

    /// <summary>
    /// 执行 JavaScript 函数
    /// </summary>
    public override LangValueType Run(
        VariateManager variateManagerFunc,
        List<LangExpression> positionalArgs,
        List<NamedArgument>? namedArgs,
        SourcePosition callPosition,
        object? obj = null)
    {
        return ExecuteJavaScriptFunction(variateManagerFunc, positionalArgs);
    }

    /// <summary>
    /// 执行 JavaScript 函数（无命名参数版本）
    /// </summary>
    public override LangValueType Run(
        VariateManager variateManagerFunc,
        List<LangExpression> positionalArgs,
        object? obj = null)
    {
        return ExecuteJavaScriptFunction(variateManagerFunc, positionalArgs);
    }

    /// <summary>
    /// 核心执行逻辑：调用 JavaScript 函数
    /// </summary>
    private LangValueType ExecuteJavaScriptFunction(
        VariateManager variateManagerFunc,
        List<LangExpression> positionalArgs)
    {
        try
        {
            // 转换参数
            var jsArgs = positionalArgs
                .Select(arg => ConvertToJavaScript(arg.Run(variateManagerFunc)))
                .ToArray();

            // 调用 JavaScript 函数
            var jsFunction = _engine.GetValue(_functionName);
            var result = _engine.Invoke(jsFunction, jsArgs);

            // 转换返回值
            return ConvertFromJavaScript(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationError((SourcePosition)default,
                $"JavaScript 函数 {_functionName} 执行失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 将 Old8Lang 值转换为 JavaScript 值
    /// </summary>
    private object? ConvertToJavaScript(LangValueType value)
    {
        return value switch
        {
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            StringLangValue strVal => strVal.Value,
            BoolLangValue boolVal => boolVal.Value,
            NullLangValue => null,
            DictionaryLangValue dictVal => ConvertDictToJavaScript(dictVal),
            ILangList listVal => ConvertListToJavaScript(listVal),
            _ => value.ToString()
        };
    }

    /// <summary>
    /// 将 Old8Lang 列表转换为 JavaScript 数组
    /// </summary>
    private object?[] ConvertListToJavaScript(ILangList listVal)
    {
        return listVal.GetItems()
            .Select(ConvertToJavaScript)
            .ToArray();
    }

    /// <summary>
    /// 将 Old8Lang 字典转换为 JavaScript 对象
    /// </summary>
    private Dictionary<string, object?> ConvertDictToJavaScript(DictionaryLangValue dictVal)
    {
        var result = new Dictionary<string, object?>();
        foreach (var (key, value) in dictVal.Value)
        {
            // 键必须是字符串才能转为 JS 对象
            var keyStr = key is StringLangValue strKey ? strKey.Value : key.ToString();
            result[keyStr] = ConvertToJavaScript(value);
        }
        return result;
    }

    /// <summary>
    /// 将 JavaScript 值转换为 Old8Lang 值
    /// </summary>
    private LangValueType ConvertFromJavaScript(JsValue jsValue)
    {
        // null 或 undefined
        if (jsValue.IsNull() || jsValue.IsUndefined())
        {
            return new NullLangValue();
        }

        // 布尔值
        if (jsValue.IsBoolean())
        {
            return new BoolLangValue(jsValue.AsBoolean());
        }

        // 数字
        if (jsValue.IsNumber())
        {
            var num = jsValue.AsNumber();

            // 如果函数声明了 double 返回类型,始终返回 DoubleLangValue
            if (_returnType is not null && _returnType.Equals("double", StringComparison.OrdinalIgnoreCase))
            {
                return new DoubleLangValue(num);
            }

            // 否则根据数值判断是否为整数
            if (Math.Abs(num - Math.Floor(num)) < double.Epsilon)
            {
                return new IntLangValue((int)num);
            }
            return new DoubleLangValue(num);
        }

        // 字符串
        if (jsValue.IsString())
        {
            return new StringLangValue(jsValue.AsString());
        }

        // 数组
        if (jsValue.IsArray())
        {
            var array = jsValue.AsArray();
            var list = new List<LangValueType>();
            for (var i = 0; i < array.Length; i++)
            {
                var element = array.Get(i.ToString());
                list.Add(ConvertFromJavaScript(element));
            }
            return new ListLangValue(list);
        }

        // 对象（转换为字典）
        if (jsValue.IsObject())
        {
            var obj = jsValue.AsObject();
            var dict = new DictionaryLangValue();

            foreach (var key in obj.GetOwnProperties())
            {
                var value = obj.Get(key.Key);
                var keyValue = new StringLangValue(key.Key.ToString());
                var valueConverted = ConvertFromJavaScript(value);
                dict.Value.Add((Key: keyValue, Value: valueConverted));
            }

            return dict;
        }

        // 其他类型转换为字符串
        return new StringLangValue(jsValue.ToString());
    }
}
