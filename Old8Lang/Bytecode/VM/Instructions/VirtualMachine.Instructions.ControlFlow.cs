using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行控制流指令
    /// </summary>
    private void ExecuteControlFlowOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
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
                ExecuteCallInstruction(instruction, frame);
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
                ExecuteCallDynamicInstruction(instruction, frame);
            }
                break;

            case OpCode.Return:
            {
                // 返回值应该已经在栈上
                // 检查返回值类型
                if (!string.IsNullOrEmpty(frame.Function.ReturnType) && frame.Function.ReturnType != "void")
                {
                    var returnValue = _stack.Count > 0 ? _stack.Peek() : null;
                    if (!CheckTypeMatch(frame.Function.ReturnType, returnValue))
                    {
                        var actualType = GetValueTypeName(returnValue);
                        throw new TypeError(
                            GetPosition(instruction),
                            frame.Function.ReturnType,
                            actualType,
                            $"函数 '{frame.Function.Name}' 返回值类型不匹配"
                        );
                    }
                }
                // 调用者会从栈中获取返回值
                // 设置 IP 超出指令范围，终止 CallFunction 中的 while 循环
                frame.IP = frame.Function.Instructions.Count;
                return; // 退出当前函数
            }

            case OpCode.ReturnVoid:
                // 设置 IP 超出指令范围，终止 CallFunction 中的 while 循环
                frame.IP = frame.Function.Instructions.Count;
                return; // 退出当前函数

            case OpCode.Break:
                // Break指令在字节码生成阶段已经被转换为Jump指令
                // 这里不应该被执行到
                throw new InvalidOperationError(GetPosition(instruction), "Break指令不应该在运行时被执行");

            case OpCode.Continue:
                // Continue指令在字节码生成阶段已经被转换为Jump指令
                // 这里不应该被执行到
                throw new InvalidOperationError(GetPosition(instruction), "Continue指令不应该在运行时被执行");

            case OpCode.MakeFunction:
            {
                int funcIndex = (int)instruction.Operand!;
                // Get function metadata from bytecode file
                if (funcIndex >= 0 && funcIndex < _bytecodeFile.Functions.Count)
                {
                    var funcMeta = _bytecodeFile.Functions[funcIndex];
                    _stack.Push(funcMeta);
                }
                else
                {
                    throw new IndexError(GetPosition(instruction), $"无效的函数索引: {funcIndex}");
                }
            }
                break;

            case OpCode.MakeClosure:
            {
                // 操作数: [funcIndex, capturedVarCount, varNames...]
                var operands = (object[])instruction.Operand!;
                int funcIndex = (int)operands[0];
                int capturedVarCount = (int)operands[1];
                string[] varNames = (string[])operands[2];

                // 获取函数元数据
                if (funcIndex < 0 || funcIndex >= _bytecodeFile.Functions.Count)
                {
                    throw new IndexError(GetPosition(instruction), $"无效的函数索引: {funcIndex}");
                }

                var funcMeta = _bytecodeFile.Functions[funcIndex];

                // 从栈中弹出捕获的变量值（按相反顺序）
                var capturedVariables = new Dictionary<string, object?>();
                for (int i = capturedVarCount - 1; i >= 0; i--)
                {
                    var value = _stack.Pop();
                    capturedVariables[varNames[i]] = value;
                }

                // 如果当前帧有闭包环境，需要合并到新闭包中
                // 这样嵌套闭包就能访问外层闭包的变量
                if (frame.ClosureEnvironment != null)
                {
                    foreach (var (varName, value) in frame.ClosureEnvironment)
                    {
                        // 只添加新闭包中没有的变量（避免覆盖）
                        capturedVariables.TryAdd(varName, value);
                    }
                }

                // 创建闭包对象
                var closure = new ClosureValue(funcMeta, capturedVariables);
                _stack.Push(closure);
            }
                break;

        }
    }
}
