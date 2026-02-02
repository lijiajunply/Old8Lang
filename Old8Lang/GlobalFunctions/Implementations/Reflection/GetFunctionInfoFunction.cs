using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetFunctionInfo 函数 - 获取全局函数的参数信息
/// </summary>
public sealed class GetFunctionInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetFunctionInfo"];
    public override string[]? ParameterNames => ["functionName"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 1;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        string functionName = ((StringLangValue)results[0]).Value;

        // 1. 首先尝试从全局函数注册表查找
        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(functionName);
        if (globalFunc != null)
        {
            return CreateGlobalFunctionInfo(globalFunc);
        }

        // 2. 未找到函数
        throw new InvalidOperationError(position, $"函数 '{functionName}' 不存在");
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载函数名参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 调用 ReflectionHelper.GetFunctionInfo(string)
        var method = GlobalMethodInfoCache.GetMethod(typeof(ReflectionHelper), nameof(ReflectionHelper.GetFunctionInfo));
        ilGenerator.Emit(OpCodes.Call, method!);
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object? ExecuteInVMInternal(object?[] arguments)
    {
        string functionName = (string)arguments[0]!;

        // 1. 尝试从全局函数注册表查找
        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(functionName);
        if (globalFunc != null)
        {
            return CreateGlobalFunctionInfoForVM(globalFunc);
        }

        // 2. 未找到函数
        throw new InvalidOperationException($"函数 '{functionName}' 不存在");
    }

    /// <summary>
    /// 创建全局函数信息（解释器模式）
    /// </summary>
    private static LangValueType CreateGlobalFunctionInfo(IGlobalFunction func)
    {
        var tuples = new List<TupleLangValue>
        {
            // 基本信息
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(func.Names[0])]),
            new TupleLangValue([
                new StringLangValue("names"),
                new ListLangValue(func.Names.Select(n => (LangValueType)new StringLangValue(n)).ToList())
            ]),

            // 参数信息
            new TupleLangValue([
                new StringLangValue("parameters"),
                CreateParameterList(func)
            ]),

            // 参数数量
            new TupleLangValue([new StringLangValue("minParameterCount"), new IntLangValue(func.MinParameterCount)]),
            new TupleLangValue([new StringLangValue("maxParameterCount"), new IntLangValue(func.MaxParameterCount)]),

            // 返回类型（全局函数通常是动态类型）
            new TupleLangValue([new StringLangValue("returnType"), new StringLangValue("object")]),

            // 标记
            new TupleLangValue([new StringLangValue("isGlobalFunction"), new BoolLangValue(true)])
        };

        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建参数列表（解释器模式）
    /// </summary>
    private static LangValueType CreateParameterList(IGlobalFunction func)
    {
        var paramList = new List<LangValueType>();

        if (func.ParameterNames != null && func.ParameterNames.Length > 0)
        {
            foreach (var paramName in func.ParameterNames)
            {
                var paramTuples = new List<TupleLangValue>
                {
                    new TupleLangValue([new StringLangValue("name"), new StringLangValue(paramName)]),
                    new TupleLangValue([new StringLangValue("type"), new StringLangValue("object")])
                };
                paramList.Add(new DictionaryLangValue(paramTuples));
            }
        }

        return new ListLangValue(paramList);
    }

    /// <summary>
    /// 创建全局函数信息（虚拟机模式）
    /// </summary>
    private static Dictionary<object, object?> CreateGlobalFunctionInfoForVM(IGlobalFunction func)
    {
        var dict = new Dictionary<object, object?>
        {
            // 基本信息
            ["name"] = func.Names[0],
            ["names"] = func.Names.Cast<object?>().ToList(),

            // 参数信息
            ["parameters"] = CreateParameterListForVM(func),

            // 参数数量
            ["minParameterCount"] = func.MinParameterCount,
            ["maxParameterCount"] = func.MaxParameterCount,

            // 返回类型
            ["returnType"] = "object",

            // 标记
            ["isGlobalFunction"] = true
        };

        return dict;
    }

    /// <summary>
    /// 创建参数列表（虚拟机模式）
    /// </summary>
    private static List<object?> CreateParameterListForVM(IGlobalFunction func)
    {
        var paramList = new List<object?>();

        if (func.ParameterNames != null && func.ParameterNames.Length > 0)
        {
            foreach (var paramName in func.ParameterNames)
            {
                var paramDict = new Dictionary<object, object?>
                {
                    ["name"] = paramName,
                    ["type"] = "object"
                };
                paramList.Add(paramDict);
            }
        }

        return paramList;
    }
}
