using Old8Lang.Syntax;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Linq;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Statement;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Compiler.Verification;
using Old8Lang.Concurrency;
using Old8Lang.Error;
using Old8Lang.ExternProviders;
using Old8Lang.GlobalFunctions.Core;
using Old8Lang.InstanceMethods.Core;
using Old8Lang.Interpreter;
using Old8Lang.ModuleSystem.Core;
using Old8Lang.ModuleSystem.Loading;
using Old8Lang.ModuleSystem.Resolution;
using Old8Lang.ModuleSystem.StandardLibrary;
using Old8Lang.ModuleSystem.Symbols;
using Old8Lang.ProjectManagement;
using Old8Lang.Runtime;
using Old8Lang.TypeSystem;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Expression 和 Value 节点的简化实现
/// </summary>
public partial class CompilerVisitor
{
    // ===== Expression 节点实现 =====

    public object? VisitAsyncStreamExpression(AsyncStreamExpression node)
    {

        // 对于异步流，生成类似于异步函数的 IL 代码
        // 目前实现一个简化版本
        ilGenerator.Emit(OpCodes.Ldnull);

        return null;
    
    }

    public object? VisitAwaitExpression(AwaitExpression node)
    {
        // 加载表达式的值
        node.Expression.Accept(this);

        // 获取表达式的输出类型
        var exprType = node.Expression.Accept(new TypeInferenceVisitor(local));

        // 检查是否在异步状态机中
        if (local.AsyncStateMachineGenerator != null && Old8Lang.Compiler.Compiler.EnableAsyncStateMachineAwait)
        {
            if (exprType != null) node.GenerateAsyncAwait(ilGenerator, local, exprType);
            return null;
        }

        // --- 同步等待逻辑 (不在异步函数中) ---

        // 确保表达式类型是 Task 或 Task<object>
        if (exprType == typeof(Task))
        {
            // 对于 Task 类型，简化处理：直接调用 GetAwaiter() 和 GetResult()
            // Task.GetAwaiter() 返回 TaskAwaiter（非泛型）
            // 我们需要等待任务完成，然后返回 null

            // 栈上现在是 Task 对象
            // 保存 Task 到局部变量
            var taskLocal = ilGenerator.DeclareLocal(typeof(Task));
            ilGenerator.Emit(OpCodes.Stloc, taskLocal);

            // 加载 Task，调用 GetAwaiter()
            ilGenerator.Emit(OpCodes.Ldloc, taskLocal);
            var taskGetAwaiterMethod = typeof(Task).GetMethod("GetAwaiter")!;
            ilGenerator.Emit(OpCodes.Callvirt, taskGetAwaiterMethod);

            // 获取 TaskAwaiter（非泛型）
            var taskAwaiterType = taskGetAwaiterMethod.ReturnType;
            var taskAwaiterLocal = ilGenerator.DeclareLocal(taskAwaiterType);
            ilGenerator.Emit(OpCodes.Stloc, taskAwaiterLocal);

            // 调用 GetResult() 等待完成（同步等待）
            ilGenerator.Emit(OpCodes.Ldloca, taskAwaiterLocal);
            var taskGetResultMethod = taskAwaiterType.GetMethod("GetResult")!;
            ilGenerator.Emit(OpCodes.Call, taskGetResultMethod);

            // Task.GetResult() 返回 void，我们需要返回 null（object）
            ilGenerator.Emit(OpCodes.Ldnull);

            // 直接返回，跳过后续的 Task<object> 处理
            return null;
        }

        // 现在栈上是 Task<object>，调用 GetAwaiter() 方法获取等待器
        // 注意：这里假设如果是泛型 Task，一定是 Task<object>，或者至少有 GetAwaiter
        // 如果是 Task<T>，GetAwaiter 返回 TaskAwaiter<T>
        var getAwaiterMethod = exprType?.GetMethod("GetAwaiter")!;
        ilGenerator.Emit(OpCodes.Callvirt, getAwaiterMethod);

        // 获取等待器类型
        var awaiterType = getAwaiterMethod.ReturnType;

        // 将等待器保存到局部变量
        var awaiterLocal = ilGenerator.DeclareLocal(awaiterType);
        ilGenerator.Emit(OpCodes.Stloc, awaiterLocal);

        // 检查等待器是否已完成
        var isCompletedProperty = awaiterType.GetProperty("IsCompleted")!;
        var isCompletedGetMethod = isCompletedProperty.GetGetMethod()!;

        // 定义标签
        var completedLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        // 加载等待器地址，调用 IsCompleted 属性
        // TaskAwaiter<T> 是结构体，需要使用 Ldloca 和 Call
        ilGenerator.Emit(OpCodes.Ldloca, awaiterLocal);
        ilGenerator.Emit(OpCodes.Call, isCompletedGetMethod);

        // 如果已完成，跳转到获取结果
        ilGenerator.Emit(OpCodes.Brtrue_S, completedLabel);

        // 未完成，这里需要异步状态机支持
        // 目前生成同步等待代码，后续将替换为异步状态机切换

        // 加载等待器地址，调用 GetResult() 方法获取结果（同步等待，直到任务完成）
        ilGenerator.MarkLabel(completedLabel);
        ilGenerator.Emit(OpCodes.Ldloca, awaiterLocal);
        var getResultMethod = awaiterType.GetMethod("GetResult")!;
        ilGenerator.Emit(OpCodes.Call, getResultMethod);

        // 结束
        ilGenerator.MarkLabel(endLabel);
    
    
        return null;
    }

    public object? VisitFunctionCallExpression(FunctionCallExpression node)
    {

        // 1. 优先尝试直接方法调用（针对 LangId 且在 DelegateVar 中存在的情况）
        if (node.FunctionExpression is LangId functionId)
        {
            var funcName = functionId.IdName;
            var paramTypes = node.Arguments.Select(arg => arg.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToArray();
            var paramTypeNames = string.Join("_", paramTypes.Select(t => t.Name));
            var delegateKey = $"{funcName}${paramTypeNames}";

            if (local.DelegateVar.TryGetValue(delegateKey, out var method))
            {
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                ilGenerator.Emit(OpCodes.Call, method);
                return null;
            }
        }

        // 2. 通用委托调用逻辑
        // 加载函数表达式（期望是一个委托实例）
        node.FunctionExpression.Accept(this);
        var funcType = node.FunctionExpression.Accept(new TypeInferenceVisitor(local));

        if (typeof(Delegate).IsAssignableFrom(funcType))
        {
            var invokeMethod = funcType.GetMethod("Invoke");
            if (invokeMethod != null)
            {
                // 加载参数
                foreach (var arg in node.Arguments)
                {
                    arg.Accept(this);
                }

                // 调用委托的 Invoke 方法
                ilGenerator.Emit(OpCodes.Callvirt, invokeMethod);
                return null;
            }
        }

        // 集合索引、字典读取等表达式在编译期通常只能推断为 object，但运行期可能保存
        // lambda / 函数值。保留接收者与实参，通过 Delegate.DynamicInvoke 进行动态调用。
        if (funcType == typeof(object) && node.NamedArguments.Count == 0)
        {
            ilGenerator.Emit(OpCodes.Ldc_I4, node.Arguments.Count);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));
            for (var index = 0; index < node.Arguments.Count; index++)
            {
                ilGenerator.Emit(OpCodes.Dup);
                ilGenerator.Emit(OpCodes.Ldc_I4, index);
                var argument = node.Arguments[index];
                argument.Accept(this);
                var argumentType = argument.Accept(new TypeInferenceVisitor(local));
                if (argumentType?.IsValueType == true)
                {
                    ilGenerator.Emit(OpCodes.Box, argumentType);
                }

                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            ilGenerator.Emit(OpCodes.Call,
                typeof(Old8Lang.Runtime.DynamicDelegateInvoker).GetMethod(nameof(Old8Lang.Runtime.DynamicDelegateInvoker.Invoke))!);
            return null;
        }

        // 如果是复杂的表达式函数调用且无法解析为委托，抛出异常
        throw new InvalidOperationError(node, $"无法调用类型为 {funcType} 的表达式");
    
    
    }

    public object? VisitOperation(Operation node)
    {

        // 调用带有ILGenerator参数的OutputType方法，该方法会生成实际的IL指令
        OperationTypeCache.Set(node, EmitOperationOutputType(node));

        return null;
    
    }

    public object? VisitSuperExpression(SuperExpression node)
    {
        // 迁移自 SuperExpression.LoadIlValue()
        ilGenerator.Emit(OpCodes.Ldarg_0);
        return null;
    }

    public object? VisitThisExpression(ThisExpression node)
    {

        // Lambda 的第 0 个参数是闭包数组，不是实例本身。类方法中创建的
        // lambda 会把 node 作为捕获值保存，因此应从对应槽位读取实例。
        if (local.TryGetCapturedLocal("this", out var captured))
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, captured.Index);
            ilGenerator.Emit(OpCodes.Ldelem_Ref);
            if (captured.Type != typeof(object) && captured.Type is not TypeBuilder)
            {
                ilGenerator.Emit(OpCodes.Castclass, captured.Type);
            }

            return null;
        }

        // 加载 node 指针（ldarg.0）
        // 在实例方法中，第一个参数（索引 0）总是 node 指针
        ilGenerator.Emit(OpCodes.Ldarg_0);
    
    
        return null;
    }

    public object? VisitTernaryExpression(TernaryExpression node)
    {

        // 条件表达式（object 静态类型统一转换成原生 bool）
        ConditionIlHelper.EmitBoolOperand(node.Condition, ilGenerator, local);

        // 创建条件分支
        var elseLabel = ilGenerator.DefineLabel();
        var endLabel = ilGenerator.DefineLabel();

        // 如果条件为假，跳转到else分支
        ilGenerator.Emit(OpCodes.Brfalse, elseLabel);

        // 条件为真时执行的表达式
        node.TrueExpression.Accept(this);
        ilGenerator.Emit(OpCodes.Br, endLabel);

        // 条件为假时执行的表达式
        ilGenerator.MarkLabel(elseLabel);
        node.FalseExpression.Accept(this);

        // 结束标签
        ilGenerator.MarkLabel(endLabel);

        return null;
    
    }

    // ===== Value 节点实现 =====
    // Value节点在 IL 模式下通常直接加载常量或调用LoadIlValue

    public object? VisitAnyLangValue(AnyLangValue node)
    {

        // IL 代码生成（IL 模式）
        var dictType = typeof(Dictionary<string, object>);
        var constructor = dictType.GetConstructor(Type.EmptyTypes)!;

        ilGenerator.Emit(OpCodes.Newobj, constructor);

        foreach (var (fieldName, fieldValue) in node.InstanceData)
        {
            ilGenerator.Emit(OpCodes.Dup);
            ilGenerator.Emit(OpCodes.Ldstr, fieldName);

            fieldValue.Accept(this);

            var valueType = fieldValue.Accept(new TypeInferenceVisitor(local));
            if (valueType is { IsValueType: true })
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }

            var addMethod = dictType.GetMethod("Add", [typeof(string), typeof(object)])!;
            ilGenerator.Emit(OpCodes.Callvirt, addMethod);
        }

        return null;
    
    }

    public object? VisitArrayLangValue(ArrayLangValue node)
    {

        // 创建一个长度为 len 的对象数组
        var len = node.RunResult.Length;
        ilGenerator.Emit(OpCodes.Ldc_I4, len); // 加载数组长度
        ilGenerator.Emit(OpCodes.Newarr, typeof(object)); // 创建新数组

        for (var i = 0; i < len; i++)
        {
            ilGenerator.Emit(OpCodes.Dup); // 复制数组引用
            ilGenerator.Emit(OpCodes.Ldc_I4, i); // 加载索引

            Type t;
            if (len == node.Values.Count)
            {
                // 如果Values列表有元素，使用Values[i]
                node.Values[i].Accept(new CompilerVisitor(ilGenerator, local));
                t = node.Values[i].Accept(new TypeInferenceVisitor(local))!;
            }
            else
            {
                // 否则，使用RunResult[i]，但要确保它不为null
                var item = node.RunResult[i];
                if (item is null)
                {
                    // 如果item为null，加载一个null值
                    ilGenerator.Emit(OpCodes.Ldnull);
                    t = typeof(object);
                }
                else
                {
                    item.Accept(this);
                    t = item.Accept(new TypeInferenceVisitor(local))!;
                }
            }

            ilGenerator.Emit(OpCodes.Box, t); // 将值转换为object
            ilGenerator.Emit(OpCodes.Stelem_Ref); // 将值存入数组
        }

        return null;
    
    }

    public object? VisitAsyncGeneratorLangValue(AsyncGeneratorLangValue node)
    {

        // 异步生成器在 IL 模式下返回一个可以迭代的对象
        // 我们创建一个委托，当调用时会返回一个IAsyncEnumerable<object>
        
        // 1. 确定参数类型和返回类型
        var parameterTypes = node.AsyncFunc.Ids?.Select(item => item.Accept(new TypeInferenceVisitor(local))).ToArray() ?? Array.Empty<Type>();
        var returnType = typeof(IAsyncEnumerable<object>);

        // 2. 创建 DynamicMethod
        var dynamicMethod = new DynamicMethod(
            $"AsyncGenerator_{node.AsyncFunc.Id?.IdName ?? "Anonymous"}",
            returnType,
            parameterTypes!,
            true
        );

        // 3. 生成方法体
        var methodIl = dynamicMethod.GetILGenerator();

        // 创建新的LocalManager实例，专门用于生成器函数体的IL生成
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        foreach (var (key, value) in local.DelegateVar)
        {
            funcLocal.DelegateVar[key] = value;
        }

        foreach (var (key, value) in local.ClassVar)
        {
            funcLocal.ClassVar[key] = value;
        }

        foreach (var (key, value) in local.GlobalStaticClasses)
        {
            funcLocal.GlobalStaticClasses[key] = value;
        }

        foreach (var (key, value) in local.FuncParameters)
        {
            funcLocal.FuncParameters[key] = value;
        }

        foreach (var (key, value) in local.GenericFunctions)
        {
            funcLocal.GenericFunctions[key] = value;
        }

        // 处理参数
        if (node.AsyncFunc.Ids != null)
        {
            for (var i = 0; i < node.AsyncFunc.Ids.Count; i++)
            {
                var id = node.AsyncFunc.Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType!);
                funcLocal.AddLocalVar(id.IdName, localVar);
                funcLocal.LocalVarTypes[id.IdName] = paramType!;

                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }
        }

        // 生成异步生成器的方法体
        node.GenerateGeneratorMethodBody(methodIl, funcLocal);

        // 4. 创建委托并加载到栈上
        var delegateType = System.Linq.Expressions.Expression.GetDelegateType(
            parameterTypes!.Cast<Type>().Concat([returnType]).ToArray());

        ilGenerator.Emit(OpCodes.Ldnull); // target (null for static method)
        ilGenerator.Emit(OpCodes.Ldftn, dynamicMethod);
        ilGenerator.Emit(OpCodes.Newobj, delegateType.GetConstructors()[0]);

        return null;
    
    }

    public object? VisitAsyncStreamLangValue(AsyncStreamLangValue node)
    {

        // 异步流的 IL 代码生成（后续实现）
        ilGenerator.Emit(OpCodes.Ldnull);

        return null;
    
    }

    public object? VisitCancellationTokenLangValue(CancellationTokenLangValue node)
    {

        throw new NotImplementedError(
            node.Position,
            "IL 模式暂不支持 CancellationToken 类型"
        );

    
    }

    public object? VisitCancellationTokenSourceLangValue(CancellationTokenSourceLangValue node)
    {

        throw new NotImplementedError(
            node.Position,
            "IL 模式暂不支持 CancellationTokenSource 类型"
        );

    
    }

    public object? VisitDictionaryLangValue(DictionaryLangValue node)
    {

        var listConstructor = typeof(Dictionary<object, object>).GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor); // 创建 List<int> 实例

        var l = ilGenerator.DeclareLocal(typeof(Dictionary<object, object>));
        ilGenerator.Emit(OpCodes.Stloc, l);

        // 向 Dictionary<object, object> 中添加元素
        var addMethod = typeof(Dictionary<object, object>).GetMethod("Add")!;
        foreach (var expr in node.Tuples)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l);
            expr.Elements[0].Accept(new CompilerVisitor(ilGenerator, local));
            var keyType = expr.Elements[0].Accept(new TypeInferenceVisitor(local));
            // 只有值类型才需要装箱
            if (keyType!.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, keyType);
            }

            expr.Elements[1].Accept(new CompilerVisitor(ilGenerator, local));
            var valueType = expr.Elements[1].Accept(new TypeInferenceVisitor(local));
            // 只有值类型才需要装箱
            if (valueType!.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }

            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用 Add 方法
        }

        ilGenerator.Emit(OpCodes.Ldloc, l);

        return null;
    
    }

    public object? VisitErrorLangValue(ErrorLangValue node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitGeneratorLangValue(GeneratorLangValue node)
    {

        // 生成器的IL代码生成（后续实现）

        return null;
    
    }

    public object? VisitInstance(Instance node)
    {

        // 首先尝试通过全局函数注册器生成 IL 代码
        if (node.TryGenerateGlobalFunctionIl(ilGenerator, local))
        {
            return null;
        }

        if (node.TryEmitLocalDelegateCall(ilGenerator, local))
        {
            return null;
        }

        // 查找匹配的方法
        MethodInfo? matchingMethod = null;
        List<LangId>? funcParams = null;
        string? matchedDelegateKey = null;

        // 首先，尝试通过实际参数类型构建键进行精确匹配
        var actualParamTypes = node.Ids.Select(id => id.Accept(new TypeInferenceVisitor(local))).ToArray();
        var actualParamTypeNames = string.Join("_", actualParamTypes.Select(t => t?.Name ?? "object"));
        var exactDelegateKey = $"{node.Id.IdName}${actualParamTypeNames}";

        if (local.DelegateVar.TryGetValue(exactDelegateKey, out var exactResult))
        {
            matchingMethod = exactResult;
            local.FuncParameters.TryGetValue(exactDelegateKey, out funcParams);
            matchedDelegateKey = exactDelegateKey;
        }
        else
        {
            // 如果精确匹配失败，尝试查找参数数量匹配的方法（支持默认参数）
            // 对于带默认参数的情况，从实际参数数量开始，逐步增加参数数量尝试匹配
            for (int paramCount = node.Ids.Count; matchingMethod is null && paramCount <= node.Ids.Count + 10; paramCount++)
            {
                // 遍历所有委托变量，查找参数数量匹配的方法
                foreach (var (key, result) in local.DelegateVar)
                {
                    if (!key.StartsWith($"{node.Id.IdName}$"))
                        continue;

                    // 获取方法参数信息
                    var methodParams = result.GetParameters();

                    // 检查参数数量是否匹配
                    if (methodParams.Length == paramCount)
                    {
                        // 获取函数的参数列表信息
                        local.FuncParameters.TryGetValue(key, out funcParams);

                        // 检查是否有默认参数可以补充
                        if (funcParams is not null && node.Ids.Count <= methodParams.Length)
                        {
                            // 计算必需参数的数量（没有默认值的参数）
                            int requiredParamsCount = funcParams.Count(t => t.DefaultValue is null);

                            // 如果实际参数数量大于等于必需参数数量，则可以匹配
                            if (node.Ids.Count >= requiredParamsCount)
                            {
                                matchingMethod = result;
                                matchedDelegateKey = key;
                                break;
                            }
                        }
                        else if (methodParams.Length == node.Ids.Count)
                        {
                            // 完全匹配
                            matchingMethod = result;
                            matchedDelegateKey = key;
                            break;
                        }
                    }
                }
            }
        }

        if (matchingMethod is null && local.GenericFunctions.TryGetValue(node.Id.IdName, out var genericFunc))
        {
            var argTypes = node.Ids.Select(id => id.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToList();
            var typeArgs = GenericMethodSpecializer.InferTypeArguments(genericFunc, argTypes, local);

            if (typeArgs != null)
            {
                // 创建特化版本
                matchingMethod = GenericMethodSpecializer.CreateSpecialization(genericFunc, typeArgs, local);
                // 使用函数定义的 Ids 作为参数信息
                funcParams = genericFunc.Ids;
                matchedDelegateKey = $"{node.Id.IdName}${string.Join("_", typeArgs.Values.Select(t => t.Name))}";
            }
        }

        if (matchingMethod is null)
        {
            // 名字不是委托类型的局部变量、也不是已知函数：可能是静态类型为 object 的
            // 函数值（函数返回值、集合里取出的函数值）。此时按运行期实际委托调用，
            // 不能落到下面的「当类名构造实例」分支（那里找不到类会发 ldnull，
            // 结果静默变成 null）。
            if (local.GetLocalVar(node.Id.IdName) is { } functionLocal)
            {
                ilGenerator.Emit(OpCodes.Ldloc, functionLocal);
                ilGenerator.Emit(OpCodes.Ldc_I4, node.Ids.Count);
                ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                for (var index = 0; index < node.Ids.Count; index++)
                {
                    ilGenerator.Emit(OpCodes.Dup);
                    ilGenerator.Emit(OpCodes.Ldc_I4, index);
                    var argument = node.Ids[index];
                    argument.Accept(RuntimeVisitors.CreateCompiler(ilGenerator, local));
                    var argumentType = argument.Accept(new TypeInferenceVisitor(local));
                    if (argumentType is { IsValueType: true })
                    {
                        ilGenerator.Emit(OpCodes.Box, argumentType);
                    }

                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }

                ilGenerator.Emit(OpCodes.Call,
                    typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.InvokeFunctionValue))!);
                return null;
            }

            var classType = local.ClassVar.GetValueOrDefault(node.Id.IdName);
            if (classType is null)
            {
                // 如果找不到类类型，可能是因为类还在编译中
                // 检查是否有对应的TypeBuilder
                if (local.InClassEnv?.Name == node.Id.IdName && local.InClassEnv is TypeBuilder builder)
                {
                    classType = builder;
                }
                else
                {
                    // 在当前编译上下文中查找未完成的类型
                    foreach (var kv in local.ClassVar)
                    {
                        if (kv.Key == node.Id.IdName && kv.Value is TypeBuilder)
                        {
                            classType = kv.Value;
                            break;
                        }
                    }

                    if (classType is null)
                    {
                        // 泛型类没有带类型参数时，IL 模式不做「由构造函数实参推断」，
                        // 此前会静默发 ldnull，随后调用实例方法变成 NRE。这里明确报错。
                        if (local.GenericClasses.ContainsKey(node.Id.IdName))
                        {
                            throw new CompilerException(
                                $"IL 模式暂不支持由构造函数实参推断泛型类 '{node.Id.IdName}' 的类型参数；" +
                                $"请显式写成 {node.Id.IdName}<int>(...)",
                                node.Position);
                        }

                        // 创建一个临时的object类型引用，允许编译继续进行
                        // 这是一个临时解决方案，IL 模式下类前向引用的处理需要改进
                        classType = typeof(object);

                        // 生成临时的null对象，实际运行时将通过解释器处理
                        ilGenerator.Emit(OpCodes.Ldnull);
                        return null;
                    }
                }
            }

            // 获取默认构造函数
            ConstructorInfo? constructorInfo = null;
            if (classType is TypeBuilder)
            {
                if (local.CurrentConstructorBuilder != null && local.InClassEnv == classType)
                {
                    // 如果是在当前正在编译的类中，使用ConstructorBuilder
                    constructorInfo = local.CurrentConstructorBuilder;
                }
                else
                {
                    constructorInfo = classType.GetConstructor(Type.EmptyTypes);
                }
            }
            else
            {
                constructorInfo = classType.GetConstructor(Type.EmptyTypes);
            }

            if (constructorInfo is not null)
            {
                ilGenerator.Emit(OpCodes.Newobj, constructorInfo);
            }

            var localA = ilGenerator.DeclareLocal(classType);
            ilGenerator.Emit(OpCodes.Stloc, localA);

            // 使用BindingFlags.DeclaredOnly来只查找当前类声明的方法，避免与继承的方法冲突
            MethodInfo? initFunc = null;

            if (classType is TypeBuilder && local.CurrentInitMethodBuilder != null && local.InClassEnv == classType)
            {
                // 如果是在当前正在编译的类中，使用CurrentInitMethodBuilder
                initFunc = local.CurrentInitMethodBuilder;
            }
            else
            {
                try
                {
                    var initMethods = classType
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(m => m.Name == "init")
                        .ToArray();

                    if (initMethods.Length == 1)
                    {
                        initFunc = initMethods[0];
                    }
                    else if (initMethods.Length > 1)
                    {
                        var argTypes = node.Ids.Select(id => id.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToArray();

                        static int Score(ParameterInfo[] ps, Type[] args)
                        {
                            if (ps.Length != args.Length)
                            {
                                return -1;
                            }

                            var score = 0;
                            for (var i = 0; i < ps.Length; i++)
                            {
                                var pt = ps[i].ParameterType;
                                var at = args[i];
                                if (pt == typeof(object))
                                {
                                    continue;
                                }

                                if (pt == at)
                                {
                                    score += 2;
                                    continue;
                                }

                                if (pt.IsAssignableFrom(at))
                                {
                                    score += 1;
                                    continue;
                                }

                                return -1;
                            }

                            return score;
                        }

                        initFunc = initMethods
                            .Select(m => (method: m, ps: m.GetParameters()))
                            .Where(x => x.ps.Length == argTypes.Length)
                            .Select(x => (x.method, x.ps, score: Score(x.ps, argTypes)))
                            .Where(x => x.score >= 0)
                            .OrderByDescending(x => x.score)
                            .Select(x => x.method)
                            .FirstOrDefault();
                    }
                }
                catch
                {
                    // 忽略错误，可能是类型尚未创建
                }
            }

            // 子类未声明 init 时，构造调用应继承父类的可匹配初始化方法。
            // DeclaredOnly 仅用于优先选择子类自己的 init；不能使 Parent.init
            // 在 Child(args) 场景中静默丢失。
            if (initFunc is null && classType is not TypeBuilder)
            {
                var argumentTypes = node.Ids.Select(id => id.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToArray();
                initFunc = classType
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.Name == "init")
                    .FirstOrDefault(method =>
                    {
                        var parameters = method.GetParameters();
                        return parameters.Length == argumentTypes.Length &&
                               parameters.Zip(argumentTypes).All(pair =>
                                   pair.First.ParameterType == pair.Second ||
                                   pair.First.ParameterType.IsAssignableFrom(pair.Second) ||
                                   pair.Second == typeof(object));
                    });
            }

            if (initFunc is not null)
            {
                // 加载 node 指针
                ilGenerator.Emit(OpCodes.Ldloc, localA);

                // 加载参数
                var a = initFunc.GetParameters();
                for (var i = 0; i < node.Ids.Count; i++)
                {
                    var id = node.Ids[i];
                    id.Accept(this);
                    var idType = id.Accept(new TypeInferenceVisitor(local));
                    if (a[i].ParameterType == typeof(object) && idType!.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }
                }

                // 调用 init 方法（实例方法使用 Callvirt）
                ilGenerator.Emit(OpCodes.Callvirt, initFunc);
            }

            // 加载对象实例作为返回值
            ilGenerator.Emit(OpCodes.Ldloc, localA);

            return null;
        }

        // 处理所有类型的方法调用，包括DynamicMethod和MethodBuilder
        var matchingParams = matchingMethod.GetParameters();

        // 检查是否有 params 参数
        int paramsIndex = -1;
        if (funcParams is not null)
        {
            for (int i = 0; i < funcParams.Count; i++)
            {
                if (funcParams[i].IsParams)
                {
                    paramsIndex = i;
                    break;
                }
            }
        }

        // 如果有 params 参数，需要特殊处理
        if (paramsIndex >= 0)
        {
            // 加载 params 之前的普通参数
            for (var i = 0; i < paramsIndex; i++)
            {
                if (i < node.Ids.Count)
                {
                    var id = node.Ids[i];
                    id.Accept(this);

                    // 参数类型匹配和转换
                    if (i < matchingParams.Length)
                    {
                        var paramType = matchingParams[i].ParameterType;
                        var idType = id.Accept(new TypeInferenceVisitor(local));
                        node.LoadParameterWithConversion(ilGenerator, local, id, idType, paramType);
                    }
                }
            }

            // 处理 params 参数：创建数组
            var paramsParam = matchingParams[paramsIndex];
            var arrayElementType = paramsParam.ParameterType.GetElementType()!;
            var paramsArgsCount = Math.Max(0, node.Ids.Count - paramsIndex);

            // 创建数组：ldc.i4 <count>; newarr <elementType>
            ilGenerator.Emit(OpCodes.Ldc_I4, paramsArgsCount);
            ilGenerator.Emit(OpCodes.Newarr, arrayElementType);

            // 填充数组元素
            for (var i = 0; i < paramsArgsCount; i++)
            {
                var argIndex = paramsIndex + i;
                var id = node.Ids[argIndex];

                // dup 数组引用
                ilGenerator.Emit(OpCodes.Dup);
                // 加载索引
                ilGenerator.Emit(OpCodes.Ldc_I4, i);
                // 加载参数值
                id.Accept(this);

                // 类型转换
                var idType = id.Accept(new TypeInferenceVisitor(local));
                if (idType is not null && arrayElementType != idType)
                {
                    if (arrayElementType == typeof(object) && idType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, idType);
                    }
                    else if (arrayElementType == typeof(int) && idType == typeof(double))
                    {
                        ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
                    }
                    else if (arrayElementType == typeof(double) && idType == typeof(int))
                    {
                        ilGenerator.Emit(OpCodes.Conv_R8);
                    }
                }

                // 存储到数组：stelem <elementType>
                if (arrayElementType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }
                else if (arrayElementType == typeof(int))
                {
                    ilGenerator.Emit(OpCodes.Stelem_I4);
                }
                else if (arrayElementType == typeof(double))
                {
                    ilGenerator.Emit(OpCodes.Stelem_R8);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Stelem, arrayElementType);
                }
            }
            // 此时栈顶是填充好的数组，作为 params 参数传递
        }
        else
        {
            // 没有 params 参数，使用原有逻辑
            // 加载实际传递的参数
            for (var i = 0; i < node.Ids.Count; i++)
            {
                var id = node.Ids[i];
                id.Accept(this);

                // 确保参数类型匹配
                if (i < matchingParams.Length)
                {
                    var paramType = matchingParams[i].ParameterType;
                    var idType = id.Accept(new TypeInferenceVisitor(local));
                    node.LoadParameterWithConversion(ilGenerator, local, id, idType, paramType);
                }
            }
        }

        // 如果有默认参数需要补充
        if (funcParams is not null && node.Ids.Count < matchingParams.Length)
        {
            // 补充默认参数值
            for (var i = node.Ids.Count; i < funcParams.Count; i++)
            {
                var param = funcParams[i];
                if (param.DefaultValue is not null)
                {
                    // 加载默认参数值
                    param.DefaultValue.Accept(this);

                    // 确保类型匹配
                    var paramType = matchingParams[i].ParameterType;
                    var defaultType = param.DefaultValue.Accept(new TypeInferenceVisitor(local));

                    // 使用LocalManager的ValidateType方法验证默认参数类型
                    if (defaultType is not null)
                    {
                        local.ValidateType(paramType, defaultType, param.Position);
                    }

                    // 处理必要的类型转换
                    if (defaultType is not null && paramType != defaultType)
                    {
                        if (paramType == typeof(object) && defaultType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, defaultType);
                        }
                        else if (paramType == typeof(int) && defaultType == typeof(double))
                        {
                            ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToInt32", [typeof(double)])!);
                        }
                        else if (paramType == typeof(double) && defaultType == typeof(int))
                        {
                            ilGenerator.Emit(OpCodes.Conv_R8);
                        }
                    }
                }
            }
        }

        // 调用方法：根据方法类型采用不同的调用方式
        if (matchingMethod is DynamicMethod dynamicMethod)
        {
            if (local.AsyncStateMachineGenerator != null && matchedDelegateKey is not null)
            {
                var parameterTypes = matchingParams.Select(p => p.ParameterType).ToArray();
                var argLocals = new LocalBuilder[parameterTypes.Length];
                for (int i = parameterTypes.Length - 1; i >= 0; i--)
                {
                    argLocals[i] = ilGenerator.DeclareLocal(parameterTypes[i]);
                    ilGenerator.Emit(OpCodes.Stloc, argLocals[i]);
                }

                var argsArrayLocal = ilGenerator.DeclareLocal(typeof(object[]));
                ilGenerator.Emit(OpCodes.Ldc_I4, parameterTypes.Length);
                ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                ilGenerator.Emit(OpCodes.Stloc, argsArrayLocal);

                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    ilGenerator.Emit(OpCodes.Ldloc, argsArrayLocal);
                    ilGenerator.Emit(OpCodes.Ldc_I4, i);
                    ilGenerator.Emit(OpCodes.Ldloc, argLocals[i]);
                    if (parameterTypes[i].IsValueType) ilGenerator.Emit(OpCodes.Box, parameterTypes[i]);
                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }

                ilGenerator.Emit(OpCodes.Ldstr, matchedDelegateKey);
                ilGenerator.Emit(OpCodes.Ldloc, argsArrayLocal);
                ilGenerator.Emit(OpCodes.Call,
                    typeof(CompiledDelegateRegistry).GetMethod(nameof(CompiledDelegateRegistry.Invoke))!);

                var returnType = matchingMethod.ReturnType;
                if (returnType == typeof(void))
                {
                    ilGenerator.Emit(OpCodes.Pop);
                }
                else if (returnType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, returnType);
                }
                else if (returnType != typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Castclass, returnType);
                }
            }
            else
            {
                ilGenerator.Emit(OpCodes.Call, dynamicMethod);
            }
        }
        else
        {
            // 对于普通方法，使用Call指令直接调用
            ilGenerator.Emit(OpCodes.Call, matchingMethod);
        }
    
    
        return null;
    }

    public object? VisitLangListItem(LangListItem node)
    {

        var listType = node.ListId.Accept(new TypeInferenceVisitor(local));

        if (listType == typeof(string))
        {
            // 处理字符串索引访问。负索引由 IlIndexHelper 归一化（-1 表示最后一个字符）。
            node.ListId.Accept(this); // 加载字符串
            node.EmitIntIndex(ilGenerator, local); // 加载索引
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetStringChar))!);
        }
        else if (listType!.IsGenericType && listType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // 处理Dictionary<TKey, TValue>类型的字典索引访问
            node.ListId.Accept(this); // 加载字典
            node.Key.Accept(this); // 加载键

            // 获取字典的泛型参数
            var genericArgs = listType.GetGenericArguments();
            var dictKeyType = genericArgs[0];

            // 确保键类型匹配
            var keyType = node.Key.Accept(new TypeInferenceVisitor(local));
            if (keyType != dictKeyType)
            {
                if (dictKeyType.IsValueType && !keyType!.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, dictKeyType);
                }
                else if (!dictKeyType.IsValueType && keyType!.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, keyType);
                }
            }

            // 调用字典的索引器（get_Item方法）
            var getDictionaryItemMethod = listType.GetMethod("get_Item", [dictKeyType])!;
            ilGenerator.Emit(OpCodes.Callvirt, getDictionaryItemMethod);
        }
        else if (listType == typeof(Dictionary<string, object>))
        {
            // 处理Dictionary<string, object>类型的字典索引访问
            node.ListId.Accept(this); // 加载字典

            // 处理键
            if (node.Key is StringLangValue stringKey)
            {
                // 键是字符串字面量，直接加载字符串值
                ilGenerator.Emit(OpCodes.Ldstr, stringKey.Value);
            }
            else
            {
                // 键不是字符串字面量，加载并转换为字符串
                node.Key.Accept(this);
                var keyType = node.Key.Accept(new TypeInferenceVisitor(local));
                if (keyType != typeof(string))
                {
                    // 如果键不是字符串类型，转换为字符串
                    ilGenerator.Emit(OpCodes.Call, typeof(Convert).GetMethod("ToString", [typeof(object)])!);
                }
            }

            // 调用字典的索引器（get_Item方法）
            var getDictionaryItemMethod = typeof(Dictionary<string, object>).GetMethod("get_Item", [typeof(string)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getDictionaryItemMethod);
        }
        else if (listType.IsArray)
        {
            // 处理数组索引访问（负索引 -1 表示最后一个元素）
            node.ListId.Accept(this); // 加载数组
            node.EmitIntIndex(ilGenerator, local); // 加载索引
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetArrayItem))!);
            var arrayElementType = listType.GetElementType() ?? typeof(object);
            if (arrayElementType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, arrayElementType);
            }
            else if (arrayElementType != typeof(object))
            {
                ilGenerator.Emit(OpCodes.Castclass, arrayElementType);
            }
        }
        else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // 处理List<T>索引访问。索引器经非泛型 IList 调用，负索引由 IlIndexHelper 归一化，
            // 避免 List<int> / List<object> 的泛型实例与静态类型不一致时解析错误。
            node.ListId.Accept(this); // 加载List
            node.EmitIntIndex(ilGenerator, local); // 加载索引
            ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetListItem))!);

            var elementType = listType.GetGenericArguments()[0];
            if (elementType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, elementType);
            }
            else if (elementType != typeof(object))
            {
                ilGenerator.Emit(OpCodes.Castclass, elementType);
            }
        }
        else if (listType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问
            node.ListId.Accept(this);

            if (node.Key is ILiteralSyntax { Kind: LiteralKind.Int, LiteralValue: int and >= 0 } intLiteral)
            {
                // 非负常量索引，优化为字段访问
                Old8Lang.AST.Expression.OperationHelpers.DotOperatorILHelper.GenerateValueTupleItemAccess(ilGenerator, listType, (int)intLiteral.LiteralValue!);
            }
            else
            {
                // 变量索引或负常量索引，装箱为 ITuple 并经 IlIndexHelper 归一化下标
                ilGenerator.Emit(OpCodes.Box, listType);
                node.EmitIntIndex(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Call, typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetTupleItem))!);
            }
        }
        else if (listType == typeof(object))
        {
            // 静态类型为 object：运行期按字典 / 列表 / 数组 / 元组 / 字符串分派。
            node.ListId.Accept(this);
            node.Key.Accept(this);
            var keyType = node.Key.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (keyType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, keyType);
            }
            ilGenerator.Emit(OpCodes.Call,
                typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.GetDynamicObjectItem))!);
        }
        else if (listType == typeof(DictionaryLangValue))
        {
            // 处理 DictionaryLangValue 类型的索引访问。
            // 键必须在语言值层面传递（Get 的形参是 LangValueType），
            // 结果再还原成原生对象，才能与索引读取的静态类型 object 保持一致；
            // 之前直接把原始 string/int 当作 LangValueType 传入，会生成无效 IL。
            node.ListId.Accept(this); // 加载字典
            IlValueBridge.EmitLoadWrapped(node.Key, ilGenerator, local);
            ilGenerator.Emit(OpCodes.Callvirt,
                typeof(DictionaryLangValue).GetMethod(nameof(DictionaryLangValue.Get))!);
            ilGenerator.Emit(OpCodes.Call,
                typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.Unwrap))!);
        }
        else
        {
            throw new InvalidOperationError(node, "不支持的集合类型: " + listType.Name);
        }

        return null;
    
    }

    public object? VisitListComprehension(ListComprehension node)
    {

        // 为列表推导式创建一个空的List<object>实例
        // 这是一个简化的实现，实际的列表推导式IL生成需要更复杂的逻辑
        var listType = typeof(List<object>);
        var listConstructor = listType.GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor);

        return null;
    
    }

    public object? VisitListLangValue(ListLangValue node)
    {

        // IL 模式下统一用 List<object> 表示列表字面量。
        // 之前会按元素推断出 List<int> / List<string>，但 `list` 参数的静态类型是 List<object>，
        // 把 List<int> 传给 List<object> 形参后，索引器/Add 等 callvirt 会按错误的泛型实例解析，
        // 运行期直接 AccessViolation。动态语言的列表本身就是异构的，统一装箱更安全。
        var itemType = typeof(object);

        var listType = typeof(List<>).MakeGenericType(itemType);
        // 创建泛型List实例
        var listConstructor = listType.GetConstructor(Type.EmptyTypes)!;
        ilGenerator.Emit(OpCodes.Newobj, listConstructor);

        // 如果没有元素需要添加，直接返回List实例
        if (node.Value.Count == 0)
        {
            return null;
        }

        // 否则，将List实例存储到局部变量
        var l = ilGenerator.DeclareLocal(listType);
        ilGenerator.Emit(OpCodes.Stloc, l);

        // 向List中添加元素
        var addMethod = listType.GetMethod("Add", [itemType])!;
        foreach (var expr in node.Value)
        {
            ilGenerator.Emit(OpCodes.Ldloc, l);
            expr.Accept(this);
            var t = expr.Accept(new TypeInferenceVisitor(local));
            if (t != itemType)
            {
                ilGenerator.Emit(OpCodes.Box, t!);
            }

            ilGenerator.Emit(OpCodes.Callvirt, addMethod); // 调用Add方法
        }

        // 将填充好的List实例加载到堆栈
        ilGenerator.Emit(OpCodes.Ldloc, l);
    
    
        return null;
    }

    public object? VisitMethodOverloadList(MethodOverloadList node)
    {
        // MethodOverloadList 是方法重载列表，不需要生成 IL 代码
        // 避免循环调用 LoadIlValue -> Accept -> VisitMethodOverloadList -> LoadIlValue
        // 方法重载列表通常在函数调用时解析，而不是直接加载
        return null;
    }

    public object? VisitNestedIndexAccess(NestedIndexAccess node)
    {

        // 根据基础表达式的输出类型选择适当的索引访问方法
        var baseType = node.BaseExpression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);

        // 特殊处理 ValueTuple 的常量索引访问
        if (baseType.FullName?.StartsWith("System.ValueTuple") == true && node.NestedIndex is ILiteralSyntax { Kind: LiteralKind.Int, LiteralValue: int indexValue })
        {
            var index = indexValue;

            // 只加载基础表达式的结果，不加载索引值
            node.BaseExpression.Accept(this);

            // 使用递归方法加载字段（支持超过 7 个元素）
            node.LoadValueTupleField(ilGenerator, baseType, index);
            return null;
        }

        // 对于其他情况，先加载基础表达式的结果和索引值
        node.BaseExpression.Accept(this);
        node.NestedIndex.Accept(this);

        if (baseType == typeof(string))
        {
            // 字符串索引访问
            var getCharMethod = typeof(string).GetMethod("get_Chars", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getCharMethod);
        }
        else if (baseType.IsArray)
        {
            // 数组索引访问
            var elementType = baseType.GetElementType()!;
            if (elementType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Ldelem, elementType);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldelem_Ref);
            }
        }
        else if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // List<T> 索引访问
            var getItemMethod = baseType.GetMethod("get_Item", [typeof(int)])!;
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        }
        else if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            // Dictionary<TKey, TValue> 索引访问
            var keyType = baseType.GetGenericArguments()[0];
            var getItemMethod = baseType.GetMethod("get_Item", [keyType])!;
            ilGenerator.Emit(OpCodes.Callvirt, getItemMethod);
        }
        else if (baseType.FullName?.StartsWith("System.ValueTuple") == true)
        {
            // ValueTuple 索引访问（非常量索引）
            // 使用 ITuple 索引器
            var indexType = node.NestedIndex.Accept(new TypeInferenceVisitor(local));
            if (indexType == typeof(object))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
            }

            var indexLocal = ilGenerator.DeclareLocal(typeof(int));
            ilGenerator.Emit(OpCodes.Stloc, indexLocal.LocalIndex);

            // Box Tuple
            ilGenerator.Emit(OpCodes.Box, baseType);

            // Load Index
            ilGenerator.Emit(OpCodes.Ldloc, indexLocal.LocalIndex);

            var indexer = typeof(System.Runtime.CompilerServices.ITuple).GetProperty("Item")!;
            ilGenerator.Emit(OpCodes.Callvirt, indexer.GetGetMethod()!);
        }
        else if (baseType == typeof(object))
        {
            // 动态索引访问
            // 调用 DotOperatorILHelper.GenerateDynamicIndexAccess
            // 该方法假设栈上有 [Left] [Right]，并生成动态分发代码
            // 我们的栈上正好有 BaseIndex 和 NestedIndex 的值

            var indexType = node.NestedIndex.Accept(new TypeInferenceVisitor(local));

            if (indexType != null)
            {
                Old8Lang.AST.Expression.OperationHelpers.DotOperatorILHelper.GenerateDynamicIndexAccess(
                    ilGenerator,
                    indexType
                );
            }
        }
        else
        {
            throw new InvalidOperationError(node, "不支持的嵌套索引访问类型: " + baseType.Name);
        }
    
    
        return null;
    }

    public object? VisitNestedSliceAccess(NestedSliceAccess node)
    {

        // 首先加载基础表达式的结果
        node.BaseExpression.Accept(this);

        // 如果基础表达式不是object类型，需要装箱
        var baseType = node.BaseExpression.Accept(new TypeInferenceVisitor(local));
        if (baseType is not null && baseType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, baseType);
        }

        // 加载切片参数 - start
        node.SliceStart.Accept(this);
        var startType = node.SliceStart.Accept(new TypeInferenceVisitor(local));
        // 转换为int
        if (startType is not null && startType != typeof(int))
        {
            TypeConversion.GenerateTypeConversionIl(ilGenerator, startType, typeof(int), node.SliceStart);
        }

        // 处理结束索引（可能为null）
        if (node.SliceEnd is not null)
        {
            node.SliceEnd.Accept(this);
            var endType = node.SliceEnd.Accept(new TypeInferenceVisitor(local));
            // 转换为int
            if (endType is not null && endType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, endType, typeof(int), node.SliceEnd);
            }
        }
        else
        {
            // 如果没有指定结束索引，使用int.MaxValue表示到末尾
            ilGenerator.Emit(OpCodes.Ldc_I4, int.MaxValue);
        }

        // 处理步长（可能为null）
        if (node.SliceStep is not null)
        {
            node.SliceStep.Accept(this);
            var stepType = node.SliceStep.Accept(new TypeInferenceVisitor(local));
            // 转换为int
            if (stepType is not null && stepType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, stepType, typeof(int), node.SliceStep);
            }
        }
        else
        {
            // 如果没有指定步长，默认为1
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
        }

        // 调用CollectionHelper.Slice方法
        var sliceMethod = typeof(CollectionHelper).GetMethod(
            nameof(CollectionHelper.Slice),
            [typeof(object), typeof(int), typeof(int), typeof(int)]);

        if (sliceMethod is null)
        {
            throw new InvalidOperationError(node, "无法找到CollectionHelper.Slice方法");
        }

        ilGenerator.Emit(OpCodes.Call, sliceMethod);

        return null;
    
    }

    public object? VisitRangeLangValue(RangeLangValue node)
    {

        // 我们需要生成与解释器相同的行为：
        // 1. 根据 includeStart 和 includeEnd 调整范围
        // 2. 检查是正向还是反向
        // 3. 生成相应的数组

        // 为了简化，我们直接调用一个辅助方法来生成范围数组
        // 加载起始值
        node.Start?.Accept(new CompilerVisitor(ilGenerator, local));

        // 加载结束值
        node.End?.Accept(new CompilerVisitor(ilGenerator, local));

        // 加载 includeStart
        ilGenerator.Emit(node.IncludeStart ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // 加载 includeEnd
        ilGenerator.Emit(node.IncludeEnd ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);

        // 调用辅助方法 CreateRangeArray(int start, int end, bool includeStart, bool includeEnd)
        var createRangeMethod = typeof(RangeLangValue).GetMethod(nameof(node.CreateRangeArray),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;

        ilGenerator.Emit(OpCodes.Call, createRangeMethod);

        return null;
    
    }

    public object? VisitSliceLangValue(SliceLangValue node)
    {

        // 加载集合（id）
        node.Id.Accept(this);

        // 加载起始索引
        if (node.Start is not null)
        {
            node.Start.Accept(this);
            var startType = node.Start.Accept(new TypeInferenceVisitor(local));
            if (startType is not null && startType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, startType, typeof(int), node.Start);
            }
        }
        else
        {
            // 默认起始索引为 0
            ilGenerator.Emit(OpCodes.Ldc_I4_0);
        }

        // 加载结束索引
        if (node.End is not null)
        {
            node.End.Accept(this);
            var endType = node.End.Accept(new TypeInferenceVisitor(local));
            if (endType is not null && endType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, endType, typeof(int), node.End);
            }
        }
        else
        {
            // 默认结束索引为 int.MaxValue (表示到末尾)
            ilGenerator.Emit(OpCodes.Ldc_I4, int.MaxValue);
        }

        // 加载步长
        if (node.Step is not null)
        {
            node.Step.Accept(this);
            var stepType = node.Step.Accept(new TypeInferenceVisitor(local));
            if (stepType is not null && stepType != typeof(int))
            {
                TypeConversion.GenerateTypeConversionIl(ilGenerator, stepType, typeof(int), node.Step);
            }
        }
        else
        {
            // 默认步长为 1
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
        }

        // 调用 CollectionHelper.Slice 方法
        var sliceMethod = typeof(CollectionHelper).GetMethod(
            nameof(CollectionHelper.Slice),
            [typeof(object), typeof(int), typeof(int), typeof(int)]);

        if (sliceMethod is null)
        {
            throw new InvalidOperationError(node, "无法找到CollectionHelper.Slice方法");
        }

        ilGenerator.Emit(OpCodes.Call, sliceMethod);

        return null;
    
    }

    public object? VisitStringTemplateValue(StringTemplateValue node)
    {

        // 对于字符串模板，我们将所有部分转换为字符串，然后使用string.Concat方法连接
        // 我们将使用string.Concat(object[])方法，这样可以处理不同类型的参数

        // 首先创建一个数组，大小为List的长度
        ilGenerator.Emit(OpCodes.Ldc_I4, node.ExpressionList.Count);
        ilGenerator.Emit(OpCodes.Newarr, typeof(object));

        // 遍历所有字符串片段和表达式，将它们添加到数组中
        for (int i = 0; i < node.ExpressionList.Count; i++)
        {
            var item = node.ExpressionList[i];

            // 复制数组引用到栈顶
            ilGenerator.Emit(OpCodes.Dup);

            // 加载索引i
            ilGenerator.Emit(OpCodes.Ldc_I4, i);

            // 加载当前项的值
            item.Accept(this);

            // 将值转换为object类型（如果需要）
            var itemType = item.Accept(new TypeInferenceVisitor(local));
            if (itemType is { IsValueType: true })
            {
                // 值类型需要装箱
                ilGenerator.Emit(OpCodes.Box, itemType);
            }
            // 引用类型不需要转换，直接可以作为object

            // 将值存储到数组的第i个位置
            ilGenerator.Emit(OpCodes.Stelem_Ref);
        }

        // 调用string.Concat(object[])方法将所有部分连接起来
        var concatMethod = typeof(string).GetMethod("Concat", [typeof(object[])])!;
        ilGenerator.Emit(OpCodes.Call, concatMethod);

        return null;
    
    }

    public object? VisitSuperProxy(SuperProxy node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitTaskClassLangValue(TaskClassLangValue node)
    {

        // 对于 Task 类静态方法，我们不需要加载实例
        // 直接返回 Task 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(Task));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitTaskCompletionSourceLangValue(TaskCompletionSourceLangValue node)
    {

        throw new NotImplementedError(
            node.Position,
            "IL 模式暂不支持 TaskCompletionSource 类型"
        );

    
    }

    public object? VisitTaskFactoryClassLangValue(TaskFactoryClassLangValue node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitTaskFactoryStaticMethodWrapper(TaskFactoryStaticMethodWrapper node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitTaskLangValue(TaskLangValue node)
    {

        // 加载 node 指针
        ilGenerator.Emit(OpCodes.Ldarg_0);
        // 调用 GetValue() 方法获取底层 Task 对象
        ilGenerator.Emit(OpCodes.Call, GetType().GetMethod("GetValue")!);
        // 将 Task<LangValueType> 转换为 Task<object>
        ilGenerator.Emit(OpCodes.Castclass, typeof(Task<object>));

        return null;
    
    }

    public object? VisitTaskSchedulerClassLangValue(TaskSchedulerClassLangValue node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitTaskSchedulerLangValue(TaskSchedulerLangValue node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitTaskStaticMethodWrapper(TaskStaticMethodWrapper node)
    {

        // 对于 Task 类静态方法，我们不需要加载实例
        // 直接返回 Task 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(Task));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitThreadClassLangValue(ThreadClassLangValue node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitThreadLangValue(ThreadLangValue node)
    {

        // 加载当前线程对象到 IL 栈
        ilGenerator.Emit(OpCodes.Ldarg_0);

        return null;
    
    }

    public object? VisitThreadStaticMethodWrapper(ThreadStaticMethodWrapper node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    public object? VisitTupleLangValue(TupleLangValue node)
    {

        // 获取所有元素的类型
        var types = node.Elements.Select(e => e.Accept(new TypeInferenceVisitor(local)) ?? typeof(object)).ToArray();

        // 递归生成创建代码
        node.EmitValueTupleCreation(ilGenerator, local, node.Elements, types);

        return null;
    
    }

    public object? VisitTypeLangValue(TypeLangValue node)
    {

        // 在 IL 模式下，创建 TypeLangValue 对象
        ilGenerator.Emit(OpCodes.Ldstr, node.Value ?? "");
        ilGenerator.Emit(OpCodes.Newobj, typeof(TypeLangValue).GetConstructor([typeof(string)])!);

        return null;
    
    }

    public object? VisitGenericInstanceExpression(GenericInstanceExpression node)
    {

        // IL 模式下的IL生成
        // 处理泛型类实例化和泛型函数调用

        // 获取基础表达式名称
        if (node.BaseExpression is not LangId identifier)
        {
            throw new InvalidOperationError(node, "IL 模式下泛型表达式必须使用简单的标识符");
        }

        var name = identifier.IdName;

        // 尝试判断是泛型类还是泛型函数
        bool isGenericClass = local.GenericClasses.ContainsKey(name);
        bool isGenericFunction = local.GenericFunctions.ContainsKey(name);

        if (isGenericClass)
        {
            // 处理泛型类实例化
            node.HandleGenericClassInstantiation(ilGenerator, local, name);
        }
        else if (isGenericFunction)
        {
            // 处理泛型函数调用
            node.HandleGenericFunctionCall(ilGenerator, local, name);
        }
        else
        {
            throw new InvalidOperationError(node, $"找不到泛型类或泛型函数定义：{name}");
        }

        return null;
    
    }

    public object? VisitLinqExpression(LinqExpression node)
    {

        if (node.CanGenerateStreamingQuery())
        {
            node.GenerateStreamingQuery(ilGenerator, local);
            return null;
        }
        // 在 IL 模式下，LINQ 查询需要生成 IL 代码来执行查询逻辑
        // 由于 IL 模式下无法直接访问 VariateManager（生成的方法是无参数的 Action），
        // 我们需要通过 LocalManager.Interpreter.Manager 来访问

        // 检查是否有 Interpreter
        if (local.Interpreter?.Manager == null)
        {
            throw new NotSupportedException("LINQ 查询在 IL 模式下需要 Interpreter 支持");
        }

        // 将 LINQ 查询转换为手动的循环和过滤逻辑
        node.GenerateLinqMethodCalls(ilGenerator, local);
    
    
        return null;
    }

    public object? VisitAssertClassLangValue(AssertClassLangValue node)
    {

        // 对于 Assert 类静态方法，我们不需要加载实例
        // 直接返回 Assert 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(AssertHelper));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitAssertStaticMethodWrapper(AssertStaticMethodWrapper node)
    {

        // 对于 Assert 类静态方法，我们不需要加载实例
        // 直接返回 Assert 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(AssertHelper));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitMockLibClassLangValue(MockLibClassLangValue node)
    {

        // 对于 MockLib 类静态方法，我们不需要加载实例
        // 直接返回 MockLib 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(MockLibClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitMockObjectLangValue(MockObjectLangValue node)
    {

        // 对于 MockLib 类静态方法，我们不需要加载实例
        // 直接返回 MockLib 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(MockLibClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitMockLibStaticMethodWrapper(MockLibStaticMethodWrapper node)
    {

        // 对于 MockLib 类静态方法，我们不需要加载实例
        // 直接返回 MockLib 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(MockLibClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitTestRunnerClassLangValue(TestRunnerClassLangValue node)
    {

        // 对于 TestRunner 类静态方法，我们不需要加载实例
        // 直接返回 TestRunner 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(TestRunnerClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitTestRunnerStaticMethodWrapper(TestRunnerStaticMethodWrapper node)
    {

        // 对于 TestRunner 类静态方法，我们不需要加载实例
        // 直接返回 TestRunner 类型本身
        ilGenerator.Emit(OpCodes.Ldtoken, typeof(TestRunnerClassLangValue));
        ilGenerator.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle")!);

        return null;
    
    }

    public object? VisitLockedVariableLangValue(LockedVariableLangValue node)
    {
        // 该节点没有 IL 实现，与旧实现一致地不生成代码
        return null;
    }

    /// <summary>
    /// 把字符串拼接的操作数转换成语言显示文本（string 保持不变）。
    /// </summary>
    private static void EmitConcatOperand(ILGenerator ilGenerator, Type? operandType)
    {
        if (operandType == typeof(string)) return;

        if (operandType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, operandType);
        }

        ilGenerator.Emit(OpCodes.Call,
            typeof(Old8Lang.Runtime.VMValueFormatter).GetMethod(
                nameof(Old8Lang.Runtime.VMValueFormatter.ToDisplayString))!);
    }

    private Type EmitOperationOutputType(Operation operation)
    {
        var right = operation.Opera == LangTokenType.Dot
            ? CallSyntaxAdapter.Normalize(operation.Right)
            : operation.Right;

        if (operation.Opera == LangTokenType.Dot && StandardLibraryILHelper.TryResolve(operation.Left, right, local, out var library, out var libraryCall))
            return StandardLibraryILHelper.EmitCall(library, libraryCall, ilGenerator, local);

        var leftType = operation.Left?.Accept(new TypeInferenceVisitor(local));
        var rightType = right?.Accept(new TypeInferenceVisitor(local));

        // 如果leftType是TypeBuilder，需要查找对应的已完成的类型
        if (leftType is TypeBuilder typeBuilder && typeBuilder != local.InClassEnv)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = typeBuilder.Name;
            leftType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        // 如果rightType是TypeBuilder，需要查找对应的已完成的类型
        if (rightType is TypeBuilder rightTypeBuilder)
        {
            // 尝试从local.ClassVar中查找对应的已完成类型
            var typeName = rightTypeBuilder.Name;
            rightType = local.ClassVar.TryGetValue(typeName, out var completedType)
                ? completedType
                : typeof(object); // 如果找不到对应的类型，说明类还在编译中，返回object
        }

        if (operation.Left is null)
        {
            // 处理单目运算符
            switch (operation.Opera)
            {
                case LangTokenType.Exclamation:
                    // object 静态类型（动态方法调用结果等）必须转成原生 bool 再取反，
                    // 否则 xor 作用在引用上会得到垃圾结果。
                    ConditionIlHelper.EmitBoolOperand(operation.Right, ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Ldc_I4_1); // 加载常量 1
                    ilGenerator.Emit(OpCodes.Xor); // 进行异或运算
                    return typeof(bool);
                case LangTokenType.Minus:
                    operation.Right?.Accept(new CompilerVisitor(ilGenerator, local));
                    if (rightType == typeof(object))
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                        rightType = typeof(int);
                    }
                    ilGenerator.Emit(OpCodes.Neg);
                    return rightType ?? throw new InvalidOperationError(operation, "右操作数不能为空");

                default:
                    throw new InvalidOperationError(operation, $"不支持的一元运算符: {operation.Opera}");
            }
        }

        switch (operation.Opera)
        {
            case LangTokenType.Plus:
                if (leftType == typeof(string) || rightType == typeof(string))
                {
                    // 处理字符串连接：先按语言的显示规则把操作数转成字符串，再拼接。
                    // 直接 string.Concat(object, object) 会走 CLR ToString(),
                    // bool 会输出 True/False、原生集合会输出 System.Collections.Generic.List`1[...]，
                    // 与解释器的 true/false 和 [1, 2] 不一致。
                    operation.Left?.Accept(new CompilerVisitor(ilGenerator, local));
                    EmitConcatOperand(ilGenerator, leftType);

                    operation.Right?.Accept(new CompilerVisitor(ilGenerator, local));
                    EmitConcatOperand(ilGenerator, rightType);

                    var concatMethod = typeof(string).GetMethod("Concat", [typeof(string), typeof(string)])!;
                    ilGenerator.Emit(OpCodes.Call, concatMethod);
                    return typeof(string);
                }

                // 数组 / 列表的 + 按顺序拼接（解释器与虚拟机同样支持）。
                if (leftType is { IsArray: true } && rightType is { IsArray: true })
                {
                    operation.Left!.Accept(new CompilerVisitor(ilGenerator, local));
                    operation.Right!.Accept(new CompilerVisitor(ilGenerator, local));
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(Old8Lang.Runtime.LangCollectionOperations).GetMethod(
                            nameof(Old8Lang.Runtime.LangCollectionOperations.ConcatArrays))!);
                    return typeof(object[]);
                }

                static bool IsListType(Type? type) =>
                    type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(List<>);

                if (IsListType(leftType) && IsListType(rightType))
                {
                    operation.Left!.Accept(new CompilerVisitor(ilGenerator, local));
                    operation.Right!.Accept(new CompilerVisitor(ilGenerator, local));
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(Old8Lang.Runtime.LangCollectionOperations).GetMethod(
                            nameof(Old8Lang.Runtime.LangCollectionOperations.ConcatLists))!);
                    return typeof(List<object>);
                }

                // 处理数值类型加法
                return NumericBinaryOpHelper.GenerateAddition(operation.Left, operation.Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Minus:
                return NumericBinaryOpHelper.GenerateSubtraction(operation.Left, operation.Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Star:
                return NumericBinaryOpHelper.GenerateMultiplication(operation.Left, operation.Right, ilGenerator, local, leftType, rightType, operation);
            case LangTokenType.Slash:
                return NumericBinaryOpHelper.GenerateDivision(operation.Left, operation.Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Percent:
                return NumericBinaryOpHelper.GenerateModulo(operation.Left, operation.Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.Caret:
                return NumericBinaryOpHelper.GeneratePower(operation.Left, operation.Right, ilGenerator, local, leftType, rightType);
            case LangTokenType.GreaterThan:
                return ComparisonOpHelper.GenerateGreaterThan(operation.Left, operation.Right, ilGenerator, local);
            case LangTokenType.LessThan:
                return ComparisonOpHelper.GenerateLessThan(operation.Left, operation.Right, ilGenerator, local);
            case LangTokenType.Equals:
                return ComparisonOpHelper.GenerateEquals(operation.Left, operation.Right, ilGenerator, local);
            case LangTokenType.NotEquals:
                return ComparisonOpHelper.GenerateNotEquals(operation.Left, operation.Right, ilGenerator, local);
            case LangTokenType.And:
                return LogicalOpILHelper.GenerateAnd(operation.Left!, operation.Right!, ilGenerator, local);
            case LangTokenType.Or:
                return LogicalOpILHelper.GenerateOr(operation.Left!, operation.Right!, ilGenerator, local);
            case LangTokenType.Xor:
                return LogicalOpILHelper.GenerateXor(operation.Left!, operation.Right!, ilGenerator, local);
            case LangTokenType.LessThanEquals:
                return ComparisonOpHelper.GenerateLessThanEquals(operation.Left, operation.Right, ilGenerator, local);
            case LangTokenType.GreaterThanEquals:
                return ComparisonOpHelper.GenerateGreaterThanEquals(operation.Left, operation.Right, ilGenerator, local);
            case LangTokenType.In:
                return InOperatorHelper.GenerateInOperator(operation.Left!, operation.Right!, ilGenerator, local, rightType, operation);
            case LangTokenType.As:
                return TypeCheckILHelper.GenerateAsOperator(operation.Left!, operation.Right!, ilGenerator, local, leftType, operation);
            case LangTokenType.Is:
                return TypeCheckILHelper.GenerateIsOperator(operation.Left!, operation.Right!, ilGenerator, local, leftType);
            case LangTokenType.IsNot:
                return TypeCheckILHelper.GenerateIsNotOperator(operation.Left!, operation.Right!, ilGenerator, local, leftType);
            case LangTokenType.NullishCoalescing:
                return NullishCoalescingILHelper.GenerateNullishCoalescing(operation.Left!, operation.Right!, ilGenerator, local, leftType!);
            case LangTokenType.Dot:
                return DotOperatorILHelper.GenerateDotOperator(operation.Left, right, ilGenerator, local, leftType, rightType, operation);
            default:
                throw new InvalidOperationError(operation, $"不支持的二元运算符: {operation.Opera}");
        }
    }
}
