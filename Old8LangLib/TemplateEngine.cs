using System.Text;
using System.Text.RegularExpressions;

namespace Old8LangLib;

/// <summary>
/// 模板引擎 - 用于HTML和配置文件生成
/// 支持变量替换、条件语句、循环等基本模板功能
/// </summary>
public class TemplateEngine
{
    private readonly Dictionary<string, object> _globalVariables;
    private string _template;
    private readonly Regex _variableRegex;
    private readonly Regex _ifRegex;
    private readonly Regex _forRegex;
    private readonly Regex _commentRegex;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TemplateEngine()
    {
        _globalVariables = new Dictionary<string, object>();
        // 匹配 {{variable}} 或 {{object.property}} 格式的变量(支持点号属性访问)
        _variableRegex = new Regex(@"\{\{\s*([\w.]+)\s*\}\}", RegexOptions.Compiled);
        // 匹配 {% if condition %}...{% endif %} 格式的条件语句
        _ifRegex = new Regex(@"\{\%\s*if\s+([^\%]+?)\s*\%\}(.*?)\{\%\s*endif\s*\%\}",
            RegexOptions.Singleline | RegexOptions.Compiled);
        // 匹配 {% for item in collection %}...{% endfor %} 格式的循环语句
        _forRegex = new Regex(@"\{\%\s*for\s+(\w+)\s+in\s+(\w+)\s*\%\}(.*?)\{\%\s*endfor\s*\%\}",
            RegexOptions.Singleline | RegexOptions.Compiled);
        // 匹配 {# #} 格式的注释
        _commentRegex = new Regex(@"\{#\s*.*?\s*#\}", RegexOptions.Singleline | RegexOptions.Compiled);
    }

    /// <summary>
    /// 添加全局变量
    /// </summary>
    /// <param name="key">变量名</param>
    /// <param name="value">变量值</param>
    public void AddGlobalVariable(string key, object value)
    {
        _globalVariables[key] = value;
    }

    /// <summary>
    /// 批量添加全局变量
    /// </summary>
    /// <param name="variables">变量字典</param>
    public void AddGlobalVariables(Dictionary<string, object> variables)
    {
        foreach (var kvp in variables)
        {
            _globalVariables[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 从字符串加载模板
    /// <param name="template">模板字符串</param>
    /// <returns>模板对象</returns>
    /// </summary>
    public void LoadTemplate(string template)
    {
        _template = template;
    }

    /// <summary>
    /// 从文件加载模板
    /// <param name="filePath">模板文件路径</param>
    /// <returns>模板对象</returns>
    /// </summary>
    public void LoadTemplateFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"模板文件不存在: {filePath}");
        }

        _template = File.ReadAllText(filePath, Encoding.UTF8);
    }

    /// <summary>
    /// 渲染模板
    /// <param name="variables">局部变量（会覆盖同名全局变量）</param>
    /// <returns>渲染后的字符串</returns>
    /// </summary>
    public string Render(Dictionary<string, object>? variables = null)
    {
        if (string.IsNullOrEmpty(_template))
        {
            throw new InvalidOperationException("模板未加载");
        }

        var context = new Dictionary<string, object>(_globalVariables);

        if (variables != null)
        {
            foreach (var kvp in variables)
            {
                context[kvp.Key] = kvp.Value;
            }
        }

        var result = _template;

        // 处理注释
        result = _commentRegex.Replace(result, "");

        // 先处理循环语句(因为循环内部可能包含条件语句和变量)
        result = ProcessForLoops(result, context);

        // 再处理条件语句(处理循环外部的条件语句)
        result = ProcessIfStatements(result, context);

        // 最后处理变量替换
        result = ProcessVariables(result, context);

        return result;
    }

    /// <summary>
    /// 处理条件语句
    /// <param name="template">模板字符串</param>
    /// <param name="context">变量上下文</param>
    /// <returns>处理后的字符串</returns>
    /// </summary>
    private string ProcessIfStatements(string template, Dictionary<string, object> context)
    {
        var matches = _ifRegex.Matches(template);

        foreach (Match match in matches)
        {
            var condition = match.Groups[1].Value.Trim();
            var content = match.Groups[2].Value;

            var result = EvaluateCondition(condition, context) ? content : "";
            template = template.Replace(match.Value, result);
        }

        return template;
    }

    /// <summary>
    /// 处理循环语句
    /// <param name="template">模板字符串</param>
    /// <param name="context">变量上下文</param>
    /// <returns>处理后的字符串</returns>
    /// </summary>
    private string ProcessForLoops(string template, Dictionary<string, object> context)
    {
        var matches = _forRegex.Matches(template);

        foreach (Match match in matches)
        {
            var itemName = match.Groups[1].Value.Trim();
            var collectionName = match.Groups[2].Value.Trim();
            var content = match.Groups[3].Value;

            var collection = GetVariableValue(collectionName, context) as IEnumerable<object>;

            if (collection == null)
            {
                template = template.Replace(match.Value, "");
                continue;
            }

            var result = new StringBuilder();

            foreach (var item in collection)
            {
                var itemContext = new Dictionary<string, object>(context)
                {
                    [itemName] = item
                };

                var processedContent = ProcessIfStatements(content, itemContext);
                processedContent = ProcessForLoops(processedContent, itemContext);
                processedContent = ProcessVariables(processedContent, itemContext);

                result.Append(processedContent);
            }

            template = template.Replace(match.Value, result.ToString());
        }

        return template;
    }

    /// <summary>
    /// 处理变量替换
    /// <param name="template">模板字符串</param>
    /// <param name="context">变量上下文</param>
    /// <returns>处理后的字符串</returns>
    /// </summary>
    private string ProcessVariables(string template, Dictionary<string, object> context)
    {
        var matches = _variableRegex.Matches(template);

        foreach (Match match in matches)
        {
            var variableName = match.Groups[1].Value;
            var value = GetVariableValue(variableName, context);

            template = template.Replace(match.Value, value?.ToString() ?? "");
        }

        return template;
    }

    /// <summary>
    /// 获取变量值
    /// <param name="variableName">变量名</param>
    /// <param name="context">变量上下文</param>
    /// <returns>变量值</returns>
    /// </summary> 
    private object? GetVariableValue(string variableName, Dictionary<string, object> context)
    {
        if (context.TryGetValue(variableName, out var value))
        {
            return value;
        }

        // 尝试获取属性值（如果变量是对象）
        var parts = variableName.Split('.');
        if (parts.Length > 1)
        {
            var objName = parts[0];
            if (context.TryGetValue(objName, out var obj))
            {
                return GetPropertyValue(obj, parts.Skip(1).ToArray());
            }
        }

        return null;
    }

    /// <summary>
    /// 获取对象属性值
    /// <param name="obj">对象</param>
    /// <param name="propertyPath">属性路径</param>
    /// <returns>属性值</returns>
    /// </summary>
    private object? GetPropertyValue(object obj, string[] propertyPath)
    {
        var current = obj;

        foreach (var prop in propertyPath)
        {
            if (current == null) return null;

            var propInfo = current.GetType().GetProperty(prop);
            if (propInfo != null)
            {
                current = propInfo.GetValue(current);
            }
            else
            {
                // 如果不是对象属性，尝试字典访问
                if (current is Dictionary<string, object> dict && dict.TryGetValue(prop, out var value))
                {
                    current = value;
                }
                else
                {
                    return null;
                }
            }
        }

        return current;
    }

    /// <summary>
    /// 评估条件表达式（简单实现，仅支持变量存在性检查）
    /// <param name="condition">条件表达式</param>
    /// <param name="context">变量上下文</param>
    /// <returns>条件结果</returns>
    /// </summary>
    private bool EvaluateCondition(string condition, Dictionary<string, object> context)
    {
        // 简单实现：检查变量是否存在且不为null或空
        condition = condition.Trim();

        if (condition.StartsWith("!"))
        {
            var varName = condition.Substring(1).Trim();
            var value = GetVariableValue(varName, context);
            return value == null || string.IsNullOrEmpty(value.ToString());
        }

        var value2 = GetVariableValue(condition, context);
        return value2 != null && !string.IsNullOrEmpty(value2.ToString());
    }

    /// <summary>
    /// 渲染HTML模板的便捷方法
    /// <param name="template">HTML模板</param>
    /// <param name="variables">变量字典</param>
    /// <returns>渲染后的HTML</returns>
    /// </summary>
    public static string RenderHtml(string template, Dictionary<string, object>? variables = null)
    {
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);
        return engine.Render(variables);
    }

    /// <summary>
    /// 渲染配置文件模板的便捷方法
    /// <param name="template">配置模板</param>
    /// <param name="variables">变量字典</param>
    /// <returns>渲染后的配置</returns>
    /// </summary>
    public static string RenderConfig(string template, Dictionary<string, object>? variables = null)
    {
        var engine = new TemplateEngine();
        engine.LoadTemplate(template);
        return engine.Render(variables);
    }
}