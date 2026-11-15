using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Runtime;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - 把表达式求值结果写入 IL 变量/字段/捕获槽。
/// 迁移自各节点上的 SetValueToIl 实现。
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 将表达式求值结果写入指定变量、参数、字段或闭包捕获槽。
    /// </summary>
    internal void EmitSetValueToIl(LangExpression expression, string idName)
    {
        switch (expression)
        {
            case AsyncGeneratorLangValue:
                throw new NotSupportedException("异步生成器不支持赋值操作");
            case GeneratorLangValue:
            case AsyncStreamLangValue:
                return;
            case ThreadLangValue:
            {
                var threadLocal = ilGenerator.DeclareLocal(typeof(ThreadLangValue));
                local.AddLocalVar(idName, threadLocal);
                ilGenerator.Emit(OpCodes.Stloc, threadLocal);
                return;
            }
            case VMThreadLangValue:
            {
                var vmThreadLocal = ilGenerator.DeclareLocal(typeof(VMThreadLangValue));
                local.AddLocalVar(idName, vmThreadLocal);
                ilGenerator.Emit(OpCodes.Stloc, vmThreadLocal);
                return;
            }
            case FuncLangValue func:
                EmitFuncSetValueToIl(func, idName);
                return;
            case LambdaExpression lambda:
                EmitFuncSetValueToIl(LambdaExpressionRuntimeFactory.Create(lambda) as FuncLangValue ?? throw new InvalidOperationException("async lambda 不能直接赋值为普通函数值"), idName);
                return;
            case Operation operation:
                EmitOperationSetValueToIl(operation, idName);
                return;
            default:
                EmitDefaultSetValueToIl(expression, idName);
                return;
        }
    }

    private void EmitDefaultSetValueToIl(LangExpression expression, string idName)
    {
        if (local.TryGetCapturedLocal(idName, out var captured))
        {
            var capturedValueType = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            local.ValidateType(captured.Type, capturedValueType, expression.Position);

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, captured.Index);
            expression.Accept(this);
            if (capturedValueType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, capturedValueType);
            }

            ilGenerator.Emit(OpCodes.Stelem_Ref);
            return;
        }

        // 1. 检查是否为字段赋值 (在类的方法中)
        if (!local.LocalVar.ContainsKey(idName) && !local.ArgumentIndices.ContainsKey(idName) &&
            local.FieldVar.TryGetValue(idName, out var fieldInfo))
        {
            ilGenerator.Emit(OpCodes.Ldarg_0);
            expression.Accept(this);

            var fieldValueType = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (fieldInfo.FieldType != fieldValueType)
            {
                if (fieldInfo.FieldType == typeof(object) && fieldValueType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Box, fieldValueType);
                }
                else if (fieldInfo.FieldType.IsValueType && fieldValueType == typeof(object))
                {
                    ilGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                }
            }

            ilGenerator.Emit(OpCodes.Stfld, fieldInfo);
            return;
        }

        // 2. 检查是否为函数参数赋值 (优化访问)
        if (local.ArgumentIndices.TryGetValue(idName, out var argIndex))
        {
            var targetType = local.LocalVarTypes.TryGetValue(idName, out var type) ? type : typeof(object);
            var argValueType = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(int);

            local.ValidateType(targetType, argValueType, expression.Position);

            expression.Accept(this);

            if (argIndex <= 255) ilGenerator.Emit(OpCodes.Starg_S, (byte)argIndex);
            else ilGenerator.Emit(OpCodes.Starg, argIndex);
            return;
        }

        // 3. 局部变量赋值处理
        var valueType = expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(int);

        // catch 块会把捕获的异常赋给外层变量（类型与初始值不同）。这类变量的槽位
        // 一律用 object 承载：否则 catch 处的赋值会在编译期换掉槽位，
        // 成功路径上写入的值在后续读取时变成未初始化的 null。
        // 只对引用类型值放宽，值类型仍按原值类型收窄（保持数值宽度的动态重赋值）。
        if (!valueType.IsValueType && local.CatchAssignedVariables.Contains(idName))
        {
            valueType = typeof(object);
        }

        if (local.LocalVarTypes.TryGetValue(idName, out var existingType))
        {
            if (local.ValidateType(existingType, valueType, expression.Position))
            {
                if (existingType != valueType && !local.StrictTypeChecking)
                {
                    local.LocalVarTypes[idName] = valueType;
                }
            }
        }
        else
        {
            local.LocalVarTypes[idName] = valueType;
        }

        var localVar = local.GetLocalVar(idName);
        var isNewLocal = localVar is null;
        if (localVar is not null)
        {
            if (localVar.LocalType != valueType)
            {
                // object 槽位天然能接收任何值：装箱后写入即可，不要换成新槽位，
                // 否则其它路径上已写入的值会丢失。
                if (localVar.LocalType == typeof(object))
                {
                    expression.Accept(this);
                    if (valueType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, valueType);
                    }

                    ilGenerator.Emit(OpCodes.Stloc, localVar);
                    local.LocalVarTypes[idName] = typeof(object);
                    return;
                }

                local.RemoveLocalVar(idName);
                localVar = ilGenerator.DeclareLocal(valueType);
                local.AddLocalVar(idName, localVar);
            }
        }

        if (isNewLocal)
        {
            // 先求值到不进入 LocalManager 的临时槽，避免 lambda 捕获尚未初始化的目标槽。
            var evaluatedValue = ilGenerator.DeclareLocal(valueType);
            expression.Accept(this);
            ilGenerator.Emit(OpCodes.Stloc, evaluatedValue);

            localVar = ilGenerator.DeclareLocal(valueType);
            local.AddLocalVar(idName, localVar);
            ilGenerator.Emit(OpCodes.Ldloc, evaluatedValue);
            ilGenerator.Emit(OpCodes.Stloc, localVar);
            return;
        }

        expression.Accept(this);
        ilGenerator.Emit(OpCodes.Stloc, localVar!);
    }

    private void EmitOperationSetValueToIl(Operation operation, string idName)
    {
        if (local.TryGetCapturedLocal(idName, out var captured))
        {
            var capturedValueType = operation.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            local.ValidateType(captured.Type, capturedValueType, operation.Position);

            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldc_I4, captured.Index);
            operation.Accept(this);
            if (capturedValueType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, capturedValueType);
            }

            ilGenerator.Emit(OpCodes.Stelem_Ref);
            return;
        }

        // 首先生成计算值的 IL 指令，将结果压入栈中
        operation.Accept(this);
        var type = operation.Accept(new TypeInferenceVisitor(local));
        if (type is null) return;

        var existingLocal = local.GetLocalVar(idName);
        if (existingLocal is not null)
        {
            if (existingLocal.LocalType != type)
            {
                // object 槽位能接收任何值：值类型装箱后写入，不能换成新槽位，
                // 否则其它路径（例如 try 成功分支）上写入的值会丢失。
                if (existingLocal.LocalType == typeof(object))
                {
                    if (type.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, type);
                    }

                    ilGenerator.Emit(OpCodes.Stloc, existingLocal);
                    local.LocalVarTypes[idName] = typeof(object);
                    return;
                }

                local.RemoveLocalVar(idName);
                var newLocal = ilGenerator.DeclareLocal(type);
                local.AddLocalVar(idName, newLocal);
                ilGenerator.Emit(OpCodes.Stloc, newLocal);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Stloc, existingLocal);
            }
        }
        else
        {
            var newLocal = ilGenerator.DeclareLocal(type);
            local.AddLocalVar(idName, newLocal);
            ilGenerator.Emit(OpCodes.Stloc, newLocal);
        }
    }

    private void EmitFuncSetValueToIl(FuncLangValue func, string idName)
    {
        // Lambda 表达式类型注解验证
        if (func.IsLambdaExpression)
        {
            func.ValidateLambdaTypeAnnotations(local, idName);
        }

        var methodName = func.Id?.IdName ?? idName;

        // 如果已经是编译好的方法，直接注册
        if (func.Method is not null)
        {
            local.DelegateVar.Add(methodName, func.Method);
            return;
        }

        // 创建一个新的 LocalManager 实例，专门用于函数体的 IL 生成
        var funcLocal = new LocalManager()
        {
            FilePath = local.FilePath,
            Interpreter = local.Interpreter,
            Globals = local.Globals
        };

        foreach (var (key, value) in local.GlobalStaticClasses)
            funcLocal.GlobalStaticClasses[key] = value;

        var funcIds = func.Ids!;
        var parameterTypes = funcIds
            .Select(item => item.Accept(new TypeInferenceVisitor(funcLocal))!)
            .ToArray();

        for (var i = 0; i < funcIds.Count; i++)
        {
            funcLocal.LocalVarTypes[funcIds[i].IdName] = parameterTypes[i];
        }

        var returnType = FuncLangValue.GetItemType(func.BlockStatement, funcLocal);

        if (func.IsLambdaExpression)
        {
            // Lambda：在运行时创建绑定当前局部变量的委托实例
            var lambdaDynamic = func.EmitLambdaClosure(ilGenerator, local, methodName);
            var lambdaLocal = local.GetOrCreateLocalVar(ilGenerator, idName, typeof(Delegate));
            ilGenerator.Emit(OpCodes.Stloc, lambdaLocal);
            local.LocalVarTypes[idName] = typeof(Delegate);
            local.LocalDelegateReturnTypes[idName] = lambdaDynamic.ReturnType;
        }
        else
        {
            // 普通方法：编译成 DynamicMethod
            var dynamicMethod = new DynamicMethod(
                methodName,
                returnType,
                parameterTypes,
                true
            );

            var methodIl = dynamicMethod.GetILGenerator();

            for (var i = 0; i < funcIds.Count; i++)
            {
                var id = funcIds[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType);
                funcLocal.AddLocalVar(id.IdName, localVar);
                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }

            func.BlockStatement.Accept(new CompilerVisitor(methodIl, funcLocal));

            var lastStatement = func.BlockStatement.Count > 0
                ? func.BlockStatement[^1]
                : null;

            if (lastStatement is not Old8Lang.AST.Statement.ReturnStatement)
            {
                if (returnType == typeof(void))
                {
                    methodIl.Emit(OpCodes.Ret);
                }
                else
                {
                    if (returnType.IsValueType)
                    {
                        if (returnType == typeof(int))
                        {
                            methodIl.Emit(OpCodes.Ldc_I4_0);
                        }
                        else if (returnType == typeof(double))
                        {
                            methodIl.Emit(OpCodes.Ldc_R8, 0.0);
                        }
                        else if (returnType == typeof(bool))
                        {
                            methodIl.Emit(OpCodes.Ldc_I4_0);
                        }
                        else
                        {
                            var defaultValueLocal = methodIl.DeclareLocal(returnType);
                            methodIl.Emit(OpCodes.Initobj, returnType);
                            methodIl.Emit(OpCodes.Ldloc, defaultValueLocal);
                        }
                    }
                    else
                    {
                        methodIl.Emit(OpCodes.Ldnull);
                    }

                    methodIl.Emit(OpCodes.Ret);
                }
            }

            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));

            if (func.IsGeneric)
            {
                local.DelegateVar.TryAdd(methodName, dynamicMethod);

                var delegateKeyGeneric = $"{methodName}${paramTypeNames}";
                local.DelegateVar.TryAdd(delegateKeyGeneric, dynamicMethod);
            }
            else
            {
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
            }

            var key = $"{methodName}${paramTypeNames}";
            local.FuncParameters.TryAdd(key, funcIds);
        }
    }
}
