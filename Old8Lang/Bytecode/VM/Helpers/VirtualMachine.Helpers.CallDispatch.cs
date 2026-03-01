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
            var args = PopArguments(argCount);

            // extern 函数优先保持原有行为
            if (_globals.TryGetValue(funcName, out var externFuncObj) &&
                externFuncObj is ExternFunctionWrapper externFunc)
            {
                var result = externFunc.Invoke(args);
                _stack.Push(result);
                return;
            }

            if (TryResolveCallableFunction(frame, funcName, functionIndexHint, out var function, out var closureEnvironment,
                    out var closureConstantPool))
            {
                var normalizedArgs = CanSkipNormalizeForPositionalCall(function, args.Length)
                    ? args
                    : NormalizeArguments(function, args, position);
                ValidateParameterTypes(function, normalizedArgs, instruction);
                InvokeResolvedFunction(function, normalizedArgs, closureEnvironment, closureConstantPool);
                return;
            }

            if (TryResolveClassByName(funcName, out var classMetadata))
            {
                var obj = CreateObjectInstance(classMetadata, args);
                _stack.Push(obj);
                return;
            }

            var globalFunction = GlobalFunctionRegistry.Instance.TryGetFunction(funcName);
            if (globalFunction != null)
            {
                _stack.Push(globalFunction.ExecuteInVM(args));
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

        if (!TryResolveCallableFunction(frame, namedFuncName, namedFunctionIndexHint, out var namedFunction,
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
        Dictionary<string, object?>? closureEnvironment, ConstantPool? closureConstantPool)
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
        out FunctionMetadata function, out Dictionary<string, object?>? closureEnvironment, out ConstantPool? closureConstantPool)
    {
        function = null!;
        closureEnvironment = null;
        closureConstantPool = null;

        // 新协议优先：函数索引命中时快速直达
        if (functionIndexHint >= 0 && functionIndexHint < _bytecodeFile.Functions.Count)
        {
            var indexedFunction = _bytecodeFile.Functions[functionIndexHint];
            if (indexedFunction.Name == funcName)
            {
                function = indexedFunction;
                return true;
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
