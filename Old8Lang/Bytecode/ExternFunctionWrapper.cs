using System.Runtime.InteropServices;
using Old8Lang.AST.Statement;

namespace Old8Lang.Bytecode;

/// <summary>
/// Extern 函数包装器 - 用于在虚拟机中调用外部函数
/// </summary>
public class ExternFunctionWrapper
{
    private readonly string _dllName;
    private readonly string _funcName;
    private readonly ExternType _externType;
    private readonly CallingConventionType _callingConvention;
    private readonly string _signatureStr;

    // 缓存的委托和函数指针
    private Delegate? _cachedDelegate;
    private IntPtr _cachedFuncPtr;
    private IntPtr _cachedLibHandle;

    public ExternFunctionWrapper(
        string dllName,
        string funcName,
        ExternType externType,
        CallingConventionType callingConvention,
        string signatureStr)
    {
        _dllName = dllName;
        _funcName = funcName;
        _externType = externType;
        _callingConvention = callingConvention;
        _signatureStr = signatureStr;
    }

    /// <summary>
    /// 调用 extern 函数
    /// </summary>
    public object? Invoke(object?[] args)
    {
        return _externType switch
        {
            ExternType.NativeDll => InvokeNativeDll(args),
            ExternType.PythonScript => throw new NotSupportedException("虚拟机模式暂不支持 Python 脚本调用"),
            ExternType.PythonModule => throw new NotSupportedException("虚拟机模式暂不支持 Python 模块调用"),
            ExternType.JavaScript => throw new NotSupportedException("虚拟机模式暂不支持 JavaScript 调用"),
            _ => throw new NotSupportedException($"不支持的 extern 类型: {_externType}")
        };
    }

    /// <summary>
    /// 调用原生 DLL 函数
    /// </summary>
    private object? InvokeNativeDll(object?[] args)
    {
        // 解析函数签名
        var (paramTypes, returnType) = ParseSignature(_signatureStr);

        // 加载 DLL（如果尚未加载）
        if (_cachedLibHandle == IntPtr.Zero)
        {
            try
            {
                _cachedLibHandle = NativeLibrary.Load(_dllName);
            }
            catch (DllNotFoundException ex)
            {
                throw new Exception($"无法加载 DLL '{_dllName}': {ex.Message}");
            }
        }

        // 获取函数指针（如果尚未获取）
        if (_cachedFuncPtr == IntPtr.Zero)
        {
            try
            {
                _cachedFuncPtr = NativeLibrary.GetExport(_cachedLibHandle, _funcName);
            }
            catch (EntryPointNotFoundException ex)
            {
                throw new Exception($"在 DLL '{_dllName}' 中找不到函数 '{_funcName}': {ex.Message}");
            }
        }

        // 创建委托（如果尚未创建）
        if (_cachedDelegate == null)
        {
            var delegateType = CreateDelegateType(paramTypes, returnType);
            _cachedDelegate = Marshal.GetDelegateForFunctionPointer(_cachedFuncPtr, delegateType);
        }

        // 转换参数类型
        var convertedArgs = ConvertArguments(args, paramTypes);

        // 调用委托
        var result = _cachedDelegate.DynamicInvoke(convertedArgs);
        return result;
    }

    /// <summary>
    /// 解析函数签名字符串
    /// </summary>
    private (Type[] paramTypes, Type returnType) ParseSignature(string signatureStr)
    {
        if (string.IsNullOrEmpty(signatureStr))
        {
            return (Array.Empty<Type>(), typeof(void));
        }

        var parts = signatureStr.Split(':');
        var paramTypesStr = parts[0];
        var returnTypeStr = parts.Length > 1 ? parts[1] : "void";

        var paramTypes = string.IsNullOrEmpty(paramTypesStr)
            ? Array.Empty<Type>()
            : paramTypesStr.Split(',').Select(ConvertOld8TypeToCSharpType).ToArray();

        var returnType = ConvertOld8TypeToCSharpType(returnTypeStr);

        return (paramTypes, returnType);
    }

    /// <summary>
    /// 将 Old8Lang 类型转换为 C# 类型
    /// </summary>
    private Type ConvertOld8TypeToCSharpType(string old8Type)
    {
        return old8Type.ToLower() switch
        {
            "int" => typeof(int),
            "long" => typeof(long),
            "double" => typeof(double),
            "float" => typeof(float),
            "bool" => typeof(bool),
            "string" => typeof(string),
            "void" => typeof(void),
            "object" => typeof(object),
            "char" => typeof(char),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "uint" => typeof(uint),
            "ulong" => typeof(ulong),
            "ushort" => typeof(ushort),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// 创建委托类型
    /// </summary>
    private Type CreateDelegateType(Type[] paramTypes, Type returnType)
    {
        // 使用 Func 或 Action 委托
        if (returnType == typeof(void))
        {
            return paramTypes.Length switch
            {
                0 => typeof(Action),
                1 => typeof(Action<>).MakeGenericType(paramTypes),
                2 => typeof(Action<,>).MakeGenericType(paramTypes),
                3 => typeof(Action<,,>).MakeGenericType(paramTypes),
                4 => typeof(Action<,,,>).MakeGenericType(paramTypes),
                _ => throw new NotSupportedException($"不支持超过 4 个参数的 Action 委托")
            };
        }
        else
        {
            var allTypes = paramTypes.Concat(new[] { returnType }).ToArray();
            return allTypes.Length switch
            {
                1 => typeof(Func<>).MakeGenericType(allTypes),
                2 => typeof(Func<,>).MakeGenericType(allTypes),
                3 => typeof(Func<,,>).MakeGenericType(allTypes),
                4 => typeof(Func<,,,>).MakeGenericType(allTypes),
                5 => typeof(Func<,,,,>).MakeGenericType(allTypes),
                _ => throw new NotSupportedException($"不支持超过 4 个参数的 Func 委托")
            };
        }
    }

    /// <summary>
    /// 转换参数类型
    /// </summary>
    private object?[] ConvertArguments(object?[] args, Type[] targetTypes)
    {
        if (args.Length != targetTypes.Length)
        {
            throw new ArgumentException($"参数数量不匹配: 期望 {targetTypes.Length} 个，实际 {args.Length} 个");
        }

        var convertedArgs = new object?[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            convertedArgs[i] = ConvertArgument(args[i], targetTypes[i]);
        }

        return convertedArgs;
    }

    /// <summary>
    /// 转换单个参数
    /// </summary>
    private object? ConvertArgument(object? arg, Type targetType)
    {
        if (arg == null)
            return null;

        if (targetType.IsAssignableFrom(arg.GetType()))
            return arg;

        // 尝试类型转换
        try
        {
            return Convert.ChangeType(arg, targetType);
        }
        catch
        {
            throw new InvalidCastException($"无法将参数从 {arg.GetType().Name} 转换为 {targetType.Name}");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    ~ExternFunctionWrapper()
    {
        if (_cachedLibHandle != IntPtr.Zero)
        {
            try
            {
                NativeLibrary.Free(_cachedLibHandle);
            }
            catch
            {
                // 忽略释放错误
            }
        }
    }
}
