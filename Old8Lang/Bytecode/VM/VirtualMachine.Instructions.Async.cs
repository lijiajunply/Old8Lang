using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行异步支持指令
    /// </summary>
    private void ExecuteAsyncOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
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
                               ?? throw new MethodNotFoundError(GetPosition(instruction), funcName);
                }
                else if (funcObj is FunctionMetadata funcMeta)
                {
                    function = funcMeta;
                }
                else
                {
                    throw new TypeError(GetPosition(instruction),
                        $"Invalid function for NewTask: {funcObj?.GetType().Name}");
                }

                // 创建并启动任务
                var task = Task.Run(() =>
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
                    throw new MethodNotFoundError(GetPosition(instruction), funcName);
                }

                // 创建并启动任务
                var task = Task.Run(() =>
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
                // 栈顶: TaskLangValue 或 VMThreadLangValue
                var value = _stack.Pop();
                if (value is TaskLangValue taskValue)
                {
                    // 阻塞等待任务完成
                    var result = taskValue.Await();
                    _stack.Push(result);
                }
                else if (value is VMThreadLangValue vmThreadValue)
                {
                    // 等待虚拟机线程完成
                    var result = vmThreadValue.Join();
                    _stack.Push(result);
                }
                else if (value is Task task)
                {
                    // 直接是 Task 对象
                    task.GetAwaiter().GetResult();
                    // 如果是 Task<T>，获取结果
                    var resultProperty = task.GetType().GetProperty("Result");
                    _stack.Push(resultProperty != null ? resultProperty.GetValue(task) : null);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction),
                        $"await 只能用于 Task 或 VMThreadLangValue 类型，实际类型为 {value?.GetType().Name ?? "null"}");
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


        }
    }
}
