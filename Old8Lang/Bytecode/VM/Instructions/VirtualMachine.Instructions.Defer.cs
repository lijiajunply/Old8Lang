using System.Reflection;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Statement;
using Old8Lang.Bytecode.Core;
using Old8Lang.Bytecode.Interop;
using Old8Lang.Bytecode.Metadata;
using Old8Lang.Error;

// ReSharper disable once CheckNamespace
namespace Old8Lang.Bytecode.VM;

public partial class VirtualMachine
{
    /// <summary>
    /// 执行Defer 支持指令
    /// </summary>
    private void ExecuteDeferOperation(Instruction instruction, CallFrame frame)
    {
        switch (instruction.OpCode)
        {
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
}
