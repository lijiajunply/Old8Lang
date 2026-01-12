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
        CallFunction(entryFunction, new object?[0]);
    }

    /// <summary>
    /// 调用函数
    /// </summary>
    private void CallFunction(FunctionMetadata function, object?[] arguments)
    {
        // 创建调用帧
        var frame = new CallFrame(function, function.LocalCount);
        frame.Arguments = arguments;

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

                    // 调用函数
                    CallFunction(function, args);

                    // 如果有返回值,它应该在栈上
                }
                break;

            case OpCode.CallNative:
                {
                    var operands = (object[])instruction.Operand!;
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
                break;

            case OpCode.Return:
                {
                    // 返回值应该已经在栈上
                    // 调用者会从栈中获取返回值
                    return; // 退出当前函数
                }

            case OpCode.ReturnVoid:
                return; // 退出当前函数

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
                        var tuple = _stack.Pop() as Tuple<object?, object?>;
                        if (tuple != null && tuple.Item1 != null)
                        {
                            dict[tuple.Item1] = tuple.Item2;
                        }
                    }
                    _stack.Push(dict);
                }
                break;

            // === 迭代器操作 ===
            case OpCode.GetIterator:
                {
                    var collection = _stack.Pop();
                    if (collection is System.Collections.IEnumerable enumerable)
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
                    var enumerator = _stack.Peek() as System.Collections.IEnumerator;
                    if (enumerator != null)
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
                    var enumerator = top as System.Collections.IEnumerator;
                    if (enumerator != null)
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

            // === 异常处理 ===
            case OpCode.Throw:
                {
                    var exceptionValue = _stack.Pop();
                    var message = ToString(exceptionValue);
                    throw new Exception(message);
                }

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

            default:
                throw new Exception($"未知的原生函数: {funcName}");
        }
    }
}
