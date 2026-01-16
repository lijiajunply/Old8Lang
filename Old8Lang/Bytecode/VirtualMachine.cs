using System.Collections;
using System.Reflection;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Statement;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.Bytecode.ModuleSystem;

namespace Old8Lang.Bytecode;

/// <summary>
/// 虚拟机 - 执行字节码指令
/// </summary>
public class VirtualMachine
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
        // 从入口点开始执行
        if (_bytecodeFile.EntryPointIndex < 0 || _bytecodeFile.EntryPointIndex >= _bytecodeFile.Functions.Count)
        {
            throw new Exception("无效的入口点索引");
        }

        var entryFunction = _bytecodeFile.Functions[_bytecodeFile.EntryPointIndex];
        CallFunction(entryFunction, []);
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
    /// 恢复生成器执行
    /// </summary>
    /// <param name="generatorId">生成器ID</param>
    /// <returns>yield的值，如果生成器已完成则返回null</returns>
    public LangValueType? ResumeGenerator(int generatorId)
    {
        if (!_generators.TryGetValue(generatorId, out var generatorState))
        {
            throw new Exception($"生成器 {generatorId} 不存在");
        }

        // 检查生成器状态
        if (generatorState.Status == GeneratorStatus.Completed)
        {
            return null;
        }

        int ip;
        object?[] locals;
        Stack<LangValueType> stack;

        // 首次执行：初始化参数到局部变量
        if (generatorState.Status == GeneratorStatus.NotStarted)
        {
            ip = 0;
            locals = new object?[generatorState.Function.LocalCount];
            stack = new Stack<LangValueType>();

            // 将参数复制到局部变量（参数占据前N个局部变量槽位）
            if (generatorState.Arguments != null)
            {
                for (int i = 0; i < generatorState.Arguments.Length && i < locals.Length; i++)
                {
                    locals[i] = generatorState.Arguments[i];
                }
            }
        }
        else
        {
            // 恢复执行状态
            generatorState.RestoreState(out ip, out locals, out stack);
        }

        // 创建调用帧
        var frame = new CallFrame(generatorState.Function, generatorState.Function.LocalCount)
        {
            IP = ip,
            GeneratorId = generatorId
        };

        // 设置局部变量
        for (int i = 0; i < locals.Length && i < frame.Locals.Length; i++)
        {
            frame.Locals[i] = locals[i];
        }

        // 恢复栈
        _stack.Clear();
        foreach (var item in stack.Reverse())
        {
            _stack.Push(item);
        }

        _callStack.Push(frame);

        try
        {
            // 继续执行指令直到下一个yield或函数结束
            while (frame.IP < generatorState.Function.Instructions.Count)
            {
                var instruction = generatorState.Function.Instructions[frame.IP];
                frame.IP++;

                try
                {
                    ExecuteInstruction(instruction, frame);

                    // 检查是否遇到了yield（通过检查IP是否被设置到函数末尾）
                    if (frame.IP >= generatorState.Function.Instructions.Count)
                    {
                        // 生成器已暂停或完成
                        if (generatorState.Status == GeneratorStatus.Suspended)
                        {
                            // 返回yield的值
                            return generatorState.CurrentValue;
                        }
                        else
                        {
                            // 生成器已完成
                            generatorState.Complete();
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 异常处理
                    ExecuteDefers(frame);
                    if (!HandleException(ex, frame, generatorState.Function))
                    {
                        throw;
                    }
                }
            }

            // 函数正常结束
            generatorState.Complete();
            return null;
        }
        finally
        {
            ExecuteDefers(frame);
            _callStack.Pop();
        }
    }

    /// <summary>
    /// 异步恢复异步生成器的执行
    /// </summary>
    /// <param name="asyncGeneratorId">异步生成器ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>yield的值，如果生成器已完成则返回null</returns>
    public async Task<LangValueType?> ResumeAsyncGeneratorAsync(int asyncGeneratorId,
        CancellationToken cancellationToken = default)
    {
        if (!_asyncGenerators.TryGetValue(asyncGeneratorId, out var asyncGeneratorState))
        {
            throw new Exception($"异步生成器 {asyncGeneratorId} 不存在");
        }

        // 检查生成器状态
        if (asyncGeneratorState.Status == GeneratorStatus.Completed)
        {
            return null;
        }

        // 检查取消令牌
        cancellationToken.ThrowIfCancellationRequested();

        int ip;
        object?[] locals;
        Stack<LangValueType> stack;

        // 首次执行：初始化参数到局部变量
        if (asyncGeneratorState.Status == GeneratorStatus.NotStarted)
        {
            ip = 0;
            locals = new object?[asyncGeneratorState.Function.LocalCount];
            stack = new Stack<LangValueType>();

            // 将参数复制到局部变量（参数占据前N个局部变量槽位）
            if (asyncGeneratorState.Arguments != null)
            {
                for (int i = 0; i < asyncGeneratorState.Arguments.Length && i < locals.Length; i++)
                {
                    locals[i] = asyncGeneratorState.Arguments[i];
                }
            }
        }
        else
        {
            // 恢复执行状态
            asyncGeneratorState.RestoreState(out ip, out locals, out stack);
        }

        // 创建调用帧
        var frame = new CallFrame(asyncGeneratorState.Function, asyncGeneratorState.Function.LocalCount)
        {
            IP = ip,
            AsyncGeneratorId = asyncGeneratorId
        };

        // 设置局部变量
        for (int i = 0; i < locals.Length && i < frame.Locals.Length; i++)
        {
            frame.Locals[i] = locals[i];
        }

        // 恢复栈
        _stack.Clear();
        foreach (var item in stack.Reverse())
        {
            _stack.Push(item);
        }

        _callStack.Push(frame);

        try
        {
            // 将状态重置为NotStarted,这样当生成器自然完成时可以正确识别
            // (如果执行了yield,状态会被重新设置为Suspended)
            if (asyncGeneratorState.Status == GeneratorStatus.Suspended)
            {
                asyncGeneratorState.Status = GeneratorStatus.NotStarted;
            }

            // 继续执行指令直到下一个yield或函数结束
            while (frame.IP < asyncGeneratorState.Function.Instructions.Count)
            {
                var instruction = asyncGeneratorState.Function.Instructions[frame.IP];
                frame.IP++;

                try
                {
                    // 异步执行指令
                    await ExecuteInstructionAsync(instruction, frame, cancellationToken);

                    // 检查是否遇到了yield（IP被设置到函数末尾）
                    if (frame.IP >= asyncGeneratorState.Function.Instructions.Count)
                    {
                        // 检查是否是yield（状态为Suspended）还是函数结束
                        if (asyncGeneratorState.Status == GeneratorStatus.Suspended)
                        {
                            // 刚刚执行了yield，返回值
                            return asyncGeneratorState.CurrentValue;
                        }
                        else
                        {
                            // 函数正常结束
                            asyncGeneratorState.Complete();
                            return null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 异常处理
                    ExecuteDefers(frame);
                    if (!HandleException(ex, frame, asyncGeneratorState.Function))
                    {
                        throw;
                    }
                }
            }

            // 函数正常结束
            asyncGeneratorState.Complete();
            return null;
        }
        finally
        {
            ExecuteDefers(frame);
            _callStack.Pop();
        }
    }

    /// <summary>
    /// 异步执行单条指令（用于异步生成器）
    /// </summary>
    private async Task ExecuteInstructionAsync(Instruction instruction, CallFrame frame,
        CancellationToken cancellationToken)
    {
        // 检查取消令牌
        cancellationToken.ThrowIfCancellationRequested();

        // 对于大多数指令，直接调用同步版本
        // 只有异步相关的指令需要特殊处理
        switch (instruction.OpCode)
        {
            case OpCode.Await:
            {
                // 从栈中弹出 TaskLangValue (或 Task ID)
                var value = _stack.Pop();

                TaskLangValue taskLangValue;
                if (value is TaskLangValue t)
                {
                    taskLangValue = t;
                }
                else if (value is int taskId)
                {
                    taskLangValue = GetTask(taskId);
                }
                else
                {
                    throw new Exception(
                        $"Await 指令期望 TaskLangValue 或 Task ID (int)，但得到 {value?.GetType().Name ?? "null"}");
                }

                // 异步等待 Task 完成
                var result = await taskLangValue.AwaitAsync(cancellationToken);

                // 将结果压入栈
                _stack.Push(result);
            }
                break;

            case OpCode.AwaitYield:
            {
                // 异步生成器的 yield 操作
                // 从栈中弹出要 yield 的值
                var yieldValue = _stack.Pop();

                // 获取当前异步生成器状态
                if (frame.AsyncGeneratorId.HasValue)
                {
                    var asyncGeneratorId = frame.AsyncGeneratorId.Value;
                    if (_asyncGenerators.TryGetValue(asyncGeneratorId, out var asyncGeneratorState))
                    {
                        // 保存当前执行状态
                        var stackArray = _stack.ToArray();
                        var stackCopy = new Stack<LangValueType>();
                        foreach (var item in stackArray.Reverse())
                        {
                            if (item is LangValueType langValue)
                                stackCopy.Push(langValue);
                        }

                        // 保存状态并标记为暂停
                        asyncGeneratorState.SaveState(frame.IP, frame.Locals, stackCopy);

                        // 将 yield 的值转换为 LangValueType
                        asyncGeneratorState.CurrentValue = yieldValue as LangValueType ??
                                                           ConvertToLangValue(yieldValue);

                        asyncGeneratorState.Status = GeneratorStatus.Suspended;

                        // 设置 IP 到函数末尾，触发返回
                        frame.IP = asyncGeneratorState.Function.Instructions.Count;
                    }
                }
            }
                break;

            default:
                // 其他指令使用同步版本
                ExecuteInstruction(instruction, frame);
                break;
        }
    }

    /// <summary>
    /// 执行单条指令
    /// </summary>
    private void ExecuteInstruction(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
            // === 栈操作 ===
            case OpCode.Nop:
                // 无操作
                break;

            case OpCode.LoadConst:
            {
                int constIndex = (int)instruction.Operand!;
                var constant = _bytecodeFile.ConstantPool.GetConstant(constIndex);
                _stack.Push(constant);
            }
                break;

            case OpCode.LoadLocal:
            {
                int localIndex = (int)instruction.Operand!;
                _stack.Push(frame.Locals[localIndex]);
            }
                break;

            case OpCode.StoreLocal:
            {
                int localIndex = (int)instruction.Operand!;
                frame.Locals[localIndex] = _stack.Pop();
            }
                break;

            case OpCode.LoadGlobal:
            {
                string varName = (string)instruction.Operand!;
                if (!_globals.TryGetValue(varName, out var value))
                {
                    throw new Exception($"未定义的全局变量: {varName}");
                }

                _stack.Push(value);
            }
                break;

            case OpCode.StoreGlobal:
            {
                string varName = (string)instruction.Operand!;
                _globals[varName] = _stack.Pop();
            }
                break;

            case OpCode.Pop:
                _stack.Pop();
                break;

            case OpCode.Dup:
                _stack.Push(_stack.Peek());
                break;

            case OpCode.LoadNull:
                _stack.Push(null);
                break;

            case OpCode.LoadTrue:
                _stack.Push(true);
                break;

            case OpCode.LoadFalse:
                _stack.Push(false);
                break;

            // === 算术运算 ===
            case OpCode.Add:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Add(a, b));
            }
                break;

            case OpCode.Sub:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Sub(a, b));
            }
                break;

            case OpCode.Mul:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Mul(a, b));
            }
                break;

            case OpCode.Div:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Div(a, b));
            }
                break;

            case OpCode.Mod:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Mod(a, b));
            }
                break;

            case OpCode.Pow:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Pow(a, b));
            }
                break;

            case OpCode.Neg:
            {
                var a = _stack.Pop();
                _stack.Push(Neg(a));
            }
                break;

            // === 比较运算 ===
            case OpCode.Equal:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Equals(a, b));
            }
                break;

            case OpCode.NotEqual:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(!Equals(a, b));
            }
                break;

            case OpCode.Greater:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Greater(a, b));
            }
                break;

            case OpCode.Less:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(Less(a, b));
            }
                break;

            case OpCode.GreaterEqual:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(GreaterEqual(a, b));
            }
                break;

            case OpCode.LessEqual:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(LessEqual(a, b));
            }
                break;

            // === 逻辑运算 ===
            case OpCode.And:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(ToBool(a) && ToBool(b));
            }
                break;

            case OpCode.Or:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                _stack.Push(ToBool(a) || ToBool(b));
            }
                break;

            case OpCode.Not:
            {
                var a = _stack.Pop();
                _stack.Push(!ToBool(a));
            }
                break;

            // === 控制流 ===
            case OpCode.Jump:
            {
                int targetIP = (int)instruction.Operand!;
                frame.IP = targetIP;
            }
                break;

            case OpCode.JumpIfFalse:
            {
                int targetIP = (int)instruction.Operand!;
                var condition = _stack.Pop();
                if (!ToBool(condition))
                {
                    frame.IP = targetIP;
                }
            }
                break;

            case OpCode.JumpIfTrue:
            {
                int targetIP = (int)instruction.Operand!;
                var condition = _stack.Pop();
                if (ToBool(condition))
                {
                    frame.IP = targetIP;
                }
            }
                break;

            case OpCode.Call:
            {
                var operands = (object[])instruction.Operand!;

                // 检查操作数格式
                if (operands.Length == 2)
                {
                    // 无命名参数: [argCount, funcName]
                    int argCount = (int)operands[0];
                    string funcName = (string)operands[1];

                    // 从栈中弹出参数
                    var args = new object?[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        args[i] = _stack.Pop();
                    }

                    // 首先检查是否是 extern 函数（从全局变量中查找）
                    if (_globals.TryGetValue(funcName, out var externFuncObj) &&
                        externFuncObj is ExternFunctionWrapper externFunc)
                    {
                        // 调用 extern 函数
                        var result = externFunc.Invoke(args);
                        _stack.Push(result);
                        break;
                    }

                    // 首先检查全局变量中是否有该函数（可能是从模块导入的）
                    FunctionMetadata? function = null;
                    if (_globals.TryGetValue(funcName, out var funcObj) && funcObj is FunctionMetadata funcMeta)
                    {
                        function = funcMeta;
                    }

                    // 如果全局变量中没有，从当前字节码文件中查找
                    if (function == null)
                    {
                        function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);
                    }

                    // 如果还没找到，从所有已加载模块的导出符号中查找
                    if (function == null)
                    {
                        foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                        {
                            try
                            {
                                var symbol = _moduleRegistry.GetModuleSymbol(loadedModuleName, funcName);
                                if (symbol is FunctionMetadata moduleFuncMeta)
                                {
                                    function = moduleFuncMeta;
                                    break;
                                }
                            }
                            catch
                            {
                                // 模块中没有该符号，继续查找
                            }
                        }
                    }

                    // 如果还没找到函数，检查是否是导入的类（类实例化）
                    if (function == null)
                    {
                        // 检查全局变量中是否有该类
                        ClassMetadata? classMetadata = null;
                        if (_globals.TryGetValue(funcName, out var globalClass) &&
                            globalClass is ClassMetadata importedClass)
                        {
                            classMetadata = importedClass;
                        }

                        // 从已加载模块中查找类
                        if (classMetadata == null)
                        {
                            foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                            {
                                try
                                {
                                    var symbol = _moduleRegistry.GetModuleSymbol(loadedModuleName, funcName);
                                    if (symbol is ClassMetadata moduleClass)
                                    {
                                        classMetadata = moduleClass;
                                        break;
                                    }
                                }
                                catch
                                {
                                    // 模块中没有该符号，继续查找
                                }
                            }
                        }

                        // 如果找到类，执行类实例化
                        if (classMetadata != null)
                        {
                            // 创建对象实例
                            var obj = CreateObjectInstance(classMetadata, args);
                            _stack.Push(obj);
                            break;
                        }

                        throw new Exception($"未定义的函数: {funcName}");
                    }

                    // 检查参数数量，如果不足则使用默认值补全
                    if (argCount < function.Parameters.Count)
                    {
                        var fullArgs = new object?[function.Parameters.Count];
                        Array.Copy(args, fullArgs, argCount);

                        // 使用默认值填充剩余参数
                        for (int i = argCount; i < function.Parameters.Count; i++)
                        {
                            if (i < function.DefaultValues.Count && function.DefaultValues[i] != null)
                            {
                                fullArgs[i] = function.DefaultValues[i];
                            }
                            else if (function.ParamsParameterIndex == i)
                            {
                                // 如果是params参数，创建空数组
                                fullArgs[i] = new object?[0];
                            }
                            else
                            {
                                throw new Exception($"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                            }
                        }

                        args = fullArgs;
                    }

                    // 检查是否是生成器函数
                    if (function.IsGenerator)
                    {
                        // 检查是否是异步生成器
                        if (function.IsAsync)
                        {
                            // 创建异步生成器状态
                            var asyncGeneratorId = _nextAsyncGeneratorId++;
                            var asyncGeneratorState = new AsyncGeneratorState(function, args);
                            _asyncGenerators[asyncGeneratorId] = asyncGeneratorState;

                            // 创建异步生成器对象并压入栈
                            var asyncGeneratorValue = new BytecodeAsyncGeneratorLangValue(asyncGeneratorId, this);
                            _stack.Push(asyncGeneratorValue);
                        }
                        else
                        {
                            // 创建普通生成器状态
                            var generatorId = _nextGeneratorId++;
                            var generatorState = new GeneratorState(function, args);
                            _generators[generatorId] = generatorState;

                            // 创建生成器对象并压入栈
                            var generatorValue = new BytecodeGeneratorLangValue(generatorId, this);
                            _stack.Push(generatorValue);
                        }
                    }
                    else
                    {
                        // 调用普通函数
                        CallFunction(function, args);
                    }
                }
                else
                {
                    // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                    int positionalCount = (int)operands[0];
                    int namedCount = (int)operands[1];
                    string funcName = (string)operands[2];
                    string[] namedArgNames = (string[])operands[3];

                    // 从栈中弹出参数 (命名参数值在栈顶,位置参数在下面)
                    // 注意: 栈是后进先出,所以先弹出的是最后压入的
                    var namedArgValues = new object?[namedCount];
                    for (int i = namedCount - 1; i >= 0; i--)
                    {
                        namedArgValues[i] = _stack.Pop();
                    }

                    var positionalArgs = new object?[positionalCount];
                    for (int i = positionalCount - 1; i >= 0; i--)
                    {
                        positionalArgs[i] = _stack.Pop();
                    }

                    // 查找函数
                    FunctionMetadata? function = null;

                    // 首先检查全局变量中是否有该函数（可能是从模块导入的）
                    if (_globals.TryGetValue(funcName, out var funcObj) && funcObj is FunctionMetadata funcMeta)
                    {
                        function = funcMeta;
                    }

                    // 如果全局变量中没有，从当前字节码文件中查找
                    if (function == null)
                    {
                        function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);
                    }

                    // 如果还没找到，从所有已加载模块的导出符号中查找
                    if (function == null)
                    {
                        foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                        {
                            try
                            {
                                var symbol = _moduleRegistry.GetModuleSymbol(loadedModuleName, funcName);
                                if (symbol is FunctionMetadata moduleFuncMeta)
                                {
                                    function = moduleFuncMeta;
                                    break;
                                }
                            }
                            catch
                            {
                                // 模块中没有该符号，继续查找
                            }
                        }
                    }

                    if (function == null)
                    {
                        throw new Exception($"未定义的函数: {funcName}");
                    }

                    // 重新排列参数以匹配函数参数定义
                    var args = ArrangeArgumentsWithNamed(function, positionalArgs, namedArgNames, namedArgValues);

                    // 检查是否是生成器函数
                    if (function.IsGenerator)
                    {
                        // 检查是否是异步生成器
                        if (function.IsAsync)
                        {
                            // 创建异步生成器状态
                            var asyncGeneratorId = _nextAsyncGeneratorId++;
                            var asyncGeneratorState = new AsyncGeneratorState(function, args);
                            _asyncGenerators[asyncGeneratorId] = asyncGeneratorState;

                            // 创建异步生成器对象并压入栈
                            var asyncGeneratorValue = new BytecodeAsyncGeneratorLangValue(asyncGeneratorId, this);
                            _stack.Push(asyncGeneratorValue);
                        }
                        else
                        {
                            // 创建普通生成器状态
                            var generatorId = _nextGeneratorId++;
                            var generatorState = new GeneratorState(function, args);
                            _generators[generatorId] = generatorState;

                            // 创建生成器对象并压入栈
                            var generatorValue = new BytecodeGeneratorLangValue(generatorId, this);
                            _stack.Push(generatorValue);
                        }
                    }
                    else
                    {
                        // 调用普通函数
                        CallFunction(function, args);
                    }
                }

                // 如果有返回值,它应该在栈上
            }
                break;


            case OpCode.CallNative:
            {
                var operands = (object[])instruction.Operand!;

                // 检查操作数格式
                if (operands.Length == 2)
                {
                    // 无命名参数: [argCount, funcName]
                    int argCount = (int)operands[0];
                    string funcName = (string)operands[1];

                    // 从栈中弹出参数
                    var args = new object?[argCount];
                    for (int i = argCount - 1; i >= 0; i--)
                    {
                        args[i] = _stack.Pop();
                    }

                    // 调用原生函数
                    var result = CallNativeFunction(funcName, args);
                    if (result != null)
                    {
                        _stack.Push(result);
                    }
                }
                else
                {
                    // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                    int positionalCount = (int)operands[0];
                    int namedCount = (int)operands[1];
                    string funcName = (string)operands[2];
                    string[] namedArgNames = (string[])operands[3];

                    // 从栈中弹出参数 (位置参数 + 命名参数值)
                    var positionalArgs = new object?[positionalCount];
                    for (int i = positionalCount - 1; i >= 0; i--)
                    {
                        positionalArgs[i] = _stack.Pop();
                    }

                    var namedArgValues = new object?[namedCount];
                    for (int i = namedCount - 1; i >= 0; i--)
                    {
                        namedArgValues[i] = _stack.Pop();
                    }

                    // 原生函数暂时不支持命名参数重排,直接按顺序传递
                    // TODO: 如果需要支持原生函数的命名参数,需要获取原生函数的参数信息
                    var args = new object?[positionalCount + namedCount];
                    Array.Copy(positionalArgs, 0, args, 0, positionalCount);
                    Array.Copy(namedArgValues, 0, args, positionalCount, namedCount);

                    // 调用原生函数
                    var result = CallNativeFunction(funcName, args);
                    if (result != null)
                    {
                        _stack.Push(result);
                    }
                }
            }
                break;

            case OpCode.CallDynamic:
            {
                int argCount = (int)instruction.Operand!;

                // 从栈中弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 弹出函数对象
                var funcObj = _stack.Pop();

                if (funcObj is FunctionMetadata funcMeta)
                {
                    // 调用函数
                    if (funcMeta.IsGenerator)
                    {
                        // 复制生成器逻辑
                        if (funcMeta.IsAsync)
                        {
                            var asyncGeneratorId = _nextAsyncGeneratorId++;
                            var asyncGeneratorState = new AsyncGeneratorState(funcMeta, args);
                            _asyncGenerators[asyncGeneratorId] = asyncGeneratorState;
                            var asyncGeneratorValue = new BytecodeAsyncGeneratorLangValue(asyncGeneratorId, this);
                            _stack.Push(asyncGeneratorValue);
                        }
                        else
                        {
                            var generatorId = _nextGeneratorId++;
                            var generatorState = new GeneratorState(funcMeta, args);
                            _generators[generatorId] = generatorState;
                            var generatorValue = new BytecodeGeneratorLangValue(generatorId, this);
                            _stack.Push(generatorValue);
                        }
                    }
                    else
                    {
                        CallFunction(funcMeta, args);
                    }
                }
                else
                {
                    throw new Exception($"尝试调用非函数对象: {funcObj?.GetType().Name}");
                }
            }
                break;

            case OpCode.Return:
            {
                // 返回值应该已经在栈上
                // 调用者会从栈中获取返回值
                return; // 退出当前函数
            }

            case OpCode.ReturnVoid:
                return; // 退出当前函数

            case OpCode.Break:
                // Break指令在字节码生成阶段已经被转换为Jump指令
                // 这里不应该被执行到
                throw new Exception("Break指令不应该在运行时被执行");

            case OpCode.Continue:
                // Continue指令在字节码生成阶段已经被转换为Jump指令
                // 这里不应该被执行到
                throw new Exception("Continue指令不应该在运行时被执行");

            case OpCode.MakeFunction:
            {
                int funcIndex = (int)instruction.Operand!;
                // Get function metadata from bytecode file
                if (funcIndex >= 0 && funcIndex < _bytecodeFile.Functions.Count)
                {
                    var funcMeta = _bytecodeFile.Functions[funcIndex];
                    // TODO: Wrap in Closure if needed for capturing variables
                    _stack.Push(funcMeta);
                }
                else
                {
                    throw new Exception($"无效的函数索引: {funcIndex}");
                }
            }
                break;

            // === 容器操作 ===
            case OpCode.NewArray:
            {
                int count = (int)instruction.Operand!;
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = _stack.Pop();
                }

                _stack.Push(elements);
            }
                break;

            case OpCode.NewList:
            {
                int count = (int)instruction.Operand!;
                var list = new List<object?>();
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = _stack.Pop();
                }

                list.AddRange(elements);
                _stack.Push(list);
            }
                break;

            case OpCode.NewTuple:
            {
                int count = (int)instruction.Operand!;
                var elements = new object?[count];
                for (int i = count - 1; i >= 0; i--)
                {
                    elements[i] = _stack.Pop();
                }

                if (count == 0)
                {
                    _stack.Push(new Tuple<object?, object?>(null, null));
                }
                else if (count == 1)
                {
                    _stack.Push(new Tuple<object?, object?>(elements[0], null));
                }
                else if (count == 2)
                {
                    _stack.Push(new Tuple<object?, object?>(elements[0], elements[1]));
                }
                else
                {
                    // 构建嵌套元组: (1, 2, 3) -> (1, (2, 3))
                    // 从后往前构建
                    object? current = new Tuple<object?, object?>(elements[count - 2], elements[count - 1]);

                    for (int i = count - 3; i >= 0; i--)
                    {
                        current = new Tuple<object?, object?>(elements[i], current);
                    }

                    _stack.Push(current);
                }
            }
                break;

            case OpCode.NewDict:
            {
                int pairCount = (int)instruction.Operand!;
                var dict = new Dictionary<object, object?>();
                // 每个键值对作为一个元组在栈上
                for (int i = 0; i < pairCount; i++)
                {
                    if (_stack.Pop() is Tuple<object?, object?> { Item1: not null } tuple)
                    {
                        dict[tuple.Item1] = tuple.Item2;
                    }
                }

                _stack.Push(dict);
            }
                break;

            case OpCode.ArrayLength:
            {
                var collection = _stack.Pop();
                int length = 0;

                if (collection is Array array)
                {
                    length = array.Length;
                }
                else if (collection is ICollection<object?> list)
                {
                    length = list.Count;
                }
                else if (collection is ICollection col)
                {
                    length = col.Count;
                }
                else if (collection is string str)
                {
                    length = str.Length;
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    // 递归计算嵌套元组的长度
                    length = 0;
                    var traverseStack = new Stack<object?>();
                    traverseStack.Push(tuple);

                    while (traverseStack.Count > 0)
                    {
                        var current = traverseStack.Pop();
                        if (current is Tuple<object?, object?> t)
                        {
                            traverseStack.Push(t.Item2);
                            traverseStack.Push(t.Item1);
                        }
                        else if (current != null)
                        {
                            length++;
                        }
                    }
                }
                else
                {
                    throw new Exception($"无法获取类型 {collection?.GetType().Name} 的长度");
                }

                _stack.Push(length);
            }
                break;

            case OpCode.GetIndex:
            {
                // 栈顶: index, collection
                var index = _stack.Pop();
                var collection = _stack.Pop();

                if (collection is Array array)
                {
                    int idx = Convert.ToInt32(index);
                    _stack.Push(array.GetValue(idx));
                }
                else if (collection is IList list)
                {
                    int idx = Convert.ToInt32(index);
                    _stack.Push(list[idx]);
                }
                else if (collection is IDictionary dict)
                {
                    _stack.Push(dict[index]);
                }
                else if (collection is string str)
                {
                    int idx = Convert.ToInt32(index);
                    _stack.Push(str[idx]);
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    int idx = Convert.ToInt32(index);
                    int currentIdx = 0;
                    bool found = false;

                    // 使用栈进行迭代遍历，确保能处理任意嵌套结构的元组
                    var traverseStack = new Stack<object?>();
                    traverseStack.Push(tuple);

                    while (traverseStack.Count > 0)
                    {
                        var current = traverseStack.Pop();
                        if (current is Tuple<object?, object?> t)
                        {
                            // 保持顺序：先处理 Item1，再处理 Item2
                            // 栈是后进先出，所以先压入 Item2，再压入 Item1
                            traverseStack.Push(t.Item2);
                            traverseStack.Push(t.Item1);
                        }
                        else if (current != null) // 跳过 null (与 TupleLangValue 行为一致)
                        {
                            if (currentIdx == idx)
                            {
                                _stack.Push(current);
                                found = true;
                                break;
                            }

                            currentIdx++;
                        }
                    }

                    if (!found)
                    {
                        throw new Exception($"元组索引越界: {idx}");
                    }
                }
                else
                {
                    throw new Exception($"无法对类型 {collection?.GetType().Name} 执行索引访问");
                }
            }
                break;

            case OpCode.SetIndex:
            {
                // 栈顶: value, index, collection
                var value = _stack.Pop();
                var index = _stack.Pop();
                var collection = _stack.Pop();

                if (collection is Array array)
                {
                    int idx = Convert.ToInt32(index);
                    array.SetValue(value, idx);
                }
                else if (collection is IList list)
                {
                    int idx = Convert.ToInt32(index);
                    list[idx] = value;
                }
                else if (collection is IDictionary dict)
                {
                    dict[index] = value;
                }
                else
                {
                    throw new Exception($"无法对类型 {collection?.GetType().Name} 执行索引赋值");
                }
            }
                break;

            case OpCode.NewRange:
            {
                // 栈顶: includeEnd, includeStart, end, start
                var includeEndObj = _stack.Pop();
                var includeStartObj = _stack.Pop();
                var endObj = _stack.Pop();
                var startObj = _stack.Pop();

                int start = Convert.ToInt32(startObj);
                int end = Convert.ToInt32(endObj);
                bool includeStart = Convert.ToBoolean(includeStartObj);
                bool includeEnd = Convert.ToBoolean(includeEndObj);

                var results = new List<int>();

                // 根据包含规则调整起始值
                var startNum = start;
                var endNum = end;

                if (!includeStart)
                    startNum++;
                if (!includeEnd)
                    endNum--;

                // 检查范围是否有效
                // 如果start原本就大于end,说明是反向范围
                if (start > end)
                {
                    // 反向范围:从大到小
                    for (var i = startNum; i >= endNum; i--)
                    {
                        results.Add(i);
                    }
                }
                else if (startNum <= endNum)
                {
                    // 正向范围:从小到大
                    for (var i = startNum; i <= endNum; i++)
                    {
                        results.Add(i);
                    }
                }
                // 如果调整后startNum > endNum但原本start <= end,说明排除导致范围为空,返回空数组

                _stack.Push(results.ToArray());
            }
                break;

            // === 迭代器操作 ===
            case OpCode.GetIterator:
            {
                var collection = _stack.Pop();
                if (collection is IEnumerable enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    _stack.Push(enumerator);
                }
                else
                {
                    throw new Exception($"对象类型 {collection?.GetType().Name} 不可迭代");
                }
            }
                break;

            case OpCode.IteratorMoveNext:
            {
                // 栈顶应该是迭代器，调用MoveNext后将bool结果压入栈
                // 注意：迭代器对象保持在栈上，以便后续的IteratorCurrent使用
                if (_stack.Peek() is IEnumerator enumerator)
                {
                    bool hasNext = enumerator.MoveNext();
                    _stack.Push(hasNext);
                }
                else
                {
                    var top = _stack.Count > 0 ? _stack.Peek() : null;
                    var topType = top?.GetType().FullName ?? "null";
                    throw new Exception($"栈顶不是迭代器对象，而是: {topType}");
                }
            }
                break;

            case OpCode.IteratorCurrent:
            {
                // 栈顶应该是迭代器
                if (_stack.Count == 0)
                {
                    throw new Exception("IteratorCurrent: 栈为空");
                }

                var top = _stack.Peek();
                if (top is IEnumerator enumerator)
                {
                    _stack.Push(enumerator.Current);
                }
                else
                {
                    // 调试：输出栈的详细信息
                    var stackContents = string.Join(", ", _stack.Select(x => x?.GetType().Name ?? "null"));
                    var topType = top?.GetType().FullName ?? "null";
                    throw new Exception($"IteratorCurrent 失败: 栈顶类型是 {topType}, 栈内容({_stack.Count}): [{stackContents}]");
                }
            }
                break;

            case OpCode.Slice:
            {
                // 栈顶: step, end, start, collection
                var step = _stack.Pop();
                var end = _stack.Pop();
                var start = _stack.Pop();
                var collection = _stack.Pop();

                int startIdx = Convert.ToInt32(start);
                int endIdx = end != null ? Convert.ToInt32(end) : int.MaxValue;
                int stepVal = step != null ? Convert.ToInt32(step) : 1;

                if (collection is Array array)
                {
                    var result = SliceArray(array, startIdx, endIdx, stepVal);
                    _stack.Push(result);
                }
                else if (collection is IList list)
                {
                    var result = SliceList(list, startIdx, endIdx, stepVal);
                    _stack.Push(result);
                }
                else if (collection is string str)
                {
                    var result = SliceString(str, startIdx, endIdx, stepVal);
                    _stack.Push(result);
                }
                else if (collection is Tuple<object?, object?> tuple)
                {
                    // 1. 展平 Tuple 为 List
                    var tupleAsList = new List<object?>();
                    var traverseStack = new Stack<object?>();
                    traverseStack.Push(tuple);

                    while (traverseStack.Count > 0)
                    {
                        var current = traverseStack.Pop();
                        if (current is Tuple<object?, object?> t)
                        {
                            traverseStack.Push(t.Item2);
                            traverseStack.Push(t.Item1);
                        }
                        else if (current != null)
                        {
                            tupleAsList.Add(current);
                        }
                    }

                    // 2. 对 List 进行切片
                    var sliceResult = SliceList(tupleAsList, startIdx, endIdx, stepVal);
                    var slicedList = sliceResult as List<object?>;

                    if (slicedList == null)
                    {
                        slicedList = new List<object?>();
                        if (sliceResult is System.Collections.IEnumerable enumerable)
                        {
                            foreach (var item in enumerable)
                            {
                                slicedList.Add(item);
                            }
                        }
                    }

                    // 3. 将切片后的 List 重新构建为 Tuple
                    object? resultTuple;

                    if (slicedList.Count == 0)
                    {
                        resultTuple = new Tuple<object?, object?>(null, null);
                    }
                    else if (slicedList.Count == 1)
                    {
                        resultTuple = new Tuple<object?, object?>(slicedList[0], null);
                    }
                    else if (slicedList.Count == 2)
                    {
                        resultTuple = new Tuple<object?, object?>(slicedList[0], slicedList[1]);
                    }
                    else
                    {
                        // 构建嵌套元组: (1, 2, 3) -> (1, (2, 3))
                        // 从后往前构建
                        object? current = new Tuple<object?, object?>(slicedList[slicedList.Count - 2],
                            slicedList[slicedList.Count - 1]);

                        for (int i = slicedList.Count - 3; i >= 0; i--)
                        {
                            current = new Tuple<object?, object?>(slicedList[i], current);
                        }

                        resultTuple = current;
                    }

                    _stack.Push(resultTuple);
                }
                else
                {
                    throw new Exception($"无法对类型 {collection?.GetType().Name} 执行切片操作");
                }
            }
                break;

            case OpCode.NewGroupDict:
            {
                // 创建一个分组字典 Dictionary<object, List<object>>
                var groupDict = new Dictionary<object, List<object?>>(new ObjectEqualityComparer());
                _stack.Push(groupDict);
            }
                break;

            case OpCode.AddToGroup:
            {
                // 栈顶: element, key, groupDict
                var element = _stack.Pop();
                var key = _stack.Pop();
                var groupDict = _stack.Pop() as Dictionary<object, List<object?>>;

                if (groupDict == null)
                {
                    throw new Exception("AddToGroup 操作需要一个分组字典");
                }

                // 如果键不存在,创建新的列表
                if (!groupDict.ContainsKey(key!))
                {
                    groupDict[key!] = new List<object?>();
                }

                // 将元素添加到对应键的列表中
                groupDict[key!].Add(element);

                // 注意: 不需要将字典重新压栈,因为字典是引用类型,修改会直接反映到原对象
            }
                break;

            case OpCode.GroupDictToList:
            {
                // 将分组字典转换为分组列表
                // 每个分组是一个包含 Key 和 Values 的对象
                var groupDict = _stack.Pop() as Dictionary<object, List<object?>>;

                if (groupDict == null)
                {
                    throw new Exception("GroupDictToList 操作需要一个分组字典");
                }

                var resultList = new List<object?>();

                foreach (var kvp in groupDict)
                {
                    // 创建一个分组对象,包含 Key 和 Values
                    var group = new Dictionary<string, object?>
                    {
                        ["Key"] = kvp.Key,
                        ["Values"] = kvp.Value
                    };
                    resultList.Add(group);
                }

                _stack.Push(resultList);
            }
                break;

            // === 类型操作 ===
            case OpCode.Cast:
            {
                var targetTypeName = (string)instruction.Operand!;
                var value = _stack.Pop();

                // 1. 如果类型完全匹配（包括泛型检查），直接返回原值
                if (CheckTypeMatch(targetTypeName, value))
                {
                    _stack.Push(value);
                    break;
                }

                try
                {
                    // 执行类型转换
                    object? convertedValue = targetTypeName.ToLower() switch
                    {
                        "int" => value == null
                            ? throw new InvalidCastException("Cannot cast null to int")
                            : Convert.ToInt32(value),
                        "double" => value == null
                            ? throw new InvalidCastException("Cannot cast null to double")
                            : Convert.ToDouble(value),
                        "string" => value?.ToString() ?? "",
                        "bool" => Convert.ToBoolean(value),
                        "char" => Convert.ToChar(value),
                        "list" => ConvertToList(value),
                        "array" => ConvertToArray(value),
                        "dict" => ConvertToDict(value),
                        _ => value // 其他类型直接返回原值
                    };
                    _stack.Push(convertedValue);
                }
                catch (Exception ex)
                {
                    throw new Exception($"类型转换失败: 无法将 {value?.GetType().Name ?? "null"} 转换为 {targetTypeName}", ex);
                }
            }
                break;

            case OpCode.IsType:
            {
                var targetTypeName = (string)instruction.Operand!;
                var value = _stack.Pop();
                _stack.Push(CheckTypeMatch(targetTypeName, value));
            }
                break;

            case OpCode.TypeOf:
            {
                var value = _stack.Pop();
                string typeName;

                if (value == null)
                {
                    typeName = "null";
                }
                else if (value is int)
                {
                    typeName = "int";
                }
                else if (value is double)
                {
                    typeName = "double";
                }
                else if (value is string)
                {
                    typeName = "string";
                }
                else if (value is bool)
                {
                    typeName = "bool";
                }
                else if (value is char)
                {
                    typeName = "char";
                }
                else if (value is Array)
                {
                    typeName = "array";
                }
                else if (value is IList)
                {
                    typeName = "list";
                }
                else if (value is IDictionary)
                {
                    typeName = "dict";
                }
                else
                {
                    typeName = value.GetType().Name;
                }

                _stack.Push(typeName);
            }
                break;

            case OpCode.DefineEnum:
            {
                // 操作数格式: [enumNameIndex, memberCount, memberDataIndex]
                var operands = (object[])instruction.Operand!;
                var enumNameIndex = Convert.ToInt32(operands[0]);
                var memberCount = Convert.ToInt32(operands[1]);
                var memberDataIndex = Convert.ToInt32(operands[2]);

                // 从常量池获取枚举名称
                var enumName = (string)_bytecodeFile.ConstantPool.GetConstant(enumNameIndex);

                // 从常量池获取成员数据
                var memberData = (object[])_bytecodeFile.ConstantPool.GetConstant(memberDataIndex);

                // 构建成员字典
                var members = new Dictionary<string, int>();
                for (int i = 0; i < memberCount; i++)
                {
                    var memberName = (string)memberData[i * 2];
                    var memberValue = Convert.ToInt32(memberData[i * 2 + 1]);
                    members[memberName] = memberValue;
                }

                // 创建枚举模板
                var enumTemplate = new Old8Lang.AST.Expression.AnyValues.EnumTemplate(
                    enumName,
                    members,
                    default);

                // 将枚举模板存储到全局变量
                _globals[enumName] = enumTemplate;
            }
                break;

            case OpCode.DefineInterface:
            case OpCode.DefineMixin:
            case OpCode.ApplyMixin:
            case OpCode.CheckInterface:
                // 接口和Mixin在编译时处理，运行时不需要额外操作
                // 这些指令主要用于元数据记录和类型检查
                break;

            // === 并发原语 ===
            case OpCode.MutexCreate:
            {
                var mutexId = Concurrency.ResourceManager.CreateMutex();
                _stack.Push(mutexId);
            }
                break;

            case OpCode.MutexLock:
            {
                var mutexId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.LockMutex(mutexId);
            }
                break;

            case OpCode.MutexUnlock:
            {
                var mutexId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.UnlockMutex(mutexId);
            }
                break;

            case OpCode.MutexDispose:
            {
                var mutexId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.DisposeMutex(mutexId);
            }
                break;

            case OpCode.ChannelCreate:
            {
                var channelId = Concurrency.ResourceManager.CreateChannel();
                _stack.Push(channelId);
            }
                break;

            case OpCode.ChannelSend:
            {
                var value = _stack.Pop();
                var channelId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.SendChannel(channelId, value);
            }
                break;

            case OpCode.ChannelReceive:
            {
                var channelId = Convert.ToInt32(_stack.Pop());
                var value = Concurrency.ResourceManager.ReceiveChannel(channelId);
                _stack.Push(value);
            }
                break;

            case OpCode.ChannelClose:
            {
                var channelId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.CloseChannel(channelId);
            }
                break;

            case OpCode.ChannelTrySend:
            {
                // 栈顶: timeoutMs, value, channelId
                var timeoutMs = Convert.ToInt32(_stack.Pop());
                var value = _stack.Pop();
                var channelId = Convert.ToInt32(_stack.Pop());
                var success = Concurrency.ResourceManager.TrySendChannel(channelId, value, timeoutMs);
                _stack.Push(success);
            }
                break;

            case OpCode.ChannelTryReceive:
            {
                // 栈顶: timeoutMs, channelId
                var timeoutMs = Convert.ToInt32(_stack.Pop());
                var channelId = Convert.ToInt32(_stack.Pop());
                var result = Concurrency.ResourceManager.TryReceiveChannel(channelId, timeoutMs);
                _stack.Push(result);
            }
                break;

            case OpCode.SemaphoreCreate:
            {
                // 栈顶: maxCount, initialCount
                var maxCount = Convert.ToInt32(_stack.Pop());
                var initialCount = Convert.ToInt32(_stack.Pop());
                var semaphoreId = Concurrency.ResourceManager.CreateSemaphore(initialCount, maxCount);
                _stack.Push(semaphoreId);
            }
                break;

            case OpCode.SemaphoreAcquire:
            {
                var semaphoreId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.AcquireSemaphore(semaphoreId);
            }
                break;

            case OpCode.SemaphoreRelease:
            {
                var semaphoreId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.ReleaseSemaphore(semaphoreId);
            }
                break;

            // === Thread 支持 ===
            case OpCode.ThreadCreate:
            {
                // 栈顶: func (int/string/Metadata), argCount (int)
                var funcObj = _stack.Pop();
                var argCount = Convert.ToInt32(_stack.Pop());

                // 弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                FunctionMetadata function;
                if (funcObj is int funcIndex)
                {
                    function = _bytecodeFile.Functions[funcIndex];
                }
                else if (funcObj is string funcName)
                {
                    function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName)
                               ?? throw new Exception($"Function not found: {funcName}");
                }
                else if (funcObj is FunctionMetadata funcMeta)
                {
                    function = funcMeta;
                }
                else
                {
                    throw new Exception($"Invalid function for ThreadCreate: {funcObj?.GetType().Name}");
                }

                // 创建线程
                var threadId = Concurrency.ResourceManager.CreateThread(() =>
                {
                    // Create new VM instance
                    var threadVm = new VirtualMachine(_bytecodeFile, _baseDirectory);
                    foreach (var kvp in _globals) threadVm._globals[kvp.Key] = kvp.Value;

                    threadVm.CallFunction(function, args);
                });

                _stack.Push(threadId);
            }
                break;

            case OpCode.ThreadStart:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.StartThread(threadId);
            }
                break;

            case OpCode.ThreadJoin:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                var result = Concurrency.ResourceManager.JoinThread(threadId);
                _stack.Push(result);
            }
                break;

            case OpCode.ThreadIsAlive:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                var isAlive = Concurrency.ResourceManager.IsThreadAlive(threadId);
                _stack.Push(isAlive);
            }
                break;

            case OpCode.ThreadDispose:
            {
                var threadId = Convert.ToInt32(_stack.Pop());
                Concurrency.ResourceManager.DisposeThread(threadId);
            }
                break;

            // === 异步支持 ===
            case OpCode.NewTask:
            {
                // 栈顶: func, argCount
                var funcObj = _stack.Pop();
                var argCount = Convert.ToInt32(_stack.Pop());

                // 弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                FunctionMetadata function;
                if (funcObj is int funcIndex)
                {
                    function = _bytecodeFile.Functions[funcIndex];
                }
                else if (funcObj is string funcName)
                {
                    function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName)
                               ?? throw new Exception($"Function not found: {funcName}");
                }
                else if (funcObj is FunctionMetadata funcMeta)
                {
                    function = funcMeta;
                }
                else
                {
                    throw new Exception($"Invalid function for NewTask: {funcObj?.GetType().Name}");
                }

                // 创建并启动任务
                var task = Task.Run<LangValueType>(() =>
                {
                    var asyncVm = new VirtualMachine(_bytecodeFile, _baseDirectory);
                    foreach (var kvp in _globals) asyncVm._globals[kvp.Key] = kvp.Value;
                    var result = asyncVm.ExecuteFunctionAndGetResult(function, args);
                    return ConvertToLangValue(result);
                });

                _stack.Push(new TaskLangValue(task));
            }
                break;

            case OpCode.CallAsync:
            {
                // 栈布局: [arg1, arg2, ..., argCount, funcName]
                var operands = (object[])instruction.Operand!;
                int argCount = (int)operands[0];
                string funcName = (string)operands[1];

                // 弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 查找函数
                var function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);
                if (function == null)
                {
                    throw new Exception($"未定义的异步函数: {funcName}");
                }

                // 创建并启动任务
                var task = Task.Run<LangValueType>(() =>
                {
                    // 在新线程中执行函数
                    // 这里我们创建一个新的 VirtualMachine 实例来执行异步任务
                    // 共享全局变量和常量池
                    var asyncVm = new VirtualMachine(_bytecodeFile, _baseDirectory);
                    // 复制全局变量
                    foreach (var kvp in _globals)
                    {
                        asyncVm._globals[kvp.Key] = kvp.Value;
                    }

                    // 执行函数
                    var result = asyncVm.ExecuteFunctionAndGetResult(function, args);
                    return ConvertToLangValue(result);
                });

                // 将 Task 包装并压入栈
                _stack.Push(new TaskLangValue(task));
            }
                break;

            case OpCode.Await:
            {
                // 栈顶: TaskLangValue
                var value = _stack.Pop();
                if (value is TaskLangValue taskValue)
                {
                    // 阻塞等待任务完成
                    var result = taskValue.Await();
                    _stack.Push(result);
                }
                else if (value is Task task)
                {
                    // 直接是 Task 对象
                    task.GetAwaiter().GetResult();
                    // 如果是 Task<T>，获取结果
                    var resultProperty = task.GetType().GetProperty("Result");
                    if (resultProperty != null)
                    {
                        _stack.Push(resultProperty.GetValue(task));
                    }
                    else
                    {
                        _stack.Push(null);
                    }
                }
                else
                {
                    throw new Exception($"await 只能用于 Task 类型，实际类型为 {value?.GetType().Name ?? "null"}");
                }
            }
                break;

            case OpCode.NewAsyncGenerator:
                // TODO: Implement async generator creation
                throw new NotImplementedException("OpCode.NewAsyncGenerator not implemented");

            case OpCode.CallAsyncGenerator:
                // TODO: Implement async generator call
                throw new NotImplementedException("OpCode.CallAsyncGenerator not implemented");


            case OpCode.AwaitYield:
            case OpCode.Yield:
            {
                // Yield操作：生成器返回一个值并暂停执行
                // 1. 从栈中弹出要yield的值
                var yieldValue = _stack.Pop();

                // 2. 检查是否是异步生成器
                if (frame.AsyncGeneratorId.HasValue)
                {
                    var generatorId = frame.AsyncGeneratorId.Value;
                    if (_asyncGenerators.TryGetValue(generatorId, out var generatorState))
                    {
                        // 保存状态
                        var stackArray = _stack.ToArray();
                        var stackCopy = new Stack<LangValueType>();
                        foreach (var item in stackArray.Reverse())
                        {
                            if (item is LangValueType langValue)
                                stackCopy.Push(langValue);
                        }

                        generatorState.SaveState(
                            frame.IP,
                            frame.Locals,
                            stackCopy
                        );

                        generatorState.CurrentValue = yieldValue as LangValueType ?? new VoidLangValue();

                        // 将值压回栈顶
                        _stack.Push(yieldValue);

                        // 暂停执行
                        frame.IP = frame.Function.Instructions.Count;
                    }
                }
                // 3. 检查是否是普通生成器
                else if (frame.GeneratorId.HasValue)
                {
                    var generatorId = frame.GeneratorId.Value;
                    if (_generators.TryGetValue(generatorId, out var generatorState))
                    {
                        // 3. 保存当前执行状态
                        var stackArray = _stack.ToArray();
                        var stackCopy = new Stack<LangValueType>();
                        foreach (var item in stackArray.Reverse())
                        {
                            if (item is LangValueType langValue)
                                stackCopy.Push(langValue);
                        }

                        generatorState.SaveState(
                            frame.IP, // 保存下一条指令的位置
                            frame.Locals,
                            stackCopy
                        );

                        // 4. 设置当前yield的值
                        generatorState.CurrentValue = yieldValue as LangValueType ?? new VoidLangValue();

                        // 5. 将yield的值压回栈顶（作为MoveNext的返回值）
                        _stack.Push(yieldValue);

                        // 6. 通过返回来暂停执行（设置IP到函数末尾）
                        frame.IP = frame.Function.Instructions.Count;
                    }
                }
            }
                break;


            // === 异常处理 ===
            case OpCode.Throw:
            {
                var exceptionValue = _stack.Pop();
                throw new VmException(exceptionValue);
            }

            case OpCode.TryBegin:
            {
                // TryBegin操作：开始try块
                // 操作数: [catchOffset, finallyOffset]
                var operands = (int[])instruction.Operand!;
                int catchOffset = operands[0];
                int finallyOffset = operands.Length > 1 ? operands[1] : -1;

                // 创建异常处理器并压入栈
                var handler = new ExceptionHandler
                {
                    CatchIP = catchOffset,
                    FinallyIP = finallyOffset,
                    EndIP = -1, // 将在TryEnd时设置
                    InFinally = false
                };
                _exceptionHandlers.Push(handler);
            }
                break;

            case OpCode.TryEnd:
            {
                // TryEnd操作：结束try块
                // 如果没有异常，跳过catch块，执行finally块（如果有）
                if (_exceptionHandlers.Count > 0)
                {
                    var handler = _exceptionHandlers.Peek();

                    // 如果有finally块，跳转到finally
                    if (handler.FinallyIP >= 0)
                    {
                        frame.IP = handler.FinallyIP;
                    }
                    // 否则跳过整个try-catch块
                    else if (handler.EndIP >= 0)
                    {
                        frame.IP = handler.EndIP;
                    }
                }
            }
                break;

            case OpCode.CatchBegin:
            {
                // CatchBegin操作：开始catch块
                // 异常对象应该已经在栈上
                // 这里不需要做特殊处理，只是标记进入catch块
            }
                break;

            case OpCode.CatchEnd:
            {
                // CatchEnd操作：结束catch块
                // 跳转到finally块（如果有）或结束
                if (_exceptionHandlers.Count > 0)
                {
                    var handler = _exceptionHandlers.Peek();

                    // 如果有finally块，跳转到finally
                    if (handler.FinallyIP >= 0)
                    {
                        frame.IP = handler.FinallyIP;
                    }
                    // 否则跳到结束
                    else if (handler.EndIP >= 0)
                    {
                        frame.IP = handler.EndIP;
                    }
                }
            }
                break;

            case OpCode.FinallyBegin:
            {
                // FinallyBegin操作：开始finally块
                if (_exceptionHandlers.Count > 0)
                {
                    var handler = _exceptionHandlers.Peek();
                    handler.InFinally = true;
                }
            }
                break;

            case OpCode.FinallyEnd:
            {
                // FinallyEnd操作：结束finally块
                // 弹出异常处理器
                if (_exceptionHandlers.Count > 0)
                {
                    _exceptionHandlers.Pop();
                }
            }
                break;

            case OpCode.GetField:
            {
                // 栈顶: object
                // 操作数: fieldName (string)
                var obj = _stack.Pop();
                string fieldName = (string)instruction.Operand!;

                if (obj == null)
                {
                    throw new Exception($"无法访问 null 对象的字段 {fieldName}");
                }

                // 如果是 BytecodeObjectInstance（Old8Lang 对象）
                if (obj is BytecodeObjectInstance bytecodeObj)
                {
                    if (bytecodeObj.Fields.TryGetValue(fieldName, out var value))
                    {
                        _stack.Push(value);
                    }
                    else
                    {
                        throw new Exception($"对象没有字段 {fieldName}");
                    }
                }
                // 如果是字典对象（兼容旧代码）
                else if (obj is Dictionary<string, object?> dictObj)
                {
                    if (dictObj.TryGetValue(fieldName, out var value))
                    {
                        _stack.Push(value);
                    }
                    else
                    {
                        throw new Exception($"对象没有字段 {fieldName}");
                    }
                }
                // 如果是枚举模板（访问枚举成员）
                else if (obj is Old8Lang.AST.Expression.AnyValues.EnumTemplate enumTemplate)
                {
                    var enumValue = enumTemplate.GetMemberValue(fieldName);
                    _stack.Push(enumValue);
                }
                else if (obj is Tuple<object?, object?> tuple)
                {
                    if (fieldName == "Length")
                    {
                        // 计算元组长度
                        int length = 0;
                        var traverseStack = new Stack<object?>();
                        traverseStack.Push(tuple);

                        while (traverseStack.Count > 0)
                        {
                            var current = traverseStack.Pop();
                            if (current is Tuple<object?, object?> t)
                            {
                                traverseStack.Push(t.Item2);
                                traverseStack.Push(t.Item1);
                            }
                            else if (current != null)
                            {
                                length++;
                            }
                        }

                        _stack.Push(length);
                    }
                    else if (fieldName.StartsWith("Item") && int.TryParse(fieldName.Substring(4), out int itemNum))
                    {
                        // ItemN 访问 (1-based)
                        int idx = itemNum - 1;
                        int currentIdx = 0;
                        bool found = false;

                        var traverseStack = new Stack<object?>();
                        traverseStack.Push(tuple);

                        while (traverseStack.Count > 0)
                        {
                            var current = traverseStack.Pop();
                            if (current is Tuple<object?, object?> t)
                            {
                                traverseStack.Push(t.Item2);
                                traverseStack.Push(t.Item1);
                            }
                            else if (current != null)
                            {
                                if (currentIdx == idx)
                                {
                                    _stack.Push(current);
                                    found = true;
                                    break;
                                }

                                currentIdx++;
                            }
                        }

                        if (!found)
                        {
                            throw new Exception($"元组没有字段 {fieldName}");
                        }
                    }
                    else
                    {
                        throw new Exception($"类型 Tuple 没有字段或属性 {fieldName}");
                    }
                }
                else if (obj is System.Collections.IList list)
                {
                    if (fieldName == "Length")
                    {
                        _stack.Push(list.Count);
                    }
                    else
                    {
                        // 尝试使用反射获取其他属性
                        var type = obj.GetType();
                        var prop = type.GetProperty(fieldName);
                        if (prop != null)
                        {
                            _stack.Push(prop.GetValue(obj));
                        }
                        else
                        {
                            var field = type.GetField(fieldName);
                            if (field != null)
                            {
                                _stack.Push(field.GetValue(obj));
                            }
                            else
                            {
                                throw new Exception($"类型 {type.Name} 没有字段或属性 {fieldName}");
                            }
                        }
                    }
                }
                else if (obj is Array array)
                {
                    if (fieldName == "Length")
                    {
                        _stack.Push(array.Length);
                    }
                    else
                    {
                        // 尝试使用反射获取其他属性
                        var type = obj.GetType();
                        var prop = type.GetProperty(fieldName);
                        if (prop != null)
                        {
                            _stack.Push(prop.GetValue(obj));
                        }
                        else
                        {
                            throw new Exception($"类型 {type.Name} 没有字段或属性 {fieldName}");
                        }
                    }
                }
                else
                {
                    // 使用反射获取字段或属性（用于内置类型）
                    var objType = obj.GetType();

                    // 先尝试获取属性
                    var property = objType.GetProperty(fieldName);
                    if (property != null)
                    {
                        _stack.Push(property.GetValue(obj));
                    }
                    else
                    {
                        // 再尝试获取字段
                        var field = objType.GetField(fieldName);
                        if (field != null)
                        {
                            _stack.Push(field.GetValue(obj));
                        }
                        else
                        {
                            throw new Exception($"类型 {objType.Name} 没有字段或属性 {fieldName}");
                        }
                    }
                }
            }
                break;

            case OpCode.SetField:
            {
                // 栈布局(从栈顶到栈底): value, object
                // 操作数: fieldName (string)
                var value = _stack.Pop();
                var obj = _stack.Pop();
                string fieldName = (string)instruction.Operand!;

                if (obj == null)
                {
                    throw new Exception($"无法设置 null 对象的字段 {fieldName}");
                }

                // 如果是 BytecodeObjectInstance（Old8Lang 对象）
                if (obj is BytecodeObjectInstance bytecodeObj)
                {
                    bytecodeObj.Fields[fieldName] = value;
                }
                // 如果是字典对象（兼容旧代码）
                else if (obj is Dictionary<string, object?> dictObj)
                {
                    dictObj[fieldName] = value;
                }
                else
                {
                    // 使用反射设置字段或属性（用于内置类型）
                    var objType = obj.GetType();

                    // 先尝试设置属性
                    var property = objType.GetProperty(fieldName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(obj, value);
                    }
                    else
                    {
                        // 再尝试设置字段
                        var field = objType.GetField(fieldName);
                        if (field != null)
                        {
                            field.SetValue(obj, value);
                        }
                        else
                        {
                            throw new Exception($"类型 {objType.Name} 没有可写的字段或属性 {fieldName}");
                        }
                    }
                }
            }
                break;

            case OpCode.GetSuperField:
            {
                // 栈顶: this 实例
                // 操作数: fieldName (string)
                var thisInstance = _stack.Pop();
                string fieldName = (string)instruction.Operand!;

                if (thisInstance == null)
                {
                    throw new Exception($"无法访问 null 对象的父类字段 {fieldName}");
                }

                // 检查是否是 BytecodeObjectInstance
                if (thisInstance is BytecodeObjectInstance bytecodeObj)
                {
                    // 注意: 在 Old8Lang 中,所有字段(包括父类字段)都存储在对象实例的 Fields 字典中
                    // super.field 访问的是继承自父类的字段,但实际存储位置在对象本身
                    // 因此我们直接从对象的 Fields 字典中获取字段值即可

                    if (bytecodeObj.Fields.TryGetValue(fieldName, out var value))
                    {
                        _stack.Push(value);
                    }
                    else
                    {
                        // 字段不存在,返回 null
                        _stack.Push(null);
                    }
                }
                else
                {
                    // 使用反射获取父类字段或属性（用于 C# 对象）
                    var objType = thisInstance.GetType();
                    var baseType = objType.BaseType;

                    if (baseType == null || baseType == typeof(object))
                    {
                        throw new Exception($"类型 {objType.Name} 没有父类");
                    }

                    // 先尝试获取属性
                    var property = baseType.GetProperty(fieldName);
                    if (property != null)
                    {
                        _stack.Push(property.GetValue(thisInstance));
                    }
                    else
                    {
                        // 再尝试获取字段
                        var field = baseType.GetField(fieldName);
                        if (field != null)
                        {
                            _stack.Push(field.GetValue(thisInstance));
                        }
                        else
                        {
                            throw new Exception($"父类 {baseType.Name} 没有字段或属性 {fieldName}");
                        }
                    }
                }
            }
                break;

            case OpCode.SetSuperField:
            {
                // 栈布局(从栈顶到栈底): value, this 实例
                // 操作数: fieldName (string)
                var value = _stack.Pop();
                var thisInstance = _stack.Pop();
                string fieldName = (string)instruction.Operand!;

                if (thisInstance == null)
                {
                    throw new Exception($"无法设置 null 对象的父类字段 {fieldName}");
                }

                // 检查是否是 BytecodeObjectInstance
                if (thisInstance is BytecodeObjectInstance bytecodeObj)
                {
                    // 注意: 在 Old8Lang 中,所有字段(包括父类字段)都存储在对象实例的 Fields 字典中
                    // super.field <- value 设置的是继承自父类的字段,但实际存储位置在对象本身
                    // 因此我们直接设置对象的 Fields 字典中的字段值即可
                    bytecodeObj.Fields[fieldName] = value;
                }
                else
                {
                    // 使用反射设置父类字段或属性（用于 C# 对象）
                    var objType = thisInstance.GetType();
                    var baseType = objType.BaseType;

                    if (baseType == null || baseType == typeof(object))
                    {
                        throw new Exception($"类型 {objType.Name} 没有父类");
                    }

                    // 先尝试设置属性
                    var property = baseType.GetProperty(fieldName);
                    if (property != null && property.CanWrite)
                    {
                        property.SetValue(thisInstance, value);
                    }
                    else
                    {
                        // 再尝试设置字段
                        var field = baseType.GetField(fieldName);
                        if (field != null)
                        {
                            field.SetValue(thisInstance, value);
                        }
                        else
                        {
                            throw new Exception($"父类 {baseType.Name} 没有可写的字段或属性 {fieldName}");
                        }
                    }
                }
            }
                break;

            case OpCode.NewObject:
            {
                // 操作数: className (string)
                string className = (string)instruction.Operand!;

                // 从字节码文件中查找类定义
                var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == className);

                // 如果在当前字节码文件中没找到，从全局变量中查找（可能是导入的类）
                if (classMetadata == null)
                {
                    if (_globals.TryGetValue(className, out var globalClass) &&
                        globalClass is ClassMetadata importedClass)
                    {
                        classMetadata = importedClass;
                    }
                }

                // 如果还没找到，从所有已加载模块的导出符号中查找
                if (classMetadata == null)
                {
                    foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                    {
                        try
                        {
                            var symbol = _moduleRegistry.GetModuleSymbol(loadedModuleName, className);
                            if (symbol is ClassMetadata moduleClass)
                            {
                                classMetadata = moduleClass;
                                break;
                            }
                        }
                        catch
                        {
                            // 模块中没有该符号，继续查找
                        }
                    }
                }

                if (classMetadata == null)
                {
                    throw new Exception($"未找到类定义: {className}");
                }

                // 创建对象实例
                var obj = new BytecodeObjectInstance(className);

                // 初始化所有字段为默认值（包括父类字段）
                // 收集当前类及所有父类的字段
                var allFields = new List<FieldMetadata>();
                var currentClass = classMetadata;
                while (currentClass != null)
                {
                    allFields.AddRange(currentClass.Fields);

                    // 查找父类
                    if (!string.IsNullOrEmpty(currentClass.BaseClassName))
                    {
                        currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                    }
                    else
                    {
                        break;
                    }
                }

                // 初始化所有字段
                foreach (var field in allFields)
                {
                    // 避免重复初始化同名字段（子类覆盖父类字段的情况）
                    if (!obj.Fields.ContainsKey(field.Name))
                    {
                        obj.Fields[field.Name] = null;
                    }
                }

                // 应用 Mixin 方法到对象
                if (classMetadata.Mixins != null && classMetadata.Mixins.Count > 0)
                {
                    foreach (var mixinName in classMetadata.Mixins)
                    {
                        var mixinMetadata = _bytecodeFile.Mixins.FirstOrDefault(m => m.Name == mixinName);
                        if (mixinMetadata != null)
                        {
                            // Mixin 方法在运行时通过方法查找自动可用
                            // 这里只需要记录 Mixin 关联即可
                            obj.Mixins.Add(mixinName);
                        }
                    }
                }

                // 记录实现的接口
                if (classMetadata.ImplementsInterfaces != null && classMetadata.ImplementsInterfaces.Count > 0)
                {
                    foreach (var interfaceName in classMetadata.ImplementsInterfaces)
                    {
                        obj.Interfaces.Add(interfaceName);
                    }
                }

                // 将对象压入栈
                _stack.Push(obj);
            }
                break;

            case OpCode.CallMethod:
            {
                // 操作数: argCount (int), methodName (string)
                var operands = (object[])instruction.Operand!;
                int argCount = (int)operands[0];
                string methodName = (string)operands[1];

                // 从栈中弹出参数（逆序）
                var args = new object?[argCount - 1]; // -1 因为第一个参数是对象本身
                for (int i = args.Length - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 弹出对象
                var obj = _stack.Pop();
                if (obj == null)
                {
                    throw new Exception($"无法在 null 对象上调用方法 {methodName}");
                }

                // 检查是否是 BytecodeObjectInstance
                if (obj is BytecodeObjectInstance bytecodeObj)
                {
                    // Old8Lang 对象，查找类方法
                    var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == bytecodeObj.ClassName);

                    // 如果在当前字节码文件中没找到，从全局变量和已加载模块中查找
                    if (classMetadata == null)
                    {
                        if (_globals.TryGetValue(bytecodeObj.ClassName, out var globalClass) &&
                            globalClass is ClassMetadata importedClass)
                        {
                            classMetadata = importedClass;
                        }
                    }

                    if (classMetadata == null)
                    {
                        foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                        {
                            try
                            {
                                var symbol = _moduleRegistry.GetModuleSymbol(loadedModuleName, bytecodeObj.ClassName);
                                if (symbol is ClassMetadata moduleClass)
                                {
                                    classMetadata = moduleClass;
                                    break;
                                }
                            }
                            catch
                            {
                                // 继续查找
                            }
                        }
                    }

                    if (classMetadata == null)
                    {
                        throw new Exception($"未找到类定义: {bytecodeObj.ClassName}");
                    }

                    // 在类的方法列表中查找方法（包括父类方法）
                    MethodMetadata? methodMetadata = null;
                    var currentClass = classMetadata;

                    // 沿着继承链查找方法
                    while (currentClass != null && methodMetadata == null)
                    {
                        methodMetadata = currentClass.Methods.FirstOrDefault(m => m.Name == methodName);

                        if (methodMetadata == null && !string.IsNullOrEmpty(currentClass.BaseClassName))
                        {
                            // 在父类中继续查找
                            currentClass =
                                _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                        }
                        else
                        {
                            break;
                        }
                    }

                    // 如果在类继承链中没找到，尝试在 Mixin 中查找
                    if (methodMetadata == null && bytecodeObj.Mixins.Count > 0)
                    {
                        foreach (var mixinName in bytecodeObj.Mixins)
                        {
                            var mixinMetadata = _bytecodeFile.Mixins.FirstOrDefault(m => m.Name == mixinName);
                            if (mixinMetadata != null)
                            {
                                methodMetadata = mixinMetadata.Methods.FirstOrDefault(m => m.Name == methodName);
                                if (methodMetadata != null)
                                {
                                    break; // 找到方法，停止搜索
                                }
                            }
                        }
                    }

                    if (methodMetadata == null)
                    {
                        throw new Exception($"类 {bytecodeObj.ClassName} 没有方法 {methodName}");
                    }

                    // 准备方法调用参数：第一个参数是 this（对象本身）
                    var methodArgs = new object?[args.Length + 1];
                    methodArgs[0] = bytecodeObj;
                    Array.Copy(args, 0, methodArgs, 1, args.Length);

                    // 调用方法（返回值会自动压入栈）
                    CallFunction(methodMetadata.Function, methodArgs);
                }
                else
                {
                    // 原生 C# 对象或 Old8Lang 类型，使用 InvokeTypeMethod 调用方法
                    // 这个方法会优先查找扩展方法，然后查找实例方法
                    var result = InvokeTypeMethod(obj, methodName, args);

                    // 如果方法有返回值，压入栈
                    if (result != null && result is not VoidLangValue)
                    {
                        _stack.Push(result);
                    }
                }
            }
                break;

            case OpCode.LoadSuper:
            {
                // 加载当前实例（this）作为 super 上下文
                // this 是方法的第一个参数
                var currentFrame = _callStack.Peek();

                // 优先从 Arguments 中获取 this（第一个参数）
                if (currentFrame.Arguments != null && currentFrame.Arguments.Length > 0)
                {
                    var thisInstance = currentFrame.Arguments[0];
                    if (thisInstance == null)
                    {
                        throw new Exception("super 只能在实例方法中使用");
                    }

                    _stack.Push(thisInstance);
                }
                // 如果 Arguments 为空，尝试从 Locals 获取
                else if (currentFrame.Locals.Length > 0)
                {
                    var thisInstance = currentFrame.Locals[0];
                    if (thisInstance == null)
                    {
                        throw new Exception("super 只能在实例方法中使用");
                    }

                    _stack.Push(thisInstance);
                }
                else
                {
                    throw new Exception("super 只能在实例方法中使用");
                }
            }
                break;

            case OpCode.CallSuperMethod:
            {
                // 操作数: argCount (int), methodName (string)
                var operands = (object[])instruction.Operand!;
                int argCount = (int)operands[0];
                string methodName = (string)operands[1];

                // 从栈中弹出参数（逆序）
                var args = new object?[argCount - 1]; // -1 因为第一个参数是 this
                for (int i = args.Length - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 弹出 this 实例
                var thisInstance = _stack.Pop();
                if (thisInstance == null)
                {
                    throw new Exception($"无法在 null 对象上调用父类方法 {methodName}");
                }

                // 检查是否是 BytecodeObjectInstance
                if (thisInstance is BytecodeObjectInstance bytecodeObj)
                {
                    // 查找当前类的元数据
                    var currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == bytecodeObj.ClassName);
                    if (currentClass == null)
                    {
                        throw new Exception($"未找到类定义: {bytecodeObj.ClassName}");
                    }

                    // 查找父类
                    if (string.IsNullOrEmpty(currentClass.BaseClassName))
                    {
                        throw new Exception($"类 {bytecodeObj.ClassName} 没有父类");
                    }

                    var parentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                    if (parentClass == null)
                    {
                        throw new Exception($"未找到父类定义: {currentClass.BaseClassName}");
                    }

                    // 在父类中查找方法
                    var methodMetadata = parentClass.Methods.FirstOrDefault(m => m.Name == methodName);
                    if (methodMetadata == null)
                    {
                        throw new Exception($"父类 {parentClass.Name} 没有方法 {methodName}");
                    }

                    // 准备方法调用参数：第一个参数是 this
                    var methodArgs = new object?[args.Length + 1];
                    methodArgs[0] = bytecodeObj;
                    Array.Copy(args, 0, methodArgs, 1, args.Length);

                    // 调用父类方法
                    CallFunction(methodMetadata.Function, methodArgs);
                }
                else
                {
                    // 原生 C# 对象，使用反射调用父类方法
                    var objType = thisInstance.GetType();
                    var method = objType.GetMethod(methodName);

                    if (method == null)
                    {
                        throw new Exception($"类型 {objType.Name} 的父类没有方法 {methodName}");
                    }

                    var result = method.Invoke(thisInstance, args);

                    // 如果方法有返回值，压入栈
                    if (method.ReturnType != typeof(void))
                    {
                        _stack.Push(result);
                    }
                }
            }
                break;

            // === 模块操作 ===
            case OpCode.LoadModule:
            {
                // 加载模块
                string moduleName = (string)instruction.Operand!;
                LoadModule(moduleName);
            }
                break;

            case OpCode.ImportSymbol:
            {
                // 导入符号: import { symbol } from "module"
                var operands = (object[])instruction.Operand!;
                string moduleName = (string)operands[0];
                string symbolName = (string)operands[1];

                var symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);
                if (symbol == null)
                {
                    throw new Exception($"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
                }

                // 将符号添加到当前全局变量
                _globals[symbolName] = symbol;
            }
                break;

            case OpCode.ImportSymbolAs:
            {
                // 导入符号并重命名: import { symbol as alias } from "module"
                var operands = (object[])instruction.Operand!;
                string moduleName = (string)operands[0];
                string symbolName = (string)operands[1];
                string alias = (string)operands[2];

                var symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);
                if (symbol == null)
                {
                    throw new Exception($"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
                }

                // 使用别名添加到全局变量
                _globals[alias] = symbol;
            }
                break;

            case OpCode.ImportAll:
            {
                // 导入所有符号: import * from "module"
                string moduleName = (string)instruction.Operand!;

                var module = _moduleRegistry.GetModule(moduleName);
                if (module == null)
                {
                    throw new Exception($"模块 '{moduleName}' 未加载");
                }

                // 导入所有导出的符号
                foreach (var symbolName in module.GetExportedSymbolNames())
                {
                    var symbol = module.GetSymbol(symbolName);
                    _globals[symbolName] = symbol;
                }
            }
                break;

            case OpCode.GetModuleSymbol:
            {
                // 获取模块符号: moduleName.symbolName
                var operands = (object[])instruction.Operand!;
                string moduleName = (string)operands[0];
                string symbolName = (string)operands[1];

                var symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);
                if (symbol == null)
                {
                    throw new Exception($"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
                }

                _stack.Push(symbol);
            }
                break;

            case OpCode.DebugPrint:
            {
                int messageIndex = (int)instruction.Operand!;
                var message = _bytecodeFile.ConstantPool.GetConstant(messageIndex);
                var stackContents = string.Join(", ", _stack.Select(x => x?.GetType().Name ?? "null"));
                Console.WriteLine($"{message} - 栈深度:{_stack.Count}, 内容:[{stackContents}]");
            }
                break;

            // === Defer 支持 ===
            case OpCode.Defer:
            {
                // Defer 指令：将 defer 块的起始位置压入 DeferStack
                int deferStartPos = (int)instruction.Operand!;
                frame.DeferStack.Push(deferStartPos);
            }
                break;

            case OpCode.ExecuteDefers:
            {
                // ExecuteDefers 指令：执行所有 defer 块（按 LIFO 顺序）
                ExecuteDefers(frame);
            }
                break;

            case OpCode.LoadExtern:
            {
                // LoadExtern 指令：加载 extern 函数
                // 操作数格式: [dllNameIndex, funcNameIndex, externTypeIndex, callingConvIndex, signatureIndex]
                var operands = (int[])instruction.Operand!;
                var dllName = (string)_bytecodeFile.ConstantPool.GetConstant(operands[0]);
                var funcName = (string)_bytecodeFile.ConstantPool.GetConstant(operands[1]);
                var externType = (ExternType)(int)_bytecodeFile.ConstantPool.GetConstant(operands[2]);
                var callingConv = (CallingConventionType)(int)_bytecodeFile.ConstantPool.GetConstant(operands[3]);
                var signatureStr = (string)_bytecodeFile.ConstantPool.GetConstant(operands[4]);

                // 创建 extern 函数包装器
                var externFunc = new ExternFunctionWrapper(dllName, funcName, externType, callingConv, signatureStr);
                _stack.Push(externFunc);
            }
                break;

            case OpCode.CallExtern:
            {
                // CallExtern 指令：调用 extern 函数
                // 操作数格式: [argCount, funcNameIndex]
                var operands = (int[])instruction.Operand!;
                var argCount = operands[0];
                var funcNameIndex = operands[1];
                var funcName = (string)_bytecodeFile.ConstantPool.GetConstant(funcNameIndex);

                // 从全局变量中获取 extern 函数
                if (!_globals.TryGetValue(funcName, out var funcObj) || funcObj is not ExternFunctionWrapper externFunc)
                {
                    throw new Exception($"未找到 extern 函数: {funcName}");
                }

                // 弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 调用 extern 函数
                var result = externFunc.Invoke(args);
                _stack.Push(result);
            }
                break;

            case OpCode.DisposeResource:
            {
                // DisposeResource 指令：释放 using 语句的资源
                // 从栈顶弹出资源并调用相应的 Dispose 方法
                var resource = _stack.Pop();

                // 1. 如果是整数值（资源ID），尝试通过 ResourceManager 释放
                if (resource is int resourceId)
                {
                    Old8Lang.Concurrency.ResourceManager.TryDispose(resourceId);
                }
                // 2. 如果是 AnyLangValue（用户自定义类实例），尝试调用 dispose 方法
                else if (resource is AnyLangValue anyValue)
                {
                    anyValue.TryDispose();
                }
                // 3. 如果实现了 IDisposable 接口，直接调用 Dispose
                else if (resource is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                // 4. 其他类型不做处理（静默忽略）
            }
                break;

            case OpCode.ImportNative:
            {
                // ImportNative 指令：导入原生资源
                // 操作数格式: [dllNameIndex, classNameIndex, mode, p1, p2]
                var operands = (int[])instruction.Operand!;
                var dllName = (string)_bytecodeFile.ConstantPool.GetConstant(operands[0]);
                var className = (string)_bytecodeFile.ConstantPool.GetConstant(operands[1]);
                var mode = operands[2];
                var param1Index = operands[3];
                var param2Index = operands[4];

                // 解析 DLL 路径
                string basePath = Directory.GetCurrentDirectory();
                string dllPath;
                try
                {
                    dllPath = DllPathResolver.ResolveDllPath(dllName, null, basePath);
                }
                catch (FileNotFoundException)
                {
                    dllPath = dllName;
                }

                Assembly? assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == dllName);
                if (assembly == null)
                {
                    try
                    {
                        assembly = File.Exists(dllPath) ? Assembly.LoadFrom(dllPath) : Assembly.Load(dllPath);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"无法加载程序集 '{dllPath}': {ex.Message}");
                    }
                }

                Type? type = assembly.GetType($"{dllName}.{className}") ?? assembly.GetType(className);

                // 如果找不到类型，尝试在所有类型中查找
                if (type == null)
                {
                    type = assembly.GetTypes().FirstOrDefault(t => t.Name == className || t.FullName == className);
                }

                if (type == null) throw new Exception($"未找到类型: {className} in {dllName}");

                // Console.WriteLine($"Importing Native: {dllName}.{className}, Mode={mode}"); // Debug

                if (mode == 0) // Single Method
                {
                    var methodName = (string)_bytecodeFile.ConstantPool.GetConstant(param1Index);
                    var alias = (string)_bytecodeFile.ConstantPool.GetConstant(param2Index);
                    var registerName = string.IsNullOrEmpty(alias) ? methodName : alias;

                    var methodInfo = type.GetMethod(methodName,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (methodInfo == null) throw new Exception($"未找到方法: {methodName}");

                    var func = new FuncLangValue(registerName, methodInfo);
                    _globals[registerName] = func;
                }
                else if (mode == 1) // All Methods
                {
                    var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    foreach (var method in methods)
                    {
                        if (method.DeclaringType == typeof(object)) continue;
                        // 检查是否有重复（重载），如果有，FuncLangValue 支持重载吗？
                        // FuncLangValue 构造函数接受 MethodInfo。
                        // 如果全局变量中已经有该名字，可能是重载？
                        // Old8Lang 目前对重载支持有限，但在 Native 绑定中通常支持。
                        // 这里简单覆盖或忽略。
                        var func = new FuncLangValue(method.Name, method);
                        _globals[method.Name] = func;
                    }
                }
                else if (mode == 2) // Class Import
                {
                    var alias = (string)_bytecodeFile.ConstantPool.GetConstant(param1Index);
                    var registerName = string.IsNullOrEmpty(alias) ? className : alias;

                    // 使用 NativeStaticAny 包装类型，支持静态成员访问
                    var nativeClass = new NativeStaticAny(registerName, type);
                    _globals[registerName] = nativeClass;
                }
            }
                break;

            default:
                throw new Exception($"未实现的操作码: {instruction.OpCode}");
        }
    }

    // ===== 辅助方法 =====

    private bool CheckTypeMatch(string typeName, object? val)
    {
        typeName = typeName.Trim();

        // 1. Intersection Types (A & B)
        if (typeName.Contains('&'))
        {
            var types = typeName.Split('&');
            foreach (var type in types)
            {
                if (!CheckTypeMatch(type, val)) return false;
            }

            return true;
        }

        // 2. Union Types (A | B)
        if (typeName.Contains('|'))
        {
            var types = typeName.Split('|');
            foreach (var type in types)
            {
                if (CheckTypeMatch(type, val)) return true;
            }

            return false;
        }

        // 3. Nullable Types (T?)
        if (typeName.EndsWith("?"))
        {
            if (val == null) return true;
            return CheckTypeMatch(typeName.Substring(0, typeName.Length - 1), val);
        }

        // 4. Null Value Check
        if (val == null)
        {
            return typeName == "null" || typeName == "any";
        }

        // 5. Generic Types (list<T>, array<T>, dict<K,V>)
        if (typeName.StartsWith("list<") && typeName.EndsWith(">"))
        {
            if (val is not IList list) return false;
            var innerType = typeName.Substring(5, typeName.Length - 6);
            foreach (var item in list)
            {
                if (!CheckTypeMatch(innerType, item)) return false;
            }

            return true;
        }

        if (typeName.StartsWith("array<") && typeName.EndsWith(">"))
        {
            if (val is not Array array) return false;
            var innerType = typeName.Substring(6, typeName.Length - 7);
            foreach (var item in array)
            {
                if (!CheckTypeMatch(innerType, item)) return false;
            }

            return true;
        }

        if (typeName.StartsWith("dict<") && typeName.EndsWith(">"))
        {
            if (val is not IDictionary dict) return false;
            var innerTypes = SplitGenericArgs(typeName.Substring(5, typeName.Length - 6));
            if (innerTypes.Length != 2) return false; // Invalid syntax
            var keyType = innerTypes[0];
            var valueType = innerTypes[1];

            foreach (DictionaryEntry entry in dict)
            {
                if (!CheckTypeMatch(keyType, entry.Key)) return false;
                if (!CheckTypeMatch(valueType, entry.Value)) return false;
            }

            return true;
        }

        // 6. Basic Types
        return typeName.ToLower() switch
        {
            "int" => val is int,
            "double" => val is double,
            "string" => val is string,
            "bool" => val is bool,
            "char" => val is char,
            "array" => val is Array,
            "list" => val is IList,
            "dict" => val is IDictionary,
            "tuple" => val is Tuple<object?, object?>,
            "null" => val == null,
            "any" => true,
            "object" => true,
            _ => CheckCustomType(typeName, val)
        };
    }

    private string[] SplitGenericArgs(string args)
    {
        var result = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == '<') depth++;
            else if (args[i] == '>') depth--;
            else if (args[i] == ',' && depth == 0)
            {
                result.Add(args.Substring(start, i - start));
                start = i + 1;
            }
        }

        result.Add(args.Substring(start));
        return result.ToArray();
    }

    private bool CheckCustomType(string typeName, object? val)
    {
        if (val is BytecodeObjectInstance instance)
        {
            if (instance.ClassName == typeName) return true;

            // Check inheritance
            var metadata = _bytecodeFile.Classes.FirstOrDefault(m => m.Name == instance.ClassName);
            while (metadata != null)
            {
                if (metadata.Name == typeName) return true;
                if (metadata.InterfaceNames.Contains(typeName)) return true; // Check interfaces
                if (metadata.BaseClassName == typeName) return true;

                if (metadata.BaseClassName != null)
                {
                    metadata = _bytecodeFile.Classes.FirstOrDefault(m => m.Name == metadata.BaseClassName);
                }
                else
                {
                    break;
                }
            }
        }

        return false;
    }

    private List<object?> ConvertToList(object? value)
    {
        if (value == null) return new List<object?>();
        if (value is List<object?> list) return list;
        if (value is IEnumerable enumerable && value is not string)
        {
            var newList = new List<object?>();
            foreach (var item in enumerable) newList.Add(item);
            return newList;
        }

        return new List<object?> { value };
    }

    private object?[] ConvertToArray(object? value)
    {
        if (value == null) return Array.Empty<object?>();
        if (value is object?[] arr) return arr;
        if (value is List<object?> listObj) return listObj.ToArray();
        if (value is IEnumerable enumerable && value is not string)
        {
            var list = new List<object?>();
            foreach (var item in enumerable) list.Add(item);
            return list.ToArray();
        }

        return new object?[] { value };
    }

    private Dictionary<object, object?> ConvertToDict(object? value)
    {
        if (value == null) return new Dictionary<object, object?>();
        if (value is Dictionary<object, object?> dict) return dict;
        if (value is IDictionary d)
        {
            var newDict = new Dictionary<object, object?>();
            foreach (DictionaryEntry entry in d)
            {
                newDict[entry.Key] = entry.Value;
            }

            return newDict;
        }

        throw new InvalidCastException($"无法将类型 {value?.GetType().Name ?? "null"} 转换为 dict");
    }

    private object? Add(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia + ib;
        if (a is double da && b is double db) return da + db;
        if (a is int ia2 && b is double db2) return ia2 + db2;
        if (a is double da2 && b is int ib2) return da2 + ib2;
        if (a is string sa || b is string sb) return ToString(a) + ToString(b);
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行加法");
    }

    private object? Sub(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia - ib;
        if (a is double da && b is double db) return da - db;
        if (a is int ia2 && b is double db2) return ia2 - db2;
        if (a is double da2 && b is int ib2) return da2 - ib2;
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行减法");
    }

    private object? Mul(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia * ib;
        if (a is double da && b is double db) return da * db;
        if (a is int ia2 && b is double db2) return ia2 * db2;
        if (a is double da2 && b is int ib2) return da2 * ib2;
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行乘法");
    }

    private object? Div(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia / ib;
        if (a is double da && b is double db) return da / db;
        if (a is int ia2 && b is double db2) return ia2 / db2;
        if (a is double da2 && b is int ib2) return da2 / ib2;
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行除法");
    }

    private object? Mod(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia % ib;
        if (a is double da && b is double db) return da % db;
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行取模");
    }

    private object? Pow(object? a, object? b)
    {
        double da = ToDouble(a);
        double db = ToDouble(b);
        return Math.Pow(da, db);
    }

    private object? Neg(object? a)
    {
        if (a is int ia) return -ia;
        if (a is double da) return -da;
        throw new Exception($"无法对类型 {a?.GetType().Name} 执行取反");
    }

    private new bool Equals(object? a, object? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        if (a is int ia && b is int ib) return ia == ib;
        if (a is double da && b is double db) return Math.Abs(da - db) < 1e-10;
        if (a is bool ba && b is bool bb) return ba == bb;
        if (a is string sa && b is string sb) return sa == sb;

        // 处理枚举值比较
        if (a is Old8Lang.AST.Expression.Value.EnumLangValue ea && b is Old8Lang.AST.Expression.Value.EnumLangValue eb)
        {
            return ea.EnumTypeName == eb.EnumTypeName && ea.Value == eb.Value;
        }

        return object.Equals(a, b);
    }

    private bool Greater(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia > ib;
        if (a is double da && b is double db) return da > db;
        if (a is int ia2 && b is double db2) return ia2 > db2;
        if (a is double da2 && b is int ib2) return da2 > ib2;
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行大于比较");
    }

    private bool Less(object? a, object? b)
    {
        if (a is int ia && b is int ib) return ia < ib;
        if (a is double da && b is double db) return da < db;
        if (a is int ia2 && b is double db2) return ia2 < db2;
        if (a is double da2 && b is int ib2) return da2 < ib2;
        throw new Exception($"无法对类型 {a?.GetType().Name} 和 {b?.GetType().Name} 执行小于比较");
    }

    private bool GreaterEqual(object? a, object? b)
    {
        return Greater(a, b) || Equals(a, b);
    }

    private bool LessEqual(object? a, object? b)
    {
        return Less(a, b) || Equals(a, b);
    }

    private bool ToBool(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is double d) return Math.Abs(d) > 1e-10;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }

    private double ToDouble(object? value)
    {
        if (value is int i) return i;
        if (value is double d) return d;
        if (value is string s && double.TryParse(s, out double result)) return result;
        throw new Exception($"无法将 {value?.GetType().Name} 转换为 double");
    }

    /// <summary>
    /// 执行所有 defer 块（按 LIFO 顺序）
    /// </summary>
    private void ExecuteDefers(CallFrame frame)
    {
        // 按 LIFO 顺序执行所有 defer 块
        while (frame.DeferStack.Count > 0)
        {
            int deferStartPos = frame.DeferStack.Pop();

            // 保存当前 IP
            int savedIP = frame.IP;

            // 跳转到 defer 块的起始位置
            frame.IP = deferStartPos;

            // 执行 defer 块（直到遇到 ReturnVoid）
            while (frame.IP < frame.Function.Instructions.Count)
            {
                var instruction = frame.Function.Instructions[frame.IP];
                frame.IP++;

                // 执行指令
                ExecuteInstruction(instruction, frame);

                // 如果遇到 ReturnVoid，说明 defer 块执行完毕
                if (instruction.OpCode == OpCode.ReturnVoid)
                {
                    break;
                }
            }

            // 恢复 IP（继续执行原来的代码）
            frame.IP = savedIP;
        }
    }

    /// <summary>
    /// 处理异常 - 查找并执行匹配的异常处理器
    /// </summary>
    /// <returns>如果找到并处理了异常返回true，否则返回false</returns>
    private bool HandleException(Exception exception, CallFrame frame, FunctionMetadata function)
    {
        // 提取真实的异常对象
        object? exceptionValue = exception;
        if (exception is VmException vmException)
        {
            exceptionValue = vmException.Value;
        }

        // 获取异常发生时的指令位置（已经+1了，所以要-1）
        int exceptionIP = frame.IP - 1;

        // 遍历异常表，查找匹配的处理器
        foreach (var entry in function.ExceptionTable)
        {
            // 检查异常是否发生在这个try块中
            if (entry.IsInTryBlock(exceptionIP))
            {
                // 检查异常类型是否匹配
                if (IsExceptionTypeMatch(exceptionValue, entry.ExceptionType))
                {
                    // 将异常对象压入栈
                    _stack.Push(exceptionValue);

                    // 如果有catch块，跳转到catch块
                    if (entry.CatchStart >= 0)
                    {
                        frame.IP = entry.CatchStart;
                        return true;
                    }
                    // 如果没有catch块但有finally块，跳转到finally块
                    else if (entry.FinallyStart >= 0)
                    {
                        frame.IP = entry.FinallyStart;
                        return true;
                    }
                }
            }
        }

        // 没有找到匹配的处理器
        return false;
    }

    /// <summary>
    /// 检查异常类型是否匹配
    /// </summary>
    private bool IsExceptionTypeMatch(object? exceptionValue, string? expectedType)
    {
        // 如果没有指定异常类型，匹配所有异常
        if (string.IsNullOrEmpty(expectedType))
            return true;

        if (exceptionValue == null)
            return false;

        // 1. 检查 BytecodeObjectInstance
        if (exceptionValue is BytecodeObjectInstance instance)
        {
            // 检查类名
            if (instance.ClassName == expectedType)
                return true;

            // 检查继承关系
            var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == instance.ClassName);
            while (classMetadata != null && !string.IsNullOrEmpty(classMetadata.BaseClassName))
            {
                if (classMetadata.BaseClassName == expectedType)
                    return true;

                classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == classMetadata.BaseClassName);
            }

            // 检查接口实现
            // TODO: 这里需要从 ClassMetadata 中获取接口信息，或者 BytecodeObjectInstance 应该存储接口信息
            // 假设 instance.Interfaces 包含所有实现的接口
            if (instance.Interfaces.Contains(expectedType))
                return true;

            return false;
        }

        // 2. 检查 .NET 异常类型
        if (exceptionValue is Exception ex)
        {
            // 获取异常的类型名称
            string actualType = ex.GetType().Name;

            // 精确匹配
            if (actualType == expectedType)
                return true;

            // 匹配完整类型名称
            if (ex.GetType().FullName == expectedType)
                return true;

            // 检查继承关系
            Type? currentType = ex.GetType();
            while (currentType != null)
            {
                if (currentType.Name == expectedType || currentType.FullName == expectedType)
                    return true;
                currentType = currentType.BaseType;
            }

            // 特殊情况：如果是 "Exception"，匹配所有 Exception
            if (expectedType == "Exception")
                return true;
        }

        // 3. 字符串异常匹配 (如果 expectedType 是 "string" 或具体值?)
        // Old8Lang 中通常不建议用字符串作为异常类型，但为了兼容性
        if (exceptionValue is string str && expectedType == "string")
            return true;

        return false;
    }

    private string ToString(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return s;

        // 处理 LangValueType（使用 ToDisplayString 而不是 ToString）
        if (value is LangValueType langValue)
        {
            return langValue.ToDisplayString();
        }

        // 处理数组
        if (value is Array array)
        {
            var items = (from object? item in array select ToString(item)).ToList();

            return "[" + string.Join(", ", items) + "]";
        }

        // 处理列表
        if (value is IList list)
        {
            var items = (from object? item in list select ToString(item)).ToList();

            return "{" + string.Join(", ", items) + "}";
        }

        // 处理字典
        if (value is IDictionary dict)
        {
            var items = new List<string>();
            foreach (var key in dict.Keys)
            {
                items.Add($"{ToString(key)}: {ToString(dict[key])}");
            }

            return "{" + string.Join(", ", items) + "}";
        }

        return value.ToString() ?? "";
    }

    /// <summary>
    /// 调用原生函数
    /// </summary>
    private object? CallNativeFunction(string funcName, object?[] args)
    {
        // 首先尝试从全局函数注册表中查找
        var globalFunction = GlobalFunctionRegistry.Instance.TryGetFunction(funcName);
        if (globalFunction != null)
        {
            try
            {
                return globalFunction.ExecuteInVM(args);
            }
            catch (Exception ex)
            {
                throw new Exception($"调用全局函数 {funcName} 时发生错误: {ex.Message}", ex);
            }
        }

        // 处理特殊的辅助函数（不在全局函数注册表中）
        switch (funcName)
        {
            case "System.String::Concat":
            {
                // 字符串拼接
                if (args.Length > 0 && args[0] is object[] array)
                {
                    return string.Concat(array.Select(ToString));
                }

                return string.Concat(args.Select(ToString));
            }

            case "Spawn":
            case "spawn":
                // Spawn 函数在虚拟机模式下的特殊处理
                // args[0] 是函数索引(int), args[1..] 是函数参数
                if (args.Length > 0 && args[0] is int funcIndex)
                {
                    // 获取函数元数据
                    var function = _bytecodeFile.Functions[funcIndex];

                    // 提取函数参数
                    var funcArgs = new object?[args.Length - 1];
                    Array.Copy(args, 1, funcArgs, 0, args.Length - 1);

                    // 创建线程
                    var threadId = Concurrency.ResourceManager.CreateThread(() =>
                    {
                        // 在新线程中执行函数
                        CallFunction(function, funcArgs);
                    });

                    // 自动启动线程
                    Concurrency.ResourceManager.StartThread(threadId);

                    // 返回 VMThreadLangValue
                    return new VMThreadLangValue(threadId);
                }

                throw new Exception("Spawn 函数需要至少一个参数（函数引用）");

            case "ToStr":
                return args.Length > 0 ? ToString(args[0]) : "";

            case "ToInt":
                if (args.Length > 0 && int.TryParse(ToString(args[0]), out int result))
                {
                    return result;
                }

                return 0;

            case "ToDouble":
                if (args.Length > 0 && double.TryParse(ToString(args[0]), out double dresult))
                {
                    return dresult;
                }

                return 0.0;

            case "CheckRange":
                // 参数: value, start, end, includeStart, includeEnd
                if (args.Length >= 5)
                {
                    double value = Convert.ToDouble(args[0]);
                    double start = Convert.ToDouble(args[1]);
                    double end = Convert.ToDouble(args[2]);
                    bool includeStart = Convert.ToBoolean(args[3]);
                    bool includeEnd = Convert.ToBoolean(args[4]);

                    bool inRange = true;
                    if (includeStart)
                        inRange &= value >= start;
                    else
                        inRange &= value > start;

                    if (includeEnd)
                        inRange &= value <= end;
                    else
                        inRange &= value < end;

                    return inRange;
                }

                return false;

            case "FlattenTuple":
                // 展平元组为列表
                if (args.Length > 0 && args[0] is TupleLangValue tuple)
                {
                    return FlattenTupleHelper(tuple);
                }

                return new List<object?>();

            case "GetCount":
                // 获取集合元素数量
                if (args.Length > 0)
                {
                    return args[0] switch
                    {
                        string str => str.Length,
                        Array array => array.Length,
                        IList list => list.Count,
                        _ => 0
                    };
                }

                return 0;

            case "ResourceManagerTryDispose":
                if (args.Length > 0)
                {
                    int resourceId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.TryDispose(resourceId);
                }

                return null;

            default:
                throw new Exception($"未知的原生函数: {funcName}");
        }
    }

    /// <summary>
    /// 对数组执行切片操作
    /// </summary>
    private object SliceArray(Array array, int start, int end, int step)
    {
        var length = array.Length;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new List<object?>();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(array.GetValue(i));
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(array.GetValue(i));
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 对列表执行切片操作
    /// </summary>
    private object? SliceList(IList list, int start, int end, int step)
    {
        var length = list.Count;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new List<object?>();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Add(list[i]);
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Add(list[i]);
            }
        }

        return result;
    }

    /// <summary>
    /// 对字符串执行切片操作
    /// </summary>
    private string SliceString(string str, int start, int end, int step)
    {
        var length = str.Length;

        // 处理负索引
        if (start < 0) start = length + start;
        if (end < 0) end = length + end;

        // 边界检查
        start = Math.Max(0, Math.Min(start, length));
        end = Math.Min(length, end);

        var result = new System.Text.StringBuilder();

        if (step > 0)
        {
            for (int i = start; i < end; i += step)
            {
                result.Append(str[i]);
            }
        }
        else if (step < 0)
        {
            for (int i = start; i > end; i += step)
            {
                result.Append(str[i]);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// 重新排列参数以匹配函数参数定义(支持命名参数)
    /// </summary>
    /// <param name="function">函数元数据</param>
    /// <param name="positionalArgs">位置参数</param>
    /// <param name="namedArgNames">命名参数名称数组</param>
    /// <param name="namedArgValues">命名参数值数组</param>
    /// <returns>按函数参数定义顺序排列的参数数组</returns>
    private object?[] ArrangeArgumentsWithNamed(FunctionMetadata function, object?[] positionalArgs,
        string[] namedArgNames, object?[] namedArgValues)
    {
        int paramCount = function.Parameters.Count;
        var args = new object?[paramCount];
        var filled = new bool[paramCount]; // 跟踪哪些参数位置已被填充

        // 首先填充位置参数
        for (int i = 0; i < positionalArgs.Length; i++)
        {
            if (i >= paramCount)
            {
                throw new Exception($"函数 {function.Name} 期望 {paramCount} 个参数，但提供了过多的参数");
            }

            args[i] = positionalArgs[i];
            filled[i] = true;
        }

        // 然后根据命名参数填充剩余位置
        for (int i = 0; i < namedArgNames.Length; i++)
        {
            string paramName = namedArgNames[i];
            object? paramValue = namedArgValues[i];

            // 查找参数在函数参数列表中的位置
            int paramIndex = function.Parameters.IndexOf(paramName);
            if (paramIndex == -1)
            {
                throw new Exception($"函数 {function.Name} 没有名为 '{paramName}' 的参数");
            }

            // 检查该位置是否已被位置参数占用
            if (filled[paramIndex])
            {
                throw new Exception($"参数 '{paramName}' 已通过位置参数提供");
            }

            args[paramIndex] = paramValue;
            filled[paramIndex] = true;
        }

        // 检查是否所有参数都已提供，如果没有则使用默认值
        for (int i = 0; i < paramCount; i++)
        {
            if (!filled[i])
            {
                // 参数未提供，检查是否有默认值
                if (i < function.DefaultValues.Count && function.DefaultValues[i] != null)
                {
                    // 使用默认值
                    args[i] = function.DefaultValues[i];
                    filled[i] = true;
                }
                else
                {
                    // 没有默认值，抛出错误
                    throw new Exception($"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                }
            }
        }

        return args;
    }

    // ===== Task 管理 =====

    /// <summary>
    /// 注册 Task 并返回 ID
    /// </summary>
    private int RegisterTask(TaskLangValue task)
    {
        int taskId = _nextTaskId++;
        _tasks[taskId] = task;
        return taskId;
    }

    /// <summary>
    /// 获取 Task
    /// </summary>
    private TaskLangValue GetTask(int taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var task))
        {
            throw new Exception($"Task ID {taskId} 不存在");
        }

        return task;
    }

    /// <summary>
    /// 辅助方法：将 object? 转换为 LangValueType
    /// </summary>
    private LangValueType ConvertToLangValue(object? value)
    {
        if (value == null) return new VoidLangValue();
        if (value is LangValueType langValue) return langValue;
        if (value is int intValue) return new IntLangValue(intValue);
        if (value is double doubleValue) return new DoubleLangValue(doubleValue);
        if (value is string stringValue) return new StringLangValue(stringValue);
        if (value is bool boolValue) return new BoolLangValue(boolValue);
        return new VoidLangValue();
    }

    /// <summary>
    /// 展平元组为列表（用于 match 表达式的元组解构）
    /// 例如：((1, 2), 3) -> [1, 2, 3]
    /// </summary>
    private List<object?> FlattenTupleHelper(TupleLangValue tuple)
    {
        return tuple.GetItems().Cast<object?>().ToList();
    }

    /// <summary>
    /// 调用类型的扩展方法或实例方法（类似于解释器模式中的 FromClassToResult）
    /// </summary>
    /// <param name="obj">要调用方法的对象</param>
    /// <param name="methodName">方法名</param>
    /// <param name="args">方法参数</param>
    /// <returns>方法返回值</returns>
    private object? InvokeTypeMethod(object obj, string methodName, object?[] args)
    {
        if (obj == null)
        {
            throw new Exception($"无法在 null 对象上调用方法 {methodName}");
        }

        Type? extensionType = null;
        System.Reflection.MethodInfo? method = null;

        // 对于 C# 原生类型，查找对应的扩展方法类
        if (obj is string)
        {
            extensionType = typeof(StringExtensions);
        }
        else if (obj is object[] && obj.GetType() == typeof(object[]))
        {
            extensionType = typeof(ArrayExtensions);
        }
        else if (obj is List<object?>)
        {
            extensionType = typeof(ListExtensions);
        }
        else if (obj is Dictionary<object, object?>)
        {
            extensionType = typeof(DictionaryExtensions);
        }
        // 对于 Old8Lang 类型，查找对应的扩展方法类
        else if (obj is DictionaryLangValue)
        {
            extensionType = typeof(DictionaryValueFuncStatic);
        }
        else if (obj is ListLangValue)
        {
            extensionType = typeof(ListValueFuncStatic);
        }
        else if (obj is TaskLangValue)
        {
            extensionType = typeof(TaskValueFuncStatic);
        }
        else if (obj is ThreadLangValue)
        {
            extensionType = typeof(ThreadValueFuncStatic);
        }
        else if (obj is StringLangValue)
        {
            extensionType = typeof(StringValueFuncStatic);
        }
        else if (obj is TupleLangValue)
        {
            extensionType = typeof(TupleValueFuncStatic);
        }
        else if (obj is ArrayLangValue)
        {
            extensionType = typeof(ArrayValueFuncStatic);
        }
        else if (obj is CharLangValue)
        {
            extensionType = typeof(CharValueFuncStatic);
        }

        // 如果找到扩展类型，尝试查找扩展方法
        if (extensionType != null)
        {
            var allMethods = extensionType.GetMethods().Where(x => x.Name == methodName).ToArray();
            if (allMethods.Length > 0)
            {
                // 预期参数数量 = 传入参数数量 + 1 (扩展方法的第一个参数是对象本身)
                var expectedParamCount = args.Length + 1;

                // 首先查找精确匹配的参数数量
                method = allMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount);

                // 如果没找到，查找有可选参数的方法
                if (method == null)
                {
                    method = allMethods.FirstOrDefault(x =>
                    {
                        var parameters = x.GetParameters();
                        if (parameters.Length < expectedParamCount) return false;

                        // 检查除了第一个参数（对象本身）之外，剩余的参数是否都是可选的
                        for (int i = expectedParamCount; i < parameters.Length; i++)
                        {
                            if (!parameters[i].IsOptional && !parameters[i].HasDefaultValue)
                                return false;
                        }

                        return true;
                    });
                }

                // 如果还是没找到，使用第一个方法
                method ??= allMethods[0];
            }
        }

        // 如果没有找到扩展方法，尝试在类型本身上查找实例方法
        if (method == null)
        {
            var objType = obj.GetType();

            // 特殊处理：将 ToStr 映射到 ToString
            var actualMethodName = methodName == "ToStr" ? "ToString" : methodName;

            var allInstanceMethods = objType.GetMethods().Where(x => x.Name == actualMethodName).ToArray();
            if (allInstanceMethods.Length > 0)
            {
                // 对于实例方法，预期参数数量 = 传入参数数量
                var expectedParamCount = args.Length;
                method = allInstanceMethods.FirstOrDefault(x => x.GetParameters().Length == expectedParamCount)
                         ?? allInstanceMethods[0];
            }
        }

        // 如果还是找不到，尝试 ValueTypeFuncStatic
        if (method == null)
        {
            var valueTypeFuncStatic = typeof(ValueTypeFuncStatic);
            method = valueTypeFuncStatic.GetMethod(methodName);
        }

        // 如果找不到方法，抛出异常
        if (method == null)
        {
            throw new Exception($"类型 {obj.GetType().Name} 没有方法 {methodName}");
        }

        // 准备方法调用参数
        var parameters = method.GetParameters();
        var invokeArgs = new List<object?>();

        // 对于静态方法（扩展方法），第一个参数是对象本身
        if (method.IsStatic && parameters.Length > 0)
        {
            invokeArgs.Add(obj);
        }

        // 添加传入的参数
        invokeArgs.AddRange(args);

        // 补充缺失的可选参数
        if (invokeArgs.Count < parameters.Length)
        {
            for (int i = invokeArgs.Count; i < parameters.Length; i++)
            {
                if (parameters[i].IsOptional || parameters[i].HasDefaultValue)
                {
                    invokeArgs.Add(parameters[i].DefaultValue);
                }
            }
        }

        // 调用方法
        object? invokeInstance = method.IsStatic ? null : obj;
        return method.Invoke(invokeInstance, invokeArgs.ToArray());
    }

    // === 模块加载方法 ===

    /// <summary>
    /// 加载模块
    /// </summary>
    private void LoadModule(string moduleName)
    {
        // 检查模块是否已加载
        if (_moduleRegistry.IsModuleLoaded(moduleName))
        {
            return; // 模块已加载，直接返回
        }

        // 检测循环依赖
        if (!_moduleRegistry.MarkModuleLoading(moduleName))
        {
            throw new Exception($"检测到循环依赖：模块 '{moduleName}' 正在加载中");
        }

        try
        {
            // 加载并编译模块
            var moduleBytecode = _moduleLoader.LoadModule(moduleName);

            // 创建模块的全局变量空间
            var moduleGlobals = new Dictionary<string, object?>();
            foreach (var globalVar in moduleBytecode.GlobalVariables)
            {
                moduleGlobals[globalVar] = null;
            }

            // 执行模块的初始化代码（如果有入口点）
            if (moduleBytecode.EntryPointIndex >= 0)
            {
                // 创建临时虚拟机执行模块初始化
                var moduleVM = new VirtualMachine(moduleBytecode, _baseDirectory);

                // 复制模块注册表（避免重复加载依赖）
                foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                {
                    var loadedModule = _moduleRegistry.GetModule(loadedModuleName);
                    if (loadedModule != null)
                    {
                        moduleVM._moduleRegistry.RegisterModule(
                            loadedModuleName,
                            loadedModule.BytecodeFile,
                            loadedModule.Globals
                        );
                    }
                }

                // 执行模块初始化
                moduleVM.Execute();

                // 获取模块的全局变量
                moduleGlobals = moduleVM._globals;

                // 传递性导入：将模块VM加载的所有依赖模块也注册到当前VM的模块注册表中
                foreach (var depModuleName in moduleVM._moduleRegistry.GetLoadedModuleNames())
                {
                    // 跳过当前正在加载的模块自己
                    if (depModuleName == moduleName)
                    {
                        continue;
                    }

                    // 如果当前VM还没有加载这个依赖模块，则注册它
                    if (!_moduleRegistry.IsModuleLoaded(depModuleName))
                    {
                        var depModule = moduleVM._moduleRegistry.GetModule(depModuleName);
                        if (depModule != null)
                        {
                            _moduleRegistry.RegisterModule(
                                depModuleName,
                                depModule.BytecodeFile,
                                depModule.Globals
                            );
                        }
                    }
                }
            }

            // 注册模块
            _moduleRegistry.RegisterModule(moduleName, moduleBytecode, moduleGlobals);
        }
        catch (Exception ex)
        {
            throw new Exception($"加载模块 '{moduleName}' 失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建对象实例（用于从模块导入的类的实例化）
    /// </summary>
    private BytecodeObjectInstance CreateObjectInstance(ClassMetadata classMetadata, object?[] constructorArgs)
    {
        // 创建对象实例
        var obj = new BytecodeObjectInstance(classMetadata.Name);

        // 初始化所有字段为默认值（包括父类字段）
        var allFields = new List<FieldMetadata>();
        var currentClass = classMetadata;
        while (currentClass != null)
        {
            allFields.AddRange(currentClass.Fields);

            // 查找父类（首先从当前字节码文件，然后从模块）
            if (!string.IsNullOrEmpty(currentClass.BaseClassName))
            {
                currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                if (currentClass == null)
                {
                    // 从模块中查找父类
                    foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
                    {
                        try
                        {
                            var symbol =
                                _moduleRegistry.GetModuleSymbol(loadedModuleName, currentClass?.BaseClassName ?? "");
                            if (symbol is ClassMetadata baseClass)
                            {
                                currentClass = baseClass;
                                break;
                            }
                        }
                        catch
                        {
                            // 继续查找
                        }
                    }
                }
            }
            else
            {
                break;
            }
        }

        // 初始化所有字段
        foreach (var field in allFields)
        {
            if (!obj.Fields.ContainsKey(field.Name))
            {
                obj.Fields[field.Name] = null;
            }
        }

        // 查找并调用构造函数（init方法）
        var initMethod = classMetadata.Methods.FirstOrDefault(m => m.Name == "init");
        if (initMethod != null)
        {
            // 准备方法调用参数：第一个参数是 this（对象本身）
            var methodArgs = new object?[constructorArgs.Length + 1];
            methodArgs[0] = obj;
            Array.Copy(constructorArgs, 0, methodArgs, 1, constructorArgs.Length);

            // 调用构造函数
            CallFunction(initMethod.Function, methodArgs);
        }

        return obj;
    }

    /// <summary>
    /// 执行函数并获取结果（用于异步调用）
    /// </summary>
    public object? ExecuteFunctionAndGetResult(FunctionMetadata function, object?[] args)
    {
        CallFunction(function, args);
        return _stack.Count > 0 ? _stack.Pop() : null;
    }
}

/// <summary>
/// 虚拟机异常包装类
/// 用于在C#异常机制中传递Old8Lang的异常对象
/// </summary>
public class VmException : Exception
{
    public object? Value { get; }

    public VmException(object? value) : base(GetMessage(value))
    {
        Value = value;
    }

    private static string GetMessage(object? value)
    {
        if (value == null) return "null";
        if (value is LangValueType langValue) return langValue.ToDisplayString();
        return value.ToString() ?? "";
    }
}

/// <summary>
/// 对象相等性比较器 - 用于 GroupBy 操作的键比较
/// </summary>
internal class ObjectEqualityComparer : IEqualityComparer<object>
{
    public new bool Equals(object? x, object? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;

        // 使用对象的 Equals 方法进行比较
        return x.Equals(y);
    }

    public int GetHashCode(object obj)
    {
        return obj?.GetHashCode() ?? 0;
    }
}

/// <summary>
/// 异常处理器 - 跟踪try-catch-finally块
/// </summary>
internal class ExceptionHandler
{
    public int CatchIP { get; set; }
    public int FinallyIP { get; set; }
    public int EndIP { get; set; }
    public bool InFinally { get; set; }
}