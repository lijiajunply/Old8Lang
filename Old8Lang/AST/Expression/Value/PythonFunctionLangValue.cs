using System.Globalization;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Python.Runtime;
using System.Reflection.Emit;
using Old8Lang.Compiler.CodeGeneration;

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// Python 函数值类型，包装 Python 函数对象，继承自 FuncLangValue 以支持正常的函数调用
/// </summary>
public class PythonFunctionLangValue : FuncLangValue
{
    private readonly dynamic _pythonFunction;
    private readonly string _functionName;

    /// <summary>
    /// 构造 Python 函数包装器
    /// </summary>
    /// <param name="functionName">函数名称</param>
    /// <param name="pythonFunction">Python 函数对象</param>
    /// <param name="parameters">参数列表</param>
    public PythonFunctionLangValue(string functionName, dynamic pythonFunction, List<LangId> parameters)
        : base(new LangId(functionName), parameters, new BlockStatement(new List<IOldLangTree>()))
    {
        _pythonFunction = pythonFunction;
        _functionName = functionName;
    }

    /// <summary>
    /// 重写基类的Run方法 - 处理位置参数和命名参数
    /// </summary>
    public override LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> positionalArgs,
        List<NamedArgument>? namedArgs, SourcePosition callPosition, object? obj = null)
    {
        return ExecutePythonFunction(variateManagerFunc, positionalArgs);
    }

    /// <summary>
    /// 重写基类的Run方法 - 处理位置参数
    /// </summary>
    public override LangValueType Run(VariateManager variateManagerFunc, List<LangExpression> positionalArgs,
        object? obj = null)
    {
        return ExecutePythonFunction(variateManagerFunc, positionalArgs);
    }

    /// <summary>
    /// 执行 Python 函数调用
    /// </summary>
    private LangValueType ExecutePythonFunction(VariateManager variateManagerFunc, List<LangExpression> positionalArgs)
    {
        using (Py.GIL())
        {
            try
            {
                // 将 Old8Lang 参数转换为 Python 对象
                var pythonArgs = new List<LangValueType>();

                // 处理位置参数
                foreach (var arg in positionalArgs)
                {
                    var argValue = arg.Run(variateManagerFunc);
                    pythonArgs.Add(argValue);
                }

                var pyArgs = pythonArgs.Select(ConvertToPython).ToArray();

                // 调用 Python 函数
                dynamic result = _pythonFunction.Invoke(pyArgs);

                // 将 Python 返回值转换为 Old8Lang 值
                return ConvertFromPython(result);
            }
            catch (PythonException ex)
            {
                throw new InvalidOperationError(default(SourcePosition),
                    $"Python 函数 {_functionName} 调用失败：\n{ex.Message}");
            }
        }
    }

    /// <summary>
    /// 将 Old8Lang 值转换为 Python 对象
    /// </summary>
    private PyObject ConvertToPython(LangValueType value)
    {
        return value switch
        {
            IntLangValue intVal => new PyInt(intVal.Value).ToPython(),
            DoubleLangValue doubleVal => new PyFloat(doubleVal.Value).ToPython(),
            StringLangValue stringVal => new PyString(stringVal.Value).ToPython(),
            BoolLangValue boolVal => boolVal.Value.ToPython(),
            NullLangValue => PyObject.None,
            DictionaryLangValue dictVal => ConvertDictToPython(dictVal),
            ILangList listVal => ConvertListToPython(listVal),
            _ => throw new TypeError(this, $"不支持将类型 {value.GetType().Name} 转换为 Python 对象")
        };
    }

    /// <summary>
    /// 将列表转换为 Python list
    /// </summary>
    private PyObject ConvertListToPython(ILangList list)
    {
        var pyList = new PyList();
        foreach (var item in list.GetItems())
        {
            pyList.Append(ConvertToPython(item));
        }

        return pyList;
    }

    /// <summary>
    /// 将字典转换为 Python dict
    /// </summary>
    private PyObject ConvertDictToPython(DictionaryLangValue dict)
    {
        var pyDict = new PyDict();
        foreach (var kvp in dict.Value)
        {
            var key = ConvertToPython(kvp.Key);
            var value = ConvertToPython(kvp.Value);
            pyDict.SetItem(key, value);
        }

        return pyDict;
    }

    /// <summary>
    /// 将 Python 对象转换为 Old8Lang 值
    /// </summary>
    private LangValueType ConvertFromPython(dynamic? pyObj)
    {
        // 检查是否为 null 或 Python None
        if (pyObj is null)
        {
            return new NullLangValue();
        }

        // 转换为 PyObject 以便使用 Python.NET API
        PyObject pyObject;
        if (pyObj is PyObject po)
        {
            pyObject = po;
        }
        else
        {
            // 如果已经是 .NET 类型，直接转换
            return pyObj switch
            {
                int i => new IntLangValue(i),
                long l => new IntLangValue((int)l),
                double d => new DoubleLangValue(d),
                float f => new DoubleLangValue(f),
                string s => new StringLangValue(s),
                bool b => new BoolLangValue(b),
                _ => new StringLangValue(pyObj.ToString())
            };
        }

        // 检查是否是 Python None
        if (pyObject.IsNone())
        {
            return new NullLangValue();
        }

        // 获取 Python 对象的实际类型名称
        string typeName;
        try
        {
            typeName = pyObject.GetPythonType().Name;
        }
        catch
        {
            // 无法获取类型名称，使用默认转换
            typeName = "";
        }

        // 根据类型名称进行转换
        try
        {
            switch (typeName)
            {
                case "bool":
                    return new BoolLangValue(pyObject.As<bool>());

                case "int":
                    return new IntLangValue((int)pyObject.As<long>());

                case "float":
                    return new DoubleLangValue(pyObject.As<double>());

                case "str":
                    return new StringLangValue(pyObject.As<string>());

                case "list":
                    return ConvertListFromPython(pyObject);

                case "dict":
                    return ConvertDictFromPython(pyObject);

                case "tuple":
                    return ConvertTupleFromPython(pyObject);

                default:
                    // 对于其他类型，尝试检查是否是列表或字典
                    if (pyObject.HasAttr("__iter__") && pyObject.HasAttr("__getitem__") && !pyObject.HasAttr("keys"))
                    {
                        return ConvertListFromPython(pyObject);
                    }

                    if (pyObject.HasAttr("keys") && pyObject.HasAttr("__getitem__"))
                    {
                        return ConvertDictFromPython(pyObject);
                    }

                    // 默认转换为字符串
                    return new StringLangValue(pyObject.ToString(CultureInfo.InvariantCulture));
            }
        }
        catch
        {
            // 转换失败，返回字符串表示
            return new StringLangValue(pyObject.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// 将 Python list 转换为 Old8Lang 列表
    /// </summary>
    private LangValueType ConvertListFromPython(dynamic pyList)
    {
        var list = new List<LangValueType>();
        foreach (var item in pyList)
        {
            list.Add(ConvertFromPython(item));
        }

        return new ListLangValue(list);
    }

    /// <summary>
    /// 将 Python dict 转换为 Old8Lang 字典
    /// </summary>
    private LangValueType ConvertDictFromPython(dynamic pyDict)
    {
        // 创建空字典并直接填充运行时值
        var dict = new DictionaryLangValue();
        foreach (var key in pyDict.keys())
        {
            var value = pyDict[key];
            var oldKey = ConvertFromPython(key);
            var oldValue = ConvertFromPython(value);
            dict.Value.Add((Key: oldKey, Value: oldValue));
        }

        return dict;
    }

    /// <summary>
    /// 将 Python tuple 转换为 Old8Lang 列表
    /// （因为 TupleLangValue 是 AST 节点需要表达式，我们用列表表示运行时元组）
    /// </summary>
    private LangValueType ConvertTupleFromPython(dynamic pyTuple)
    {
        var items = new List<LangValueType>();
        foreach (var item in pyTuple)
        {
            items.Add(ConvertFromPython(item));
        }

        return new ListLangValue(items);
    }

    /// <summary>
    /// 在编译模式下生成 IL 代码（暂不支持）
    /// </summary>
    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        throw new NotSupportedException("编译模式暂不支持 Python 函数调用");
    }

    /// <summary>
    /// 返回输出类型（暂不支持）
    /// </summary>
    public override Type OutputType(LocalManager local)
    {
        throw new NotSupportedException("编译模式暂不支持 Python 函数调用");
    }

    public override string ToString()
    {
        return $"PythonFunction({_functionName})";
    }
}