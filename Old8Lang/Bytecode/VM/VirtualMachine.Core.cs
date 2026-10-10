using Old8Lang.AST.Expression.Intermediates;
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
using System.Collections.Concurrent;
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

    // CallFrame 对象池：复用帧实例，减少 GC 压力（每线程独立，最多缓存 32 个）
    private readonly ThreadLocal<Stack<CallFrame>> _threadFramePool = new(() => new Stack<CallFrame>(32));

    // 线程安全的全局变量字典
    private readonly ConcurrentDictionary<string, object?> _globals;
    private readonly BytecodeFile _bytecodeFile;
    private readonly Dictionary<string, FunctionMetadata> _functionByName;
    private readonly Dictionary<string, ClassMetadata> _classByName;
    private readonly ConcurrentDictionary<(string ModuleName, string SymbolName), object> _moduleSymbolCache;

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
        : this(bytecodeFile, baseDirectory, null, null, null, null, initializeRuntimeMetadata: true)
    {
    }

    private VirtualMachine(
        BytecodeFile bytecodeFile,
        string? baseDirectory,
        ConcurrentDictionary<string, object?>? sharedGlobals,
        Dictionary<string, FunctionMetadata>? sharedFunctionByName,
        Dictionary<string, ClassMetadata>? sharedClassByName,
        ConcurrentDictionary<(string ModuleName, string SymbolName), object>? sharedModuleSymbolCache,
        bool initializeRuntimeMetadata)
    {
        _bytecodeFile = bytecodeFile ?? throw new ArgumentNullException(nameof(bytecodeFile));
        _baseDirectory = baseDirectory ?? Directory.GetCurrentDirectory();
        _moduleLoader = new ModuleLoader(_baseDirectory);
        _globals = sharedGlobals ?? new ConcurrentDictionary<string, object?>();
        _functionByName = sharedFunctionByName ?? new Dictionary<string, FunctionMetadata>(StringComparer.Ordinal);
        _classByName = sharedClassByName ?? new Dictionary<string, ClassMetadata>(StringComparer.Ordinal);
        _moduleSymbolCache = sharedModuleSymbolCache ?? new ConcurrentDictionary<(string ModuleName, string SymbolName), object>();

        // 初始化全局函数注册表
        GlobalFunctionInitializer.EnsureInitialized();

        // 初始化实例方法注册表
        InstanceMethodInitializer.EnsureInitialized();

        if (initializeRuntimeMetadata)
        {
            foreach (var globalVar in _bytecodeFile.GlobalVariables)
            {
                _globals[globalVar] = null;
            }

            foreach (var function in _bytecodeFile.Functions)
            {
                _functionByName.TryAdd(function.Name, function);
            }

            foreach (var classMetadata in _bytecodeFile.Classes)
            {
                _globals[classMetadata.Name] = classMetadata;
                _classByName.TryAdd(classMetadata.Name, classMetadata);

                foreach (var staticField in classMetadata.StaticFields)
                {
                    if (staticField.IsDefaultNull)
                    {
                        classMetadata.StaticFieldValues[staticField.Name] = null;
                    }
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

            RegisterExtensionMethods();
        }
    }

    private VirtualMachine CreateWorkerVirtualMachine()
    {
        return new VirtualMachine(
            _bytecodeFile,
            _baseDirectory,
            _globals,
            _functionByName,
            _classByName,
            _moduleSymbolCache,
            initializeRuntimeMetadata: false);
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

    /// <summary>
    /// 从线程本地池租借 CallFrame（如池为空则新建），并用指定参数初始化
    /// </summary>
    private CallFrame RentCallFrame(FunctionMetadata function, object?[] locals, int localCount, bool usesPooledLocals)
    {
        var pool = _threadFramePool.Value!;
        if (pool.Count > 0)
        {
            var frame = pool.Pop();
            frame.ReinitializeFromPool(function, locals, localCount, usesPooledLocals);
            return frame;
        }

        return new CallFrame(function, locals, localCount, usesPooledLocals);
    }

    /// <summary>
    /// 将 CallFrame 归还到线程本地池（上限 32 个，超出则丢弃）
    /// </summary>
    private void ReturnCallFrame(CallFrame frame)
    {
        frame.ClearForPool();
        var pool = _threadFramePool.Value!;
        if (pool.Count < 32)
        {
            pool.Push(frame);
        }
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
        var instructions = function.Instructions;
        var instructionCount = instructions.Count;
        // 求值栈是 ThreadLocal 属性，逐指令访问要走一次线程本地查找。
        // 同一次 ExecuteFrameLoop 内当前线程不会变化，因此把栈引用提到循环外，
        // 让下面内联的极热指令零额外开销地读写栈。
        var stack = _stack;
        var locals = frame.Locals;
        while (frame.IP < instructionCount)
        {
            var instructionIndex = frame.IP;
            var instruction = instructions[instructionIndex];
            frame.IP++;

            try
            {
                // 极热指令内联分发：省掉 ExecuteInstruction 的一层 switch 与一次方法调用。
                // 语义与各分派方法完全一致，未覆盖的指令仍走 ExecuteInstruction。
                switch (instruction.OpCode)
                {
                    case OpCode.LoadConst:
                        stack.Push((frame.ConstantPool ?? _bytecodeFile.ConstantPool)
                            .GetConstant((int)instruction.Operand!));
                        continue;

                    case OpCode.LoadLocal:
                    {
                        var localValue = locals[(int)instruction.Operand!];
                        stack.Push(localValue is UpValueCell cell ? cell.Value : localValue);
                        continue;
                    }

                    case OpCode.StoreLocal:
                    {
                        int localIndex = (int)instruction.Operand!;
                        var localValue = stack.Pop();
                        if (locals[localIndex] is UpValueCell cell)
                        {
                            cell.Value = localValue;
                        }
                        else
                        {
                            locals[localIndex] = localValue;
                        }

                        continue;
                    }

                    case OpCode.RefreshLocalBinding:
                    {
                        int localIndex = (int)instruction.Operand!;
                        if (locals[localIndex] is UpValueCell existing)
                        {
                            locals[localIndex] = new UpValueCell(existing.Value);
                        }

                        continue;
                    }

                    case OpCode.LoadGlobal:
                        stack.Push(ResolveGlobalValue(frame, (string)instruction.Operand!, instruction));
                        continue;

                    case OpCode.StoreGlobal:
                    {
                        var varName = (string)instruction.Operand!;
                        var globalValue = stack.Pop();
                        if (frame.ClosureEnvironment == null ||
                            !frame.ClosureEnvironment.TrySetValue(varName, globalValue))
                        {
                            _globals[varName] = globalValue;
                        }

                        continue;
                    }

                    case OpCode.LoadNull:
                        stack.Push(null);
                        continue;

                    case OpCode.LoadTrue:
                        stack.Push(true);
                        continue;

                    case OpCode.LoadFalse:
                        stack.Push(false);
                        continue;

                    case OpCode.Pop:
                        stack.Pop();
                        continue;

                    case OpCode.Dup:
                        stack.Push(stack.Peek());
                        continue;

                    case OpCode.Add:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Add(a, b));
                        continue;
                    }

                    case OpCode.Sub:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Sub(a, b));
                        continue;
                    }

                    case OpCode.Mul:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Mul(a, b));
                        continue;
                    }

                    case OpCode.Mod:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Mod(a, b));
                        continue;
                    }

                    case OpCode.Equal:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Equals(a, b));
                        continue;
                    }

                    case OpCode.NotEqual:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(!Equals(a, b));
                        continue;
                    }

                    case OpCode.Less:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Less(a, b));
                        continue;
                    }

                    case OpCode.LessEqual:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(LessEqual(a, b));
                        continue;
                    }

                    case OpCode.Greater:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(Greater(a, b));
                        continue;
                    }

                    case OpCode.GreaterEqual:
                    {
                        var b = stack.Pop();
                        var a = stack.Pop();
                        stack.Push(GreaterEqual(a, b));
                        continue;
                    }

                    case OpCode.Jump:
                        frame.IP = (int)instruction.Operand!;
                        continue;

                    case OpCode.JumpIfFalse:
                    {
                        var targetIP = (int)instruction.Operand!;
                        if (!ToBool(stack.Pop()))
                        {
                            frame.IP = targetIP;
                        }

                        continue;
                    }

                    case OpCode.JumpIfTrue:
                    {
                        var targetIP = (int)instruction.Operand!;
                        if (ToBool(stack.Pop()))
                        {
                            frame.IP = targetIP;
                        }

                        continue;
                    }

                    case OpCode.Throw:
                    {
                        // 与 ExecuteExceptionOperation 的 Throw 分支保持一致：
                        // 帧内可捕获时直接跳转，否则抛 VmException 走跨帧慢路径
                        // （由下面的 catch 交给 HandleException）。
                        var exceptionValue = stack.Pop();
                        if (TryHandleExceptionInline(exceptionValue, frame, function))
                        {
                            // 内联分发可能已经把 IP 改到 catch 块，重取局部变量数组无必要，
                            // 但 frame.IP 的变更会自然反映到循环条件上。
                            continue;
                        }

                        throw new VmException(exceptionValue);
                    }

                    case OpCode.Return:
                    {
                        // 返回值已在栈上；声明了非 void 返回类型时校验类型。
                        if (!string.IsNullOrEmpty(function.ReturnType) && function.ReturnType != "void")
                        {
                            var returnValue = stack.Count > 0 ? stack.Peek() : null;
                            if (!CheckTypeMatch(function.ReturnType, returnValue))
                            {
                                var actualType = GetValueTypeName(returnValue);
                                throw new TypeError(
                                    GetPosition(instruction),
                                    function.ReturnType,
                                    actualType,
                                    $"函数 '{function.Name}' 返回值类型不匹配"
                                );
                            }
                        }

                        frame.LeftReturnValue = true;
                        frame.IP = instructions.Count;
                        continue;
                    }

                    case OpCode.ReturnVoid:
                        frame.LeftReturnValue = false;
                        frame.IP = instructions.Count;
                        continue;

                    default:
                        ExecuteInstruction(instruction, frame);
                        break;
                }
            }
            catch (Exception ex)
            {
                // 异常发生时，先执行所有 defer 块
                if (frame.HasDeferredInstructions)
                {
                    ExecuteDefers(frame);
                }

                // 求值栈下溢：底层只会抛出 "Stack empty." 这类信息，完全不指向真实原因。
                // 典型成因是把没有返回值的调用当作值使用（例如 "r <- f()" 而 f 无返回值），
                // 此时调用没有向栈上压入任何值，后续消费该值的指令就会读到空栈。
                // 这里补上失败的指令与所在函数，便于定位。
                if (ex is InvalidOperationException { Message: "Stack empty." })
                {
                    throw new StateError(GetPosition(instruction),
                        $"求值栈为空：指令 {instruction.OpCode} 需要从栈上取值，但栈是空的" +
                        $"（所在函数 '{function.Name}'，指令位置 {instructionIndex}）。" +
                        "常见原因是把没有返回值的调用当作值使用（例如 `r <- f()` 而 f 没有 return），" +
                        "或某个表达式没有产出值，请检查该处调用是否有返回值。");
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

            // 调用约定：一次调用在求值栈上恰好留下一个值。
            // 无返回值的函数（ReturnVoid，或函数体执行到末尾）不会压入任何值，
            // 这里补一个 VoidLangValue，使 "r <- f()" 这类把无返回值调用当作值使用的写法不再读到空栈。
            // 用 VoidLangValue 而不是 null：解释器模式下无返回值的调用同样产生 VoidLangValue，
            // 因此 "f() == null" 为 false、"print(f())" 不输出内容，两种模式行为一致。
            // 注意必须放在 ExecuteFrameLoop 正常返回之后：若函数抛出异常则不补值。
            if (!frame.LeftReturnValue)
            {
                _stack.Push(new VoidLangValue());
            }
        }
        finally
        {
            if (frame.HasDeferredInstructions)
            {
                ExecuteDefers(frame);
            }
            _callStack.Pop();
            ReturnLocalsBuffer(frame);
            ReturnCallFrame(frame);
        }
    }

    /// <summary>
    /// 调用函数
    /// </summary>
    private void CallFunction(FunctionMetadata function, object?[] arguments)
    {
        var processedArguments = NormalizeArguments(function, arguments, new SourcePosition());
        var locals = RentLocalsBuffer(function.LocalCount);

        var frame = RentCallFrame(function, locals, function.LocalCount, usesPooledLocals: function.LocalCount > 0);
        frame.Arguments = processedArguments;

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

        var frame = RentCallFrame(function, locals, function.LocalCount, usesPooledLocals: function.LocalCount > 0);
        frame.Arguments = processedArguments;
        frame.ClosureEnvironment = capturedVariables;
        frame.ConstantPool = constantPool;

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

    public object? ExecuteFunctionObjectInWorker(object? funcObj, object?[] arguments)
    {
        var workerVm = CreateWorkerVirtualMachine();
        return workerVm.CallFunctionObject(funcObj, arguments);
    }
}
