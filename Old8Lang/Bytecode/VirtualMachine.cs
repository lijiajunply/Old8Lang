using System.Collections;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;

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

                ExecuteInstruction(instruction, frame);
            }
        }
        finally
        {
            _callStack.Pop();
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

                    // 调用函数
                    CallFunction(function, args);
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

                    // 调用函数
                    CallFunction(function, args);
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
            case OpCode.Await:
            {
                var taskValue = _stack.Pop();

                // 如果是TaskLangValue，等待其完成
                if (taskValue is TaskLangValue task)
                {
                    // 同步等待Task完成
                    task.Task.Wait();

                    // 将结果压入栈
                    _stack.Push(task.Result?.GetValue());
                }
                else
                {
                    // 如果不是Task，直接返回原值
                    _stack.Push(taskValue);
                }
            }
                break;

            case OpCode.Yield:
            {
                // Yield操作：生成器返回一个值
                // 在字节码VM中，我们简化实现：
                // 1. 从栈中弹出要yield的值
                // 2. 将值存储到某个位置（例如返回值）
                // 3. 暂停执行（通过返回实现）

                // 注意：完整的生成器支持需要状态机，这里是简化版本
                var yieldValue = _stack.Pop();

                // 将yield的值压回栈顶作为返回值
                _stack.Push(yieldValue);

                // TODO: 完整的生成器实现需要：
                // - 保存当前执行状态（IP、局部变量）
                // - 创建可恢复的迭代器对象
                // - 支持多次yield和恢复执行
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

                // 如果是字典对象（我们创建的类实例）
                if (obj is Dictionary<string, object?> dictObj)
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

                // 如果是字典对象（我们创建的类实例）
                if (obj is Dictionary<string, object?> dictObj)
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

                // 创建对象实例（使用动态类型）
                var obj = new Dictionary<string, object?>();

                // 初始化字段为默认值
                foreach (var field in classMetadata.Fields)
                {
                    obj[field.Name] = null;
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
                var args = new object?[argCount];
                for (int i = argCount - 1; i >= 0; i--)
                {
                    args[i] = _stack.Pop();
                }

                // 弹出对象
                var obj = _stack.Pop();
                if (obj == null)
                {
                    throw new Exception($"无法在 null 对象上调用方法 {methodName}");
                }

                // 使用反射调用方法
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
                break;

            case OpCode.DebugPrint:
            {
                int messageIndex = (int)instruction.Operand!;
                var message = _bytecodeFile.ConstantPool.GetConstant(messageIndex);
                var stackContents = string.Join(", ", _stack.Select(x => x?.GetType().Name ?? "null"));
                Console.WriteLine($"{message} - 栈深度:{_stack.Count}, 内容:[{stackContents}]");
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

    private string ToString(object? value)
    {
        if (value == null) return "null";
        if (value is string s) return s;

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