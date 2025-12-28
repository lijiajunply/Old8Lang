using Old8Lang.AST;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;
using System.Reflection.Emit;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 泛型实例化表达式
/// 例如: Box<int>(), map<string>(arr, func)
/// </summary>
public class GenericInstanceExpression : LangExpression
{
    /// <summary>
    /// 基础表达式（通常是标识符）
    /// 例如: Box, map
    /// </summary>
    public LangExpression BaseExpression { get; }

    /// <summary>
    /// 类型参数列表（类型名称字符串）
    /// 例如: ["int"], ["string", "Person"]
    /// </summary>
    public List<string> TypeArguments { get; }

    /// <summary>
    /// 调用参数（如果是函数调用）
    /// 例如: Box<int>() 时为空列表，map<string>(arr, func) 时为 [arr, func]
    /// </summary>
    public List<LangExpression>? CallArguments { get; }

    /// <summary>
    /// 源代码位置
    /// </summary>
    public SourcePosition Position { get; }

    /// <summary>
    /// 构造函数（泛型类实例化）
    /// </summary>
    public GenericInstanceExpression(
        LangExpression baseExpression,
        List<string> typeArguments,
        SourcePosition position = default)
    {
        BaseExpression = baseExpression;
        TypeArguments = typeArguments;
        CallArguments = null;
        Position = position;
    }

    /// <summary>
    /// 构造函数（泛型函数调用）
    /// </summary>
    public GenericInstanceExpression(
        LangExpression baseExpression,
        List<string> typeArguments,
        List<LangExpression> callArguments,
        SourcePosition position = default)
    {
        BaseExpression = baseExpression;
        TypeArguments = typeArguments;
        CallArguments = callArguments;
        Position = position;
    }

    /// <summary>
    /// 是否为函数调用
    /// </summary>
    public bool IsFunctionCall => CallArguments != null;

    public override LangValueType Run(VariateManager manager)
    {
        // 获取基础类型或函数
        var baseValue = BaseExpression.Run(manager);

        // 从解释器获取类型注解管理器
        if (manager.Interpreter == null)
        {
            throw new InvalidOperationError(this, "无法获取 TypeAnnotationManager：解释器未初始化");
        }

        var typeAnnotationManager = manager.Interpreter.TypeAnnotationManager;

        var resolvedTypeArgs = new Dictionary<string, ITypeInfo>();

        // 处理泛型类
        if (baseValue is TypeTemplate typeTemplate)
        {
            if (typeTemplate.GenericParameters == null || typeTemplate.GenericParameters.Count == 0)
            {
                throw new InvalidOperationError(this, $"类型 {typeTemplate.ClassName} 不是泛型类");
            }

            // 验证类型参数数量
            if (TypeArguments.Count != typeTemplate.GenericParameters.Count)
            {
                throw new InvalidOperationError(this,
                    $"类型参数数量不匹配：期望 {typeTemplate.GenericParameters.Count} 个，实际 {TypeArguments.Count} 个");
            }

            // 解析类型参数
            for (int i = 0; i < TypeArguments.Count; i++)
            {
                var paramName = typeTemplate.GenericParameters[i].Name;
                var typeArgName = TypeArguments[i];

                var typeInfo = typeAnnotationManager.GetTypeFamily().GetType(typeArgName);
                if (typeInfo == null)
                {
                    throw new InvalidOperationError(this, $"未知的类型: {typeArgName}");
                }

                resolvedTypeArgs[paramName] = typeInfo;
            }

            // 实例化泛型类
            var instantiatedTemplate = typeTemplate.InstantiateGeneric(resolvedTypeArgs, typeAnnotationManager);

            // 如果后面跟着调用参数，创建实例
            if (IsFunctionCall)
            {
                var instance = instantiatedTemplate.CreateInstanceV2(manager);
                instance.Init(manager.Interpreter!);
                return instance;
            }

            // 否则返回实例化的类模板
            return instantiatedTemplate;
        }

        // 处理泛型函数
        if (baseValue is FuncLangValue funcValue)
        {
            if (funcValue.GenericParameters == null || funcValue.GenericParameters.Count == 0)
            {
                throw new InvalidOperationError(this, $"函数 {funcValue.Id?.IdName} 不是泛型函数");
            }

            // 验证类型参数数量
            if (TypeArguments.Count != funcValue.GenericParameters.Count)
            {
                throw new InvalidOperationError(this,
                    $"类型参数数量不匹配：期望 {funcValue.GenericParameters.Count} 个，实际 {TypeArguments.Count} 个");
            }

            // 解析类型参数
            for (int i = 0; i < TypeArguments.Count; i++)
            {
                var paramName = funcValue.GenericParameters[i].Name;
                var typeArgName = TypeArguments[i];

                var typeInfo = typeAnnotationManager.GetTypeFamily().GetType(typeArgName);
                if (typeInfo == null)
                {
                    throw new InvalidOperationError(this, $"未知的类型: {typeArgName}");
                }

                resolvedTypeArgs[paramName] = typeInfo;
            }

            // 实例化泛型函数
            var instantiatedFunc = funcValue.InstantiateGeneric(resolvedTypeArgs, typeAnnotationManager);

            // 调用实例化后的函数
            if (IsFunctionCall)
            {
                return instantiatedFunc.Run(manager, CallArguments!);
            }

            // 返回实例化的函数（作为一等公民）
            return instantiatedFunc;
        }

        throw new InvalidOperationError(this, $"表达式 {BaseExpression} 不是泛型类型或泛型函数");
    }

    public override TResult Accept<TResult>(Visitor.IVisitor<TResult> visitor)
    {
        // TODO: Visitor 模式支持
        throw new NotSupportedException("GenericInstanceExpression 暂不支持 Visitor 模式");
    }

    public override string ToString()
    {
        var typeArgsStr = string.Join(", ", TypeArguments);
        if (IsFunctionCall)
        {
            var argsStr = string.Join(", ", CallArguments!.Select(a => a.ToString()));
            return $"{BaseExpression}<{typeArgsStr}>({argsStr})";
        }
        return $"{BaseExpression}<{typeArgsStr}>";
    }
}
