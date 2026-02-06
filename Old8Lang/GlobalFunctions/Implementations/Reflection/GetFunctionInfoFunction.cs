using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Interpreter;
using Old8Lang.Runtime;
using Old8Lang.Utilities;

namespace Old8Lang.GlobalFunctions.Implementations.Reflection;

/// <summary>
/// GetFunctionInfo 函数 - 获取函数的详细信息
/// 支持：1) 全局函数（字符串名称），2) 普通函数（FuncLangValue），3) 原生函数（MethodInfo），4) 类方法（对象+方法名）
/// </summary>
public sealed class GetFunctionInfoFunction : BaseGlobalFunction
{
    public override string[] Names => ["GetFunctionInfo"];
    public override string[] ParameterNames => ["function", "methodName"];
    public override int MinParameterCount => 1;
    public override int MaxParameterCount => 2;

    protected override LangValueType ExecuteInternal(
        List<LangExpression> parameters,
        VariateManager manager,
        SourcePosition position)
    {
        var results = EvaluateParameters(parameters, manager);
        var firstParam = results[0];

        // 情况 1: 传入字符串 - 查找全局函数
        if (firstParam is StringLangValue stringValue)
        {
            string functionName = stringValue.Value;
            var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(functionName);
            if (globalFunc != null)
            {
                return CreateGlobalFunctionInfo(globalFunc);
            }
            throw new InvalidOperationError(position, $"全局函数 '{functionName}' 不存在");
        }

        // 情况 2: 传入 FuncLangValue - 普通函数或原生函数
        if (firstParam is FuncLangValue funcValue)
        {
            // 检查是否为原生函数
            if (funcValue.Method != null)
            {
                return CreateNativeFunctionInfo(funcValue.Method, funcValue.Id.IdName);
            }
            // 普通 Old8Lang 函数
            return CreateUserFunctionInfo(funcValue);
        }

        // 情况 3: 传入对象 + 方法名 - 类方法
        if (parameters.Count == 2 && firstParam is AnyLangValue anyValue)
        {
            var methodName = ((StringLangValue)results[1]).Value;
            var methods = anyValue.Metadata.MethodTable.LookupMethod(methodName);
            if (methods is null || methods.Count == 0)
            {
                throw new AttributeError(anyValue, methodName, anyValue.ClassId.IdName);
            }
            // 返回第一个重载的信息
            return CreateClassMethodInfo(methods[0], anyValue.ClassId.IdName);
        }

        throw new InvalidOperationError(position, "GetFunctionInfo 需要：全局函数名（字符串）、函数对象（FuncLangValue）或对象+方法名");
    }

    protected override void GenerateIlInternal(
        List<LangExpression> parameters,
        ILGenerator ilGenerator,
        LocalManager local,
        SourcePosition position)
    {
        // 加载第一个参数
        parameters[0].LoadIlValue(ilGenerator, local);

        // 如果有第二个参数，也加载
        if (parameters.Count == 2)
        {
            parameters[1].LoadIlValue(ilGenerator, local);
            // 调用 ReflectionHelper.GetFunctionInfo(object, string)
            var method = GlobalMethodInfoCache.GetMethod(
                typeof(ReflectionHelper),
                nameof(ReflectionHelper.GetFunctionInfo),
                [typeof(object), typeof(string)]);
            ilGenerator.Emit(OpCodes.Call, method!);
        }
        else
        {
            // 调用 ReflectionHelper.GetFunctionInfo(object)
            var method = GlobalMethodInfoCache.GetMethod(
                typeof(ReflectionHelper),
                nameof(ReflectionHelper.GetFunctionInfo),
                [typeof(object)]);
            ilGenerator.Emit(OpCodes.Call, method!);
        }
    }

    protected override Type GetReturnTypeInternal(List<LangExpression> parameters, LocalManager local)
    {
        return typeof(object);
    }

    protected override object ExecuteInVMInternal(object?[] arguments)
    {
        var firstParam = arguments[0];

        // 情况 1: 字符串 - 全局函数
        if (firstParam is string functionName)
        {
            var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(functionName);
            if (globalFunc != null)
            {
                return CreateGlobalFunctionInfoForVM(globalFunc);
            }
            throw new InvalidOperationException($"全局函数 '{functionName}' 不存在");
        }

        // 情况 2: FuncLangValue - 普通函数或原生函数
        if (firstParam is FuncLangValue funcValue)
        {
            if (funcValue.Method != null)
            {
                return CreateNativeFunctionInfoForVM(funcValue.Method, funcValue.Id.IdName);
            }
            return CreateUserFunctionInfoForVM(funcValue);
        }

        // 情况 3: 对象 + 方法名 - 类方法（暂不支持 VM 模式）
        throw new InvalidOperationException("VM 模式下暂不支持类方法反射");
    }

    /// <summary>
    /// 创建普通用户函数信息（解释器模式）
    /// </summary>
    private static LangValueType CreateUserFunctionInfo(FuncLangValue func)
    {
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(func.Id.IdName)]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("user_function")]),
            new TupleLangValue([
                new StringLangValue("parameters"),
                new ListLangValue(func.Ids.Select(id => (LangValueType)new StringLangValue(id.IdName)).ToList())
            ]),
            new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(func.Ids.Count)]),
            new TupleLangValue([new StringLangValue("hasDefaultParams"), new BoolLangValue(func.Ids.Any(id => id.DefaultValue != null))]),
            new TupleLangValue([new StringLangValue("isGeneric"), new BoolLangValue(func.GenericParameters?.Count > 0)])
        };

        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建原生函数信息（解释器模式）
    /// </summary>
    private static LangValueType CreateNativeFunctionInfo(MethodInfo method, string name)
    {
        var parameters = method.GetParameters();
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(name)]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("native_function")]),
            new TupleLangValue([
                new StringLangValue("parameters"),
                new ListLangValue(parameters.Select(p => (LangValueType)new StringLangValue(p.Name ?? "param")).ToList())
            ]),
            new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(parameters.Length)]),
            new TupleLangValue([new StringLangValue("returnType"), new StringLangValue(method.ReturnType.Name)]),
            new TupleLangValue([new StringLangValue("declaringType"), new StringLangValue(method.DeclaringType?.FullName ?? "Unknown")]),
            new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)]),
            new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(method.IsPublic)])
        };

        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建类方法信息（解释器模式）
    /// </summary>
    private static LangValueType CreateClassMethodInfo(LangMethodInfo method, string className)
    {
        var tuples = new List<TupleLangValue>
        {
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(method.MethodName)]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("class_method")]),
            new TupleLangValue([new StringLangValue("className"), new StringLangValue(className)]),
            new TupleLangValue([new StringLangValue("parameterCount"), new IntLangValue(method.ParameterCount)]),
            new TupleLangValue([new StringLangValue("isStatic"), new BoolLangValue(method.IsStatic)]),
            new TupleLangValue([new StringLangValue("isPublic"), new BoolLangValue(!method.HasModifier(AccessModifierType.Private))]),
            new TupleLangValue([new StringLangValue("isPrivate"), new BoolLangValue(method.HasModifier(AccessModifierType.Private))]),
            new TupleLangValue([new StringLangValue("isAbstract"), new BoolLangValue(method.IsAbstract)]),
            new TupleLangValue([new StringLangValue("isVirtual"), new BoolLangValue(method.IsVirtual)])
        };

        return new DictionaryLangValue(tuples);
    }

    /// <summary>
    /// 创建普通用户函数信息（虚拟机模式）
    /// </summary>
    private static Dictionary<object, object?> CreateUserFunctionInfoForVM(FuncLangValue func)
    {
        return new Dictionary<object, object?>
        {
            ["name"] = func.Id.IdName,
            ["type"] = "user_function",
            ["parameters"] = func.Ids.Select(id => id.IdName).Cast<object?>().ToList(),
            ["parameterCount"] = func.Ids.Count,
            ["hasDefaultParams"] = func.Ids.Any(id => id.DefaultValue != null),
            ["isGeneric"] = func.GenericParameters?.Count > 0
        };
    }

    /// <summary>
    /// 创建原生函数信息（虚拟机模式）
    /// </summary>
    private static Dictionary<object, object?> CreateNativeFunctionInfoForVM(MethodInfo method, string name)
    {
        var parameters = method.GetParameters();
        return new Dictionary<object, object?>
        {
            ["name"] = name,
            ["type"] = "native_function",
            ["parameters"] = parameters.Select(p => p.Name ?? "param").Cast<object?>().ToList(),
            ["parameterCount"] = parameters.Length,
            ["returnType"] = method.ReturnType.Name,
            ["declaringType"] = method.DeclaringType?.FullName ?? "Unknown",
            ["isStatic"] = method.IsStatic,
            ["isPublic"] = method.IsPublic
        };
    }
    private static LangValueType CreateGlobalFunctionInfo(IGlobalFunction func)
    {
        var tuples = new List<TupleLangValue>
        {
            // 基本信息
            new TupleLangValue([new StringLangValue("name"), new StringLangValue(func.Names[0])]),
            new TupleLangValue([new StringLangValue("type"), new StringLangValue("global_function")]),
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
            new TupleLangValue([new StringLangValue("returnType"), new StringLangValue("object")])
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
            ["type"] = "global_function",
            ["names"] = func.Names.Cast<object?>().ToList(),

            // 参数信息
            ["parameters"] = CreateParameterListForVM(func),

            // 参数数量
            ["minParameterCount"] = func.MinParameterCount,
            ["maxParameterCount"] = func.MaxParameterCount,

            // 返回类型
            ["returnType"] = "object"
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
