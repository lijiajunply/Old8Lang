using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

using Old8Lang.AST.Visitor;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 函数初始化类，用于处理Old8Lang中的函数声明
/// </summary>
/// <param name="a">函数值对象</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public partial class FuncInit(FuncLangValue a, SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 函数值对象，包含函数的完整定义
    /// </summary>
    public readonly FuncLangValue FuncValue = a;

    /// <summary>
    /// 检查函数是否为Lambda表达式（通过检查Id是否为null）
    /// </summary>
    public bool IsLambda => FuncValue.Id is null;

    /// <summary>
    /// 应用装饰器到函数
    /// </summary>
    internal FuncLangValue ApplyDecorators(FuncLangValue originalFunc, VariateManager manager)
    {
        if (originalFunc.Decorators is null || originalFunc.Decorators.Count == 0)
        {
            return originalFunc;
        }

        var currentFunc = originalFunc;

        // 从下到上应用装饰器（最接近函数的装饰器最先应用）
        for (int i = originalFunc.Decorators.Count - 1; i >= 0; i--)
        {
            var decorator = originalFunc.Decorators[i];
            currentFunc = ApplySingleDecorator(decorator, currentFunc, manager);
        }

        return currentFunc;
    }

    /// <summary>
    /// 应用单个装饰器
    /// </summary>
    internal FuncLangValue ApplySingleDecorator(FunctionDecorator decorator, FuncLangValue targetFunc,
        VariateManager manager)
    {
        // 将目标函数临时注册到全局作用域，以便装饰器可以引用它
        // 注意：不能在使用后立即清理，因为返回的包装函数可能需要引用它（闭包）
        var tempFuncName = $"__temp_func_{Guid.NewGuid():N}";
        var tempFunc = new FuncLangValue(
            new LangId(tempFuncName, position: decorator.Position),
            targetFunc.Ids ?? [],
            targetFunc.BlockStatement,
            targetFunc.GenericParameters,
            targetFunc.Position,
            isLambda: false
        )
        {
            // 保留 targetFunc 的 CapturedScope，确保闭包变量可以正确访问
            CapturedScope = targetFunc.CapturedScope
        };
        manager.AddClassAndFunc(tempFunc);

        LangValueType result;

        // 检查装饰器是否有参数
        if (decorator.Arguments is not null && decorator.Arguments.Count > 0)
        {
            // 带参数的装饰器：先调用装饰器函数获取包装器，然后用包装器包装目标函数
            // 例如：@cache(timeout: 60) -> wrapper = cache(60); result = wrapper(targetFunc)

            // 第一步：调用装饰器函数获取包装器
            var decoratorCallExpr = new FunctionCallExpression(
                new LangId(decorator.Name, position: decorator.Position),
                decorator.Arguments,
                decorator.Position
            );
            var wrapperFunc = decoratorCallExpr.Accept(new InterpreterVisitor(manager));

            if (wrapperFunc is not FuncLangValue wrapper)
            {
                throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须返回一个函数");
            }

            // 第二步：调用包装器，传入目标函数
            // 直接调用 wrapper，而不是通过 FunctionCallExpression
            // 这样可以保留 wrapper 的 CapturedScope
            result = wrapper.Run(manager, [new LangId(tempFuncName, position: decorator.Position)]);
        }
        else
        {
            // 无参数的装饰器：直接调用装饰器函数，传入目标函数
            // 例如：@log -> log(targetFunc)
            var decoratorId = new LangId(decorator.Name, position: decorator.Position);
            var decoratorFunc = manager.GetValue(decoratorId);

            if (decoratorFunc is null)
            {
                throw new NameError(decorator.Position, decorator.Name);
            }

            if (decoratorFunc is not FuncLangValue)
            {
                throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须是一个函数");
            }

            var callExpr = new FunctionCallExpression(
                decoratorId,
                [new LangId(tempFuncName, position: decorator.Position)],
                decorator.Position
            );
            result = callExpr.Accept(new InterpreterVisitor(manager));
        }

        // 装饰器应该返回一个新的函数
        if (result is not FuncLangValue wrappedFunc)
        {
            throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须返回一个函数");
        }

        // 保留原函数的名称
        if (targetFunc.Id is not null)
        {
            // 创建新的 FuncLangValue，保留 wrappedFunc 的所有属性，包括 CapturedScope
            return new FuncLangValue(
                targetFunc.Id,
                wrappedFunc.Ids ?? [],
                wrappedFunc.BlockStatement,
                wrappedFunc.GenericParameters,
                wrappedFunc.Position,
                isLambda: false
            )
            {
                CapturedScope = wrappedFunc.CapturedScope
            };
        }

        return wrappedFunc;
    }

    /// <summary>
    /// 在 IL 模式下应用装饰器到函数
    /// 策略：使用解释器执行装饰器函数，获取包装后的函数
    /// </summary>
    internal FuncLangValue ApplyDecoratorsForCompiler(FuncLangValue originalFunc, LocalManager local)
    {
        if (originalFunc.Decorators is null || originalFunc.Decorators.Count == 0)
        {
            return originalFunc;
        }

        var manager = local.Interpreter!.Manager;
        var currentFunc = originalFunc;

        // 从下到上应用装饰器（最接近函数的装饰器最先应用）
        for (int i = originalFunc.Decorators.Count - 1; i >= 0; i--)
        {
            var decorator = originalFunc.Decorators[i];
            currentFunc = ApplySingleDecoratorForCompiler(decorator, currentFunc, manager);
        }

        return currentFunc;
    }

    /// <summary>
    /// 在 IL 模式下应用单个装饰器
    /// </summary>
    internal FuncLangValue ApplySingleDecoratorForCompiler(FunctionDecorator decorator, FuncLangValue targetFunc,
        VariateManager manager)
    {
        // 将目标函数临时注册到全局作用域，以便装饰器可以引用它
        var tempFuncName = $"__temp_func_{Guid.NewGuid():N}";
        var tempFunc = new FuncLangValue(
            new LangId(tempFuncName, position: decorator.Position),
            targetFunc.Ids ?? [],
            targetFunc.BlockStatement,
            targetFunc.GenericParameters,
            targetFunc.Position,
            isLambda: false
        )
        {
            CapturedScope = targetFunc.CapturedScope
        };
        manager.AddClassAndFunc(tempFunc);

        LangValueType result;

        // 检查装饰器是否有参数
        if (decorator.Arguments is not null && decorator.Arguments.Count > 0)
        {
            // 带参数的装饰器：先调用装饰器函数获取包装器，然后用包装器包装目标函数
            var decoratorCallExpr = new FunctionCallExpression(
                new LangId(decorator.Name, position: decorator.Position),
                decorator.Arguments,
                decorator.Position
            );
            var wrapperFunc = decoratorCallExpr.Accept(new InterpreterVisitor(manager));

            if (wrapperFunc is not FuncLangValue wrapper)
            {
                throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须返回一个函数");
            }

            // 第二步：调用包装器，传入目标函数
            result = wrapper.Run(manager, [new LangId(tempFuncName, position: decorator.Position)]);
        }
        else
        {
            // 无参数的装饰器：直接调用装饰器函数，传入目标函数
            var decoratorId = new LangId(decorator.Name, position: decorator.Position);
            var decoratorFunc = manager.GetValue(decoratorId);

            if (decoratorFunc is null)
            {
                throw new NameError(decorator.Position, decorator.Name);
            }

            if (decoratorFunc is not FuncLangValue)
            {
                throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须是一个函数");
            }

            var callExpr = new FunctionCallExpression(
                decoratorId,
                [new LangId(tempFuncName, position: decorator.Position)],
                decorator.Position
            );
            result = callExpr.Accept(new InterpreterVisitor(manager));
        }

        // 装饰器应该返回一个新的函数
        if (result is not FuncLangValue wrappedFunc)
        {
            throw new InvalidOperationError(decorator.Position, $"装饰器 '{decorator.Name}' 必须返回一个函数");
        }

        // 保留原函数的名称
        if (targetFunc.Id is not null)
        {
            return new FuncLangValue(
                targetFunc.Id,
                wrappedFunc.Ids ?? [],
                wrappedFunc.BlockStatement,
                wrappedFunc.GenericParameters,
                wrappedFunc.Position,
                isLambda: false
            )
            {
                CapturedScope = wrappedFunc.CapturedScope
            };
        }

        return wrappedFunc;
    }

    /// <summary>
    /// 验证函数的类型注解完整性（IL 模式要求）
    /// </summary>
    /// <param name="local">局部变量管理器，用于报告错误</param>
    internal void ValidateTypeAnnotations(LocalManager local)
    {
        var inferenceEnabled = TypeInferenceConfig.Instance.EnableTypeInference;

        // 1. 验证所有参数的类型注解
        if (FuncValue.Ids is not null)
        {
            for (int i = 0; i < FuncValue.Ids.Count; i++)
            {
                var param = FuncValue.Ids[i];

                // 如果参数没有类型注解，检查是否有默认值
                if (string.IsNullOrEmpty(param.AssumptionType))
                {
                    // 如果有默认值，可以从默认值推断类型，不报错
                    if (param.DefaultValue is not null)
                    {
                        // 验证默认值的类型有效性
                        if (param.DefaultValue.Accept(new TypeInferenceVisitor(local)) is null)
                        {
                            var defaultErrorMsg =
                                $"[IL 模式错误] 函数 '{FuncValue.Id?.IdName}' 的参数 '{param.IdName}' 的默认值类型无效\n\n" +
                                $"默认值必须是一个有效的表达式，可以推断出具体类型。\n\n" +
                                $"修复示例：\n" +
                                $"  func {FuncValue.Id?.IdName}(..., {param.IdName}: 0, ...) -> returnType {{ ... }}\n" +
                                $"  func {FuncValue.Id?.IdName}(..., {param.IdName}: \"string\", ...) -> returnType {{ ... }}";
                            local.ReportError(defaultErrorMsg, param.Position);
                        }

                        continue;
                    }

                    // 如果启用了类型推断，允许缺少类型注解
                    if (inferenceEnabled)
                    {
                        if (TypeInferenceConfig.Instance.DebugOutput)
                        {
                            Console.WriteLine($"  ℹ️  参数 {param.IdName} 缺少类型注解，将尝试推断");
                        }

                        continue;
                    }

                    // 既没有类型注解也没有默认值，且未启用类型推断，报错
                    var errorMsg =
                        $"[IL 模式错误] 函数 '{FuncValue.Id?.IdName}' 的参数 '{param.IdName}' (第{i + 1}个参数) 缺少类型注解\n\n" +
                        $"IL 模式下所有函数参数必须满足以下之一：\n" +
                        $"  1. 显式声明类型注解：{param.IdName}:int\n" +
                        $"  2. 提供默认值以推断类型：{param.IdName}: 123\n" +
                        $"  3. 启用类型推断功能（通过 TypeInferenceConfig）\n\n" +
                        $"修复示例：\n" +
                        $"  func {FuncValue.Id?.IdName}(..., {param.IdName}:int, ...) -> returnType {{ ... }}\n" +
                        $"  func {FuncValue.Id?.IdName}(..., {param.IdName}: 0, ...) -> returnType {{ ... }}\n\n" +
                        $"支持的类型：int, double, string, bool, char, void, list<T>, array<T>, dictionary<K,V>";
                    local.ReportError(errorMsg, param.Position);
                }
            }
        }

        // 2. 验证返回值类型注解
        if (FuncValue.Id is not null && string.IsNullOrEmpty(FuncValue.Id.AssumptionType))
        {
            // 对于Lambda表达式，如果没有显式的返回类型注解，尝试从函数体推断
            if (!IsLambda)
            {
                // 如果启用了类型推断，允许缺少返回类型注解
                if (inferenceEnabled && TypeInferenceConfig.Instance.InferReturnTypesFromBody)
                {
                    if (TypeInferenceConfig.Instance.DebugOutput)
                    {
                        Console.WriteLine($"  ℹ️  函数 {FuncValue.Id.IdName} 缺少返回类型注解，将尝试推断");
                    }

                    return;
                }

                // 普通函数必须显式声明返回类型
                var errorMsg = $"[IL 模式错误] 函数 '{FuncValue.Id.IdName}' 缺少返回值类型注解\n\n" +
                               $"IL 模式下所有函数必须显式声明返回类型，或启用类型推断功能。\n\n" +
                               $"修复示例：\n" +
                               $"  方式1：func {FuncValue.Id.IdName}(...) -> int {{ return ... }}\n" +
                               $"  方式2：func {FuncValue.Id.IdName}(...) -> void {{ ... }}\n" +
                               $"  方式3：{FuncValue.Id.IdName}:int(...) -> {{ return ... }}\n" +
                               $"  方式4：启用类型推断 (TypeInferenceConfig.Instance.EnableTypeInference = true)";
                local.ReportError(errorMsg, FuncValue.Id.Position);
            }
        }
        else if (FuncValue.Id is not null && !string.IsNullOrEmpty(FuncValue.Id.AssumptionType))
        {
            // 验证返回类型注解的有效性
            var returnType = FuncValue.Id.Accept(new TypeInferenceVisitor(local));
            if (returnType is null)
            {
                var errorMsg = $"[IL 模式错误] 函数 '{FuncValue.Id.IdName}' 的返回类型注解 '{FuncValue.Id.AssumptionType}' 无效\n\n" +
                               $"请使用有效的类型注解，如：int, double, string, bool, char, void, list<T>, array<T>, dictionary<K,V>\n\n" +
                               $"修复示例：\n" +
                               $"  func {FuncValue.Id.IdName}(...) -> int {{ return ... }}\n" +
                               $"  func {FuncValue.Id.IdName}(...) -> void {{ ... }}";
                local.ReportError(errorMsg, FuncValue.Id.Position);
            }
        }
    }

    /// <summary>
    /// 从语句块中推断返回类型
    /// </summary>
    /// <param name="statement">要分析的语句块</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>推断出的返回类型</returns>
    internal static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.Accept(new TypeInferenceVisitor(local));
                if (varType is not null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                // 确保返回类型不为null
                var returnType = returnStatement.Accept(new TypeInferenceVisitor(local));
                return returnType;
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

        return typeof(void); // 默认返回void类型
    }

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为FuncInit是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为FuncInit是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将函数初始化转换为字符串表示
    /// </summary>
    /// <returns>函数初始化的字符串表示</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        var paramList = FuncValue.Ids is not null ? string.Join(", ", FuncValue.Ids) : string.Empty;
        sb.AppendLine($"func {FuncValue.Id}({paramList})");
        sb.AppendLine($"{{ {FuncValue.BlockStatement} }}");
        return sb.ToString();
    }

    /// <summary>
    /// 验证 params 参数的合法性
    /// </summary>
    /// <exception cref="SyntaxError">当 params 参数使用不当时抛出</exception>
    internal void ValidateParamsParameter()
    {
        if (FuncValue.Ids is null || FuncValue.Ids.Count == 0)
        {
            return;
        }

        // 查找所有 params 参数
        var paramsIndices = new List<int>();
        for (int i = 0; i < FuncValue.Ids.Count; i++)
        {
            if (FuncValue.Ids[i].IsParams)
            {
                paramsIndices.Add(i);
            }
        }

        // 如果没有 params 参数，直接返回
        if (paramsIndices.Count == 0)
        {
            return;
        }

        // 规则1: 只能有一个 params 参数
        if (paramsIndices.Count > 1)
        {
            var paramsParam = FuncValue.Ids[paramsIndices[1]];
            throw new SyntaxError(
                paramsParam.Position,
                $"函数 '{FuncValue.Id?.IdName}' 只能有一个 params 参数，但发现了 {paramsIndices.Count} 个");
        }

        var paramsIndex = paramsIndices[0];
        var paramsId = FuncValue.Ids[paramsIndex];

        // 规则2: params 参数必须是最后一个参数
        if (paramsIndex != FuncValue.Ids.Count - 1)
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 必须是参数列表的最后一个参数");
        }

        // 规则3: params 参数必须有类型注解，且必须是数组类型
        if (string.IsNullOrEmpty(paramsId.AssumptionType))
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 必须有类型注解，且必须是数组类型（例如 array<int>）");
        }

        // 检查是否是数组类型
        var typeAnnotation = paramsId.AssumptionType.ToLower().Trim();
        if (!typeAnnotation.StartsWith("array<") || !typeAnnotation.EndsWith(">"))
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 的类型必须是数组类型（例如 array<int>），但当前类型为 '{paramsId.AssumptionType}'");
        }

        // 规则4: params 参数不能有默认值
        if (paramsId.DefaultValue is not null)
        {
            throw new SyntaxError(
                paramsId.Position,
                $"函数 '{FuncValue.Id?.IdName}' 的 params 参数 '{paramsId.IdName}' 不能有默认值，params 参数会自动处理为空数组");
        }
    }
}