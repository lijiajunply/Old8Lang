using System.Collections.Generic;

namespace Old8Lang.LangParser;

/// <summary>
/// 预编译符号管理器，用于管理预编译指令中的符号定义
/// </summary>
public class PreprocessorSymbols
{
    /// <summary>
    /// 已定义的符号集合
    /// </summary>
    private readonly HashSet<string> _symbols;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PreprocessorSymbols()
    {
        _symbols = new HashSet<string>();
    }

    /// <summary>
    /// 构造函数（带预定义符号）
    /// </summary>
    /// <param name="predefinedSymbols">预定义符号列表</param>
    public PreprocessorSymbols(IEnumerable<string> predefinedSymbols)
    {
        _symbols = new HashSet<string>(predefinedSymbols);
    }

    /// <summary>
    /// 定义一个符号（#define）
    /// </summary>
    /// <param name="symbol">符号名称</param>
    public void DefineSymbol(string symbol)
    {
        _symbols.Add(symbol);
    }

    /// <summary>
    /// 取消定义一个符号（#undef）
    /// </summary>
    /// <param name="symbol">符号名称</param>
    public void UndefineSymbol(string symbol)
    {
        _symbols.Remove(symbol);
    }

    /// <summary>
    /// 检查符号是否已定义
    /// </summary>
    /// <param name="symbol">符号名称</param>
    /// <returns>符号是否已定义</returns>
    public bool IsDefined(string symbol)
    {
        return _symbols.Contains(symbol);
    }

    /// <summary>
    /// 计算条件表达式的值
    /// 支持: 符号名, !, &&, ||, 括号
    /// 例如: DEBUG, !RELEASE, DEBUG && FEATURE_A, DEBUG || RELEASE
    /// </summary>
    /// <param name="expression">条件表达式</param>
    /// <returns>表达式计算结果</returns>
    public bool EvaluateCondition(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return false;
        }

        // 移除两端空格
        expression = expression.Trim();

        // 递归处理括号
        int depth = 0;
        int startIndex = -1;
        for (int i = 0; i < expression.Length; i++)
        {
            if (expression[i] == '(')
            {
                if (depth == 0)
                {
                    startIndex = i;
                }
                depth++;
            }
            else if (expression[i] == ')')
            {
                depth--;
                if (depth == 0 && startIndex >= 0)
                {
                    // 找到匹配的括号对，递归计算括号内的表达式
                    string innerExpr = expression.Substring(startIndex + 1, i - startIndex - 1);
                    bool innerResult = EvaluateCondition(innerExpr);

                    // 替换括号及其内容为计算结果
                    string replacement = innerResult ? "true" : "false";
                    expression = expression.Substring(0, startIndex) + replacement + expression.Substring(i + 1);

                    // 重新开始处理
                    return EvaluateCondition(expression);
                }
            }
        }

        // 处理 || (逻辑或，优先级最低)
        depth = 0;
        for (int i = 0; i < expression.Length - 1; i++)
        {
            if (expression[i] == '(')
            {
                depth++;
            }
            else if (expression[i] == ')')
            {
                depth--;
            }
            else if (depth == 0 && expression[i] == '|' && expression[i + 1] == '|')
            {
                // 找到 ||，分割并递归计算
                string left = expression.Substring(0, i).Trim();
                string right = expression.Substring(i + 2).Trim();
                return EvaluateCondition(left) || EvaluateCondition(right);
            }
        }

        // 处理 && (逻辑与)
        depth = 0;
        for (int i = 0; i < expression.Length - 1; i++)
        {
            if (expression[i] == '(')
            {
                depth++;
            }
            else if (expression[i] == ')')
            {
                depth--;
            }
            else if (depth == 0 && expression[i] == '&' && expression[i + 1] == '&')
            {
                // 找到 &&，分割并递归计算
                string left = expression.Substring(0, i).Trim();
                string right = expression.Substring(i + 2).Trim();
                return EvaluateCondition(left) && EvaluateCondition(right);
            }
        }

        // 处理 ! (逻辑非)
        if (expression.StartsWith("!"))
        {
            string operand = expression.Substring(1).Trim();
            return !EvaluateCondition(operand);
        }

        // 处理字面量 true/false
        if (expression == "true")
        {
            return true;
        }
        if (expression == "false")
        {
            return false;
        }

        // 处理符号名称
        return IsDefined(expression);
    }

    /// <summary>
    /// 获取所有已定义的符号
    /// </summary>
    /// <returns>符号集合</returns>
    public IReadOnlySet<string> GetDefinedSymbols()
    {
        return _symbols;
    }
}
