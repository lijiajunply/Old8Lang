using System.Collections;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Closures;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

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

    /// <summary>
    /// 执行单条指令
    /// </summary>
    private void ExecuteInstruction(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {

            // === 栈操作 ===
            case OpCode.Nop:
            case OpCode.LoadConst:
            case OpCode.LoadLocal:
            case OpCode.StoreLocal:
            case OpCode.LoadGlobal:
            case OpCode.StoreGlobal:
            case OpCode.Pop:
            case OpCode.Dup:
            case OpCode.LoadNull:
            case OpCode.LoadTrue:
            case OpCode.LoadFalse:
            case OpCode.Swap:
                ExecuteStackOperation(instruction, frame);
                break;

            // === 算术运算 ===
            case OpCode.Add:
            case OpCode.Sub:
            case OpCode.Mul:
            case OpCode.Div:
            case OpCode.Mod:
            case OpCode.Pow:
            case OpCode.Neg:
                ExecuteArithmeticOperation(instruction, frame);
                break;

            // === 比较运算 ===
            case OpCode.Equal:
            case OpCode.NotEqual:
            case OpCode.Greater:
            case OpCode.Less:
            case OpCode.GreaterEqual:
            case OpCode.LessEqual:
                ExecuteComparisonOperation(instruction, frame);
                break;

            // === 逻辑运算 ===
            case OpCode.And:
            case OpCode.Or:
            case OpCode.Not:
                ExecuteLogicalOperation(instruction, frame);
                break;

            // === 控制流 ===
            case OpCode.Jump:
            case OpCode.JumpIfFalse:
            case OpCode.JumpIfTrue:
            case OpCode.Call:
            case OpCode.CallNative:
            case OpCode.CallDynamic:
            case OpCode.Return:
            case OpCode.ReturnVoid:
            case OpCode.Break:
            case OpCode.Continue:
            case OpCode.MakeFunction:
            case OpCode.MakeClosure:
                ExecuteControlFlowOperation(instruction, frame);
                break;

            // === 容器操作 ===
            case OpCode.NewArray:
            case OpCode.NewList:
            case OpCode.NewTuple:
            case OpCode.NewDict:
            case OpCode.ArrayLength:
            case OpCode.GetIndex:
            case OpCode.SetIndex:
            case OpCode.NewRange:
                ExecuteContainerOperation(instruction, frame);
                break;

            // === 迭代器操作 ===
            case OpCode.GetIterator:
            case OpCode.IteratorMoveNext:
            case OpCode.IteratorCurrent:
            case OpCode.Slice:
            case OpCode.NewGroupDict:
            case OpCode.AddToGroup:
            case OpCode.GroupDictToList:
                ExecuteIteratorOperation(instruction, frame);
                break;

            // === 类型操作 ===
            case OpCode.Cast:
            case OpCode.IsType:
            case OpCode.TypeOf:
            case OpCode.DefineEnum:
            case OpCode.DefineInterface:
            case OpCode.DefineMixin:
            case OpCode.ApplyMixin:
            case OpCode.CheckInterface:
                ExecuteTypeOperation(instruction, frame);
                break;

            // === 并发原语 ===
            case OpCode.MutexCreate:
            case OpCode.MutexLock:
            case OpCode.MutexUnlock:
            case OpCode.MutexDispose:
            case OpCode.ChannelCreate:
            case OpCode.ChannelSend:
            case OpCode.ChannelReceive:
            case OpCode.ChannelClose:
            case OpCode.ChannelTrySend:
            case OpCode.ChannelTryReceive:
            case OpCode.SemaphoreCreate:
            case OpCode.SemaphoreAcquire:
            case OpCode.SemaphoreRelease:
                ExecuteConcurrencyOperation(instruction, frame);
                break;

            // === Thread 支持 ===
            case OpCode.ThreadCreate:
            case OpCode.ThreadStart:
            case OpCode.ThreadJoin:
            case OpCode.ThreadIsAlive:
            case OpCode.ThreadDispose:
                ExecuteThreadOperation(instruction, frame);
                break;

            // === 异步支持 ===
            case OpCode.NewTask:
            case OpCode.CallAsync:
            case OpCode.Await:
            case OpCode.NewAsyncGenerator:
            case OpCode.CallAsyncGenerator:
            case OpCode.AwaitYield:
            case OpCode.Yield:
                ExecuteAsyncOperation(instruction, frame);
                break;

            // === 异常处理 ===
            case OpCode.Throw:
            case OpCode.TryBegin:
            case OpCode.TryEnd:
            case OpCode.CatchBegin:
            case OpCode.CatchEnd:
            case OpCode.FinallyBegin:
            case OpCode.FinallyEnd:
            case OpCode.GetField:
            case OpCode.SetField:
            case OpCode.GetSuperField:
            case OpCode.SetSuperField:
            case OpCode.NewObject:
            case OpCode.CallMethod:
            case OpCode.LoadSuper:
            case OpCode.LoadThis:
            case OpCode.CallSuperMethod:
                ExecuteExceptionOperation(instruction, frame);
                break;

            // === 模块操作 ===
            case OpCode.LoadModule:
            case OpCode.ImportSymbol:
            case OpCode.ImportSymbolAs:
            case OpCode.ImportAll:
            case OpCode.GetModuleSymbol:
            case OpCode.DebugPrint:
                ExecuteModuleOperation(instruction, frame);
                break;

            // === Defer 支持 ===
            case OpCode.Defer:
            case OpCode.ExecuteDefers:
            case OpCode.LoadExtern:
            case OpCode.CallExtern:
            case OpCode.DisposeResource:
            case OpCode.ImportNative:
                ExecuteDeferOperation(instruction, frame);
                break;

            default:
                throw new InvalidOperationError(GetPosition(instruction), 
                    $"未知的指令: {instruction.OpCode}");
        }
    }

    // === 辅助方法 ===

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

        // 6. Function Types (function, function<int>, function<int, string, bool>)
        var typeNameLowerForFunc = typeName.ToLower();
        if (typeNameLowerForFunc == "function" || typeNameLowerForFunc.StartsWith("function<"))
        {
            return CheckFunctionTypeMatch(typeName, val);
        }

        // 7. Basic Types (with implicit numeric conversion: int -> double)
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
            if (function.GenericTypeMapping is { Count: > 0 })
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
            FunctionMetadata func => GetFunctionTypeString(func),
            ClosureValue closure => GetFunctionTypeString(closure.Function),
            BytecodeObjectInstance instance => instance.ClassName,
            _ => value.GetType().Name
        };
    }

    /// <summary>
    /// 获取函数的完整类型字符串
    /// 格式: function<param1, param2, ..., returnType>
    /// 规则: 最后一个泛型参数是返回值类型，前面的都是参数类型
    /// </summary>
    private string GetFunctionTypeString(FunctionMetadata func)
    {
        // 如果没有返回类型注解，返回基本 function 类型
        if (string.IsNullOrEmpty(func.ReturnType))
        {
            return "function";
        }

        var typeParams = new List<string>();

        // 添加参数类型
        if (func.ParameterTypes is { Count: > 0 })
        {
            foreach (var paramType in func.ParameterTypes)
            {
                if (string.IsNullOrEmpty(paramType))
                {
                    // 如果参数没有类型注解，返回基本 function 类型
                    return "function";
                }
                typeParams.Add(paramType);
            }
        }

        // 添加返回类型
        typeParams.Add(func.ReturnType);

        // 如果只有返回类型（无参数函数），格式为 function<returnType>
        // 如果有参数，格式为 function<param1, param2, ..., returnType>
        return $"function<{string.Join(", ", typeParams)}>";
    }

    /// <summary>
    /// 检查函数类型匹配
    /// 规则：
    /// - 基本 function 类型兼容任何函数类型
    /// - 泛型函数类型 function<...> 需要检查参数数量和类型匹配
    /// - 最后一个泛型参数是返回类型，前面的是参数类型
    /// </summary>
    private bool CheckFunctionTypeMatch(string expectedType, object? val)
    {
        // 值必须是函数类型
        if (val is not FunctionMetadata && val is not ClosureValue)
        {
            return false;
        }

        // 获取实际的函数元数据
        var func = val is ClosureValue closure ? closure.Function : (FunctionMetadata)val;

        // 如果期望类型是基本 function（无泛型参数），兼容任何函数
        if (!expectedType.Contains('<'))
        {
            return true;
        }

        // 获取实际函数的类型字符串
        var actualType = GetFunctionTypeString(func);

        // 如果实际类型是基本 function（无泛型参数），在宽松模式下允许
        // 因为实际函数可能没有完整的类型注解
        if (!actualType.Contains('<'))
        {
            return true;
        }

        // 解析期望类型的泛型参数
        var expectedParams = ParseFunctionTypeParams(expectedType);
        var actualParams = ParseFunctionTypeParams(actualType);

        if (expectedParams == null || actualParams == null)
        {
            return false;
        }

        // 检查参数数量是否匹配
        if (expectedParams.Length != actualParams.Length)
        {
            return false;
        }

        // 检查每个类型参数是否兼容
        for (int i = 0; i < expectedParams.Length; i++)
        {
            var expectedParam = expectedParams[i].Trim();
            var actualParam = actualParams[i].Trim();

            // 比较类型名称是否相同（忽略大小写）
            if (!AreTypeNamesCompatible(expectedParam, actualParam))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查两个类型名称是否兼容
    /// </summary>
    private bool AreTypeNamesCompatible(string expectedType, string actualType)
    {
        // 完全匹配（忽略大小写）
        if (expectedType.Equals(actualType, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // any 类型兼容任何类型
        if (expectedType.Equals("any", StringComparison.OrdinalIgnoreCase) ||
            expectedType.Equals("object", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // int 可以隐式转换为 double
        if (expectedType.Equals("double", StringComparison.OrdinalIgnoreCase) &&
            actualType.Equals("int", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // 处理可空类型
        if (expectedType.EndsWith("?"))
        {
            var baseExpected = expectedType.Substring(0, expectedType.Length - 1);
            if (actualType.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return AreTypeNamesCompatible(baseExpected, actualType);
        }

        // 处理泛型类型（递归检查）
        if (expectedType.Contains('<') && actualType.Contains('<'))
        {
            var expectedBase = expectedType.Substring(0, expectedType.IndexOf('<'));
            var actualBase = actualType.Substring(0, actualType.IndexOf('<'));

            if (!expectedBase.Equals(actualBase, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var expectedArgs = ParseFunctionTypeParams(expectedType);
            var actualArgs = ParseFunctionTypeParams(actualType);

            if (expectedArgs == null || actualArgs == null || expectedArgs.Length != actualArgs.Length)
            {
                return false;
            }

            for (int i = 0; i < expectedArgs.Length; i++)
            {
                if (!AreTypeNamesCompatible(expectedArgs[i].Trim(), actualArgs[i].Trim()))
                {
                    return false;
                }
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 解析函数类型的泛型参数
    /// 例如: "function<int, string, bool>" -> ["int", "string", "bool"]
    /// </summary>
    private string[]? ParseFunctionTypeParams(string functionType)
    {
        var genericStart = functionType.IndexOf('<');
        if (genericStart < 0)
        {
            return null;
        }

        var genericEnd = FindMatchingBracket(functionType, genericStart);
        if (genericEnd < 0)
        {
            return null;
        }

        var paramsPart = functionType.Substring(genericStart + 1, genericEnd - genericStart - 1);
        return SplitGenericArgs(paramsPart);
    }

    /// <summary>
    /// 查找匹配的右尖括号
    /// </summary>
    private int FindMatchingBracket(string text, int startIndex)
    {
        int depth = 0;
        for (int i = startIndex; i < text.Length; i++)
        {
            if (text[i] == '<') depth++;
            else if (text[i] == '>') depth--;
            if (depth == 0) return i;
        }
        return -1;
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
                if (metadata.Mixins.Contains(typeName)) return true; // Check mixins
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
