using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Generators;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using Old8Lang.GlobalFunctions.Core;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

/// <summary>
/// VirtualMachine - 调用分发与解析
/// </summary>
public partial class VirtualMachine
{
    private static readonly object ModuleSymbolMissing = new();
    private static readonly object ModuleSymbolNull = new();

    private object?[] PopArguments(int argCount)
    {
        var args = new object?[argCount];
        for (int i = argCount - 1; i >= 0; i--)
        {
            args[i] = _stack.Pop();
        }

        return args;
    }

    private static int ParseFunctionIndex(object[] operands, int index)
    {
        if (operands.Length <= index || operands[index] is not int functionIndex)
        {
            return -1;
        }

        return functionIndex;
    }

    private static bool CanSkipNormalizeForPositionalCall(FunctionMetadata function, int argCount)
    {
        return function.ParamsParameterIndex < 0 && argCount == function.Parameters.Count;
    }

    private bool TryInvokeResolvedFunctionPositionalFastFromStack(
        FunctionMetadata function,
        int argCount,
        FunctionMetadata.FastParameterTypeKind[] fastTypeKinds,
        ClosureEnvironment? closureEnvironment,
        ConstantPool? closureConstantPool,
        Instruction instruction)
    {
        var locals = RentLocalsBuffer(function.LocalCount);
        var fastFrame = RentCallFrame(function, locals, function.LocalCount, usesPooledLocals: function.LocalCount > 0);
        fastFrame.ClosureEnvironment = closureEnvironment;
        fastFrame.ConstantPool = closureConstantPool;

        var hasFastTypeChecks = function.ParameterTypes.Count > 0 && fastTypeKinds.Length >= argCount;
        if (function.LocalCount >= argCount)
        {
            for (var i = argCount - 1; i >= 0; i--)
            {
                var argValue = _stack.Pop();
                fastFrame.Locals[i] = argValue;

                if (!hasFastTypeChecks)
                {
                    continue;
                }

                var fastTypeKind = fastTypeKinds[i];
                if (fastTypeKind == FunctionMetadata.FastParameterTypeKind.None)
                {
                    continue;
                }

                if (!FastTypeMatches(fastTypeKind, argValue))
                {
                    var expectedType = function.ParameterTypes[i];
                    var actualType = GetValueTypeName(argValue);
                    var paramName = i < function.Parameters.Count ? function.Parameters[i] : $"参数{i}";
                    throw new TypeError(
                        GetPosition(instruction),
                        expectedType,
                        actualType,
                        $"参数 '{paramName}' 类型不匹配"
                    );
                }
            }
        }
        else
        {
            for (var i = argCount - 1; i >= 0; i--)
            {
                var argValue = _stack.Pop();
                if (i < function.LocalCount)
                {
                    fastFrame.Locals[i] = argValue;
                }

                if (!hasFastTypeChecks)
                {
                    continue;
                }

                var fastTypeKind = fastTypeKinds[i];
                if (fastTypeKind == FunctionMetadata.FastParameterTypeKind.None)
                {
                    continue;
                }

                if (!FastTypeMatches(fastTypeKind, argValue))
                {
                    var expectedType = function.ParameterTypes[i];
                    var actualType = GetValueTypeName(argValue);
                    var paramName = i < function.Parameters.Count ? function.Parameters[i] : $"参数{i}";
                    throw new TypeError(
                        GetPosition(instruction),
                        expectedType,
                        actualType,
                        $"参数 '{paramName}' 类型不匹配"
                    );
                }
            }
        }

        ExecuteFrame(fastFrame);
        return true;
    }

    private void ExecuteCallInstruction(Instruction instruction, CallFrame frame)
    {
        var operands = (object[])instruction.Operand!;
        var position = GetPosition(instruction);

        // 无命名参数: [argCount, funcName] / [argCount, funcName, funcIndex]
        if (operands.Length == 2 || operands.Length == 3)
        {
            int argCount = (int)operands[0];
            string funcName = (string)operands[1];
            int functionIndexHint = ParseFunctionIndex(operands, 2);

            // extern 函数优先保持原有行为
            if (_globals.TryGetValue(funcName, out var externFuncObj) &&
                externFuncObj is ExternFunctionWrapper externFunc)
            {
                var externArgs = PopArguments(argCount);
                var result = externFunc.Invoke(externArgs);
                _stack.Push(result);
                return;
            }

            if (TryResolveCallableFunction(frame, funcName, functionIndexHint, position, out var function, out var closureEnvironment,
                    out var closureConstantPool))
            {
                if (function.TryGetPositionalFastCallTypeKinds(argCount, out var fastTypeKinds) &&
                    TryInvokeResolvedFunctionPositionalFastFromStack(
                        function,
                        argCount,
                        fastTypeKinds,
                        closureEnvironment,
                        closureConstantPool,
                        instruction))
                {
                    return;
                }

                var resolvedArgs = PopArguments(argCount);
                var normalizedArgs = CanSkipNormalizeForPositionalCall(function, resolvedArgs.Length)
                    ? resolvedArgs
                    : NormalizeArguments(function, resolvedArgs, position);
                ValidateParameterTypes(function, normalizedArgs, instruction);
                InvokeResolvedFunction(function, normalizedArgs, closureEnvironment, closureConstantPool);
                return;
            }

            var fallbackArgs = PopArguments(argCount);
            if (TryResolveClassByName(funcName, out var classMetadata))
            {
                var obj = CreateObjectInstance(classMetadata, fallbackArgs);
                _stack.Push(obj);
                return;
            }

            var globalFunction = GlobalFunctionRegistry.Instance.TryGetFunction(funcName);
            if (globalFunction != null)
            {
                _stack.Push(globalFunction.ExecuteInVM(fallbackArgs));
                return;
            }

            throw new MethodNotFoundError(position, funcName);
        }

        // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]] / + funcIndex
        int positionalCount = (int)operands[0];
        int namedCount = (int)operands[1];
        string namedFuncName = (string)operands[2];
        string[] namedArgNames = (string[])operands[3];
        int namedFunctionIndexHint = ParseFunctionIndex(operands, 4);

        var namedArgValues = PopArguments(namedCount);
        var positionalArgs = PopArguments(positionalCount);

        if (!TryResolveCallableFunction(frame, namedFuncName, namedFunctionIndexHint, position, out var namedFunction,
                out var namedClosureEnvironment, out var namedClosureConstantPool))
        {
            throw new MethodNotFoundError(position, namedFuncName);
        }

        var arrangedArgs = ArrangeArgumentsWithNamed(namedFunction, positionalArgs, namedArgNames, namedArgValues);
        var normalizedNamedArgs = NormalizeArguments(namedFunction, arrangedArgs, position);
        ValidateParameterTypes(namedFunction, normalizedNamedArgs, instruction);
        InvokeResolvedFunction(namedFunction, normalizedNamedArgs, namedClosureEnvironment, namedClosureConstantPool);
    }

    private void ExecuteCallDynamicInstruction(Instruction instruction, CallFrame frame)
    {
        int argCount = (int)instruction.Operand!;
        var args = PopArguments(argCount);
        var funcObj = _stack.Pop();

        if (funcObj is ClosureValue closure)
        {
            var funcMeta = closure.Function;
            if (funcMeta.IsGenerator)
            {
                PushGeneratorForFunction(funcMeta, args);
                return;
            }

            if (CanSkipNormalizeForPositionalCall(funcMeta, args.Length))
            {
                var locals = RentLocalsBuffer(funcMeta.LocalCount);
                var fastFrame = RentCallFrame(funcMeta, locals, funcMeta.LocalCount, usesPooledLocals: funcMeta.LocalCount > 0);
                fastFrame.ClosureEnvironment = closure.CapturedVariables;
                fastFrame.ConstantPool = closure.ConstantPool;
                int copyLen = Math.Min(args.Length, funcMeta.LocalCount);
                for (int i = 0; i < copyLen; i++)
                    fastFrame.Locals[i] = args[i];
                ExecuteFrame(fastFrame);
                return;
            }

            CallClosureFunction(funcMeta, args, closure.CapturedVariables, closure.ConstantPool);
            return;
        }

        if (funcObj is FunctionMetadata functionMetadata)
        {
            if (functionMetadata.IsGenerator)
            {
                PushGeneratorForFunction(functionMetadata, args);
                return;
            }

            CallFunction(functionMetadata, args);
            return;
        }

        throw new TypeError(GetPosition(instruction), $"尝试调用非函数对象: {funcObj?.GetType().Name}");
    }

    private void InvokeResolvedFunction(FunctionMetadata function, object?[] args,
        ClosureEnvironment? closureEnvironment, ConstantPool? closureConstantPool)
    {
        if (function.IsGenerator)
        {
            PushGeneratorForFunction(function, args);
            return;
        }

        if (closureEnvironment != null)
        {
            CallClosureFunction(function, args, closureEnvironment, closureConstantPool);
        }
        else
        {
            CallFunction(function, args);
        }
    }

    private void PushGeneratorForFunction(FunctionMetadata function, object?[] args)
    {
        if (function.IsAsync)
        {
            var asyncGeneratorId = _nextAsyncGeneratorId++;
            _asyncGenerators[asyncGeneratorId] = new AsyncGeneratorState(function, args);
            _stack.Push(new BytecodeAsyncGeneratorLangValue(asyncGeneratorId, this));
            return;
        }

        var generatorId = _nextGeneratorId++;
        _generators[generatorId] = new GeneratorState(function, args);
        _stack.Push(new BytecodeGeneratorLangValue(generatorId, this));
    }

    private bool TryResolveCallableFunction(CallFrame frame, string funcName, int functionIndexHint,
        SourcePosition position, out FunctionMetadata function, out ClosureEnvironment? closureEnvironment,
        out ConstantPool? closureConstantPool)
    {
        function = null!;
        closureEnvironment = null;
        closureConstantPool = null;

        // 新协议优先：函数索引命中时快速直达
        //
        // 两类函数例外，必须走下面的绑定查找：
        // 1. 带装饰器的函数：装饰器在运行期把包装后的函数写回同名全局变量，
        //    函数体本身已经不是调用该名字时应该执行的东西。若按索引直达，
        //    装饰器会被静默绕过（@twice 包装的 inc(10) 得到 11）。
        // 2. 捕获了外层局部变量的嵌套具名函数：捕获值按值快照存在声明的那个
        //    ClosureValue 里，索引快捷路径给出的裸 FunctionMetadata 不带环境，
        //    函数体里对被捕获变量的读取会退化成查全局表并报「名称未定义」。
        var isDecoratedFunction = false;
        if (functionIndexHint >= 0 && functionIndexHint < _bytecodeFile.Functions.Count)
        {
            var indexedFunction = _bytecodeFile.Functions[functionIndexHint];
            if (indexedFunction.Name == funcName)
            {
                if (!indexedFunction.IsDecorated && !indexedFunction.NeedsClosureEnvironment)
                {
                    function = indexedFunction;
                    return true;
                }

                isDecoratedFunction = indexedFunction.IsDecorated;
            }
        }

        if (frame.ClosureEnvironment != null && frame.ClosureEnvironment.TryGetValue(funcName, out var closureFunc))
        {
            if (closureFunc is FunctionMetadata closureFunction)
            {
                function = closureFunction;
                return true;
            }

            if (closureFunc is ClosureValue closureValue)
            {
                function = closureValue.Function;
                closureEnvironment = closureValue.CapturedVariables;
                closureConstantPool = closureValue.ConstantPool;
                return true;
            }
        }

        if (_globals.TryGetValue(funcName, out var funcObj))
        {
            if (funcObj is FunctionMetadata globalFunction)
            {
                function = globalFunction;
                return true;
            }

            if (funcObj is ClosureValue globalClosure)
            {
                function = globalClosure.Function;
                closureEnvironment = globalClosure.CapturedVariables;
                closureConstantPool = globalClosure.ConstantPool;
                return true;
            }

            // 带装饰器的函数，其全局绑定就是调用该名字时该执行的东西。
            // 绑定不是函数说明装饰器写错了（例如返回了 123），此时若继续往下退回
            // 原函数，用户只会看到装饰器“不生效”，拿不到任何提示。
            if (isDecoratedFunction)
            {
                throw new TypeError(position,
                    $"装饰器返回值不是函数，无法调用 '{funcName}'（实际类型: {funcObj?.GetType().Name ?? "null"}）");
            }
        }

        if (_functionByName.TryGetValue(funcName, out var cachedFunction))
        {
            function = cachedFunction;
            return true;
        }

        foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
        {
            if (!TryGetModuleSymbolCached(loadedModuleName, funcName, out var symbol))
            {
                continue;
            }

            if (symbol is FunctionMetadata moduleFunction)
            {
                function = moduleFunction;
                return true;
            }

            if (symbol is ClosureValue moduleClosure)
            {
                function = moduleClosure.Function;
                closureEnvironment = moduleClosure.CapturedVariables;
                closureConstantPool = moduleClosure.ConstantPool;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveClassByName(string className, out ClassMetadata classMetadata)
    {
        classMetadata = null!;

        if (_globals.TryGetValue(className, out var globalValue) && globalValue is ClassMetadata globalClassMetadata)
        {
            classMetadata = globalClassMetadata;
            return true;
        }

        if (_classByName.TryGetValue(className, out var cachedClassMetadata))
        {
            classMetadata = cachedClassMetadata;
            return true;
        }

        foreach (var loadedModuleName in _moduleRegistry.GetLoadedModuleNames())
        {
            if (!TryGetModuleSymbolCached(loadedModuleName, className, out var symbol))
            {
                continue;
            }

            if (symbol is ClassMetadata moduleClassMetadata)
            {
                classMetadata = moduleClassMetadata;
                return true;
            }
        }

        return false;
    }

    private bool TryGetModuleSymbolCached(string moduleName, string symbolName, out object? symbol)
    {
        var key = (moduleName, symbolName);
        if (_moduleSymbolCache.TryGetValue(key, out var cached))
        {
            if (ReferenceEquals(cached, ModuleSymbolMissing))
            {
                symbol = null;
                return false;
            }

            symbol = ReferenceEquals(cached, ModuleSymbolNull) ? null : cached;
            return true;
        }

        try
        {
            symbol = _moduleRegistry.GetModuleSymbol(moduleName, symbolName);
            _moduleSymbolCache[key] = symbol ?? ModuleSymbolNull;
            return true;
        }
        catch
        {
            _moduleSymbolCache[key] = ModuleSymbolMissing;
            symbol = null;
            return false;
        }
    }
}
