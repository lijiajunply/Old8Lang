using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

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

                    // 首先检查当前调用帧的闭包环境（如果有）
                    FunctionMetadata? function = null;
                    Dictionary<string, object?>? closureEnvironment = null;
                    ConstantPool? closureConstantPool = null;

                    if (frame.ClosureEnvironment != null && frame.ClosureEnvironment.TryGetValue(funcName, out var closureFunc))
                    {
                        if (closureFunc is FunctionMetadata closureFuncMeta)
                        {
                            function = closureFuncMeta;
                        }
                        else if (closureFunc is ClosureValue closureClosure)
                        {
                            function = closureClosure.Function;
                            closureEnvironment = closureClosure.CapturedVariables;
                            closureConstantPool = closureClosure.ConstantPool;
                        }
                    }

                    // 然后检查全局变量中是否有该函数（可能是从模块导入的）
                    if (function == null && _globals.TryGetValue(funcName, out var funcObj))
                    {
                        if (funcObj is FunctionMetadata funcMeta)
                        {
                            function = funcMeta;
                        }
                        else if (funcObj is ClosureValue closure)
                        {
                            function = closure.Function;
                            closureEnvironment = closure.CapturedVariables;
                            closureConstantPool = closure.ConstantPool;
                        }
                    }

                    // 如果全局变量中没有，从当前字节码文件中查找
                    function ??= _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);

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
                                else if (symbol is ClosureValue moduleClosure)
                                {
                                    function = moduleClosure.Function;
                                    closureEnvironment = moduleClosure.CapturedVariables;
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

                        // 尝试从 GlobalFunctionRegistry 查找全局函数
                        var globalFunc = GlobalFunctionRegistry.Instance.TryGetFunction(funcName);
                        if (globalFunc != null)
                        {
                            // 调用全局函数的 ExecuteInVM 方法
                            var result = globalFunc.ExecuteInVM(args);
                            _stack.Push(result);
                            break;
                        }

                        throw new MethodNotFoundError(GetPosition(instruction), funcName);
                    }

                    // 处理 params 可变参数
                    if (function.ParamsParameterIndex >= 0)
                    {
                        // 有 params 参数的情况
                        int paramsIndex = function.ParamsParameterIndex;
                        int regularParamCount = paramsIndex; // params 之前的常规参数数量

                        if (argCount >= regularParamCount)
                        {
                            // 将 params 位置及之后的所有参数打包成数组
                            int paramsArgCount = argCount - regularParamCount;
                            var paramsArray = new object?[paramsArgCount];
                            Array.Copy(args, regularParamCount, paramsArray, 0, paramsArgCount);

                            // 创建新的参数数组
                            var fullArgs = new object?[function.Parameters.Count];
                            // 复制常规参数
                            Array.Copy(args, fullArgs, regularParamCount);
                            // 设置 params 数组
                            fullArgs[paramsIndex] = paramsArray;

                            args = fullArgs;
                        }
                        else
                        {
                            // 参数不足，需要补全
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
                                    fullArgs[i] = Array.Empty<object?>();
                                }
                                else
                                {
                                    throw new ArgumentError(GetPosition(instruction),
                                        $"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                                }
                            }

                            args = fullArgs;
                        }
                    }
                    else if (argCount < function.Parameters.Count)
                    {
                        // 没有 params 参数，但参数不足，使用默认值补全
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
                                throw new ArgumentError(GetPosition(instruction),
                                    $"函数 {function.Name} 的参数 '{function.Parameters[i]}' 未提供值且没有默认值");
                            }
                        }

                        args = fullArgs;
                    }

                    // 检查参数类型
                    ValidateParameterTypes(function, args, instruction);

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
                        // 调用普通函数或闭包函数
                        if (closureEnvironment != null)
                        {
                            CallClosureFunction(function, args, closureEnvironment, closureConstantPool);
                        }
                        else
                        {
                            CallFunction(function, args);
                        }
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
                    Dictionary<string, object?>? closureEnvironment = null;
                    ConstantPool? closureConstantPool = null;

                    // 首先检查当前调用帧的闭包环境（如果有）
                    if (frame.ClosureEnvironment != null && frame.ClosureEnvironment.TryGetValue(funcName, out var closureFunc))
                    {
                        if (closureFunc is FunctionMetadata closureFuncMeta)
                        {
                            function = closureFuncMeta;
                        }
                        else if (closureFunc is ClosureValue closureClosure)
                        {
                            function = closureClosure.Function;
                            closureEnvironment = closureClosure.CapturedVariables;
                            closureConstantPool = closureClosure.ConstantPool;
                        }
                    }

                    // 然后检查全局变量中是否有该函数（可能是从模块导入的）
                    if (function == null && _globals.TryGetValue(funcName, out var funcObj))
                    {
                        if (funcObj is FunctionMetadata funcMeta)
                        {
                            function = funcMeta;
                        }
                        else if (funcObj is ClosureValue closure)
                        {
                            function = closure.Function;
                            closureEnvironment = closure.CapturedVariables;
                            closureConstantPool = closure.ConstantPool;
                        }
                    }

                    // 如果全局变量中没有，从当前字节码文件中查找
                    function ??= _bytecodeFile.Functions.FirstOrDefault(f => f.Name == funcName);

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
                                else if (symbol is ClosureValue moduleClosure)
                                {
                                    function = moduleClosure.Function;
                                    closureEnvironment = moduleClosure.CapturedVariables;
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
                        throw new MethodNotFoundError(GetPosition(instruction), funcName);
                    }

                    // 重新排列参数以匹配函数参数定义
                    var args = ArrangeArgumentsWithNamed(function, positionalArgs, namedArgNames, namedArgValues);

                    // 检查参数类型
                    ValidateParameterTypes(function, args, instruction);

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
                        // 调用普通函数或闭包函数
                        if (closureEnvironment != null)
                        {
                            CallClosureFunction(function, args, closureEnvironment, closureConstantPool);
                        }
                        else
                        {
                            CallFunction(function, args);
                        }
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

                if (funcObj is ClosureValue closure)
                {
                    // 调用闭包：将捕获的变量作为局部变量传递
                    var funcMeta = closure.Function;

                    if (funcMeta.IsGenerator)
                    {
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
                        // 调用闭包函数，传递捕获的变量
                        CallClosureFunction(funcMeta, args, closure.CapturedVariables);
                    }
                }
                else if (funcObj is FunctionMetadata funcMeta)
                {
                    // 调用普通函数
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
                    throw new TypeError(GetPosition(instruction), $"尝试调用非函数对象: {funcObj?.GetType().Name}");
                }
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
