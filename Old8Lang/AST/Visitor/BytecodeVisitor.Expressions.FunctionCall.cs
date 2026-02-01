using Old8Lang.AST.Expression;
using Old8Lang.Bytecode.Core;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// BytecodeVisitor - 函数调用
/// </summary>
public partial class BytecodeVisitor
{
    public Instruction? VisitFunctionCallExpression(FunctionCallExpression node)
    {
        int positionalCount = node.Arguments.Count;
        int namedCount = node.NamedArguments?.Count ?? 0;

        // 检查函数表达式的类型
        bool isComplexExpression = node.FunctionExpression is not LangId;
        bool isClassName = false;
        string funcName = "";

        if (!isComplexExpression)
        {
            funcName = ((LangId)node.FunctionExpression).IdName;
            isClassName = _compiler.IsClassName(funcName);
        }

        // 如果是类实例化
        if (!isComplexExpression && isClassName)
        {
            // 类实例化: Person(arg1, arg2)
            // 1. 生成 NewObject 指令创建对象
            Emit(OpCode.NewObject, funcName);

            // 2. 查找构造函数：优先 init，其次与类名相同的方法
            var classMetadata = _compiler.GetClassMetadata(funcName);
            string? constructorName = null;

            if (classMetadata != null)
            {
                // 优先查找 init 方法
                if (classMetadata.Methods.Any(m => m.Name == "init"))
                {
                    constructorName = "init";
                }
                // 其次查找与类名相同的方法
                else if (classMetadata.Methods.Any(m => m.Name == funcName))
                {
                    constructorName = funcName;
                }
            }

            // 3. 如果找到构造函数，调用它
            if (constructorName != null)
            {
                // 复制对象引用，因为 CallMethod 会消耗它
                Emit(OpCode.Dup);

                // 生成位置参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArguments)
                    {
                        namedArg.Value.Accept(this);
                    }
                }

                // 调用构造函数
                // CallMethod 操作数: [argCount, methodName]
                // argCount 包括对象本身 + 实际参数
                int totalArgCount = positionalCount + namedCount + 1; // +1 for 'this'

                if (namedCount > 0)
                {
                    var namedArgNames = node.NamedArguments.Select(na => na.Name).ToArray();
                    Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName, namedArgNames });
                }
                else
                {
                    Emit(OpCode.CallMethod, new object[] { totalArgCount, constructorName });
                }

                // 构造函数返回 void，不需要弹出返回值
            }
        }
        else
        {
            // 普通函数调用或复杂表达式调用

            // 如果是复杂表达式，使用 CallIndirect
            if (isComplexExpression)
            {
                // 复杂表达式调用: lambda(arg1, arg2) 或 map(lambda, array)
                // 1. 编译函数表达式
                node.FunctionExpression.Accept(this);

                // 2. 生成参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                if (namedCount > 0)
                {
                    throw new Exception("字节码模式下的动态函数调用暂不支持命名参数");
                }

                // 3. 生成 CallDynamic 指令
                Emit(OpCode.CallDynamic, positionalCount);
            }
            // 1. 检查是否是局部变量 holding a function (Lambda调用)
            else if (_compiler.IsLocalVariable(funcName))
            {
                // 加载函数对象到栈底
                int localIndex = _compiler.GetLocalIndex(funcName);
                Emit(OpCode.LoadLocal, localIndex);

                // 生成参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                if (namedCount > 0)
                {
                    throw new Exception("字节码模式下的动态函数调用暂不支持命名参数");
                }

                // 调用 CallDynamic
                Emit(OpCode.CallDynamic, positionalCount);
            }
            else
            {
                // 普通静态/原生函数调用
                // 生成位置参数代码
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // 生成命名参数的值
                if (namedCount > 0)
                {
                    foreach (var namedArg in node.NamedArguments)
                    {
                        namedArg.Value.Accept(this);
                    }
                }

                // 检查是否是原生函数
                bool isNative = _compiler.IsNativeFunction(funcName);
                
                // 检查是否是异步函数
                bool isAsync = _compiler.IsAsyncFunction(funcName);

                if (namedCount > 0)
                {
                    if (isAsync)
                    {
                        throw new Exception("异步函数暂不支持命名参数调用");
                    }

                    // 有命名参数: [positionalCount, namedCount, funcName, namedArgNames[]]
                    var namedArgNames = node.NamedArguments.Select(na => na.Name).ToArray();
                    Emit(isNative ? OpCode.CallNative : OpCode.Call,
                        new object[] { positionalCount, namedCount, funcName, namedArgNames });
                }
                else
                {
                    // 无命名参数: [argCount, funcName]
                    if (isAsync)
                    {
                        Emit(OpCode.CallAsync, new object[] { positionalCount, funcName });
                    }
                    else
                    {
                        Emit(isNative ? OpCode.CallNative : OpCode.Call,
                            new object[] { positionalCount, funcName });
                    }
                }
            }
        }

        return null;
    }


}
