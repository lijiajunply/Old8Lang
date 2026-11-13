using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.TypeSystem;

/// <summary>
/// 渐进式类型推断引擎：TypeScript风格的智能类型推断
/// </summary>
public class TypeInferenceEngine
{
    private readonly LocalManager _localManager;
    private readonly TypeInferenceContext _context;
    private readonly TypeConstraintCollector _collector;
    private readonly TypeConstraintSolver _solver;
    private readonly TypeInferenceConfig _config;

    /// <summary>
    /// 函数信息缓存：函数名 -> (参数列表, 返回类型, 函数体)
    /// </summary>
    private readonly Dictionary<string, FuncInit> _functionRegistry = [];

    public TypeInferenceEngine(LocalManager localManager)
    {
        _localManager = localManager;
        _config = TypeInferenceConfig.Instance;
        _context = new TypeInferenceContext();
        _collector = new TypeConstraintCollector(_context, localManager);
        _solver = new TypeConstraintSolver(_context, _config);
    }

    /// <summary>
    /// 对整个程序进行类型推断
    /// </summary>
    public bool InferTypes(IOldLangTree program)
    {
        if (!_config.EnableTypeInference)
            return true;

        if (_config.DebugOutput)
        {
            Console.WriteLine("=== 开始渐进式类型推断 ===\n");
        }

        try
        {
            // 第一阶段：收集函数声明
            CollectFunctionDeclarations(program);

            // 第二阶段：分析函数调用并收集约束
            AnalyzeProgram(program);

            // 第三阶段：求解约束
            bool success = _solver.Solve();

            if (success)
            {
                // 第四阶段：应用推断结果到LocalManager
                ApplyInferredTypes();
            }

            return success;
        }
        catch (Exception ex)
        {
            if (_config.DebugOutput)
            {
                Console.WriteLine($"类型推断失败: {ex.Message}");
            }

            return false;
        }
    }

    /// <summary>
    /// 收集所有函数声明
    /// </summary>
    private void CollectFunctionDeclarations(IOldLangTree? tree)
    {
        if (tree is FuncInit funcInit)
        {
            var funcName = funcInit.FuncValue.Id?.IdName ?? "anonymous";
            _functionRegistry[funcName] = funcInit;

            if (_config.DebugOutput)
            {
                Console.WriteLine($"注册函数: {funcName}");
            }
        }

        // 递归遍历子节点（如果支持）
        if (tree is OldStatement stmt)
        {
            for (int i = 0; i < stmt.Count; i++)
            {
                CollectFunctionDeclarations(stmt[i]);
            }
        }
    }

    /// <summary>
    /// 分析程序并收集约束
    /// </summary>
    private void AnalyzeProgram(IOldLangTree tree)
    {
        // 遍历AST收集约束
        AnalyzeNode(tree);

        // 分析函数声明
        foreach (var funcInit in _functionRegistry.Values)
        {
            _collector.CollectFromFunction(funcInit);
        }
    }

    /// <summary>
    /// 分析单个AST节点
    /// </summary>
    private void AnalyzeNode(IOldLangTree? node)
    {
        switch (node)
        {
            case FunctionCallExpression callExpr:
                AnalyzeFunctionCall(callExpr);
                break;

            case SetStatement setStmt:
                _collector.CollectFromAssignment(setStmt);
                break;

            case FuncInit funcInit:
                // 在函数体内递归分析
                AnalyzeNode(funcInit.FuncValue);

                break;
        }

        // 递归分析子节点（如果支持）
        if (node is OldStatement stmt)
        {
            for (int i = 0; i < stmt.Count; i++)
            {
                AnalyzeNode(stmt[i]);
            }
        }
    }

    /// <summary>
    /// 分析函数调用
    /// </summary>
    private void AnalyzeFunctionCall(FunctionCallExpression callExpr)
    {
        // 获取函数名
        string? funcName = null;

        if (callExpr.FunctionExpression is LangId langId)
        {
            funcName = langId.IdName;
        }

        if (funcName is not null && _functionRegistry.ContainsKey(funcName))
        {
            // 从调用处收集参数类型约束
            _collector.CollectFromFunctionCall(callExpr, funcName);

            if (_config.DebugOutput)
            {
                Console.WriteLine($"分析函数调用: {funcName}");
            }
        }
    }

    /// <summary>
    /// 应用推断结果到LocalManager
    /// </summary>
    private void ApplyInferredTypes()
    {
        if (_config.DebugOutput)
        {
            Console.WriteLine("\n=== 应用推断结果 ===");
        }

        foreach (var (typeVar, inferredType) in _context.TypeVariableBindings)
        {
            // 解析类型变量名：{funcName}$param${index}${paramName}
            if (typeVar.Contains("$param$"))
            {
                ApplyParameterType(typeVar, inferredType);
            }
            else if (typeVar.Contains("$return"))
            {
                ApplyReturnType(typeVar, inferredType);
            }
            else
            {
                // 普通变量
                _localManager.LocalVarTypes.TryAdd(typeVar, inferredType);
            }
        }
    }

    /// <summary>
    /// 应用参数类型推断结果
    /// </summary>
    private void ApplyParameterType(string typeVar, Type inferredType)
    {
        // 解析：{funcName}$param${index}${paramName}
        var parts = typeVar.Split("$param$");
        if (parts.Length != 2)
            return;

        var funcName = parts[0];
        var paramParts = parts[1].Split('$');
        if (paramParts.Length < 2)
            return;

        if (!int.TryParse(paramParts[0], out int paramIndex))
            return;

        var paramName = paramParts[1];

        // 查找函数并更新参数类型
        if (_functionRegistry.TryGetValue(funcName, out var funcInit))
        {
            if (funcInit.FuncValue.Ids is not null &&
                paramIndex < funcInit.FuncValue.Ids.Count)
            {
                var param = funcInit.FuncValue.Ids[paramIndex];

                // 如果参数没有显式类型注解，应用推断类型
                if (string.IsNullOrEmpty(param.AssumptionType))
                {
                    // 将推断类型存储到LocalManager
                    var fullParamName = $"{funcName}_{paramName}";
                    _localManager.LocalVarTypes[fullParamName] = inferredType;

                    if (_config.DebugOutput)
                    {
                        Console.WriteLine($"  ✓ 推断参数类型: {funcName}.{paramName} = {inferredType.Name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 应用返回类型推断结果
    /// </summary>
    private void ApplyReturnType(string typeVar, Type inferredType)
    {
        // 解析：{funcName}$return
        var funcName = typeVar.Replace("$return", "");

        if (_functionRegistry.TryGetValue(funcName, out var funcInit))
        {
            if (string.IsNullOrEmpty(funcInit.FuncValue.Id?.AssumptionType))
            {
                // 存储推断的返回类型
                var returnTypeKey = $"{funcName}_return_type";
                _localManager.LocalVarTypes[returnTypeKey] = inferredType;

                if (_config.DebugOutput)
                {
                    Console.WriteLine($"  ✓ 推断返回类型: {funcName} -> {inferredType.Name}");
                }
            }
        }
    }

    /// <summary>
    /// 为特定函数推断类型
    /// </summary>
    public bool InferFunctionTypes(FuncInit funcInit)
    {
        if (!_config.EnableTypeInference)
            return true;

        try
        {
            // 清空上下文
            _context.Clear();

            // 收集函数约束
            _collector.CollectFromFunction(funcInit);

            // 分析函数体
            AnalyzeNode(funcInit.FuncValue);

            // 求解约束
            bool success = _solver.Solve();

            if (success)
            {
                ApplyInferredTypes();
            }

            return success;
        }
        catch (Exception ex)
        {
            if (_config.DebugOutput)
            {
                Console.WriteLine($"函数 {funcInit.FuncValue.Id?.IdName} 类型推断失败: {ex.Message}");
            }

            return _config.FallbackToDynamic;
        }
    }

    /// <summary>
    /// 获取变量的推断类型
    /// </summary>
    public Type? GetInferredType(string variableName)
    {
        return _context.GetTypeBinding(variableName) ??
               _localManager.LocalVarTypes.GetValueOrDefault(variableName);
    }

    /// <summary>
    /// 检查是否需要类型推断
    /// </summary>
    public bool NeedsTypeInference(FuncInit funcInit)
    {
        if (!_config.EnableTypeInference)
            return false;

        // 检查是否有参数缺少类型注解
        if (funcInit.FuncValue.Ids is not null)
        {
            foreach (var param in funcInit.FuncValue.Ids)
            {
                if (string.IsNullOrEmpty(param.AssumptionType) && param.DefaultValue is null)
                {
                    return true;
                }
            }
        }

        // 检查是否缺少返回类型
        if (string.IsNullOrEmpty(funcInit.FuncValue.Id?.AssumptionType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 获取推断统计信息
    /// </summary>
    public (int totalConstraints, int resolvedTypes, int unresolvedTypes) GetStatistics()
    {
        var totalConstraints = _context.Constraints.Count;
        var resolvedTypes = _context.TypeVariableBindings.Count;
        var unresolvedTypes = _context.Constraints
            .Where(c => _context.GetTypeBinding(c.TypeVariable) is null)
            .Select(c => c.TypeVariable)
            .Distinct()
            .Count();

        return (totalConstraints, resolvedTypes, unresolvedTypes);
    }

    /// <summary>
    /// 推断表达式的类型名称
    /// </summary>
    public string InferExpressionType(LangExpression expr, LocalManager localManager)
    {
        return expr switch
        {
            IntLangValue => "int",
            DoubleLangValue => "double",
            StringLangValue => "string",
            BoolLangValue => "bool",
            CharLangValue => "char",
            ListLangValue list => list.ElementType is not null ? $"list<{list.ElementType}>" : "list",
            ArrayLangValue array => array.ElementType is not null ? $"array<{array.ElementType}>" : "array",
            DictionaryLangValue dict => dict.KeyType is not null && dict.ValueType is not null
                ? $"dict<{dict.KeyType},{dict.ValueType}>"
                : "dict",
            _ => "any"
        };
    }

    /// <summary>
    /// 推断集合字面量的泛型类型
    /// </summary>
    public string InferCollectionElementType(List<LangExpression> elements, LocalManager localManager)
    {
        if (elements.Count == 0)
        {
            return "any";  // 空集合默认为 any
        }

        // 收集所有元素的类型
        var elementTypes = new HashSet<string>();
        foreach (var expr in elements)
        {
            var type = InferExpressionType(expr, localManager);
            elementTypes.Add(type);
        }

        // 如果所有元素类型一致，返回该类型
        if (elementTypes.Count == 1)
        {
            return elementTypes.First();
        }

        // 混合类型：默认返回 any
        return "any";
    }

    /// <summary>
    /// 推断字典键值类型
    /// </summary>
    public (string KeyType, string ValueType) InferDictTypes(
        List<KeyValuePair<LangExpression, LangExpression>> dict,
        LocalManager localManager)
    {
        if (dict.Count == 0)
        {
            return ("any", "any");
        }

        var keyTypes = new HashSet<string>();
        var valueTypes = new HashSet<string>();

        foreach (var (key, value) in dict)
        {
            keyTypes.Add(InferExpressionType(key, localManager));
            valueTypes.Add(InferExpressionType(value, localManager));
        }

        var keyType = keyTypes.Count == 1 ? keyTypes.First() : "any";
        var valueType = valueTypes.Count == 1 ? valueTypes.First() : "any";

        return (keyType, valueType);
    }
}