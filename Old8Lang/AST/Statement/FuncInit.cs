using Old8Lang.LangParser;
using System.Reflection.Emit;
using System.Text;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Compiler;
using Old8Lang.Error;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 函数初始化类，用于处理Old8Lang中的函数声明
/// </summary>
/// <param name="a">函数值对象</param>
/// <param name="position">源代码位置信息，用于错误报告</param>
public class FuncInit(FuncLangValue a, SourcePosition position = default) : OldStatement(position)
{
    /// <summary>
    /// 函数值对象，包含函数的完整定义
    /// </summary>
    public readonly FuncLangValue FuncLangValue = a;
    
    /// <summary>
    /// 检查函数是否为Lambda表达式（通过检查Id是否为null）
    /// </summary>
    public bool IsLambda => FuncLangValue.Id == null;

    /// <summary>
    /// 在解释模式下执行函数初始化
    /// </summary>
    /// <param name="manager">变量管理器，用于管理函数的声明和访问</param>
    /// <exception cref="DuplicateNameError">当函数已存在时抛出</exception>
    public override void Run(VariateManager manager)
    {
        // 检查函数是否已存在（只有当函数名和参数数量都相同时才视为重复）
        if (FuncLangValue.Id != null)
        {
            var existingFunc = manager.ImportInfos.FirstOrDefault(info =>
                info is FuncLangValue func &&
                func.Id?.IdName == FuncLangValue.Id.IdName &&
                func.Ids?.Count == FuncLangValue.Ids?.Count);

            if (existingFunc != null)
            {
                throw new DuplicateNameError(this, FuncLangValue.Id.IdName, "函数");
            }
        }

        manager.AddClassAndFunc(FuncLangValue);
    }

    /// <summary>
    /// 在编译模式下生成函数的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器，用于管理函数的声明和访问</param>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        // 验证函数类型注解完整性（编译模式要求）
        ValidateTypeAnnotations(local);

        // 获取方法的名称
        var methodName = FuncLangValue.Id!.IdName;
        if (FuncLangValue.Method != null)
        {
            local.DelegateVar.Add(methodName, FuncLangValue.Method);
            return;
        }

        // 使用参数的类型注解来确定参数类型
        var parameterTypes = FuncLangValue.Ids!.Select(item => item.OutputType(local)).ToArray();

        // 创建一个新的LocalManager实例，专门用于函数体的IL生成
        // 这样可以避免函数内部的局部变量与外部的局部变量冲突
        var funcLocal = new LocalManager() { FilePath = local.FilePath, Interpreter = local.Interpreter };

        // 先处理参数，将它们添加到funcLocal中，这样GetItemType才能正确推断返回类型
        for (var i = 0; i < FuncLangValue.Ids!.Count; i++)
        {
            var id = FuncLangValue.Ids[i];
            var paramType = parameterTypes[i];
            // 创建一个临时的LocalBuilder来表示参数
            // 注意：这里我们不能使用真正的LocalBuilder，因为还没有创建ILGenerator
            // 所以我们使用一个占位符，稍后会替换
            funcLocal.LocalVarTypes[id.IdName] = paramType;
        }

        // 优先使用显式声明的返回类型
        // 如果类型注解存在但OutputType返回null/object，则仍尝试推断（用于兼容性）
        var returnType = FuncLangValue.Id?.OutputType(local);
        if (returnType == null || returnType == typeof(object))
        {
            // 如果OutputType无法解析，尝试从函数体推断
            returnType = GetItemType(FuncLangValue.BlockStatement, funcLocal);
        }

        // 定义新的方法
        var dynamicMethod = new DynamicMethod(
            methodName,
            returnType,
            parameterTypes,
            true
        );

        // 创建方法的 IL 发射器
        var methodIl = dynamicMethod.GetILGenerator();

        // 清空funcLocal，重新添加参数（这次使用真正的LocalBuilder）
        funcLocal.LocalVar.Clear();

        // 处理参数
        for (var i = 0; i < FuncLangValue.Ids!.Count; i++)
        {
            var id = FuncLangValue.Ids[i];
            // 使用实际的参数类型声明局部变量
            var paramType = parameterTypes[i];
            var localVar = methodIl.DeclareLocal(paramType);
            funcLocal.AddLocalVar(id.IdName, localVar);
            // 加载参数并存储到局部变量
            methodIl.Emit(OpCodes.Ldarg, i);
            methodIl.Emit(OpCodes.Stloc, localVar);
        }

        // 生成方法体的 IL 代码
        FuncLangValue.BlockStatement.GenerateIl(methodIl, funcLocal);

        // 检查函数体的最后一个语句是否是 ReturnStatement
        var lastStatement = FuncLangValue.BlockStatement.Count > 0
            ? FuncLangValue.BlockStatement[^1]
            : null;

        // 只有当最后一个语句不是 ReturnStatement 时，才添加 Ret 指令
        if (lastStatement is not ReturnStatement)
        {
            // 确保栈平衡
            if (returnType == typeof(void))
            {
                // 对于 void 方法，直接添加 Ret 指令
                methodIl.Emit(OpCodes.Ret);
            }
            else
            {
                // 对于有返回值的方法，如果没有显式 return，需要提供默认返回值
                if (returnType.IsValueType)
                {
                    // 对于值类型，创建默认值
                    var defaultValue = Activator.CreateInstance(returnType);
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
                        var defaultLocal = methodIl.DeclareLocal(returnType);
                        methodIl.Emit(OpCodes.Initobj, returnType);
                        methodIl.Emit(OpCodes.Ldloc, defaultLocal);
                    }
                }
                else
                {
                    // 对于引用类型，返回 null
                    methodIl.Emit(OpCodes.Ldnull);
                }
                methodIl.Emit(OpCodes.Ret);
            }
        }

        // 将方法添加到本地变量管理器
        // 对于用户定义的函数，我们需要保留原始方法名以便调用
        // 对于重载函数，使用函数名+参数类型组合作为键，支持更准确的函数重载
        var delegateKey = methodName;
        if (FuncLangValue.Ids != null)
        {
            // 使用函数名+参数类型作为键，支持更准确的函数重载
            var paramTypeNames = string.Join("_", parameterTypes.Select(t => t.Name));
            delegateKey = $"{methodName}${paramTypeNames}";
        }
        local.DelegateVar.TryAdd(delegateKey, dynamicMethod);

        // 同时存储函数的参数列表信息，用于支持默认参数
        if (FuncLangValue.Ids != null)
        {
            local.FuncParameters.TryAdd(delegateKey, FuncLangValue.Ids);
        }
    }

    /// <summary>
    /// 验证函数的类型注解完整性（编译模式要求）
    /// </summary>
    /// <param name="local">局部变量管理器，用于报告错误</param>
    private void ValidateTypeAnnotations(LocalManager local)
    {
        // 1. 验证所有参数的类型注解
        if (FuncLangValue.Ids != null)
        {
            for (int i = 0; i < FuncLangValue.Ids.Count; i++)
            {
                var param = FuncLangValue.Ids[i];

                // 如果参数没有类型注解，检查是否有默认值
                if (string.IsNullOrEmpty(param.AssumptionType))
                {
                    // 如果有默认值，可以从默认值推断类型，不报错
                    if (param.DefaultValue != null)
                    {
                        // 验证默认值的类型有效性
                        if (param.DefaultValue.OutputType(local) == null)
                        {
                            var defaultErrorMsg = $"[编译模式错误] 函数 '{FuncLangValue.Id?.IdName}' 的参数 '{param.IdName}' 的默认值类型无效\n\n" +
                                               $"默认值必须是一个有效的表达式，可以推断出具体类型。\n\n" +
                                               $"修复示例：\n" +
                                               $"  func {FuncLangValue.Id?.IdName}(..., {param.IdName}: 0, ...) -> returnType {{ ... }}\n" +
                                               $"  func {FuncLangValue.Id?.IdName}(..., {param.IdName}: \"string\", ...) -> returnType {{ ... }}";
                            local.ReportError(defaultErrorMsg, param.Position);
                        }
                        continue;
                    }

                    // 既没有类型注解也没有默认值，报错
                    var errorMsg = $"[编译模式错误] 函数 '{FuncLangValue.Id?.IdName}' 的参数 '{param.IdName}' (第{i + 1}个参数) 缺少类型注解\n\n" +
                                  $"编译模式下所有函数参数必须满足以下之一：\n" +
                                  $"  1. 显式声明类型注解：{param.IdName}:int\n" +
                                  $"  2. 提供默认值以推断类型：{param.IdName}: 123\n\n" +
                                  $"修复示例：\n" +
                                  $"  func {FuncLangValue.Id?.IdName}(..., {param.IdName}:int, ...) -> returnType {{ ... }}\n" +
                                  $"  func {FuncLangValue.Id?.IdName}(..., {param.IdName}: 0, ...) -> returnType {{ ... }}\n\n" +
                                  $"支持的类型：int, double, string, bool, char, void, list<T>, array<T>, dictionary<K,V>";
                    local.ReportError(errorMsg, param.Position);
                }
            }
        }

        // 2. 验证返回值类型注解
        if (FuncLangValue.Id != null && string.IsNullOrEmpty(FuncLangValue.Id.AssumptionType))
        {
            // 对于Lambda表达式，如果没有显式的返回类型注解，尝试从函数体推断
            if (!IsLambda)
            {
                // 普通函数必须显式声明返回类型
                var errorMsg = $"[编译模式错误] 函数 '{FuncLangValue.Id.IdName}' 缺少返回值类型注解\n\n" +
                              $"编译模式下所有函数必须显式声明返回类型，不能通过return语句推断。\n\n" +
                              $"修复示例：\n" +
                              $"  方式1：func {FuncLangValue.Id.IdName}(...) -> int {{ return ... }}\n" +
                              $"  方式2：func {FuncLangValue.Id.IdName}(...) -> void {{ ... }}\n" +
                              $"  方式3：{FuncLangValue.Id.IdName}:int(...) -> {{ return ... }}";
                local.ReportError(errorMsg, FuncLangValue.Id.Position);
            }
        }
        else if (FuncLangValue.Id != null && !string.IsNullOrEmpty(FuncLangValue.Id.AssumptionType))
        {
            // 验证返回类型注解的有效性
            var returnType = FuncLangValue.Id.OutputType(local);
            if (returnType == null)
            {
                var errorMsg = $"[编译模式错误] 函数 '{FuncLangValue.Id.IdName}' 的返回类型注解 '{FuncLangValue.Id.AssumptionType}' 无效\n\n" +
                              $"请使用有效的类型注解，如：int, double, string, bool, char, void, list<T>, array<T>, dictionary<K,V>\n\n" +
                              $"修复示例：\n" +
                              $"  func {FuncLangValue.Id.IdName}(...) -> int {{ return ... }}\n" +
                              $"  func {FuncLangValue.Id.IdName}(...) -> void {{ ... }}";
                local.ReportError(errorMsg, FuncLangValue.Id.Position);
            }
        }
    }

    /// <summary>
    /// 从语句块中推断返回类型
    /// </summary>
    /// <param name="statement">要分析的语句块</param>
    /// <param name="local">局部变量管理器</param>
    /// <returns>推断出的返回类型</returns>
    private static Type GetItemType(OldStatement statement, LocalManager local)
    {
        for (var i = 0; i < statement.Count; i++)
        {
            var item = statement[i];

            // 如果是SetStatement，记录局部变量的类型
            if (item is SetStatement { Id: not null } setStatement)
            {
                var varType = setStatement.Value.OutputType(local);
                if (varType != null)
                {
                    local.LocalVarTypes[setStatement.Id.IdName] = varType;
                }
            }

            if (item is ReturnStatement returnStatement)
            {
                // 确保返回类型不为null
                var returnType = returnStatement.OutputType(local);
                return returnType;
            }

            if (item == null || item.Count == 0)
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
        var paramList = FuncLangValue.Ids != null ? string.Join(", ", FuncLangValue.Ids) : string.Empty;
        sb.AppendLine($"func {FuncLangValue.Id}({paramList})");
        sb.AppendLine($"{{ {FuncLangValue.BlockStatement} }}");
        return sb.ToString();
    }
}