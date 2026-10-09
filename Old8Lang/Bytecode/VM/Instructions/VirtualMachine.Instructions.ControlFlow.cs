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
                    // 必须无条件压入：原生函数常常没有返回值（如 PrintLine、Sleep），
                    // 但调用方按"一次调用在栈上留下一个值"的约定消费，漏压会导致栈失衡
                    _stack.Push(CallNativeFunction(funcName, args));
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
                    // 必须无条件压入：原生函数常常没有返回值（如 PrintLine、Sleep），
                    // 但调用方按"一次调用在栈上留下一个值"的约定消费，漏压会导致栈失衡
                    _stack.Push(CallNativeFunction(funcName, args));
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
                frame.LeftReturnValue = true;
                // 设置 IP 超出指令范围，终止 CallFunction 中的 while 循环
                frame.IP = frame.Function.Instructions.Count;
                return; // 退出当前函数
            }

            case OpCode.ReturnVoid:
                // 无返回值：调用方需要一个占位值，由 ExecuteFrame 统一补 VoidLangValue
                frame.LeftReturnValue = false;
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
                var operands = (object[])instruction.Operand!;
                int funcIndex = (int)operands[0];
                int capturedVarCount = (int)operands[1];

                if (funcIndex < 0 || funcIndex >= _bytecodeFile.Functions.Count)
                {
                    throw new IndexError(GetPosition(instruction), $"无效的函数索引: {funcIndex}");
                }

                var funcMeta = _bytecodeFile.Functions[funcIndex];
                var localCaptureCount = 0;

                for (var i = 0; i < capturedVarCount; i++)
                {
                    var baseIndex = 2 + i * 3;
                    if ((int)operands[baseIndex] == 0)
                    {
                        localCaptureCount++;
                    }
                }

                var localCapturedNames = localCaptureCount == 0 ? [] : new string[localCaptureCount];
                var localCapturedCells = localCaptureCount == 0 ? [] : new UpValueCell[localCaptureCount];
                var localCaptureIndex = 0;

                for (var i = 0; i < capturedVarCount; i++)
                {
                    var baseIndex = 2 + i * 3;
                    var captureKind = (int)operands[baseIndex];
                    var captureOperand = (int)operands[baseIndex + 1];
                    var captureName = (string)operands[baseIndex + 2];

                    switch (captureKind)
                    {
                        case 0:
                            // 按引用捕获：把外层帧的槽位原地装箱，闭包与外层此后共用同一个共享单元，
                            // 因此外层之后再赋值闭包也能读到，闭包内的赋值也能传回外层。
                            localCapturedNames[localCaptureIndex] = captureName;
                            localCapturedCells[localCaptureIndex] =
                                UpValueCell.Box(ref frame.Locals[captureOperand]);
                            localCaptureIndex++;
                            break;
                        case 1:
                            // 外层闭包已捕获的变量：新环境的父链就是外层环境，父链上持有同一个共享单元，
                            // 无需在此复制
                            break;
                        case 2:
                            break;
                        default:
                            throw new InvalidOperationError(GetPosition(instruction), $"未知的闭包捕获类型: {captureKind}");
                    }
                }

                ClosureEnvironment closureEnvironment = localCaptureCount == 0
                    ? new ClosureEnvironment([], [], frame.ClosureEnvironment)
                    : new ClosureEnvironment(
                        localCapturedNames,
                        localCapturedCells,
                        frame.ClosureEnvironment);
                var closure = new ClosureValue(funcMeta, closureEnvironment);
                _stack.Push(closure);
            }
                break;

        }
    }
}
