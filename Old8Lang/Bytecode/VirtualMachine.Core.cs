using Old8Lang.AST.Expression.Value;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Bytecode.ModuleSystem;

namespace Old8Lang.Bytecode;

/// <summary>
/// 虚拟机 - 执行字节码指令
/// </summary>
public partial class VirtualMachine
{
    private readonly Stack<object?> _stack = new();
    private readonly Stack<CallFrame> _callStack = new();
    private readonly Dictionary<string, object?> _globals = new();
    private readonly BytecodeFile _bytecodeFile;
    private readonly Stack<ExceptionHandler> _exceptionHandlers = new();

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

        // 初始化全局变量
        foreach (var globalVar in _bytecodeFile.GlobalVariables)
        {
            _globals[globalVar] = null;
        }
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
    /// 调用函数
    /// </summary>
    private void CallFunction(FunctionMetadata function, object?[] arguments)
    {
        // Console.WriteLine($"[VM Debug] Calling {function.Name} with args: {string.Join(", ", arguments.Select(a => a?.ToString() ?? "null"))}");
        // 处理params参数：如果函数有params参数,需要将多余的参数打包成数组
        object?[] processedArguments = arguments;
        if (function.ParamsParameterIndex >= 0)
        {
            processedArguments = ProcessParamsArguments(function, arguments);
        }

        // 创建调用帧
        var frame = new CallFrame(function, function.LocalCount)
        {
            Arguments = processedArguments
        };

        // 将参数复制到局部变量槽(前N个局部变量是参数)
        for (int i = 0; i < processedArguments.Length && i < function.LocalCount; i++)
        {
            frame.Locals[i] = processedArguments[i];
        }

        _callStack.Push(frame);

        try
        {
            // 执行指令
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
                    ExecuteDefers(frame);

                    // 异常处理：查找异常表中匹配的处理器
                    if (!HandleException(ex, frame, function))
                    {
                        // 如果没有找到匹配的处理器，重新抛出异常
                        throw;
                    }
                }
            }
        }
        finally
        {
            // 函数正常退出时，执行所有 defer 块
            ExecuteDefers(frame);
            _callStack.Pop();
        }
    }

    /// <summary>
    /// 调用闭包函数（带捕获变量）
    /// </summary>
    private void CallClosureFunction(FunctionMetadata function, object?[] arguments, Dictionary<string, object?> capturedVariables)
    {
        // 处理params参数：如果函数有params参数,需要将多余的参数打包成数组
        object?[] processedArguments = arguments;
        if (function.ParamsParameterIndex >= 0)
        {
            processedArguments = ProcessParamsArguments(function, arguments);
        }

        // 创建调用帧，并设置闭包环境
        var frame = new CallFrame(function, function.LocalCount)
        {
            Arguments = processedArguments,
            ClosureEnvironment = capturedVariables  // 将捕获的变量设置为闭包环境
        };

        // 将参数复制到局部变量槽(前N个局部变量是参数)
        for (int i = 0; i < processedArguments.Length && i < function.LocalCount; i++)
        {
            frame.Locals[i] = processedArguments[i];
        }

        _callStack.Push(frame);

        try
        {
            // 执行指令
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
                    ExecuteDefers(frame);

                    // 异常处理：查找异常表中匹配的处理器
                    if (!HandleException(ex, frame, function))
                    {
                        // 如果没有找到匹配的处理器，重新抛出异常
                        throw;
                    }
                }
            }
        }
        finally
        {
            // 函数正常退出时，执行所有 defer 块
            ExecuteDefers(frame);
            _callStack.Pop();
        }
    }

    /// <summary>
    /// 处理params参数：将多余的参数打包成数组
    /// </summary>
    private object?[] ProcessParamsArguments(FunctionMetadata function, object?[] arguments)
    {
        int paramsIndex = function.ParamsParameterIndex;
        int regularParamCount = paramsIndex; // params参数之前的普通参数数量
        int totalParamCount = function.Parameters.Count;

        // 如果参数数量已经等于函数参数总数,说明params参数已经被处理过了(可能在OpCode.Call中)
        // 直接返回原参数数组
        if (arguments.Length == totalParamCount)
        {
            return arguments;
        }

        // 检查是否提供了足够的普通参数
        if (arguments.Length < regularParamCount)
        {
            throw new Exception($"函数 '{function.Name}' 至少需要 {regularParamCount} 个参数，但实际提供了 {arguments.Length} 个参数");
        }

        // 创建新的参数数组：普通参数 + params数组
        var processedArgs = new object?[totalParamCount];

        // 复制普通参数
        for (int i = 0; i < regularParamCount; i++)
        {
            processedArgs[i] = arguments[i];
        }

        // 将剩余参数打包成数组
        var paramsArgs = new object?[arguments.Length - regularParamCount];
        for (int i = 0; i < paramsArgs.Length; i++)
        {
            paramsArgs[i] = arguments[regularParamCount + i];
        }

        // 将params数组放入对应位置
        processedArgs[paramsIndex] = paramsArgs;

        return processedArgs;
    }

    /// <summary>
    /// 调用函数对象（用于 spawn 等场景）
    /// </summary>
    /// <param name="funcObj">函数对象（ClosureValue 或 FunctionMetadata 或函数索引）</param>
    /// <param name="arguments">函数参数</param>
    public void CallFunctionObject(object? funcObj, object?[] arguments)
    {
        // 设置当前虚拟机上下文（对于新线程）
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
        }
        finally
        {
            // 清理虚拟机上下文
            VMContext.CurrentVM = null;
        }
    }
}
