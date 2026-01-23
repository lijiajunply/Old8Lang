using System.Collections;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;
using ClassMetadata = Old8Lang.Bytecode.Metadata.ClassMetadata;

namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行异常处理指令
    /// </summary>
    private void ExecuteExceptionOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
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

                // 如果是 ClassMetadata（访问静态字段或嵌套类）
                if (obj is ClassMetadata classMetadata)
                {
                    // 首先检查是否是嵌套类访问
                    // 嵌套类的完整名称格式为 "OuterClass.NestedClass"
                    string nestedClassName = classMetadata.Name + "." + fieldName;
                    var nestedClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == nestedClassName);

                    // 如果在当前字节码文件中没找到，从全局变量中查找
                    if (nestedClass == null && _globals.TryGetValue(nestedClassName, out var globalNestedClass) &&
                        globalNestedClass is ClassMetadata importedNestedClass)
                    {
                        nestedClass = importedNestedClass;
                    }

                    if (nestedClass != null)
                    {
                        // 这是嵌套类访问，将嵌套类的 ClassMetadata 压入栈
                        _stack.Push(nestedClass);
                    }
                    else if (classMetadata.StaticFieldValues.TryGetValue(fieldName, out var staticValue))
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

                    // 特殊处理：Old8Exception 的 Message 属性返回 OriginalMessage
                    if (obj is Old8Exception old8Ex && fieldName == "Message")
                    {
                        _stack.Push(old8Ex.OriginalMessage);
                    }
                    else
                    {
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

                // 检查是否是 ClassMetadata（静态方法调用或嵌套类访问）
                if (obj is ClassMetadata staticClassMetadata)
                {
                    // 首先检查是否是嵌套类访问
                    // 嵌套类的完整名称格式为 "OuterClass.NestedClass"
                    string nestedClassName = staticClassMetadata.Name + "." + methodName;
                    var nestedClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == nestedClassName);

                    // 如果在当前字节码文件中没找到，从全局变量中查找
                    if (nestedClass == null && _globals.TryGetValue(nestedClassName, out var globalNestedClass) &&
                        globalNestedClass is ClassMetadata importedNestedClass)
                    {
                        nestedClass = importedNestedClass;
                    }

                    if (nestedClass != null)
                    {
                        // 这是嵌套类访问
                        // 如果有参数（argCount > 1，因为第一个参数是类本身），说明是调用构造函数，需要创建对象实例
                        if (argCount > 1 || (argCount == 1 && args.Length == 0))
                        {
                            // 创建嵌套类的实例
                            // 首先创建对象
                            var nestedObj = new BytecodeObjectInstance(nestedClassName);

                            // 初始化所有字段为默认值
                            var allFields = new List<FieldMetadata>();
                            var currentClass = nestedClass;
                            while (currentClass != null)
                            {
                                allFields.AddRange(currentClass.Fields);
                                if (!string.IsNullOrEmpty(currentClass.BaseClassName))
                                {
                                    currentClass = _bytecodeFile.Classes.FirstOrDefault(c => c.Name == currentClass.BaseClassName);
                                }
                                else
                                {
                                    break;
                                }
                            }

                            foreach (var field in allFields)
                            {
                                if (!nestedObj.Fields.ContainsKey(field.Name))
                                {
                                    object? defaultValue = null;
                                    if (field.DefaultValueIndex >= 0 && field.DefaultValueIndex < _bytecodeFile.ConstantPool.Count)
                                    {
                                        defaultValue = _bytecodeFile.ConstantPool.GetConstant(field.DefaultValueIndex);
                                    }
                                    else if (field.IsDefaultNull)
                                    {
                                        defaultValue = null;
                                    }
                                    nestedObj.Fields[field.Name] = defaultValue;
                                }
                            }

                            // 查找并调用构造函数（init方法）
                            var initMethod = nestedClass.Methods.FirstOrDefault(m => m.Name == "init");
                            if (initMethod != null)
                            {
                                // 调用构造函数，传入对象实例和参数
                                var initArgs = new object?[args.Length + 1];
                                initArgs[0] = nestedObj;
                                Array.Copy(args, 0, initArgs, 1, args.Length);
                                CallFunction(initMethod.Function, initArgs);
                            }

                            // 将对象实例压入栈
                            _stack.Push(nestedObj);
                        }
                        else
                        {
                            // 没有参数，只是访问嵌套类，将嵌套类的 ClassMetadata 压入栈
                            _stack.Push(nestedClass);
                        }
                        break;
                    }

                    // 不是嵌套类，尝试查找静态方法
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

            case OpCode.LoadThis:
            {
                // 加载当前实例（this）
                // this 是方法的第一个参数
                var currentFrame = _callStack.Peek();

                // 优先从 Arguments 中获取 this（第一个参数）
                if (currentFrame.Arguments is { Length: > 0 })
                {
                    var thisInstance = currentFrame.Arguments[0];
                    if (thisInstance == null)
                    {
                        throw new StateError(GetPosition(instruction), "this 只能在实例方法中使用");
                    }

                    _stack.Push(thisInstance);
                }
                // 如果 Arguments 为空，尝试从 Locals 获取
                else if (currentFrame.Locals.Length > 0)
                {
                    var thisInstance = currentFrame.Locals[0];
                    if (thisInstance == null)
                    {
                        throw new StateError(GetPosition(instruction), "this 只能在实例方法中使用");
                    }

                    _stack.Push(thisInstance);
                }
                else
                {
                    throw new StateError(GetPosition(instruction), "this 只能在实例方法中使用");
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

        }
    }
}
