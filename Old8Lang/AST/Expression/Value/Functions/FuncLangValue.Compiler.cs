using System.Reflection.Emit;
using Old8Lang.AST.Statement;
using Old8Lang.Compiler.CodeGeneration;

// ReSharper disable CheckNamespace

namespace Old8Lang.AST.Expression.Value;

/// <summary>
/// FuncLangValue - 编译器支持
/// </summary>
public partial class FuncLangValue
{
    public override Type OutputType(LocalManager local)
    {
        var idType = Id?.OutputType(local);
        if (idType is not null && idType != typeof(object)) return idType;
        var a = GetItemType(BlockStatement, local);
        return a;
    }


    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.OutputType(local);
                if (varType is not null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                return returnStatement.OutputType(local);
            }

            if (item is null || item.Count == 0)
            {
                continue;
            }

            var innerType = GetItemType(item, local);
            if (innerType != typeof(void))
            {
                return innerType;
            }
        }

        return typeof(void);
    }


    public override string ToString()
    {
        if (Method is not null)
        {
            return $"{Method}";
        }

        var paramList = Ids is not null ? string.Join(", ", Ids) : string.Empty;
        return $"func {Id}({paramList}) \n {{ {BlockStatement} }}";
    }


    public override void LoadIlValue(ILGenerator ilGenerator, LocalManager local)
    {
        // 如果是.NET方法，直接加载方法引用
        if (Method is not null)
        {
            // 对于实例方法，需要先加载对象实例到堆栈上
            // 这里假设Method已经是正确的委托类型
        }

        // 如果是Old8Lang函数，直接返回，因为函数调用是通过Instance类处理的
        // 不需要在这里加载函数委托
    }


    public override void SetValueToIl(ILGenerator ilGenerator, LocalManager local, string idName)
    {
        // 【新增】Lambda表达式类型注解验证
        if (IsLambda || Id is null)
        {
            ValidateLambdaTypeAnnotations(local, idName);
        }

        // Lambda表达式需要特殊处理：编译成Delegate
        // 普通方法：编译成DynamicMethod

        // Lambda表达式没有函数名(Id is null)，使用变量名作为方法名
        var methodName = Id?.IdName ?? idName;

        // 如果已经是编译好的方法，直接注册
        if (Method is not null)
        {
            local.DelegateVar.Add(methodName, Method);
            return;
        }

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = Ids!.Select(item => item.OutputType(funcLocal)).ToArray();

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < Ids!.Count; i++)
        {
            var id = Ids[i];
            var paramType = parameterTypes[i];
            funcLocal.LocalVarTypes[id.IdName] = paramType;
        }

        // 获取返回类型
        var returnType = GetItemType(BlockStatement, funcLocal);

        // 根据函数类型选择不同的处理方式
        if (IsLambda || Id is null)
        {
            // Lambda表达式处理：编译成Delegate

            // 定义新的方法
            var dynamicMethod = new DynamicMethod(
                methodName,
                returnType,
                parameterTypes,
                true
            );

            // 创建方法的 IL 发射器
            var methodIl = dynamicMethod.GetILGenerator();

            // 处理参数
            for (var i = 0; i < Ids!.Count; i++)
            {
                var id = Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType);
                funcLocal.AddLocalVar(id.IdName, localVar);
                // 加载参数并存储到局部变量
                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }

            // 生成方法体的 IL 代码
            BlockStatement.GenerateIl(methodIl, funcLocal);

            // 检查函数体的最后一个语句是否是 ReturnStatement
            var lastStatement = BlockStatement.Count > 0
                ? BlockStatement[^1]
                : null;

            // 确保方法有正确的返回值
            if (lastStatement is not ReturnStatement)
            {
                if (returnType == typeof(void))
                {
                    methodIl.Emit(OpCodes.Ret);
                }
                else
                {
                    // 对于有返回值的Lambda表达式，确保返回默认值
                    if (returnType.IsValueType)
                    {
                        // 根据返回类型生成不同的默认值
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
                            // 对于其他值类型，初始化并加载默认值
                            var defaultValueLocal = methodIl.DeclareLocal(returnType);
                            methodIl.Emit(OpCodes.Initobj, returnType);
                            methodIl.Emit(OpCodes.Ldloc, defaultValueLocal);
                        }
                    }
                    else
                    {
                        // 引用类型返回null
                        methodIl.Emit(OpCodes.Ldnull);
                    }

                    methodIl.Emit(OpCodes.Ret);
                }
            }

            // 注册Lambda表达式到DelegateVar
            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
            var delegateKey = $"{methodName}${paramTypeNames}";
            local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
        }
        else
        {
            // 普通方法处理：编译成DynamicMethod

            // 定义新的方法
            var dynamicMethod = new DynamicMethod(
                methodName,
                returnType,
                parameterTypes,
                true
            );

            // 创建方法的 IL 发射器
            var methodIl = dynamicMethod.GetILGenerator();

            // 处理参数
            for (var i = 0; i < Ids!.Count; i++)
            {
                var id = Ids[i];
                var paramType = parameterTypes[i];
                var localVar = methodIl.DeclareLocal(paramType);
                funcLocal.AddLocalVar(id.IdName, localVar);
                // 加载参数并存储到局部变量
                methodIl.Emit(OpCodes.Ldarg, i);
                methodIl.Emit(OpCodes.Stloc, localVar);
            }

            // 生成方法体的 IL 代码
            BlockStatement.GenerateIl(methodIl, funcLocal);

            // 检查函数体的最后一个语句是否是 ReturnStatement
            var lastStatement = BlockStatement.Count > 0
                ? BlockStatement[^1]
                : null;

            // 确保方法有正确的返回值
            if (lastStatement is not ReturnStatement)
            {
                if (returnType == typeof(void))
                {
                    methodIl.Emit(OpCodes.Ret);
                }
                else
                {
                    // 对于有返回值的方法，确保返回默认值
                    if (returnType.IsValueType)
                    {
                        // 根据返回类型生成不同的默认值
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
                            // 对于其他值类型，初始化并加载默认值
                            var defaultValueLocal = methodIl.DeclareLocal(returnType);
                            methodIl.Emit(OpCodes.Initobj, returnType);
                            methodIl.Emit(OpCodes.Ldloc, defaultValueLocal);
                        }
                    }
                    else
                    {
                        // 引用类型返回null
                        methodIl.Emit(OpCodes.Ldnull);
                    }

                    methodIl.Emit(OpCodes.Ret);
                }
            }

            // 将方法注册到本地变量管理器的DelegateVar中
            // 对于泛型函数，需要同时注册泛型版本和特化版本
            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));

            if (IsGeneric)
            {
                // 注册泛型函数的基础版本（不带类型签名）
                local.DelegateVar.TryAdd(methodName, dynamicMethod);

                // 也注册带类型签名的版本，确保兼容性
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
            }
            else
            {
                // 普通函数只注册带类型签名的版本
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.DelegateVar.TryAdd(delegateKey, dynamicMethod);
            }

            // 同时存储函数的参数列表信息，用于支持默认参数
            if (Ids is not null)
            {
                var delegateKey = $"{methodName}${paramTypeNames}";
                local.FuncParameters.TryAdd(delegateKey, Ids);
            }
        }
    }

    /// <summary>
    /// 检查表达式是否为常量表达式（可以安全缓存）
    /// </summary>

}
