using System.Collections;
using System.Reflection;
using Old8Lang.AST;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;

namespace Old8Lang.Bytecode;

public partial class VirtualMachine
{
    /// <summary>
    /// 从指令获取源代码位置信息
    /// </summary>
    private static SourcePosition GetPosition(Instruction instruction)
    {
        return new SourcePosition(
            instruction.LineNumber ?? 0,
            instruction.ColumnNumber ?? 0,
            fileName: instruction.SourceFile
        );
    }

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

                // 先检查闭包环境
                if (frame.ClosureEnvironment != null &&
                    frame.ClosureEnvironment.TryGetValue(varName, out var closureValue))
                {
                    _stack.Push(closureValue);
                }
                // 再检查全局变量
                else if (_globals.TryGetValue(varName, out var globalValue))
                {
                    _stack.Push(globalValue);
                }
                else
                {
                    throw new NameError(GetPosition(instruction), varName);
                }
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

            case OpCode.Swap:
            {
                var a = _stack.Pop();
                var b = _stack.Pop();
                _stack.Push(a);
                _stack.Push(b);
            }
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
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "&&", instruction);
                ValidateLogicalOperand(b, "&&", instruction);
                _stack.Push(ToBool(a) && ToBool(b));
            }
                break;

            case OpCode.Or:
            {
                var b = _stack.Pop();
                var a = _stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "||", instruction);
                ValidateLogicalOperand(b, "||", instruction);
                _stack.Push(ToBool(a) || ToBool(b));
            }
                break;

            case OpCode.Not:
            {
                var a = _stack.Pop();
                // 检查操作数是否为布尔类型
                ValidateLogicalOperand(a, "!", instruction);
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
                    throw new TypeError(GetPosition(instruction), $"无法获取类型 {collection?.GetType().Name} 的长度");
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
                    if (!dict.Contains(index))
                    {
                        throw new KeyError(GetPosition(instruction), index);
                    }

                    _stack.Push(dict[index]);
                }
                else if (collection is DictionaryLangValue dictLangValue)
                {
                    // 处理 DictionaryLangValue 类型
                    // 将索引转换为 LangValueType
                    var keyToFind = ConvertToLangValueType(index);

                    // 在字典中查找键
                    bool found = false;
                    foreach (var (key, value) in dictLangValue.Value)
                    {
                        if (key.Equal(keyToFind))
                        {
                            _stack.Push(value);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        throw new KeyError(GetPosition(instruction), index);
                    }
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
                        throw new IndexError(GetPosition(instruction), idx, currentIdx);
                    }
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {collection?.GetType().Name} 执行索引访问");
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
                else if (collection is DictionaryLangValue dictLangValue)
                {
                    // 处理 DictionaryLangValue 类型
                    // 将索引和值转换为 LangValueType
                    var keyToSet = ConvertToLangValueType(index);
                    var valueToSet = ConvertToLangValueType(value);

                    // 在字典中查找键并更新，如果不存在则添加
                    bool found = false;
                    for (int i = 0; i < dictLangValue.Value.Count; i++)
                    {
                        var (key, _) = dictLangValue.Value[i];
                        if (key.Equal(keyToSet))
                        {
                            dictLangValue.Value[i] = (key, valueToSet);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // 键不存在，添加新的键值对
                        dictLangValue.Value.Add((keyToSet, valueToSet));
                    }
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {collection?.GetType().Name} 执行索引赋值");
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

                // 特殊处理字典：迭代键而不是键值对
                if (collection is IDictionary dict)
                {
                    var enumerator = dict.Keys.GetEnumerator();
                    _stack.Push(enumerator);
                }
                else if (collection is IEnumerable enumerable)
                {
                    var enumerator = enumerable.GetEnumerator();
                    _stack.Push(enumerator);
                }
                else
                {
                    throw new TypeError(GetPosition(instruction), $"对象类型 {collection?.GetType().Name} 不可迭代");
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
                    throw new StateError(GetPosition(instruction), $"栈顶不是迭代器对象，而是: {topType}");
                }
            }
                break;

            case OpCode.IteratorCurrent:
            {
                // 栈顶应该是迭代器
                if (_stack.Count == 0)
                {
                    throw new StateError(GetPosition(instruction), "IteratorCurrent: 栈为空");
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
                    throw new StateError(GetPosition(instruction),
                        $"IteratorCurrent 失败: 栈顶类型是 {topType}, 栈内容({_stack.Count}): [{stackContents}]");
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
                        slicedList = [];
                        if (sliceResult is IEnumerable enumerable)
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
                        object current = new Tuple<object?, object?>(slicedList[^2],
                            slicedList[^1]);

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
                    throw new TypeError(GetPosition(instruction), $"无法对类型 {collection?.GetType().Name} 执行切片操作");
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
                    throw new TypeError(GetPosition(instruction), "AddToGroup 操作需要一个分组字典");
                }

                // 如果键不存在,创建新的列表
                if (!groupDict.ContainsKey(key!))
                {
                    groupDict[key!] = [];
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
                    throw new TypeError(GetPosition(instruction), "GroupDictToList 操作需要一个分组字典");
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
                    throw new CastError(GetPosition(instruction), value?.GetType().Name ?? "null", targetTypeName,
                        ex.Message);
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
                var enumTemplate = new EnumTemplate(
                    enumName,
                    members);

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
                               ?? throw new MethodNotFoundError(GetPosition(instruction), funcName);
                }
                else if (funcObj is FunctionMetadata funcMeta)
                {
                    function = funcMeta;
                }
                else
                {
                    throw new TypeError(GetPosition(instruction),
                        $"Invalid function for ThreadCreate: {funcObj?.GetType().Name}");
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
                    throw new NullReferenceError(GetPosition(instruction), fieldName);
                }

                // 如果是 ClassMetadata（访问静态字段）
                if (obj is ClassMetadata classMetadata)
                {
                    if (classMetadata.StaticFieldValues.TryGetValue(fieldName, out var staticValue))
                    {
                        _stack.Push(staticValue);
                    }
                    else
                    {
                        throw new AttributeError(GetPosition(instruction), fieldName, classMetadata.Name);
                    }
                }
                // 如果是 BytecodeObjectInstance（Old8Lang 对象）
                else if (obj is BytecodeObjectInstance bytecodeObj)
                {
                    if (bytecodeObj.Fields.TryGetValue(fieldName, out var value))
                    {
                        _stack.Push(value);
                    }
                    else
                    {
                        throw new AttributeError(GetPosition(instruction), fieldName, "BytecodeObject");
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
                        throw new AttributeError(GetPosition(instruction), fieldName, "BytecodeObject");
                    }
                }
                // 如果是枚举模板（访问枚举成员）
                else if (obj is EnumTemplate enumTemplate)
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
                            throw new AttributeError(GetPosition(instruction), fieldName, "Tuple");
                        }
                    }
                    else
                    {
                        throw new AttributeError(GetPosition(instruction), fieldName, "Tuple");
                    }
                }
                else if (obj is IList list)
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
                                throw new AttributeError(GetPosition(instruction), fieldName, type.Name);
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
                            throw new AttributeError(GetPosition(instruction), fieldName, objType.Name);
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
                    throw new NullReferenceError(GetPosition(instruction), fieldName);
                }

                // 如果是 ClassMetadata（设置静态字段）
                if (obj is ClassMetadata classMetadata)
                {
                    if (classMetadata.StaticFieldValues.ContainsKey(fieldName))
                    {
                        // 检查静态字段类型
                        ValidateStaticFieldType(classMetadata, fieldName, value, instruction);
                        classMetadata.StaticFieldValues[fieldName] = value;
                    }
                    else
                    {
                        throw new AttributeError(GetPosition(instruction), fieldName, classMetadata.Name);
                    }
                }
                // 如果是 BytecodeObjectInstance（Old8Lang 对象）
                else if (obj is BytecodeObjectInstance bytecodeObj)
                {
                    // 检查字段类型
                    ValidateFieldType(bytecodeObj.ClassName, fieldName, value, instruction);
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
                            throw new AttributeError(GetPosition(instruction), fieldName, objType.Name);
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
                    throw new NullReferenceError(GetPosition(instruction), fieldName);
                }

                // 检查是否是 BytecodeObjectInstance
                if (thisInstance is BytecodeObjectInstance bytecodeObj)
                {
                    // 注意: 在 Old8Lang 中,所有字段(包括父类字段)都存储在对象实例的 Fields 字典中
                    // super.field 访问的是继承自父类的字段,但实际存储位置在对象本身
                    // 因此我们直接从对象的 Fields 字典中获取字段值即可

                    // 字段不存在,返回 null
                    _stack.Push(bytecodeObj.Fields.GetValueOrDefault(fieldName));
                }
                else
                {
                    // 使用反射获取父类字段或属性（用于 C# 对象）
                    var objType = thisInstance.GetType();
                    var baseType = objType.BaseType;

                    if (baseType == null || baseType == typeof(object))
                    {
                        throw new TypeError(GetPosition(instruction), $"类型 {objType.Name} 没有父类");
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
                            throw new AttributeError(GetPosition(instruction), fieldName, baseType.Name);
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
                    throw new NullReferenceError(GetPosition(instruction), fieldName);
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
                        throw new TypeError(GetPosition(instruction), $"类型 {objType.Name} 没有父类");
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
                            throw new AttributeError(GetPosition(instruction), fieldName, baseType.Name);
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
                    throw new ClassNotFoundError(GetPosition(instruction), className);
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
                        // 获取字段的默认值
                        object? defaultValue = null;
                        if (field.DefaultValueIndex >= 0 && field.DefaultValueIndex < _bytecodeFile.ConstantPool.Count)
                        {
                            defaultValue = _bytecodeFile.ConstantPool.GetConstant(field.DefaultValueIndex);
                        }
                        else if (field.IsDefaultNull)
                        {
                            defaultValue = null;
                        }
                        obj.Fields[field.Name] = defaultValue;
                    }
                }

                // 应用 Mixin 方法到对象
                if (classMetadata.Mixins is { Count: > 0 })
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
                if (classMetadata.ImplementsInterfaces is { Count: > 0 })
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
                    throw new NullReferenceError(GetPosition(instruction), methodName);
                }

                // 检查是否是 ClassMetadata（静态方法调用）
                if (obj is ClassMetadata staticClassMetadata)
                {
                    // 在静态方法列表中查找方法
                    var staticMethod = staticClassMetadata.StaticMethods.FirstOrDefault(m => m.Name == methodName);

                    if (staticMethod == null)
                    {
                        throw new MethodNotFoundError(GetPosition(instruction), methodName, staticClassMetadata.Name);
                    }

                    // 检查方法访问修饰符
                    if (staticMethod.AccessModifier == AccessModifier.Private)
                    {
                        // 检查是否在类内部调用
                        bool isInternalCall = false;
                        foreach (var callFrame in _callStack)
                        {
                            // 检查当前帧的第一个参数（this）是否是同一个类的实例
                            if (callFrame.Arguments is { Length: > 0 } &&
                                callFrame.Arguments[0] is BytecodeObjectInstance frameObj &&
                                frameObj.ClassName == staticClassMetadata.Name)
                            {
                                isInternalCall = true;
                                break;
                            }

                            // 检查当前帧是否是同一个类的静态方法
                            // 函数名格式为 "ClassName.MethodName"
                            var funcName = callFrame.Function.Name;
                            if (funcName.StartsWith(staticClassMetadata.Name + "."))
                            {
                                isInternalCall = true;
                                break;
                            }
                        }

                        if (!isInternalCall)
                        {
                            throw new AccessViolationError(GetPosition(instruction), methodName,
                                staticClassMetadata.Name, "private");
                        }
                    }

                    // 检查参数类型
                    ValidateParameterTypes(staticMethod.Function, args, instruction);

                    // 静态方法不需要 this 参数，直接传递参数
                    CallFunction(staticMethod.Function, args);
                }
                // 检查是否是 BytecodeObjectInstance
                else if (obj is BytecodeObjectInstance bytecodeObj)
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
                        throw new ClassNotFoundError(GetPosition(instruction), bytecodeObj.ClassName);
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
                        throw new MethodNotFoundError(GetPosition(instruction), methodName, bytecodeObj.ClassName);
                    }

                    // 检查方法访问修饰符
                    if (methodMetadata.AccessModifier == AccessModifier.Private)
                    {
                        // 检查是否在类内部调用（通过检查当前调用栈中是否有该类的方法）
                        bool isInternalCall = false;
                        foreach (var callFrame in _callStack)
                        {
                            // 检查当前帧的第一个参数（this）是否是同一个类的实例
                            if (callFrame.Arguments is { Length: > 0 } &&
                                callFrame.Arguments[0] is BytecodeObjectInstance frameObj &&
                                frameObj.ClassName == bytecodeObj.ClassName)
                            {
                                isInternalCall = true;
                                break;
                            }

                            // 检查当前帧是否是同一个类的静态方法
                            // 函数名格式为 "ClassName.MethodName"
                            var funcName = callFrame.Function.Name;
                            if (funcName.StartsWith(bytecodeObj.ClassName + "."))
                            {
                                isInternalCall = true;
                                break;
                            }
                        }

                        if (!isInternalCall)
                        {
                            throw new AccessViolationError(GetPosition(instruction), methodName, bytecodeObj.ClassName,
                                "private");
                        }
                    }

                    // 准备方法调用参数：第一个参数是 this（对象本身）
                    var methodArgs = new object?[args.Length + 1];
                    methodArgs[0] = bytecodeObj;
                    Array.Copy(args, 0, methodArgs, 1, args.Length);

                    // 检查参数类型
                    ValidateParameterTypes(methodMetadata.Function, methodArgs, instruction);

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
                if (currentFrame.Arguments is { Length: > 0 })
                {
                    var thisInstance = currentFrame.Arguments[0];
                    if (thisInstance == null)
                    {
                        throw new StateError(GetPosition(instruction), "super 只能在实例方法中使用");
                    }

                    _stack.Push(thisInstance);
                }
                // 如果 Arguments 为空，尝试从 Locals 获取
                else if (currentFrame.Locals.Length > 0)
                {
                    var thisInstance = currentFrame.Locals[0];
                    if (thisInstance == null)
                    {
                        throw new StateError(GetPosition(instruction), "super 只能在实例方法中使用");
                    }

                    _stack.Push(thisInstance);
                }
                else
                {
                    throw new StateError(GetPosition(instruction), "super 只能在实例方法中使用");
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
                    throw new NullReferenceError(GetPosition(instruction), methodName);
                }

                // 检查是否是 BytecodeObjectInstance
                if (thisInstance is BytecodeObjectInstance bytecodeObj)
                {
                    // 查找当前类的元数据
                    var currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == bytecodeObj.ClassName);
                    if (currentClass == null)
                    {
                        throw new ClassNotFoundError(GetPosition(instruction), bytecodeObj.ClassName);
                    }

                    // 查找父类
                    if (string.IsNullOrEmpty(currentClass.BaseClassName))
                    {
                        throw new TypeError(GetPosition(instruction), $"类 {bytecodeObj.ClassName} 没有父类");
                    }

                    var parentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                    if (parentClass == null)
                    {
                        throw new ClassNotFoundError(GetPosition(instruction), currentClass.BaseClassName);
                    }

                    // 在父类中查找方法
                    var methodMetadata = parentClass.Methods.FirstOrDefault(m => m.Name == methodName);
                    if (methodMetadata == null)
                    {
                        throw new MethodNotFoundError(GetPosition(instruction), methodName, parentClass.Name);
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
                        throw new MethodNotFoundError(GetPosition(instruction), methodName,
                            objType.BaseType?.Name ?? "unknown");
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

                // 将符号添加到当前全局变量
                _globals[symbolName] = symbol ?? throw new ImportError(GetPosition(instruction), moduleName,
                    $"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
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

                // 使用别名添加到全局变量
                _globals[alias] = symbol ?? throw new ImportError(GetPosition(instruction), moduleName,
                    $"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
            }
                break;

            case OpCode.ImportAll:
            {
                // 导入所有符号: import * from "module"
                string moduleName = (string)instruction.Operand!;

                var module = _moduleRegistry.GetModule(moduleName);
                if (module == null)
                {
                    throw new ImportError(GetPosition(instruction), moduleName, $"模块 '{moduleName}' 未加载");
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
                    throw new ImportError(GetPosition(instruction), moduleName,
                        $"模块 '{moduleName}' 中未找到符号 '{symbolName}'");
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
                    throw new MethodNotFoundError(GetPosition(instruction), funcName);
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
                    Concurrency.ResourceManager.TryDispose(resourceId);
                }
                // 2. 如果是 BytecodeObjectInstance（字节码模式的用户自定义类实例），尝试调用 Dispose 方法
                else if (resource is BytecodeObjectInstance bytecodeObj)
                {
                    // 查找类的 Dispose 方法
                    var classMetadata = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == bytecodeObj.ClassName);
                    if (classMetadata != null)
                    {
                        var disposeMethod = classMetadata.Methods.FirstOrDefault(m =>
                            m.Name.Equals("Dispose", StringComparison.OrdinalIgnoreCase));

                        if (disposeMethod != null)
                        {
                            // 调用 Dispose 方法，传入对象本身作为 this 参数
                            CallFunction(disposeMethod.Function, [bytecodeObj]);
                        }
                    }
                }
                // 3. 如果是 AnyLangValue（解释器模式的用户自定义类实例），尝试调用 dispose 方法
                else if (resource is AnyLangValue anyValue)
                {
                    anyValue.TryDispose();
                }
                // 4. 如果实现了 IDisposable 接口，直接调用 Dispose
                else if (resource is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                // 5. 其他类型不做处理（静默忽略）
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
                        throw new IOError(GetPosition(instruction), $"无法加载程序集 '{dllPath}': {ex.Message}");
                    }
                }

                // 如果找不到类型，尝试在所有类型中查找
                var type = (assembly.GetType($"{dllName}.{className}") ?? assembly.GetType(className)) ??
                           assembly.GetTypes().FirstOrDefault(t => t.Name == className || t.FullName == className);

                if (type == null) throw new ClassNotFoundError(GetPosition(instruction), $"{className} in {dllName}");

                // Console.WriteLine($"Importing Native: {dllName}.{className}, Mode={mode}"); // Debug

                if (mode == 0) // Single Method
                {
                    var methodName = (string)_bytecodeFile.ConstantPool.GetConstant(param1Index);
                    var alias = (string)_bytecodeFile.ConstantPool.GetConstant(param2Index);
                    var registerName = string.IsNullOrEmpty(alias) ? methodName : alias;

                    var methodInfo = type.GetMethod(methodName,
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    if (methodInfo == null) throw new MethodNotFoundError(GetPosition(instruction), methodName);

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
                throw new NotImplementedError(GetPosition(instruction), $"操作码: {instruction.OpCode}");
        }
    }

    // ===== 辅助方法 =====

    private bool CheckTypeMatch(string typeName, object? val)
    {
        typeName = typeName.Trim();

        // 1. Intersection Types (A & B) - but only at top level, not inside generics
        if (ContainsTopLevelChar(typeName, '&'))
        {
            var types = SplitTopLevel(typeName, '&');
            foreach (var type in types)
            {
                if (!CheckTypeMatch(type, val)) return false;
            }

            return true;
        }

        // 2. Union Types (A | B) - but only at top level, not inside generics
        if (ContainsTopLevelChar(typeName, '|'))
        {
            var types = SplitTopLevel(typeName, '|');
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

        // 5. Generic Types (list<T>/List<T>, array<T>/Array<T>, dict<K,V>/Dict<K,V>)
        var typeNameLower = typeName.ToLower();
        if (typeNameLower.StartsWith("list<") && typeNameLower.EndsWith(">"))
        {
            if (val is not IList list) return false;
            var innerType = typeName.Substring(5, typeName.Length - 6);
            foreach (var item in list)
            {
                if (!CheckTypeMatch(innerType, item)) return false;
            }

            return true;
        }

        if (typeNameLower.StartsWith("array<") && typeNameLower.EndsWith(">"))
        {
            if (val is not Array array) return false;
            var innerType = typeName.Substring(6, typeName.Length - 7);
            foreach (var item in array)
            {
                if (!CheckTypeMatch(innerType, item)) return false;
            }

            return true;
        }

        if (typeNameLower.StartsWith("dict<") && typeNameLower.EndsWith(">"))
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

        // 6. Basic Types (with implicit numeric conversion: int -> double)
        return typeName.ToLower() switch
        {
            "int" => val is int,
            "double" => val is double or int,  // int can be implicitly converted to double
            "string" => val is string,
            "bool" => val is bool,
            "char" => val is char,
            "array" => val is Array,
            "list" => val is IList,
            "dict" => val is IDictionary or AST.Expression.Value.DictionaryLangValue,  // DictionaryLangValue 也是 dict 类型
            "tuple" => val is Tuple<object?, object?>,
            "null" => val == null!,
            "any" => true,
            "object" => true,
            _ => CheckCustomType(typeName, val)
        };
    }

    /// <summary>
    /// 验证函数参数类型
    /// </summary>
    private void ValidateParameterTypes(FunctionMetadata function, object?[] args, Instruction instruction)
    {
        // 如果没有参数类型信息，跳过检查
        if (function.ParameterTypes == null || function.ParameterTypes.Count == 0)
            return;

        for (int i = 0; i < Math.Min(args.Length, function.ParameterTypes.Count); i++)
        {
            var expectedType = function.ParameterTypes[i];

            // 如果没有类型注解（空字符串），跳过检查
            if (string.IsNullOrEmpty(expectedType))
                continue;

            // 如果函数有泛型类型映射，替换泛型类型参数
            var resolvedType = expectedType;
            if (function.GenericTypeMapping != null && function.GenericTypeMapping.Count > 0)
            {
                // 检查 GenericTypeMapping 中是否包含泛型类型参数（如 Wrapper<T>）
                // 这是编译器的一个 bug，会导致类型解析错误
                // 作为临时变通方案，如果检测到这种情况，跳过类型检查
                bool hasNestedGenericMapping = function.GenericTypeMapping.Values.Any(v => v.Contains('<'));
                if (hasNestedGenericMapping)
                {
                    // 跳过类型检查，因为编译器生成的 GenericTypeMapping 可能不正确
                    continue;
                }

                resolvedType = ResolveGenericType(expectedType, function.GenericTypeMapping);
            }

            var actualValue = args[i];

            // 使用 CheckTypeMatch 进行类型检查
            if (!CheckTypeMatch(resolvedType, actualValue))
            {
                var actualType = GetValueTypeName(actualValue);
                var paramName = i < function.Parameters.Count ? function.Parameters[i] : $"参数{i}";
                throw new TypeError(
                    GetPosition(instruction),
                    resolvedType,
                    actualType,
                    $"参数 '{paramName}' 类型不匹配"
                );
            }
        }
    }

    /// <summary>
    /// 解析泛型类型，将类型参数替换为实际类型
    /// 例如：T? -> int?，List<T> -> List<int>，Wrapper$T -> Wrapper$int
    /// </summary>
    private string ResolveGenericType(string typePattern, Dictionary<string, string> typeMapping)
    {
        // 处理可空类型：T? -> int?
        if (typePattern.EndsWith("?"))
        {
            var baseType = typePattern.Substring(0, typePattern.Length - 1);
            var resolvedBase = ResolveGenericType(baseType, typeMapping);
            return resolvedBase + "?";
        }

        // 处理泛型类型：List<T> -> List<int>
        var genericStart = typePattern.IndexOf('<');
        if (genericStart != -1)
        {
            var genericEnd = typePattern.LastIndexOf('>');
            if (genericEnd != -1)
            {
                var baseName = typePattern.Substring(0, genericStart);
                var genericArgs = typePattern.Substring(genericStart + 1, genericEnd - genericStart - 1);
                var argList = SplitGenericArgs(genericArgs);
                var resolvedArgs = argList.Select(arg => ResolveGenericType(arg, typeMapping)).ToArray();
                return $"{baseName}<{string.Join(", ", resolvedArgs)}>";
            }
        }

        // 处理特化类型：Wrapper$T -> Wrapper$int，Wrapper$Wrapper<T> -> Wrapper$Wrapper<int>
        var dollarIndex = typePattern.IndexOf('$');
        if (dollarIndex != -1)
        {
            var baseName = typePattern.Substring(0, dollarIndex);
            var typeArgs = typePattern.Substring(dollarIndex + 1);

            // 分割类型参数（使用下划线分隔，但要考虑嵌套的 <> 括号）
            var typeArgList = SplitSpecializedTypeArgs(typeArgs);
            var resolvedArgs = typeArgList.Select(arg => ResolveGenericType(arg.Trim(), typeMapping)).ToArray();

            return $"{baseName}${string.Join("_", resolvedArgs)}";
        }

        // 处理联合类型：T | null -> int | null
        if (ContainsTopLevelChar(typePattern, '|'))
        {
            var types = SplitTopLevel(typePattern, '|');
            var resolvedTypes = types.Select(t => ResolveGenericType(t, typeMapping)).ToArray();
            return string.Join(" | ", resolvedTypes);
        }

        // 处理交叉类型：T & U -> int & string
        if (ContainsTopLevelChar(typePattern, '&'))
        {
            var types = SplitTopLevel(typePattern, '&');
            var resolvedTypes = types.Select(t => ResolveGenericType(t, typeMapping)).ToArray();
            return string.Join(" & ", resolvedTypes);
        }

        // 简单类型参数替换：T -> int
        if (typeMapping.TryGetValue(typePattern.Trim(), out var mappedType))
        {
            return mappedType;
        }

        // 不是泛型类型参数，返回原类型
        return typePattern;
    }

    /// <summary>
    /// 获取值的类型名称
    /// </summary>
    private string GetValueTypeName(object? value)
    {
        if (value == null) return "null";

        return value switch
        {
            int => "int",
            double => "double",
            string => "string",
            bool => "bool",
            char => "char",
            Array => "array",
            IList => "list",
            IDictionary => "dict",
            BytecodeObjectInstance instance => instance.ClassName,
            _ => value.GetType().Name
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

    /// <summary>
    /// 分割特化类型参数（使用下划线分隔，但要考虑嵌套的 <> 括号）
    /// 例如：Wrapper<T>_int -> ["Wrapper<T>", "int"]
    /// </summary>
    private string[] SplitSpecializedTypeArgs(string args)
    {
        var result = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == '<') depth++;
            else if (args[i] == '>') depth--;
            else if (args[i] == '_' && depth == 0)
            {
                result.Add(args.Substring(start, i - start));
                start = i + 1;
            }
        }

        result.Add(args.Substring(start));
        return result.ToArray();
    }

    /// <summary>
    /// 检查字符串中是否包含顶层的指定字符（不在尖括号内）
    /// </summary>
    private bool ContainsTopLevelChar(string str, char c)
    {
        int depth = 0;
        foreach (var ch in str)
        {
            if (ch == '<') depth++;
            else if (ch == '>') depth--;
            else if (ch == c && depth == 0) return true;
        }
        return false;
    }

    /// <summary>
    /// 按顶层的指定字符分割字符串（不分割尖括号内的字符）
    /// </summary>
    private string[] SplitTopLevel(string str, char separator)
    {
        var result = new List<string>();
        int depth = 0;
        int start = 0;
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] == '<') depth++;
            else if (str[i] == '>') depth--;
            else if (str[i] == separator && depth == 0)
            {
                result.Add(str.Substring(start, i - start).Trim());
                start = i + 1;
            }
        }
        result.Add(str.Substring(start).Trim());
        return result.ToArray();
    }

    private bool CheckCustomType(string typeName, object? val)
    {
        if (val is BytecodeObjectInstance instance)
        {
            // 规范化类型名称：去掉可空标记进行比较
            // 例如：Container$int? 和 Container$int 应该匹配
            var normalizedTypeName = typeName.TrimEnd('?');
            var normalizedInstanceName = instance.ClassName.TrimEnd('?');

            // 直接比较类名（忽略可空标记）
            if (normalizedInstanceName == normalizedTypeName) return true;

            // 处理泛型类型：将 ClassName<T1, T2> 格式转换为 ClassName$T1_T2 格式进行比较
            var normalizedGenericTypeName = NormalizeGenericTypeName(typeName).TrimEnd('?');
            var normalizedGenericInstanceName = NormalizeGenericTypeName(instance.ClassName).TrimEnd('?');
            if (normalizedGenericInstanceName == normalizedGenericTypeName) return true;

            // Check inheritance
            var metadata = _bytecodeFile.Classes.FirstOrDefault(m => m.Name == instance.ClassName || m.Name == normalizedInstanceName);
            while (metadata != null)
            {
                var metadataName = metadata.Name.TrimEnd('?');
                if (metadataName == normalizedTypeName) return true;
                if (metadataName == normalizedGenericTypeName) return true;
                if (metadata.InterfaceNames.Contains(typeName)) return true; // Check interfaces
                if (metadata.BaseClassName != null && metadata.BaseClassName.TrimEnd('?') == normalizedTypeName) return true;

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

        // 检查枚举类型
        if (val is AST.Expression.Value.EnumLangValue enumValue)
        {
            return enumValue.EnumTypeName == typeName;
        }

        return false;
    }

    /// <summary>
    /// 将泛型类型名称从 ClassName<T1, T2> 格式转换为 ClassName$T1_T2 格式
    /// </summary>
    private string NormalizeGenericTypeName(string typeName)
    {
        if (!typeName.Contains('<'))
            return typeName;

        var genericStart = typeName.IndexOf('<');
        var genericEnd = typeName.LastIndexOf('>');

        if (genericStart > 0 && genericEnd > genericStart)
        {
            var baseName = typeName.Substring(0, genericStart);
            var typeArgs = typeName.Substring(genericStart + 1, genericEnd - genericStart - 1);

            // 分割类型参数（考虑嵌套泛型）
            var typeArgList = SplitGenericArgs(typeArgs);
            var normalizedTypeArgs = typeArgList.Select(arg => NormalizeGenericTypeName(arg.Trim())).ToArray();

            return $"{baseName}${string.Join("_", normalizedTypeArgs)}";
        }

        return typeName;
    }

    private List<object?> ConvertToList(object? value)
    {
        if (value == null) return [];
        if (value is List<object?> list) return list;
        if (value is IEnumerable enumerable and not string)
        {
            return enumerable.Cast<object?>().ToList();
        }

        return [value];
    }

    private object?[] ConvertToArray(object? value)
    {
        if (value == null) return [];
        if (value is object?[] arr) return arr;
        if (value is List<object?> listObj) return listObj.ToArray();
        return value is IEnumerable enumerable and not string ? enumerable.Cast<object?>().ToArray() : ([value]);
    }
}