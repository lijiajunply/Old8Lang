using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

// ReSharper disable CheckNamespace
namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 参数处理
/// </summary>
public partial class FuncLangValue
{
    private static bool IsConstantExpression(LangExpression? expr)
    {
        if (expr is null) return false;

        return expr switch
        {
            // 字面量都是常量
            IntLangValue => true,
            DoubleLangValue => true,
            StringLangValue => true,
            BoolLangValue => true,
            CharLangValue => true,
            NullLangValue => true,

            // 算术运算：如果操作数都是常量，结果也是常量
            Operation op => IsConstantExpression(op.Left) && IsConstantExpression(op.Right),

            // 其他情况（变量、函数调用等）不是常量
            _ => false
        };
    }

    /// <summary>
    /// 初始化默认参数值缓存
    /// </summary>

    private void InitializeDefaultValueCache(VariateManager manager)
    {
        if (Ids is null || Ids.Count == 0) return;

        for (int i = 0; i < Ids.Count; i++)
        {
            var param = Ids[i];
            if (param.DefaultValue is not null && IsConstantExpression(param.DefaultValue))
            {
                // 延迟初始化缓存字典
                CachedDefaultValues ??= new Dictionary<int, LangValueType>();

                // 预先求值并缓存
                var defaultValue = param.DefaultValue.Run(manager);
                CachedDefaultValues[i] = defaultValue;
            }
        }
    }

    /// <summary>
    /// 验证Lambda表达式的类型注解完整性（编译模式要求）
    /// </summary>

    private void ValidateLambdaTypeAnnotations(LocalManager local, string variableName)
    {
        // 验证Lambda参数的类型注解
        if (Ids is not null)
        {
            for (int i = 0; i < Ids.Count; i++)
            {
                var param = Ids[i];
                if (string.IsNullOrEmpty(param.AssumptionType))
                {
                    var errorMsg =
                        $"[编译模式错误] Lambda表达式 '{variableName}' 的参数 '{param.IdName}' (第{i + 1}个参数) 缺少类型注解\n\n" +
                        $"编译模式下Lambda表达式的所有参数必须显式声明类型注解。\n\n" +
                        $"修复示例：\n" +
                        $"  {variableName} <- ({param.IdName}:int, ...) -> {{ ... }}\n" +
                        $"  {variableName} <- ({param.IdName}:int, ...) -> expression\n\n" +
                        $"支持的类型：int, double, string, bool, char, list<T>, array<T>";
                    local.ReportError(errorMsg, param.Position);
                }
            }
        }

        // 注意：Lambda返回类型允许推断，不需要强制声明
    }

    /// <summary>
    /// 验证函数调用时的参数类型匹配
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="argumentValues">计算后的参数值列表</param>
    /// <param name="executionManager">执行管理器，用于获取泛型类型参数映射</param>

    private void ValidateParameterTypes(
        List<LangExpression> argumentExpressions,
        List<LangValueType> argumentValues,
        VariateManager? executionManager = null)
    {
        if (Ids is null) return;

        // 从执行管理器获取泛型类型映射（泛型类的方法）
        // 如果没有，则使用函数自身的泛型映射（泛型函数）
        var typeMapping = executionManager?.CurrentFunctionTypeArgumentMapping ?? TypeArgumentMapping;

        // 使用全局类型检查器进行验证
        TypeChecker.ValidateParameterTypes(
            argumentExpressions.Cast<IOldLangTree>().ToList(),
            argumentValues,
            Ids,
            typeMapping);
    }

    /// <summary>
    /// 统一的参数处理方法：计算、验证、处理默认值和 params 参数，并返回最终参数值列表
    /// </summary>
    /// <param name="argumentExpressions">传入的参数表达式列表</param>
    /// <param name="variManager">外部变量管理器，用于计算参数值</param>
    /// <param name="executionManager">执行管理器，用于获取缓存的默认值</param>
    /// <returns>处理完成的参数值列表</returns>

    private List<LangValueType> ProcessAndValidateParameters(
        List<LangExpression> argumentExpressions,
        VariateManager variManager,
        VariateManager? executionManager = null)
    {
        if (Ids is null) return [];

        // 1. 计算所有传入参数的值
        var paramValues = argumentExpressions.Select(expr => expr.Run(variManager)).ToList();

        // 检查是否有 params 参数
        var paramsIndex = -1;
        for (int i = 0; i < Ids.Count; i++)
        {
            if (Ids[i].IsParams)
            {
                paramsIndex = i;
                break;
            }
        }

        // 如果有 params 参数，需要特殊处理
        if (paramsIndex >= 0)
        {
            // params 参数之前的普通参数数量
            var regularParamCount = paramsIndex;

            // 检查是否提供了足够的参数
            if (paramValues.Count < regularParamCount)
            {
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 至少需要 {regularParamCount} 个参数，但实际提供了 {paramValues.Count} 个参数");
            }

            // 2. 验证普通参数的类型匹配（仅在有类型注解时进行检查）
            if (regularParamCount > 0)
            {
                var regularArgExpressions = argumentExpressions.Take(regularParamCount).ToList();
                var regularParamValues = paramValues.Take(regularParamCount).ToList();
                ValidateParameterTypes(regularArgExpressions, regularParamValues, executionManager);
            }

            // 3. 处理 params 参数：将剩余的参数打包成数组
            var paramsValues = paramValues.Skip(regularParamCount).ToList();

            // 创建 ArrayLangValue
            var paramsArrayValue = new ArrayLangValue(paramsValues);

            // 替换 paramValues：保留普通参数 + params 数组
            var finalParamValues = paramValues.Take(regularParamCount).ToList();
            finalParamValues.Add(paramsArrayValue);

            return finalParamValues;
        }

        // 没有 params 参数，使用原有逻辑
        // 2. 验证参数类型匹配（仅在有类型注解时进行检查）
        ValidateParameterTypes(argumentExpressions, paramValues, executionManager);

        // 3. 处理默认参数，补全缺失的参数值
        for (var i = paramValues.Count; i < Ids.Count; i++)
        {
            var parameter = Ids[i];
            if (parameter.DefaultValue is not null)
            {
                // 优先使用缓存的默认值（如果提供了执行管理器）
                if (executionManager is not null && CachedDefaultValues?.TryGetValue(i, out var cachedValue) == true)
                {
                    paramValues.Add(cachedValue);
                }
                else
                {
                    // 非常量表达式，需要每次计算
                    var defaultValueManager = executionManager ?? variManager;
                    var defaultValue = parameter.DefaultValue.Run(defaultValueManager);
                    paramValues.Add(defaultValue);
                }
            }
            else
            {
                // 没有默认参数且没有传入参数，抛出错误
                throw new ArgumentError(Position,
                    $"函数 '{Id?.IdName}' 的参数 '{parameter.IdName}' 缺少实参且没有默认值");
            }
        }

        return paramValues;
    }

    /// <summary>
    /// 实例化泛型函数
    /// </summary>

}
