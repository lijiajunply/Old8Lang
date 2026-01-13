using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;

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

    public VirtualMachine(BytecodeFile bytecodeFile)
    {
        _bytecodeFile = bytecodeFile ?? throw new ArgumentNullException(nameof(bytecodeFile));

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
        return _globals.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// 调用函数
    /// </summary>
    private void CallFunction(FunctionMetadata function, object?[] arguments)
    {
        // 创建调用帧
        var frame = new CallFrame(function, function.LocalCount)
        {
            Arguments = arguments
        };

        // 将参数复制到局部变量槽(前N个局部变量是参数)
        for (int i = 0; i < arguments.Length && i < function.LocalCount; i++)
        {
            frame.Locals[i] = arguments[i];
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
                // 从栈中弹出 Task ID
                var taskIdObj = _stack.Pop();
                if (taskIdObj is not int taskId)
                {
                    throw new Exception($"Await 指令期望 Task ID (int)，但得到 {taskIdObj?.GetType().Name ?? "null"}");
                }

                // 获取 Task
                var taskLangValue = GetTask(taskId);

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

                    // 查找函数
                    var function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);
                    if (function == null)
                    {
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
                    var function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);
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

            case OpCode.CallAsync:
            {
                var operands = (object[])instruction.Operand!;
                int argCount = (int)operands[0];
                string funcName = (string)(operands.Length == 2 ? operands[1] : operands[2]);

                // 从栈中弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 查找异步函数
                var function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName && f.IsAsync);
                if (function == null)
                {
                    throw new Exception($"未定义的异步函数: {funcName}");
                }

                // 创建 Task 并异步执行
                var task = Task.Run(() =>
                {
                    var asyncVm = new VirtualMachine(_bytecodeFile);
                    asyncVm.CallFunction(function, args);
                    return asyncVm._stack.Count > 0 ? asyncVm._stack.Pop() : null;
                });

                var taskLangValue = new TaskLangValue(
                    task.ContinueWith(t => ConvertToLangValue(t.Result)),
                    CancellationToken.None
                );

                int taskId = RegisterTask(taskLangValue);
                _stack.Push(taskId);
            }
                break;

            case OpCode.Await:
            {
                // 从栈中弹出 Task ID
                var taskIdObj = _stack.Pop();
                if (taskIdObj is not int taskId)
                {
                    throw new Exception($"Await 指令期望 Task ID (int)，但得到 {taskIdObj?.GetType().Name ?? "null"}");
                }

                // 获取 Task
                var taskLangValue = GetTask(taskId);

                // 同步等待 Task 完成
                var result = taskLangValue.Await();

                // 将结果压入栈
                _stack.Push(result);
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

                _stack.Push(new Tuple<object?, object?>(elements[0], elements[1]));
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
                // 栈顶: step, end, start
                var step = _stack.Pop();
                var end = _stack.Pop();
                var start = _stack.Pop();

                // 转换为整数
                int startInt = Convert.ToInt32(start);
                int endInt = Convert.ToInt32(end);
                int stepInt = step != null ? Convert.ToInt32(step) : 1;

                // 创建范围对象 (使用List<int>表示范围)
                var range = new List<int>();
                if (stepInt > 0)
                {
                    for (int i = startInt; i < endInt; i += stepInt)
                    {
                        range.Add(i);
                    }
                }
                else if (stepInt < 0)
                {
                    for (int i = startInt; i > endInt; i += stepInt)
                    {
                        range.Add(i);
                    }
                }
                else
                {
                    throw new Exception("范围的步长不能为0");
                }

                _stack.Push(range);
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
                else
                {
                    throw new Exception($"无法对类型 {collection?.GetType().Name} 执行切片操作");
                }
            }
                break;

            // === 类型操作 ===
            case OpCode.Cast:
            {
                var targetTypeName = (string)instruction.Operand!;
                var value = _stack.Pop();

                // 执行类型转换
                object? convertedValue = targetTypeName.ToLower() switch
                {
                    "int" => Convert.ToInt32(value),
                    "double" => Convert.ToDouble(value),
                    "string" => value?.ToString() ?? "",
                    "bool" => Convert.ToBoolean(value),
                    "char" => Convert.ToChar(value),
                    _ => value // 其他类型直接返回原值
                };

                _stack.Push(convertedValue);
            }
                break;

            case OpCode.IsType:
            {
                var targetTypeName = (string)instruction.Operand!;
                var value = _stack.Pop();

                // 检查类型
                bool isType = targetTypeName.ToLower() switch
                {
                    "int" => value is int,
                    "double" => value is double,
                    "string" => value is string,
                    "bool" => value is bool,
                    "char" => value is char,
                    "array" => value is Array,
                    "list" => value is IList,
                    "dict" => value is IDictionary,
                    "null" => value == null,
                    _ => false
                };

                _stack.Push(isType);
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

            // === 异步支持 ===


            case OpCode.Yield:
            {
                // Yield操作：生成器返回一个值并暂停执行
                // 1. 从栈中弹出要yield的值
                var yieldValue = _stack.Pop();

                // 2. 获取当前生成器状态（从frame的GeneratorId）
                if (frame.GeneratorId.HasValue)
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

            case OpCode.NewTask:
            {
                // NewTask操作：创建一个新的异步任务
                // 栈布局：[函数索引, 参数数量, arg1, arg2, ...]

                var operands = (object[])instruction.Operand!;
                int argCount = (int)operands[0];
                string funcName = (string)operands[1];

                // 从栈中弹出参数
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 查找函数
                var function = _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);
                if (function == null)
                {
                    throw new Exception($"未定义的函数: {funcName}");
                }

                // 创建异步任务
                var task = Task.Run(() =>
                {
                    // 在新线程中执行函数
                    CallFunction(function, args);

                    // 返回栈顶的值作为结果
                    var result = _stack.Count > 0 ? _stack.Pop() : null;
                    return result != null ? LangValueType.ObjToValue(result) : new NullLangValue();
                });

                // 将Task包装为TaskLangValue并压入栈
                var taskValue = new TaskLangValue(task);
                _stack.Push(taskValue);
            }
                break;

            // === 异常处理 ===
            case OpCode.Throw:
            {
                var exceptionValue = _stack.Pop();
                var message = ToString(exceptionValue);
                throw new Exception(message);
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
                if (classMetadata == null)
                {
                    throw new Exception($"未找到类定义: {className}");
                }

                // 创建对象实例
                var obj = new BytecodeObjectInstance(className);

                // 初始化字段为默认值
                foreach (var field in classMetadata.Fields)
                {
                    obj.Fields[field.Name] = null;
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
                    if (classMetadata == null)
                    {
                        throw new Exception($"未找到类定义: {bytecodeObj.ClassName}");
                    }

                    // 在类的方法列表中查找方法
                    var methodMetadata = classMetadata.Methods.FirstOrDefault(m => m.Name == methodName);
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
                    // 原生 C# 对象，使用反射调用方法
                    var objType = obj.GetType();
                    var method = objType.GetMethod(methodName);

                    if (method == null)
                    {
                        throw new Exception($"类型 {objType.Name} 没有方法 {methodName}");
                    }

                    var result = method.Invoke(obj, args);

                    // 如果方法有返回值，压入栈
                    if (method.ReturnType != typeof(void))
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

            default:
                throw new Exception($"未实现的操作码: {instruction.OpCode}");
        }
    }

    // ===== 辅助方法 =====

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
        // 获取异常发生时的指令位置（已经+1了，所以要-1）
        int exceptionIP = frame.IP - 1;

        // 遍历异常表，查找匹配的处理器
        foreach (var entry in function.ExceptionTable)
        {
            // 检查异常是否发生在这个try块中
            if (entry.IsInTryBlock(exceptionIP))
            {
                // 检查异常类型是否匹配
                if (IsExceptionTypeMatch(exception, entry.ExceptionType))
                {
                    // 将异常对象压入栈
                    _stack.Push(exception.Message);

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
    private bool IsExceptionTypeMatch(Exception exception, string? expectedType)
    {
        // 如果没有指定异常类型，匹配所有异常
        if (string.IsNullOrEmpty(expectedType))
            return true;

        // 获取异常的类型名称
        string actualType = exception.GetType().Name;

        // 精确匹配
        if (actualType == expectedType)
            return true;

        // 匹配完整类型名称
        if (exception.GetType().FullName == expectedType)
            return true;

        // 检查继承关系
        Type? currentType = exception.GetType();
        while (currentType != null)
        {
            if (currentType.Name == expectedType || currentType.FullName == expectedType)
                return true;
            currentType = currentType.BaseType;
        }

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
            var items = (from DictionaryEntry entry in dict select $"{ToString(entry.Key)}: {ToString(entry.Value)}")
                .ToList();
            return "{" + string.Join(", ", items) + "}";
        }

        return value.ToString() ?? "";
    }

    /// <summary>
    /// 调用原生函数
    /// </summary>
    private object? CallNativeFunction(string funcName, object?[] args)
    {
        switch (funcName)
        {
            case "PrintLine":
                if (args.Length > 0)
                {
                    Console.WriteLine(ToString(args[0]));
                }
                else
                {
                    Console.WriteLine();
                }

                return null;

            case "Print":
                if (args.Length > 0)
                {
                    Console.Write(ToString(args[0]));
                }

                return null;

            case "ReadLine":
                return Console.ReadLine();

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

            // === Mutex函数 ===
            case "MutexCreate":
                return Concurrency.ResourceManager.CreateMutex();

            case "MutexLock":
                if (args.Length > 0)
                {
                    int mutexId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.LockMutex(mutexId);
                }

                return null;

            case "MutexUnlock":
                if (args.Length > 0)
                {
                    int mutexId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.UnlockMutex(mutexId);
                }

                return null;

            case "MutexDispose":
                if (args.Length > 0)
                {
                    int mutexId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.DisposeMutex(mutexId);
                }

                return null;

            // === Channel函数 ===
            case "ChannelCreate":
                return Concurrency.ResourceManager.CreateChannel();

            case "ChannelSend":
                if (args.Length >= 2)
                {
                    int channelId = Convert.ToInt32(args[0]);
                    object? value = args[1];
                    Concurrency.ResourceManager.SendChannel(channelId, value);
                }

                return null;

            case "ChannelReceive":
                if (args.Length > 0)
                {
                    int channelId = Convert.ToInt32(args[0]);
                    return Concurrency.ResourceManager.ReceiveChannel(channelId);
                }

                return null;

            case "ChannelClose":
                if (args.Length > 0)
                {
                    int channelId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.CloseChannel(channelId);
                }

                return null;

            // === Semaphore函数 ===
            case "SemaphoreCreate":
                if (args.Length >= 2)
                {
                    int initialCount = Convert.ToInt32(args[0]);
                    int maxCount = Convert.ToInt32(args[1]);
                    return Concurrency.ResourceManager.CreateSemaphore(initialCount, maxCount);
                }

                return 0;

            case "SemaphoreAcquire":
                if (args.Length > 0)
                {
                    int semaphoreId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.AcquireSemaphore(semaphoreId);
                }

                return null;

            case "SemaphoreRelease":
                if (args.Length > 0)
                {
                    int semaphoreId = Convert.ToInt32(args[0]);
                    Concurrency.ResourceManager.ReleaseSemaphore(semaphoreId);
                }

                return null;

            // === Match 表达式辅助函数 ===
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
        var result = new List<object?>();

        var first = tuple.Value.Item1;
        var second = tuple.Value.Item2;

        // 递归展平第一个元素
        if (first is TupleLangValue firstTuple)
        {
            result.AddRange(FlattenTupleHelper(firstTuple));
        }
        else if (first is not NullLangValue) // 排除单元素元组的 null 占位符
        {
            result.Add(first);
        }

        // 递归展平第二个元素
        if (second is TupleLangValue secondTuple)
        {
            result.AddRange(FlattenTupleHelper(secondTuple));
        }
        else if (second is not NullLangValue) // 排除单元素元组的 null 占位符
        {
            result.Add(second);
        }

        return result;
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