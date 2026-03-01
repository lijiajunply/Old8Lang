using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Bytecode.ModuleSystem;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using System.Buffers;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.Bytecode.VM;

/// <summary>
/// 虚拟机 - 执行字节码指令
/// </summary>
public partial class VirtualMachine
{
    // 使用 ThreadLocal 为每个线程创建独立的栈和调用栈
    private readonly ThreadLocal<Stack<object?>> _threadStack = new(() => new Stack<object?>());
    private readonly ThreadLocal<Stack<CallFrame>> _threadCallStack = new(() => new Stack<CallFrame>());
    private readonly ThreadLocal<Stack<ExceptionHandler>> _threadExceptionHandlers = new(() => new Stack<ExceptionHandler>());

    // 线程安全的全局变量字典
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, object?> _globals = new();
    private readonly BytecodeFile _bytecodeFile;
    private readonly Dictionary<string, FunctionMetadata> _functionByName = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ClassMetadata> _classByName = new(StringComparer.Ordinal);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(string ModuleName, string SymbolName), object> _moduleSymbolCache = new();

    // 便捷属性，获取当前线程的栈
    private Stack<object?> _stack => _threadStack.Value!;
    private Stack<CallFrame> _callStack => _threadCallStack.Value!;
    private Stack<ExceptionHandler> _exceptionHandlers => _threadExceptionHandlers.Value!;

    // Task 管理
    private readonly Dictionary<int, TaskLangValue> _tasks = new();
    private int _nextTaskId = 1;

    // Generator 管理
    private readonly Dictionary<int, GeneratorState> _generators = new();
    private int _nextGeneratorId = 1;

    // AsyncGenerator 管理
    private readonly Dictionary<int, AsyncGeneratorState> _asyncGenerators = new();
    private int _nextAsyncGeneratorId = 1;

    // 模块系统
    private readonly ModuleRegistry _moduleRegistry = new();
    private readonly ModuleLoader _moduleLoader;
    private readonly string? _baseDirectory;

    public VirtualMachine(BytecodeFile bytecodeFile, string? baseDirectory = null)
    {
        _bytecodeFile = bytecodeFile ?? throw new ArgumentNullException(nameof(bytecodeFile));
        _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
        _moduleLoader = new ModuleLoader(_baseDirectory);

        // 初始化全局函数注册表
        GlobalFunctionInitializer.EnsureInitialized();

        // 初始化实例方法注册表
        InstanceMethodInitializer.EnsureInitialized();

        // 初始化全局变量
        foreach (var globalVar in _bytecodeFile.GlobalVariables)
        {
            _globals[globalVar] = null;
        }

        foreach (var function in _bytecodeFile.Functions)
        {
            _functionByName.TryAdd(function.Name, function);
        }

        // 将所有类元数据注册到全局变量表中
        // 这样在运行时可以通过类名访问类元数据（用于嵌套类访问等）
        foreach (var classMetadata in _bytecodeFile.Classes)
        {
            _globals[classMetadata.Name] = classMetadata;
            _classByName.TryAdd(classMetadata.Name, classMetadata);

            // 初始化静态字段的默认值
            foreach (var staticField in classMetadata.StaticFields)
            {
                // 处理 null 默认值
                if (staticField.IsDefaultNull)
                {
                    classMetadata.StaticFieldValues[staticField.Name] = null;
                }
                // 从常量池获取默认值
                else if (staticField.DefaultValueIndex >= 0 && staticField.DefaultValueIndex < _bytecodeFile.ConstantPool.Count)
                {
                    var defaultValue = _bytecodeFile.ConstantPool.GetConstant(staticField.DefaultValueIndex);
                    classMetadata.StaticFieldValues[staticField.Name] = defaultValue;
                }
                else
                {
                    classMetadata.StaticFieldValues[staticField.Name] = null;
                }
            }
        }

        // 注册扩展方法到实例方法注册表
        RegisterExtensionMethods();
    }

    /// <summary>
    /// 注册扩展方法到实例方法注册表
    /// </summary>
    private void RegisterExtensionMethods()
    {
        foreach (var extension in _bytecodeFile.Extensions)
        {
            // 解析目标类型
            var targetType = ResolveTargetType(extension.TargetTypeName);
            if (targetType == null)
            {
                // 如果无法解析类型，跳过此扩展方法
                continue;
            }

            // 为每个扩展方法创建包装器并注册
            foreach (var method in extension.Methods)
            {
                var extensionMethod = new BytecodeExtensionMethod(
                    targetType,
                    method,
                    this
                );

                InstanceMethodRegistry.Instance.Register(extensionMethod);
            }
        }
    }

    /// <summary>
    /// 解析目标类型名称到 .NET Type
    /// </summary>
    private static Type? ResolveTargetType(string typeName)
    {
        // 内置类型映射到 Old8Lang 的包装类型
        return typeName.ToLower() switch
        {
            "string" => typeof(string),
            "int" => typeof(IntLangValue),
            "double" => typeof(DoubleLangValue),
            "bool" => typeof(BoolLangValue),
            "char" => typeof(CharLangValue),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "decimal" => typeof(decimal),
            "object" => typeof(object),
            "list" => typeof(ListLangValue),
            "array" => typeof(Array),
            "dict" => typeof(DictionaryLangValue),
            _ => Type.GetType(typeName) // 尝试通过完全限定名解析
        };
    }

    /// <summary>
    /// 执行字节码
    /// </summary>
    public void Execute()
    {
        // 设置当前虚拟机上下文
        VMContext.CurrentVM = this;

        try
        {
            // 从入口点开始执行
            if (_bytecodeFile.EntryPointIndex < 0 || _bytecodeFile.EntryPointIndex >= _bytecodeFile.Functions.Count)
            {
                throw new Exception("无效的入口点索引");
            }

            var entryFunction = _bytecodeFile.Functions[_bytecodeFile.EntryPointIndex];
            CallFunction(entryFunction, []);
        }
        finally
        {
            // 清理虚拟机上下文
            VMContext.CurrentVM = null;
        }
    }

    /// <summary>
    /// 获取全局变量的值
    /// </summary>
    public object? GetGlobalVariable(string name)
    {
        return _globals.GetValueOrDefault(name);
    }

    /// <summary>
    /// 获取所有全局变量
    /// </summary>
    public IEnumerable<KeyValuePair<string, object?>> GetAllGlobalVariables()
    {
        return _globals;
    }

    /// <summary>
    /// 从常量池获取常量
    /// </summary>
    public object? GetConstant(int index)
    {
        if (index < 0 || index >= _bytecodeFile.ConstantPool.Count)
        {
            return null;
        }
        return _bytecodeFile.ConstantPool.GetConstant(index);
    }

    private static object?[] RentLocalsBuffer(int localCount)
    {
        if (localCount <= 0)
        {
            return Array.Empty<object?>();
        }

        var buffer = ArrayPool<object?>.Shared.Rent(localCount);
        Array.Clear(buffer, 0, localCount);
        return buffer;
    }

    private static void ReturnLocalsBuffer(CallFrame frame)
    {
        if (!frame.UsesPooledLocals || frame.LocalCount <= 0)
        {
            return;
        }

        Array.Clear(frame.Locals, 0, frame.LocalCount);
        ArrayPool<object?>.Shared.Return(frame.Locals, clearArray: false);
    }

    private void ExecuteFrameLoop(CallFrame frame)
    {
        var function = frame.Function;
        while (frame.IP < function.Instructions.Count)
        {
            var instruction = function.Instructions[frame.IP];
            frame.IP++;

            try
            {
                ExecuteInstruction(instruction, frame);
            }
            catch (Exception ex)
            {
                // 异常发生时，先执行所有 defer 块
                if (frame.HasDeferredInstructions)
                {
                    ExecuteDefers(frame);
                }

                // 异常处理：查找异常表中匹配的处理器
                if (!HandleException(ex, frame, function))
                {
                    // 如果没有找到匹配的处理器，重新抛出异常
                    throw;
                }
            }
        }
    }

    private void ExecuteFrame(CallFrame frame)
    {
        _callStack.Push(frame);
        try
        {
            ExecuteFrameLoop(frame);
        }
        finally
        {
            if (frame.HasDeferredInstructions)
            {
                ExecuteDefers(frame);
            }
            _callStack.Pop();
            ReturnLocalsBuffer(frame);
        }
    }

    /// <summary>
    /// 调用函数
    /// </summary>
    private void CallFunction(FunctionMetadata function, object?[] arguments)
    {
        var processedArguments = NormalizeArguments(function, arguments, new SourcePosition());
        var locals = RentLocalsBuffer(function.LocalCount);

        // 创建调用帧
        var frame = new CallFrame(function, locals, function.LocalCount, usesPooledLocals: function.LocalCount > 0)
        {
            Arguments = processedArguments
        };

        // 将参数复制到局部变量槽(前N个局部变量是参数)
        for (int i = 0; i < processedArguments.Length && i < function.LocalCount; i++)
        {
            frame.Locals[i] = processedArguments[i];
        }

        ExecuteFrame(frame);
    }

    /// <summary>
    /// 公共方法：执行指定的函数（用于扩展方法等场景）
    /// </summary>
    public object? ExecuteFunction(FunctionMetadata function, object?[] arguments)
    {
        // 保存当前栈状态
        var stackSnapshot = _stack.Count;

        try
        {
            // 调用函数
            CallFunction(function, arguments);

            // 如果栈上有返回值，弹出并返回
            if (_stack.Count > stackSnapshot)
            {
                return _stack.Pop();
            }

            return null;
        }
        catch
        {
            // 恢复栈状态
            while (_stack.Count > stackSnapshot)
            {
                _stack.Pop();
            }
            throw;
        }
    }

    /// <summary>
    /// 调用闭包函数（带捕获变量）
    /// </summary>
    private void CallClosureFunction(FunctionMetadata function, object?[] arguments, ClosureEnvironment capturedVariables, ConstantPool? constantPool = null)
    {
        var processedArguments = NormalizeArguments(function, arguments, new SourcePosition());
        var locals = RentLocalsBuffer(function.LocalCount);

        // 创建调用帧，并设置闭包环境和常量池
        var frame = new CallFrame(function, locals, function.LocalCount, usesPooledLocals: function.LocalCount > 0)
        {
            Arguments = processedArguments,
            ClosureEnvironment = capturedVariables,  // 将捕获的变量设置为闭包环境
            ConstantPool = constantPool  // 设置常量池（用于模块导入的函数）
        };

        // 将参数复制到局部变量槽(前N个局部变量是参数)
        for (int i = 0; i < processedArguments.Length && i < function.LocalCount; i++)
        {
            frame.Locals[i] = processedArguments[i];
        }

        ExecuteFrame(frame);
    }

    private object?[] NormalizeArguments(FunctionMetadata function, object?[] arguments, SourcePosition position)
    {
        int paramsIndex = function.ParamsParameterIndex;
        int totalParamCount = function.Parameters.Count;
        if (paramsIndex >= totalParamCount)
        {
            return arguments;
        }

        // 无 params 参数：仅在参数不足时补默认值，多余参数保持向后兼容
        if (paramsIndex < 0)
        {
            if (arguments.Length >= totalParamCount)
            {
                return arguments;
            }

            var normalized = new object?[totalParamCount];
            Array.Copy(arguments, normalized, arguments.Length);
            for (int i = arguments.Length; i < totalParamCount; i++)
            {
                if (i < function.DefaultValues.Count && function.DefaultValues[i] != null)
                {
                    normalized[i] = function.DefaultValues[i];
                }
                else
                {
                    throw new ArgumentError(position, $"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                }
            }

            return normalized;
        }

        int regularParamCount = paramsIndex;
        if (arguments.Length < regularParamCount)
        {
            throw new ArgumentError(position, $"函数 '{function.Name}' 至少需要 {regularParamCount} 个参数，但实际提供了 {arguments.Length} 个参数");
        }

        // 参数数量与声明一致时，视为可能已归一化（兼容旧调用路径）
        if (arguments.Length == totalParamCount)
        {
            return arguments;
        }

        var normalizedArgs = new object?[totalParamCount];
        for (int i = 0; i < regularParamCount; i++)
        {
            normalizedArgs[i] = arguments[i];
        }

        for (int i = regularParamCount; i < totalParamCount; i++)
        {
            if (i == paramsIndex)
            {
                int paramsArgCount = Math.Max(0, arguments.Length - regularParamCount);
                var paramsArray = new object?[paramsArgCount];
                if (paramsArgCount > 0)
                {
                    Array.Copy(arguments, regularParamCount, paramsArray, 0, paramsArgCount);
                }

                normalizedArgs[i] = paramsArray;
                continue;
            }

            if (i < function.DefaultValues.Count && function.DefaultValues[i] != null)
            {
                normalizedArgs[i] = function.DefaultValues[i];
                continue;
            }

            throw new ArgumentError(position, $"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
        }

        return normalizedArgs;
    }

    /// <summary>
    /// 调用函数对象（用于 spawn 等场景）
    /// </summary>
    /// <param name="funcObj">函数对象（ClosureValue 或 FunctionMetadata 或函数索引）</param>
    /// <param name="arguments">函数参数</param>
    /// <returns>函数的返回值（如果有）</returns>
    public object? CallFunctionObject(object? funcObj, object?[] arguments)
    {
        // 保存当前虚拟机上下文（可能已经被设置）
        var previousVM = VMContext.CurrentVM;

        // 设置当前虚拟机上下文（对于新线程或嵌套调用）
        VMContext.CurrentVM = this;

        try
        {
            if (funcObj is ClosureValue closure)
            {
                // 闭包：调用闭包的函数，传递捕获的变量
                CallClosureFunction(closure.Function, arguments, closure.CapturedVariables);
            }
            else if (funcObj is FunctionMetadata function)
            {
                // 函数元数据：直接调用
                CallFunction(function, arguments);
            }
            else if (funcObj is int funcIndex)
            {
                // 函数索引：从字节码文件中获取函数
                if (funcIndex >= 0 && funcIndex < _bytecodeFile.Functions.Count)
                {
                    var func = _bytecodeFile.Functions[funcIndex];
                    CallFunction(func, arguments);
                }
                else
                {
                    throw new Exception($"无效的函数索引: {funcIndex}");
                }
            }
            else
            {
                throw new Exception($"无效的函数对象类型: {funcObj?.GetType().Name ?? "null"}");
            }

            // 返回栈顶的值（如果有）
            if (_stack.Count > 0)
            {
                return _stack.Pop();
            }
            return null;
        }
        finally
        {
            // 恢复之前的虚拟机上下文（而不是清理）
            VMContext.CurrentVM = previousVM;
        }
    }
}
