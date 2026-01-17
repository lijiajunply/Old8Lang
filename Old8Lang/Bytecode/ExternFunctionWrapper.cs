using System.Runtime.InteropServices;
using System.Reflection;
using Old8Lang.AST.Statement;
using Python.Runtime;
using Jint;

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
    private MethodInfo? _cachedMethodInfo;

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
            ExternType.CSharpDll => InvokeCSharpDll(args),
            ExternType.PythonScript => InvokePythonScript(args),
            ExternType.PythonModule => InvokePythonModule(args),
            ExternType.JavaScript => InvokeJavaScript(args),
            _ => throw new NotSupportedException($"不支持的 extern 类型: {_externType}")
        };
    }

    /// <summary>
    /// 调用 C# DLL 函数
    /// </summary>
    private object? InvokeCSharpDll(object?[] args)
    {
        if (_cachedMethodInfo == null)
        {
            // 尝试加载程序集
            Assembly assembly;
            try
            {
                // 尝试作为文件路径加载
                if (File.Exists(_dllName))
                {
                    assembly = Assembly.LoadFrom(_dllName);
                }
                else
                {
                    // 尝试作为程序集名称加载
                    assembly = Assembly.Load(_dllName);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"无法加载程序集 '{_dllName}': {ex.Message}");
            }

            // 查找类型 (假设 _funcName 是 "ClassName.MethodName" 格式? 
            // 不，ExternStatement 分离了 ClassName 和 MethodName 吗？
            // LoadExtern 传入的 funcName 是函数名。
            // 对于 ExternStatement (C#), 我们没有传递 ClassName 到 LoadExtern!
            // 这就是为什么我需要 ImportNative 指令。
            // 但是这里是处理 ExternStatement (func import)。
            // ExternStatement 语法: extern "dll" func Name...
            // 它没有 ClassName! 
            // 除非 DllName 包含了 ClassName? e.g. "Assembly.Namespace.Class"
            // 或者 funcName 是 "Class.Method"?
            
            // 让我们假设 funcName 可能包含类名，或者 DllName 是全限定类名？
            // 按照 Old8Lang 习惯，C# 导入通常用 NativeStatement。
            // 如果使用 ExternStatement 导入 C#，可能需要约定。
            // 暂时假设 _funcName 是 MethodName，而 _dllName 是 AssemblyName。
            // 那么 ClassName 去哪了？
            // 如果无法确定 ClassName，这个方法可能无法工作。
            
            // 重新查看 ExternStatement.cs，它没有 ClassName 字段，只有 DllName。
            // 所以使用 ExternStatement 导入 C# 必须把类名放在 DllName 或 funcName 中。
            // 比如 DllName = "MyAssembly", funcName = "MyClass.MyMethod"
            
            // 尝试解析 funcName 为 "Class.Method"
            string typeName;
            string methodName;
            int lastDot = _funcName.LastIndexOf('.');
            if (lastDot > 0)
            {
                typeName = _funcName.Substring(0, lastDot);
                methodName = _funcName.Substring(lastDot + 1);
            }
            else
            {
                // 无法确定类名，抛出异常或尝试在所有导出类型中查找（太慢）
                throw new Exception($"C# Extern 函数名必须包含类名 (例如 'ClassName.MethodName')，当前为: {_funcName}");
            }
            
            var type = assembly.GetType(typeName) ?? assembly.GetTypes().FirstOrDefault(t => t.Name == typeName || t.FullName == typeName);
            if (type == null)
            {
                 throw new Exception($"在程序集 '{assembly.FullName}' 中找不到类型 '{typeName}'");
            }
            
            _cachedMethodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance); // 暂时只支持静态?
             if (_cachedMethodInfo == null)
            {
                 throw new Exception($"在类型 '{typeName}' 中找不到方法 '{methodName}'");
            }
        }

        // 参数转换
        var parameters = _cachedMethodInfo.GetParameters();
        var convertedArgs = new object?[parameters.Length];
        
        // 处理参数数量不匹配（可能是可选参数）
        int argCount = Math.Min(args.Length, parameters.Length);
        
        for (int i = 0; i < argCount; i++)
        {
            convertedArgs[i] = ConvertArgument(args[i], parameters[i].ParameterType);
        }
        
        // 填充可选参数
        for (int i = argCount; i < parameters.Length; i++)
        {
            if (parameters[i].HasDefaultValue)
            {
                convertedArgs[i] = parameters[i].DefaultValue;
            }
            else
            {
                 throw new ArgumentException($"参数数量不足且无默认值: {parameters[i].Name}");
            }
        }

        return _cachedMethodInfo.Invoke(null, convertedArgs); // 假设是静态方法
    }

    /// <summary>
    /// 调用 Python 脚本函数
    /// </summary>
    private object? InvokePythonScript(object?[] args)
    {
        if (!PythonEngine.IsInitialized)
        {
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
        }

        using (Py.GIL())
        {
            // _dllName 是脚本路径
            // _funcName 是函数名
            
            // 设置 sys.path
            dynamic sys = Py.Import("sys");
            string scriptDir = Path.GetDirectoryName(_dllName) ?? ".";
            sys.path.append(scriptDir);
            
            string scriptName = Path.GetFileNameWithoutExtension(_dllName);
            dynamic module = Py.Import(scriptName);
            dynamic func = module.GetAttr(_funcName);
            
            // 转换参数
            var pyArgs = args.Select(a => a.ToPython()).ToArray();
            
            dynamic result = func.Invoke(pyArgs);
            return result.ToString(); // 简单转换返回值为字符串? 或者需要更复杂的转换
        }
    }
    
    /// <summary>
    /// 调用 Python 模块函数
    /// </summary>
    private object? InvokePythonModule(object?[] args)
    {
         if (!PythonEngine.IsInitialized)
        {
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
        }

        using (Py.GIL())
        {
            // _dllName 格式为 "pymodule:ModuleName"
            string moduleName = _dllName.Substring("pymodule:".Length);
            
            dynamic module = Py.Import(moduleName);
            dynamic func = module.GetAttr(_funcName);
            
            var pyArgs = args.Select(a => a.ToPython()).ToArray();
            
            dynamic result = func.Invoke(pyArgs);
            return result.ToString();
        }
    }

    /// <summary>
    /// 调用 JavaScript 函数
    /// </summary>
    private object? InvokeJavaScript(object?[] args)
    {
        // _dllName 是 JS 文件路径
        string scriptContent = File.ReadAllText(_dllName);
        
        var engine = new Engine();
        engine.Execute(scriptContent);
        
        var jsArgs = args.Select(a => Jint.Native.JsValue.FromObject(engine, a)).ToArray();
        var result = engine.Invoke(_funcName, jsArgs);
        
        return result.ToObject();
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
            return ([], typeof(void));
        }

        var parts = signatureStr.Split(':');
        var paramTypesStr = parts[0];
        var returnTypeStr = parts.Length > 1 ? parts[1] : "void";

        var paramTypes = string.IsNullOrEmpty(paramTypesStr)
            ? []
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
    /// 创建委托类型（使用动态程序集，避免泛型类型）
    /// </summary>
    private Type CreateDelegateType(Type[] paramTypes, Type returnType)
    {
        // 动态创建委托类型
        var assemblyName = new AssemblyName($"ExternDelegate_{Guid.NewGuid():N}");
        var assemblyBuilder = System.Reflection.Emit.AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            System.Reflection.Emit.AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ExternModule");
        var typeBuilder = moduleBuilder.DefineType(
            $"ExternDelegate_{_funcName}",
            TypeAttributes.Public | TypeAttributes.Sealed,
            typeof(MulticastDelegate));

        // 添加 UnmanagedFunctionPointer 特性
        var callingConv = _callingConvention switch
        {
            CallingConventionType.Cdecl => CallingConvention.Cdecl,
            CallingConventionType.StdCall => CallingConvention.StdCall,
            CallingConventionType.WinApi => CallingConvention.Winapi,
            _ => CallingConvention.Cdecl
        };

        var unmanagedAttr = new System.Reflection.Emit.CustomAttributeBuilder(
            typeof(UnmanagedFunctionPointerAttribute)
                .GetConstructor([typeof(CallingConvention)])!,
            [callingConv]);
        typeBuilder.SetCustomAttribute(unmanagedAttr);

        // 定义构造函数
        var ctorBuilder = typeBuilder.DefineConstructor(
            MethodAttributes.RTSpecialName |
            MethodAttributes.HideBySig |
            MethodAttributes.Public,
            CallingConventions.Standard,
            [typeof(object), typeof(IntPtr)]);
        ctorBuilder.SetImplementationFlags(
            MethodImplAttributes.Runtime |
            MethodImplAttributes.Managed);

        // 定义 Invoke 方法
        var invokeBuilder = typeBuilder.DefineMethod(
            "Invoke",
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.NewSlot |
            MethodAttributes.Virtual,
            returnType,
            paramTypes);
        invokeBuilder.SetImplementationFlags(
            MethodImplAttributes.Runtime |
            MethodImplAttributes.Managed);

        return typeBuilder.CreateType()!;
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
