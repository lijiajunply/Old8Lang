using System.Reflection.Emit;
using Old8Lang.Syntax;
using Old8Lang.AST.Statement;
using Old8Lang.Error;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Generators;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.ModuleObjects;
using Old8Lang.AST.Expression.OperationHelpers;
using Old8Lang.AST.Expression.StaticValues;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.ValueFunctions;
using Old8Lang.AST.Visitor;
using Old8Lang.AsyncGeneration;
using Old8Lang.Compiler;
using Old8Lang.Compiler.CodeGeneration;
using Old8Lang.Compiler.Helpers;
using Old8Lang.Compiler.Specialization;
using Old8Lang.Compiler.Verification;
using Old8Lang.Concurrency;
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
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using Old8Lang.LangParser;

namespace Old8Lang.AST.Visitor;

/// <summary>
/// CompilerVisitor - Statement 节点的 Visit 方法实现
/// </summary>
public partial class CompilerVisitor
{
    /// <summary>
    /// 访问 ExtensionDeclaration 节点
    /// </summary>
    public object? VisitExtensionDeclaration(ExtensionDeclaration node)
    {
        // 扩展方法在编译前已由解释器预执行注册进实例方法表（见 ExtensionDeclaration.GenerateIl），
        // 调用点按普通实例方法解析到 ExtensionMethodWrapper，这里无需生成 IL。
        return null;
    }

    /// <summary>
    /// 访问 BreakStatement 节点
    /// </summary>
    public object? VisitBreakStatement(BreakStatement node)
    {
        // 迁移自 BreakStatement.GenerateIl()
        if (local.BreakLabel.HasValue)
        {
            // 从 try/catch 内部跳出必须用 leave（br 不能跨越受保护区域边界）
            ilGenerator.Emit(local.IsInProtectedRegion ? OpCodes.Leave : OpCodes.Br, local.BreakLabel.Value);
        }
        else
        {
            throw new InvalidOperationError(node.Position, "Break statement outside of loop",
                "break语句只能在循环内部使用");
        }
        return null;
    }

    /// <summary>
    /// 访问 ContinueStatement 节点
    /// </summary>
    public object? VisitContinueStatement(ContinueStatement node)
    {
        // 迁移自 ContinueStatement.GenerateIl()
        if (local.ContinueLabel.HasValue)
        {
            ilGenerator.Emit(local.IsInProtectedRegion ? OpCodes.Leave : OpCodes.Br, local.ContinueLabel.Value);
        }
        else
        {
            throw new InvalidOperationError(node.Position, "Continue statement outside of loop",
                "continue语句只能在循环内部使用");
        }
        return null;
    }

    /// <summary>
    /// 访问 IfStatement 节点
    /// </summary>
    public object? VisitIfStatement(IfStatement node)
    {
        // 迁移自 IfStatement.GenerateIl()
        var labelElse = ilGenerator.DefineLabel();
        var labelEnd = ilGenerator.DefineLabel();

        // 处理 if 块
        var ifChild = node[0] as IfChild;
        if (ifChild is not null)
        {
            ConditionIlHelper.EmitBoolOperand(ifChild.Condition, ilGenerator, local);
            ilGenerator.Emit(OpCodes.Brfalse, labelElse);

            // if 部分
            ifChild.Block.Accept(this);
            ilGenerator.Emit(OpCodes.Br, labelEnd);
        }

        // 处理 elif 块
        ilGenerator.MarkLabel(labelElse);
        for (int i = 1; i < node.Count; i++)
        {
            var elifChild = node[i] as IfChild;
            if (elifChild is not null)
            {
                var nextElif = ilGenerator.DefineLabel();
                ConditionIlHelper.EmitBoolOperand(elifChild.Condition, ilGenerator, local);
                ilGenerator.Emit(OpCodes.Brfalse, nextElif);

                // elif 部分
                elifChild.Block.Accept(this);
                ilGenerator.Emit(OpCodes.Br, labelEnd);

                ilGenerator.MarkLabel(nextElif);
            }
            else if (node[i] is BlockStatement elseBlock)
            {
                // 处理 else 块
                elseBlock.Accept(this);
            }
        }

        // 结束标签
        ilGenerator.MarkLabel(labelEnd);
        return null;
    }

    /// <summary>
    /// 访问 BlockStatement 节点
    /// </summary>
    public object? VisitBlockStatement(BlockStatement node)
    {
        // 迁移自 BlockStatement.GenerateIl()
        // 先生成导入语句的IL
        node.GenerateImportIl(ilGenerator, local);

        // 生成其他语句的IL
        for (int i = 0; i < node.Count; i++)
        {
            var statement = node[i];
            if (statement is null)
            {
                continue;
            }

            // FuncRunStatement 的 Accept 会把访问直接转发给内部表达式，绕过它自己 GenerateIl 里
            // 「丢弃非 void 结果」的那段逻辑，导致裸调用语句在栈上留下一个值、生成无效 IL
            // （例如 `d.ForEach(f)`、`l.Count()`）。这里直接走 GenerateIl。
            if (statement is FuncRunStatement funcRunStatement)
            {
                funcRunStatement.Accept(this);
                continue;
            }

            statement.Accept(this);
        }

        return null;
    }

    /// <summary>
    /// 访问 WhileStatement 节点（编译器）
    /// </summary>
    public object? VisitWhileStatement(WhileStatement node)
    {

        // 创建循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签，以便嵌套循环使用
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = loopStart; // while循环中continue直接跳转到循环开始

        // 循环开始标签
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件（object 静态类型统一转换成原生 bool）
        ConditionIlHelper.EmitBoolOperand(node.Condition, ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果条件为false，跳转到循环结束

        node.Block.Accept(this);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到循环开始

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;

        return null;
    
    }

    /// <summary>
    /// 访问 ForStatement 节点（编译器）
    /// </summary>
    public object? VisitForStatement(ForStatement node)
    {

        // 循环变量在解释器中属于循环作用域。若同名顶层变量已被提升为全局槽位，
        // 循环内必须用局部变量遮蔽它，否则循环结束后函数会读到循环变量的最终值。
        var shadow = node.BeginLoopVariableShadow(ilGenerator, local);
        // 初始化表达式与循环变量的局部槽位已在 BeginLoopVariableShadow 中处理。
        if (shadow is null)
        {
            node.Init.Accept(this);
        }

        // 创建循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签，以便嵌套循环使用
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 跳转到循环开始
        ilGenerator.MarkLabel(loopStart);

        // 检查循环条件（object 静态类型统一转换成原生 bool）
        ConditionIlHelper.EmitBoolOperand(node.Condition, ilGenerator, local);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd); // 如果条件为false，跳转到循环结束

        node.Block.Accept(this);

        // continue标签：执行循环迭代语句
        ilGenerator.MarkLabel(continueLabel);
        node.Operation.Accept(this);

        // 跳转回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart); // 跳转到循环开始

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;

        ForStatementRuntimeExtensions.EndLoopVariableShadow(local, shadow);

        return null;
    
    }

    /// <summary>
    /// 访问 ForInStatement 节点（编译器）
    /// </summary>
    public object? VisitForInStatement(ForInStatement node)
    {

        var ty = node.Expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);

        // 对于字典类型，使用特殊处理
        if (ty == typeof(Dictionary<object, object>))
        {
            node.GenerateDictionaryIl(ilGenerator, local);
            return null;
        }

        // 非字典类型，使用普通的IEnumerator处理
        var enumerator = ilGenerator.DeclareLocal(typeof(IEnumerator));
        var current = ilGenerator.DeclareLocal(typeof(object));

        // 获取枚举器
        var getEnumeratorMethod = typeof(IEnumerable).GetMethod("GetEnumerator")!;
        node.Expression.Accept(this);
        ilGenerator.Emit(OpCodes.Callvirt, getEnumeratorMethod);
        ilGenerator.Emit(OpCodes.Stloc, enumerator);

        // 定义循环标签
        var loopStart = ilGenerator.DefineLabel();
        var loopEnd = ilGenerator.DefineLabel();
        var continueLabel = ilGenerator.DefineLabel();

        // 保存当前的break和continue标签
        var oldBreakLabel = local.BreakLabel;
        var oldContinueLabel = local.ContinueLabel;

        // 设置当前循环的break和continue标签
        local.BreakLabel = loopEnd;
        local.ContinueLabel = continueLabel;

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 调用MoveNext
        var moveNextMethod = typeof(IEnumerator).GetMethod("MoveNext")!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, moveNextMethod);
        ilGenerator.Emit(OpCodes.Brfalse, loopEnd);

        // 获取当前元素
        var currentProperty = typeof(IEnumerator).GetProperty("Current")!;
        var getCurrentMethod = currentProperty.GetGetMethod()!;
        ilGenerator.Emit(OpCodes.Ldloc, enumerator);
        ilGenerator.Emit(OpCodes.Callvirt, getCurrentMethod);

        // 尝试确定元素类型并进行类型转换
        var exprType = node.Expression.Accept(new TypeInferenceVisitor(local));
        if (exprType is not null)
        {
            // 如果是数组类型
            if (exprType.IsArray && exprType.GetElementType() == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                ilGenerator.Emit(OpCodes.Box, typeof(int)); // 重新装箱以保持统一性
            }
            // 如果是泛型集合且元素类型是 int
            else if (exprType.IsGenericType && exprType.GetGenericArguments().Length > 0
                     && exprType.GetGenericArguments()[0] == typeof(int))
            {
                ilGenerator.Emit(OpCodes.Unbox_Any, typeof(int));
                ilGenerator.Emit(OpCodes.Box, typeof(int)); // 重新装箱以保持统一性
            }
        }

        ilGenerator.Emit(OpCodes.Stloc, current);

        // 处理标识符赋值
        if (node.Ids.Count == 1)
        {
            // 单个标识符，直接赋值
            // 在异步状态机中，需要定义变量并存储
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, node.Ids[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, node.Ids[0].IdName, node.Position);
            }
            else
            {
                local.AddLocalVar(node.Ids[0].IdName, current);
            }
        }
        else
        {
            // 多个标识符，只赋值给第一个
            if (local.AsyncStateMachineGenerator != null)
            {
                local.DefineVariable(ilGenerator, node.Ids[0].IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Ldloc, current);
                local.StoreVariable(ilGenerator, node.Ids[0].IdName, node.Position);
            }
            else
            {
                local.AddLocalVar(node.Ids[0].IdName, current);
            }
        }

        // 生成循环体
        node.Body.Accept(this);

        // 继续标签
        ilGenerator.MarkLabel(continueLabel);

        // 跳回循环开始
        ilGenerator.Emit(OpCodes.Br, loopStart);

        // 循环结束
        ilGenerator.MarkLabel(loopEnd);

        // 恢复之前的break和continue标签
        local.BreakLabel = oldBreakLabel;
        local.ContinueLabel = oldContinueLabel;
    
    
        return null;
    }

    /// <summary>
    /// 访问 SetStatement 节点（编译器）
    /// </summary>

    /// <summary>
    /// 赋值场景的类型兼容判断（与解释器/VM 的自动转换保持一致）。
    /// </summary>
    private static bool IsTypeCompatibleForAssignment(Type expected, Type actual)
    {
        if (expected == actual || expected.IsAssignableFrom(actual))
        {
            return true;
        }

        // 数值/字符之间按解释器语义允许赋值（运行时做转换/截断）
        static bool IsNumeric(Type type) =>
            type == typeof(int) || type == typeof(double) || type == typeof(char) ||
            type == typeof(long) || type == typeof(float);

        if (IsNumeric(expected) && IsNumeric(actual))
        {
            return true;
        }

        // 值类型写入 string 注解时有 ToString 转换
        if (expected == typeof(string) && actual.IsValueType)
        {
            return true;
        }

        return false;
    }

    public object? VisitSetStatement(SetStatement node)
    {

        var tuplePattern = node.LeftExpression switch
        {
            TupleLiteralExpression syntax => syntax.Elements,
            TupleLangValue legacy => (IReadOnlyList<LangExpression>)legacy.Elements,
            _ => null
        };
        if (tuplePattern is not null)
        {
            var tupleType = node.Value.Accept(new TypeInferenceVisitor(local));
            if (tupleType is null || !tupleType.IsValueType ||
                !typeof(System.Runtime.CompilerServices.ITuple).IsAssignableFrom(tupleType))
                throw new InvalidOperationError(node, "IL 元组解构赋值右侧必须是元组");

            node.Value.Accept(this);
            var tupleLocal = ilGenerator.DeclareLocal(tupleType);
            ilGenerator.Emit(OpCodes.Stloc, tupleLocal);

            for (var i = 0; i < tuplePattern.Count; i++)
            {
                if (tuplePattern[i] is not LangId id)
                    throw new InvalidOperationError(node, "元组解构赋值只支持变量名");

                var currentType = tupleType;
                var index = i;
                ilGenerator.Emit(OpCodes.Ldloca, tupleLocal);
                while (index >= 7)
                {
                    var rest = currentType.GetField("Rest") ??
                               throw new InvalidOperationError(node, $"元组解构赋值索引 {i} 超出范围");
                    ilGenerator.Emit(OpCodes.Ldflda, rest);
                    currentType = rest.FieldType;
                    index -= 7;
                }

                var field = currentType.GetField($"Item{index + 1}") ??
                            throw new InvalidOperationError(node, $"元组解构赋值索引 {i} 超出范围");
                ilGenerator.Emit(OpCodes.Ldfld, field);
                var target = local.GetOrCreateLocalVar(ilGenerator, id.IdName, field.FieldType);
                if (target.LocalType == typeof(object) && field.FieldType.IsValueType)
                    ilGenerator.Emit(OpCodes.Box, field.FieldType);
                else if (target.LocalType != field.FieldType && !target.LocalType.IsAssignableFrom(field.FieldType))
                    throw new InvalidOperationError(node, $"无法将 {field.FieldType.Name} 赋给 {id.IdName}");
                ilGenerator.Emit(OpCodes.Stloc, target);
            }
            return null;
        }

        if (Old8Lang.Runtime.LiteralCollectionRuntimeFactory.TryCreateNestedIndex(node.LeftExpression, out var nestedIndexAccess))
        {
            // 成员或方法结果后再索引的赋值（例如 node.data_dict[key]）会被解析成
            // NestedIndexAccess。静态元素类型在这里通常不可知，交给运行期分派，
            // 既可写字典也保留列表/数组的负索引行为。
            nestedIndexAccess.BaseExpression.Accept(this);
            var baseType = nestedIndexAccess.BaseExpression.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (baseType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, baseType);
            }

            nestedIndexAccess.NestedIndex.Accept(this);
            var keyType = nestedIndexAccess.NestedIndex.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (keyType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, keyType);
            }

            node.Value.Accept(this);
            var valueType = node.Value.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
            if (valueType.IsValueType)
            {
                ilGenerator.Emit(OpCodes.Box, valueType);
            }

            ilGenerator.Emit(OpCodes.Call,
                typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.SetObjectItem))!);
            ilGenerator.Emit(OpCodes.Pop);
            return null;
        }

        if (node.Id is not null && !string.IsNullOrEmpty(node.Id.IdName))
        {
            // 如果有类型注解,进行编译时类型检查
            if (!string.IsNullOrEmpty(node.Id.AssumptionType))
            {
                CollectionTypeValidator.ValidateCollectionTypeAnnotation(
                    node.Id.AssumptionType,
                    node.Value,
                    local,
                    node.Id.IdName,
                    node.Position
                );
            }

            // 带类型注解的赋值：静态已知的不兼容类型在运行期报 TYPE_ERROR
            // （与解释器/VM 一致），而不是把值强写进错误类型的局部变量/槽位。
            if (!string.IsNullOrEmpty(node.Id.AssumptionType))
            {
                var annotatedType = node.Id.Accept(new TypeInferenceVisitor(local));
                var assignedType = node.Value.Accept(new TypeInferenceVisitor(local));
                // 空列表/空字典字面量由后面的专门分支生成目标容器类型，不在此判断。
                var isEmptyCollectionLiteral =
                    node.Value is ListLiteralExpression { Elements.Count: 0 } ||
                    node.Value is ListLangValue { Value.Count: 0, Values.Count: 0 };

                if (!isEmptyCollectionLiteral &&
                    annotatedType is not null && assignedType is not null &&
                    annotatedType != typeof(object) && assignedType != typeof(object) &&
                    !IsTypeCompatibleForAssignment(annotatedType, assignedType))
                {
                    ilGenerator.Emit(OpCodes.Ldstr, IlTypeErrors.TypeName(annotatedType));
                    ilGenerator.Emit(OpCodes.Ldstr, IlTypeErrors.TypeName(assignedType));
                    ilGenerator.Emit(OpCodes.Ldstr, node.Id.IdName);
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(IlTypeErrors).GetMethod(nameof(IlTypeErrors.ThrowAssignmentMismatch))!);
                    ilGenerator.Emit(OpCodes.Pop);

                    // 运行期必然抛错；声明占位局部变量，保证后续读取仍能通过编译。
                    var fallbackLocal = ilGenerator.DeclareLocal(annotatedType);
                    local.AddLocalVar(node.Id.IdName, fallbackLocal);
                    local.LocalVarTypes[node.Id.IdName] = annotatedType;
                    return null;
                }
            }

            // 被函数体访问的顶层变量存放在运行期全局槽位中：
            // 顶层和函数体都通过槽位读写，避免复用 OldLangRun 的 LocalBuilder。
            if (node.TryEmitGlobalStore(ilGenerator, local))
            {
                return null;
            }

            // 异步模式：使用变量提升
            if (local.AsyncStateMachineGenerator != null)
            {
                node.Value.Accept(this);
                var valueType = node.Value.Accept(new TypeInferenceVisitor(local));
                // 确保 void 类型被视为 object (null)
                if (valueType == typeof(void)) valueType = typeof(object);

                if (valueType != null) local.DefineVariable(ilGenerator, node.Id.IdName, valueType);
                local.StoreVariable(ilGenerator, node.Id.IdName, node.Position);
                return null;
            }

            // 空花括号字面量在语法层默认是列表；显式 dict 注解应当消除该歧义，
            // 在 IL 中生成真正的空 Dictionary，才能继续调用 Keys/Values 等字典方法。
            if ((node.Value is ListLiteralExpression { Elements.Count: 0 } or ListLangValue { Value.Count: 0, Values.Count: 0 }) &&
                node.Id.AssumptionType is not null &&
                (node.Id.AssumptionType.Equals("dict", StringComparison.OrdinalIgnoreCase) ||
                 node.Id.AssumptionType.Equals("dictionary", StringComparison.OrdinalIgnoreCase)))
            {
                EmitSetValueToIl(new DictionaryLangValue(position: node.Value.Position), node.Id.IdName);
            }
            // 未加类型注解的空花括号可能在首次索引写入时成为列表或字典。以 object
            // 保存，避免先固定为 List<object> 后再以字符串键写入而生成无效 IL。
            else if ((node.Value is ListLiteralExpression { Elements.Count: 0 } or ListLangValue { Value.Count: 0, Values.Count: 0 }) &&
                     local.GetLocalVar(node.Id.IdName) is null)
            {
                node.Value.Accept(this);
                var target = local.GetOrCreateLocalVar(ilGenerator, node.Id.IdName, typeof(object));
                ilGenerator.Emit(OpCodes.Stloc, target);
            }
            else
            {
                // 普通变量赋值: name <- value
                EmitSetValueToIl(node.Value, node.Id.IdName);
            }
        }
        else if (node.LeftExpression is not null)
        {
            if (node.LeftExpression is Operation operation)
            {
                // 处理数组解构赋值: [a, b, c] <- array
                if (operation is { Left: LangId { IdName: "array_destruct" }, Opera: LangTokenType.LeftBracket })
                {
                    // 解析解构的标识符列表
                    var identifiersStr = operation.Right?.ToString() ??
                                         throw new InvalidOperationError(node, "数组解构赋值需要有效的标识符列表");
                    var identStrs = identifiersStr.Split(',');

                    // 生成右侧数组值的IL
                    node.Value.Accept(this);

                    // 保存右侧数组值到局部变量
                    var arrayLocal = ilGenerator.DeclareLocal(typeof(object));
                    ilGenerator.Emit(OpCodes.Stloc, arrayLocal);

                    // 遍历标识符列表，生成解构赋值IL
                    for (int i = 0; i < identStrs.Length; i++)
                    {
                        var identStr = identStrs[i].Trim();
                        if (identStr == "null") continue; // 跳过空元素

                        // 加载数组
                        ilGenerator.Emit(OpCodes.Ldloc, arrayLocal);

                        // 加载索引
                        ilGenerator.Emit(OpCodes.Ldc_I4, i);

                        // 获取数组元素
                        ilGenerator.Emit(OpCodes.Ldelem_Ref);

                        // 将元素赋值给对应的变量
                        var localVar = local.GetOrCreateLocalVar(ilGenerator, identStr, typeof(object));
                        ilGenerator.Emit(OpCodes.Stloc, localVar);
                    }

                    return null;
                }

                // 处理对象解构赋值: {name, age} <- person
                if (operation is { Left: LangId { IdName: "object_destruct" }, Opera: LangTokenType.LeftBrace })
                {
                    // 解析解构的属性列表
                    var propertiesStr = operation.Right?.ToString() ??
                                        throw new InvalidOperationError(node, "对象解构赋值需要有效的属性列表");
                    var propStrings = propertiesStr.Split(',');

                    // 生成右侧对象值的IL
                    node.Value.Accept(this);

                    // 保存右侧对象值到局部变量
                    var objLocal = ilGenerator.DeclareLocal(typeof(object));
                    ilGenerator.Emit(OpCodes.Stloc, objLocal);

                    // 遍历属性列表，生成解构赋值IL
                    foreach (var propStr in propStrings)
                    {
                        var propStrTrimmed = propStr.Trim();
                        string propName, aliasName;

                        if (propStrTrimmed.Contains(":"))
                        {
                            // 处理带别名的对象解构：name: newName
                            var parts = propStrTrimmed.Split(':');
                            propName = parts[0].Trim();
                            aliasName = parts[1].Trim();
                        }
                        else
                        {
                            // 只有属性名，没有别名
                            propName = propStrTrimmed;
                            aliasName = propStrTrimmed;
                        }

                        // 加载对象
                        ilGenerator.Emit(OpCodes.Ldloc, objLocal);

                        // 获取属性值（假设是AnyLangValue类型，有Result字典）
                        // 这里简化处理，直接调用反射获取属性值
                        var getPropertyMethod = typeof(AnyLangValue).GetMethod("GetPropertyValue",
                                                    BindingFlags.Public | BindingFlags.Instance) ??
                                                throw new InvalidOperationError(node,
                                                    "AnyLangValue类型缺少GetPropertyValue方法");

                        // 加载属性名
                        ilGenerator.Emit(OpCodes.Ldstr, propName);

                        // 调用GetPropertyValue方法
                        ilGenerator.Emit(OpCodes.Callvirt, getPropertyMethod);

                        // 将属性值赋值给对应的变量
                        local.DefineVariable(ilGenerator, aliasName, typeof(object));
                        local.StoreVariable(ilGenerator, aliasName, node.Position);
                    }

                    return null;
                }

                // 处理成员访问或索引访问赋值: left.right <- value 或 left[right] <- value
                if (operation is { Opera: LangTokenType.Dot, Right: LangId memberId })
                {
                    // 成员访问赋值: left.right <- value

                    // 类名仅用于解析静态成员，并不是可加载的运行期变量。
                    if (operation.Left is LangId classId &&
                        local.ClassVar.TryGetValue(classId.IdName, out var classType))
                    {
                        // TypeBuilder 在 CreateType 前不支持反射查询字段；当前类的
                        // FieldVar 已持有相应的 FieldBuilder。
                        FieldInfo? staticField = classType is TypeBuilder && classType == local.InClassEnv
                            ? local.FieldVar.GetValueOrDefault(memberId.IdName)
                            : classType.GetField(memberId.IdName,
                                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                        if (staticField is not null && !staticField.IsStatic)
                        {
                            staticField = null;
                        }
                        if (staticField is not null)
                        {
                            node.Value.Accept(this);
                            var valueType = node.Value.Accept(new TypeInferenceVisitor(local));
                            if (staticField.FieldType == typeof(object) && valueType?.IsValueType == true)
                            {
                                ilGenerator.Emit(OpCodes.Box, valueType);
                            }
                            else if (staticField.FieldType.IsValueType && valueType == typeof(object))
                            {
                                ilGenerator.Emit(OpCodes.Unbox_Any, staticField.FieldType);
                            }

                            ilGenerator.Emit(OpCodes.Stsfld, staticField);
                            return null;
                        }

                        var staticProperty = classType.GetProperty(memberId.IdName,
                            BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                        var staticSetter = staticProperty?.GetSetMethod();
                        if (staticSetter is not null && staticSetter.IsStatic)
                        {
                            node.Value.Accept(this);
                            ilGenerator.Emit(OpCodes.Call, staticSetter);
                            return null;
                        }
                    }

                    // 泛型类型引用（如 Counter<T>.count）不是 LangId；其 OutputType
                    // 已是具体特化类型，仍应直接写入静态字段，不能先实例化接收者。
                    if (operation.Left is GenericInstanceExpression { IsFunctionCall: false })
                    {
                        var specializedType = operation.Left.Accept(new TypeInferenceVisitor(local));
                        FieldInfo? staticField = local.FieldVar.GetValueOrDefault(memberId.IdName);
                        if (staticField?.IsStatic != true)
                        {
                            staticField = specializedType is TypeBuilder && specializedType == local.InClassEnv
                                ? local.FieldVar.GetValueOrDefault(memberId.IdName)
                                : specializedType?.GetField(memberId.IdName,
                                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                        }
                        if (staticField is not null && !staticField.IsStatic)
                        {
                            staticField = null;
                        }
                        if (staticField is not null)
                        {
                            node.Value.Accept(this);
                            var valueType = node.Value.Accept(new TypeInferenceVisitor(local));
                            if (staticField.FieldType == typeof(object) && valueType?.IsValueType == true)
                            {
                                ilGenerator.Emit(OpCodes.Box, valueType);
                            }
                            else if (staticField.FieldType.IsValueType && valueType == typeof(object))
                            {
                                ilGenerator.Emit(OpCodes.Unbox_Any, staticField.FieldType);
                            }

                            ilGenerator.Emit(OpCodes.Stsfld, staticField);
                            return null;
                        }
                    }

                    // 检查是否是this访问（如this.name <- value）
                    if ((operation.Left is LangId { IdName: "this" } || operation.Left is ThisExpression) &&
                        local.InClassEnv is not null)
                    {
                        // 加载 node；捕获 node 的 lambda 会从闭包环境读取实例。
                        new ThisExpression().Accept(new CompilerVisitor(ilGenerator, local));

                        // 从 FieldVar 中获取字段信息
                        if (local.FieldVar.TryGetValue(memberId.IdName, out var fieldInfo))
                        {
                            // 找到了字段
                        }
                        // 如果 FieldVar 中没有，尝试从当前类型或父类中获取
                        else if (local.InClassEnv is TypeBuilder typeBuilder)
                        {
                            // 对于 TypeBuilder，尝试从基类中查找字段
                            var baseType = typeBuilder.BaseType;
                            while (baseType is not null && baseType != typeof(object))
                            {
                                fieldInfo = baseType.GetField(memberId.IdName,
                                    BindingFlags.Public | BindingFlags.Instance);
                                if (fieldInfo is not null) break;
                                baseType = baseType.BaseType;
                            }
                        }
                        else
                        {
                            // 对于已创建的类型，直接获取字段
                            fieldInfo = local.InClassEnv.GetField(memberId.IdName,
                                BindingFlags.Public | BindingFlags.Instance);
                        }

                        if (fieldInfo is not null)
                        {
                            if (fieldInfo is FieldBuilder && local.TryGetCapturedLocal("this", out _))
                            {
                                ilGenerator.Emit(OpCodes.Ldstr, memberId.IdName);
                                node.Value.Accept(this);
                                var dynamicValueType = node.Value.Accept(new TypeInferenceVisitor(local));
                                if (dynamicValueType?.IsValueType == true)
                                {
                                    ilGenerator.Emit(OpCodes.Box, dynamicValueType);
                                }

                                ilGenerator.Emit(OpCodes.Call,
                                    typeof(DotOperatorILHelper).GetMethod(nameof(DotOperatorILHelper.SetDynamicObjectProperty))!);
                                return null;
                            }

                            // 加载右值
                            node.Value.Accept(this);

                            // 值类型与字段类型不匹配时按字段类型转换（含语言值解包）
                            EmitFieldValueConversion(
                                fieldInfo.FieldType,
                                node.Value.Accept(new TypeInferenceVisitor(local)));

                            ilGenerator.Emit(OpCodes.Stfld, fieldInfo);
                        }

                        return null;
                    }

                    // 非this访问，正常处理
                    // 加载左对象
                    operation.Left!.Accept(new CompilerVisitor(ilGenerator, local));
                    // 加载右值
                    node.Value.Accept(this);
                    // 获取字段或属性
                    var leftType = operation.Left.Accept(new TypeInferenceVisitor(local))!;

                    // 检查leftType是否是TypeBuilder或typeof(object)（表示this访问）
                    if (leftType is not TypeBuilder && leftType != typeof(object))
                    {
                        var field = leftType.GetField(memberId.IdName);
                        if (field is not null)
                        {
                            // 非 this 形式的字段写入同样要按字段类型转换：
                            // 未标注类型的字段是 object，写入 int 时若不装箱，
                            // 字段里存的就是裸 int32，读取时会被当作对象引用，
                            // 运行期直接段错误（而不是抛托管异常）。
                            EmitFieldValueConversion(
                                field.FieldType,
                                node.Value.Accept(new TypeInferenceVisitor(local)));

                            ilGenerator.Emit(OpCodes.Stfld, field);
                        }
                        else
                        {
                            var property = leftType.GetProperty(memberId.IdName);
                            if (property is not null && property.GetSetMethod() is not null)
                            {
                                EmitFieldValueConversion(
                                    property.PropertyType,
                                    node.Value.Accept(new TypeInferenceVisitor(local)));
                                ilGenerator.Emit(OpCodes.Callvirt, property.GetSetMethod()!);
                            }
                        }
                    }
                    // 如果是TypeBuilder或typeof(object)，跳过此操作
                }
                else if (operation.Opera == LangTokenType.Dot && Old8Lang.Runtime.LiteralCollectionRuntimeFactory.TryCreateIndex(operation.Right, out var nestedListItem))
                {
                    // 解析器将 obj.field[key] 表示为 obj . LangListItem(field, key)，
                    // 而不是以 obj.field 作为索引表达式。先在运行期读取该字段，再用
                    // 动态集合写入器完成索引赋值；这也覆盖初始类型为 object 的空字段。
                    operation.Left!.Accept(new CompilerVisitor(ilGenerator, local));
                    var receiverType = operation.Left.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (receiverType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, receiverType);
                    }

                    ilGenerator.Emit(OpCodes.Ldstr, nestedListItem.ListId.IdName);
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(DotOperatorILHelper).GetMethod(nameof(DotOperatorILHelper.GetDynamicObjectProperty))!);

                    nestedListItem.Key.Accept(this);
                    var keyType = nestedListItem.Key.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (keyType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, keyType);
                    }

                    node.Value.Accept(this);
                    var valueType = node.Value.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (valueType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, valueType);
                    }

                    ilGenerator.Emit(OpCodes.Call,
                        typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.SetObjectItem))!);
                    ilGenerator.Emit(OpCodes.Pop);
                    return null;
                }
                else if (operation.Opera == LangTokenType.Dot)
                {
                    // 索引访问赋值: left[right] <- value
                    // 加载左对象
                    operation.Left!.Accept(new CompilerVisitor(ilGenerator, local));
                    // 获取左对象类型
                    var leftType = operation.Left.Accept(new TypeInferenceVisitor(local))!;
                    // 加载索引
                    operation.Right!.Accept(new CompilerVisitor(ilGenerator, local));

                    if (leftType == typeof(object))
                    {
                        var rightType = operation.Right.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                        if (rightType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, rightType);
                        }

                        node.Value.Accept(this);
                        var dynamicValueType = node.Value.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                        if (dynamicValueType.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, dynamicValueType);
                        }

                        ilGenerator.Emit(OpCodes.Call,
                            typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.SetObjectItem))!);
                        ilGenerator.Emit(OpCodes.Pop);
                        return null;
                    }

                    // 非动态集合保留其原生索引和值类型，直接由下方分支发射。
                    node.Value.Accept(this);

                    if (leftType.IsArray)
                    {
                        // 数组索引赋值
                        var elementType = leftType.GetElementType()!;
                        // 获取值的类型
                        var valueType = node.Value.Accept(new TypeInferenceVisitor(local))!;
                        // 如果值类型与元素类型不匹配，添加类型转换或装箱指令
                        if (valueType != elementType)
                        {
                            if (elementType.IsValueType && !valueType.IsValueType)
                            {
                                // 值类型数组，引用类型值需要拆箱
                                ilGenerator.Emit(OpCodes.Unbox_Any, elementType);
                            }
                            else if (!elementType.IsValueType && valueType.IsValueType)
                            {
                                // 引用类型数组，值类型值需要装箱
                                ilGenerator.Emit(OpCodes.Box, valueType);
                            }
                            // 对于其他类型不匹配情况，IL会在运行时处理
                        }

                        // 根据元素类型选择适当的 Stelem 指令
                        if (elementType.IsValueType)
                        {
                            // 对于值类型数组，使用 Stelem 指令
                            ilGenerator.Emit(OpCodes.Stelem, elementType);
                        }
                        else
                        {
                            // 对于引用类型数组，使用 Stelem_Ref 指令
                            ilGenerator.Emit(OpCodes.Stelem_Ref);
                        }
                    }
                    else if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // List<T>索引赋值，调用索引器的setter方法
                        var genericArguments = leftType.GetGenericArguments();
                        var itemType = genericArguments[0];
                        // 获取值的类型
                        var valueType = node.Value.Accept(new TypeInferenceVisitor(local))!;
                        // 如果值类型与列表元素类型不匹配，添加类型转换或装箱指令
                        if (valueType != itemType)
                        {
                            if (itemType.IsValueType && !valueType.IsValueType)
                            {
                                // 值类型列表，引用类型值需要拆箱
                                ilGenerator.Emit(OpCodes.Unbox_Any, itemType);
                            }
                            else if (!itemType.IsValueType && valueType.IsValueType)
                            {
                                // 引用类型列表，值类型值需要装箱
                                ilGenerator.Emit(OpCodes.Box, valueType);
                            }
                            // 对于其他类型不匹配情况，IL会在运行时处理
                        }

                        var indexer = leftType.GetProperty("Item")!;
                        ilGenerator.Emit(OpCodes.Callvirt, indexer.GetSetMethod()!);
                    }
                    else if (leftType.IsGenericType && leftType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    {
                        // Dictionary<TKey, TValue>索引赋值，调用索引器的setter方法
                        var indexer = leftType.GetProperty("Item")!;
                        ilGenerator.Emit(OpCodes.Callvirt, indexer.GetSetMethod()!);
                    }
                }
            }
            else if (Old8Lang.Runtime.LiteralCollectionRuntimeFactory.TryCreateIndex(node.LeftExpression, out var listItem))
            {
                // 处理LangListItem索引赋值: listId[key] <- value
                // 获取集合类型
                var listType = listItem.ListId.Accept(new TypeInferenceVisitor(local));

                // 加载集合对象
                listItem.ListId.Accept(this);

                if (listType == typeof(object))
                {
                    listItem.Key.Accept(this);
                    var keyType = listItem.Key.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (keyType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, keyType);
                    }

                    node.Value.Accept(this);
                    var valueType = node.Value.Accept(new TypeInferenceVisitor(local)) ?? typeof(object);
                    if (valueType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, valueType);
                    }

                    ilGenerator.Emit(OpCodes.Call,
                        typeof(IlIndexHelper).GetMethod(nameof(IlIndexHelper.SetObjectItem))!);

                    if (local.GetLocalVar(listItem.ListId.IdName) is { } target)
                    {
                        ilGenerator.Emit(OpCodes.Stloc, target);
                    }
                    else if (local.Globals is { } globals && globals.IsSlot(listItem.ListId.IdName))
                    {
                        RuntimeGlobals.EmitStore(ilGenerator, listItem.ListId.IdName,
                            globals.GetType(listItem.ListId.IdName), typeof(object));
                    }
                    else
                    {
                        throw new InvalidOperationError(node,
                            $"无法写回动态集合 '{listItem.ListId.IdName}'");
                    }

                    return null;
                }

                // DictionaryLangValue 是解释器的语言值：键与值都要包装成语言值后调用 Set，
                // 否则原生 string/int 会被当作 LangValueType 传入，生成无效 IL。
                if (listType == typeof(DictionaryLangValue))
                {
                    IlValueBridge.EmitLoadWrapped(listItem.Key, ilGenerator, local);
                    IlValueBridge.EmitLoadWrapped(node.Value, ilGenerator, local);
                    ilGenerator.Emit(OpCodes.Callvirt,
                        typeof(DictionaryLangValue).GetMethod(nameof(DictionaryLangValue.Set))!);
                    return null;
                }

                // 对于字典，需要在加载键和值时进行类型转换
                if (listType!.IsGenericType && listType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var genericArgs = listType.GetGenericArguments();
                    var dictKeyType = genericArgs[0];
                    var dictValueType = genericArgs[1];

                    // 加载键并进行类型转换
                    listItem.Key.Accept(this);
                    var keyType = listItem.Key.Accept(new TypeInferenceVisitor(local));
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

                    // 加载值并进行类型转换
                    node.Value.Accept(this);
                    var valueType = node.Value.Accept(new TypeInferenceVisitor(local));
                    if (valueType != dictValueType)
                    {
                        if (dictValueType.IsValueType && !valueType!.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Unbox_Any, dictValueType);
                        }
                        else if (!dictValueType.IsValueType && valueType!.IsValueType)
                        {
                            ilGenerator.Emit(OpCodes.Box, valueType);
                        }
                    }

                    // 调用字典的set_Item方法
                    var indexer = listType.GetProperty("Item")!;
                    ilGenerator.Emit(OpCodes.Callvirt, indexer.GetSetMethod()!);
                    return null;
                }

                // 对于非字典类型，按原来的方式加载
                listItem.Key.Accept(this);
                node.Value.Accept(this);

                if (listType == typeof(string))
                {
                    // 字符串是不可变的，不支持赋值
                    throw new InvalidOperationError(node, "字符串是不可变的，不支持索引赋值");
                }
                else if (listType.IsArray)
                {
                    // 数组索引赋值
                    var elementType = listType.GetElementType()!;
                    // 获取值的类型
                    var valueType = node.Value.Accept(new TypeInferenceVisitor(local))!;
                    // 如果值类型与元素类型不匹配，添加类型转换或装箱指令
                    if (valueType != elementType)
                    {
                        if (elementType.IsValueType && !valueType.IsValueType)
                        {
                            // 值类型数组，引用类型值需要拆箱
                            ilGenerator.Emit(OpCodes.Unbox_Any, elementType);
                        }
                        else if (!elementType.IsValueType && valueType.IsValueType)
                        {
                            // 引用类型数组，值类型值需要装箱
                            ilGenerator.Emit(OpCodes.Box, valueType);
                        }
                        // 对于其他类型不匹配情况，IL会在运行时处理
                    }

                    // 根据元素类型选择适当的 Stelem 指令
                    if (elementType.IsValueType)
                    {
                        // 对于值类型数组，使用 Stelem 指令
                        ilGenerator.Emit(OpCodes.Stelem, elementType);
                    }
                    else
                    {
                        // 对于引用类型数组，使用 Stelem_Ref 指令
                        ilGenerator.Emit(OpCodes.Stelem_Ref);
                    }
                }
                else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // List<T>索引赋值
                    var genericArguments = listType.GetGenericArguments();
                    var itemType = genericArguments[0];
                    // 获取值的类型
                    var valueType = node.Value.Accept(new TypeInferenceVisitor(local))!;
                    // 如果值类型与列表元素类型不匹配，添加类型转换或装箱指令
                    if (valueType != itemType)
                    {
                        if (itemType.IsValueType && !valueType.IsValueType)
                        {
                            // 值类型列表，引用类型值需要拆箱
                            ilGenerator.Emit(OpCodes.Unbox_Any, itemType);
                        }
                        else if (!itemType.IsValueType && valueType.IsValueType)
                        {
                            // 引用类型列表，值类型值需要装箱
                            ilGenerator.Emit(OpCodes.Box, valueType);
                        }
                        // 对于其他类型不匹配情况，IL会在运行时处理
                    }

                    var indexer = listType.GetProperty("Item")!;
                    ilGenerator.Emit(OpCodes.Callvirt, indexer.GetSetMethod()!);
                }
            }
        }
    
    
        return null;
    }

    /// <summary>
    /// 按目标字段/属性类型转换栈顶值（非 this 形式的成员写入）。
    /// </summary>
    /// <remarks>
    /// 未标注类型的字段在 IL 里是 <c>object</c>；写入值类型时必须装箱，
    /// 否则字段保存的是裸值类型，后续读取会把它当作对象引用，直接使进程崩溃。
    /// </remarks>
    private void EmitFieldValueConversion(Type targetType, Type? valueType)
    {
        if (valueType is null || valueType == targetType) return;

        if (targetType == typeof(object) && valueType.IsValueType)
        {
            ilGenerator.Emit(OpCodes.Box, valueType);
            return;
        }

        if (targetType.IsValueType && valueType == typeof(object))
        {
            ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
            return;
        }

        if (targetType.IsValueType && typeof(LangValueType).IsAssignableFrom(valueType))
        {
            // 语言值（IntLangValue/StringLangValue…）写入原生类型字段：先解包再 unbox。
            ilGenerator.Emit(OpCodes.Call,
                typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.UnwrapDynamic))!);
            ilGenerator.Emit(OpCodes.Unbox_Any, targetType);
            return;
        }

        if (SetStatementRuntimeExtensions.IsNumericType(targetType) &&
            SetStatementRuntimeExtensions.IsNumericType(valueType))
        {
            SetStatementRuntimeExtensions.EmitNumericConversion(ilGenerator, targetType);
        }
    }

    /// <summary>
    /// 访问 ReturnStatement 节点（编译器）
    /// </summary>
    public object? VisitReturnStatement(ReturnStatement node)
    {

        // 检查是否在finally块中使用了return语句，这在.NET IL中是不允许的
        if (local.IsInFinallyBlock)
        {
            throw new Error.CompilerException("在finally块中不允许使用return语句", node.Position);
        }

        // 获取返回值表达式的类型
        var returnType = node.Expression.Accept(new TypeInferenceVisitor(local));

        var runtimeTypeErrorEmitted = false;

        // 验证返回类型是否与函数声明的返回类型匹配
        if (local.ReturnValueLocal is not null)
        {
            var expectedReturnType = local.ReturnValueLocal.LocalType;

            // 检查返回类型是否兼容
            if (returnType is not null && expectedReturnType != typeof(object))
            {
                // 检查类型是否兼容：完全匹配、可赋值、或基本类型转换
                bool isCompatible = expectedReturnType == returnType ||
                                   expectedReturnType.IsAssignableFrom(returnType) ||
                                   returnType == typeof(object) ||
                                   // 枚举成员在 IL 里是底层 int32
                                   (expectedReturnType.IsEnum && returnType == typeof(int)) ||
                                   ReturnStatementRuntimeExtensions.IsBasicTypeConversionAllowed(expectedReturnType, returnType);

                // 如果返回类型不匹配且不能自动转换：不要生成无效 IL，也不做编译期拒绝——
                // 解释器/虚拟机是在运行期报 TYPE_ERROR。这里发射运行期 TypeError，
                // 并压入一个默认值占位（helper 不会返回）。
                if (!isCompatible)
                {
                    ilGenerator.Emit(OpCodes.Ldstr, IlTypeErrors.TypeName(expectedReturnType));
                    ilGenerator.Emit(OpCodes.Ldstr, IlTypeErrors.TypeName(returnType));
                    ilGenerator.Emit(OpCodes.Call,
                        typeof(IlTypeErrors).GetMethod(nameof(IlTypeErrors.ThrowReturnMismatch))!);

                    if (expectedReturnType.IsValueType)
                    {
                        var defaultLocal = ilGenerator.DeclareLocal(expectedReturnType);
                        ilGenerator.Emit(OpCodes.Ldloca, defaultLocal);
                        ilGenerator.Emit(OpCodes.Initobj, expectedReturnType);
                        ilGenerator.Emit(OpCodes.Ldloc, defaultLocal);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Ldnull);
                    }

                    runtimeTypeErrorEmitted = true;
                }
            }
        }

        // 加载返回表达式的值到栈上（对于void类型，LoadIlValue不会加载任何值）。
        // 已经压入运行期 TypeError 的默认值占位时跳过求值。
        if (runtimeTypeErrorEmitted)
        {
            // 占位值已在栈上
        }
        else
        {
            node.Expression.Accept(this);
        }

        // 检查是否在异步状态机中且没有被 finally 块捕获
        if (local is { AsyncStateMachineGenerator: not null, ReturnLabel: null })
        {
            var type = node.Expression.Accept(new TypeInferenceVisitor(local)) ?? typeof(void);
            local.AsyncStateMachineGenerator.EmitReturn(ilGenerator, type);
            return null;
        }

        // 如果函数使用了try-finally结构（ReturnLabel不为null），必须使用Leave指令
        if (local.ReturnLabel is not null)
        {
            // 如果有返回值局部变量（非void函数），存储返回值
            if (local.ReturnValueLocal is not null)
            {
                // 获取返回值局部变量的类型
                var returnValueType = local.ReturnValueLocal.LocalType;

                // 如果返回表达式的类型与返回值局部变量的类型不匹配，进行类型转换
                if (returnType != returnValueType)
                {
                    if (returnValueType.IsValueType && returnType == typeof(object))
                    {
                        // object 到值类型，需要拆箱
                        ilGenerator.Emit(OpCodes.Unbox_Any, returnValueType);
                    }
                    else if (!returnValueType.IsValueType && returnType!.IsValueType)
                    {
                        // 值类型到引用类型，需要装箱
                        ilGenerator.Emit(OpCodes.Box, returnType);
                    }
                }

                // 存储返回值到局部变量
                ilGenerator.Emit(OpCodes.Stloc, local.ReturnValueLocal);
            }
            else if (returnType != null && returnType != typeof(void))
            {
                // 如果没有返回值局部变量（如顶层代码或void函数），但表达式产生了值，需要将其弹出
                // 否则 Leave 指令会因为堆栈非空而导致无效 IL
                ilGenerator.Emit(OpCodes.Pop);
            }
            // 使用Leave指令退出try块，跳转到函数结束标签
            ilGenerator.Emit(OpCodes.Leave, local.ReturnLabel.Value);
        }
        else
        {
            // 没有使用defer，直接返回
            // 只有当返回值类型是 Task<object> 时，才进行异步包装
            // 这表明当前函数是异步函数
            if (local.ReturnValueLocal is not null)
            {
                var expectedReturnType = local.ReturnValueLocal.LocalType;
                if (returnType != null && returnType != expectedReturnType)
                {
                    if (expectedReturnType.IsValueType && returnType == typeof(object))
                    {
                        ilGenerator.Emit(OpCodes.Unbox_Any, expectedReturnType);
                    }
                    else if (!expectedReturnType.IsValueType && returnType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, returnType);
                    }
                    else if (ReturnStatementRuntimeExtensions.IsNumericType(expectedReturnType) && ReturnStatementRuntimeExtensions.IsNumericType(returnType))
                    {
                        ReturnStatementRuntimeExtensions.EmitNumericConversion(ilGenerator, expectedReturnType);
                    }
                    else if (!expectedReturnType.IsValueType && !returnType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Castclass, expectedReturnType);
                    }
                }
            }

            if (returnType == typeof(Task<object>))
            {
                // 返回值已经是 Task<object>，直接返回
                ilGenerator.Emit(OpCodes.Ret);
            }
            else
            {
                // 对于同步函数，直接返回值，不进行任何包装
                ilGenerator.Emit(OpCodes.Ret);
            }
        }
    
    
        return null;
    }

    /// <summary>
    /// 访问 ThrowStatement 节点（编译器）
    /// </summary>
    public object? VisitThrowStatement(ThrowStatement node)
    {

        // IL 模式下的实现
        node.Expression.Accept(this);

        // 获取表达式的类型
        var exprType = node.Expression.Accept(new TypeInferenceVisitor(local));

        // 如果表达式是ExceptionWrapper类型,提取内部的Exception并重新抛出
        if (exprType == typeof(ExceptionWrapper))
        {
            // 调用ExceptionWrapper.Exception属性获取内部异常
            var exceptionProperty = typeof(ExceptionWrapper).GetProperty("Exception")!;
            var getMethod = exceptionProperty.GetGetMethod()!;
            ilGenerator.Emit(OpCodes.Callvirt, getMethod);
            ilGenerator.Emit(OpCodes.Throw);
            return null;
        }

        // 如果是Exception类型,直接抛出
        if (exprType == typeof(Exception) || (exprType != null && exprType.IsSubclassOf(typeof(Exception))))
        {
            ilGenerator.Emit(OpCodes.Throw);
            return null;
        }

        // 值类型先装箱，再包装成语言值挂到异常上：catch 处可以解包回原始值
        if (exprType is { IsValueType: true })
        {
            ilGenerator.Emit(OpCodes.Box, exprType);
        }

        ilGenerator.Emit(OpCodes.Call,
            typeof(IlValueBridge).GetMethod(nameof(IlValueBridge.Wrap))!);
        ilGenerator.Emit(OpCodes.Newobj, typeof(LangThrownException).GetConstructor([typeof(LangValueType)])!);
        ilGenerator.Emit(OpCodes.Throw);
    
    
        return null;
    }

    /// <summary>
    /// 访问 DeferStatement 节点（编译器）
    /// </summary>
    public object? VisitDeferStatement(DeferStatement node)
    {

        // IL 模式下的defer实现
        // 将defer语句添加到LocalManager的defer栈中
        local.RegisterDefer(node.Statement);

        return null;
    
    }

    /// <summary>
    /// 访问 SelectStatement 节点（编译器）
    /// </summary>
    public object? VisitSelectStatement(SelectStatement node)
    {

        // 实现 select 语句的轮询策略：
        // 1. 无限循环检查所有 case
        // 2. 如果任意 case 就绪，执行对应块并退出
        // 3. 如果有 default 且所有 case 未就绪，执行 default 并退出
        // 4. 否则短暂休眠后继续循环

        var loopStart = ilGenerator.DefineLabel(); // 轮询循环开始
        var loopEnd = ilGenerator.DefineLabel(); // 轮询循环结束
        var defaultLabel = ilGenerator.DefineLabel(); // default 分支
        var sleepLabel = ilGenerator.DefineLabel(); // 休眠逻辑

        // 循环开始
        ilGenerator.MarkLabel(loopStart);

        // 遍历所有 case，尝试非阻塞操作
        foreach (var selectCase in node.Cases)
        {
            var caseBlockEnd = ilGenerator.DefineLabel();

            if (selectCase.IsReceive)
            {
                // 接收 case: 尝试非阻塞接收
                // var receiveResult = ResourceManager.TryReceiveChannel(channelId, 0);
                // if (receiveResult.Success) { 执行块; return; }

                // 加载 channelId
                selectCase.ChannelExpression.Accept(this);

                // 加载超时时间 0（非阻塞）
                ilGenerator.Emit(OpCodes.Ldc_I4_0);

                // 调用 ResourceManager.TryReceiveChannel(int, int)
                var tryReceiveMethod = typeof(ResourceManager).GetMethod(
                    nameof(ResourceManager.TryReceiveChannel),
                    [typeof(int), typeof(int)]);
                if (tryReceiveMethod != null) ilGenerator.Emit(OpCodes.Call, tryReceiveMethod);

                // 结果存储到临时局部变量
                var receiveResultLocal = ilGenerator.DeclareLocal(typeof(ChannelReceiveResult));
                ilGenerator.Emit(OpCodes.Stloc, receiveResultLocal);

                // 检查 receiveResult.Success
                ilGenerator.Emit(OpCodes.Ldloca, receiveResultLocal);
                var successProperty = typeof(ChannelReceiveResult).GetProperty(nameof(ChannelReceiveResult.Success));
                if (successProperty?.GetMethod != null)
                {
                    ilGenerator.Emit(OpCodes.Call, successProperty.GetMethod);

                    // 如果 Success == false，跳过此 case
                    ilGenerator.Emit(OpCodes.Brfalse, caseBlockEnd);

                    // Success == true: 设置变量（如果有）并执行块
                    if (selectCase.VariableName is not null)
                    {
                        // 获取 receiveResult.Value
                        ilGenerator.Emit(OpCodes.Ldloca, receiveResultLocal);
                        var valueProperty =
                            typeof(ChannelReceiveResult).GetProperty(nameof(ChannelReceiveResult.Value));
                        if (valueProperty?.GetMethod != null)
                            ilGenerator.Emit(OpCodes.Call, valueProperty.GetMethod);

                        // 将值转换为 LangValueType
                        var objToValueMethod = typeof(LangValueType).GetMethod(
                            nameof(LangValueType.ObjToValue),
                            [typeof(object)]);
                        if (objToValueMethod != null) ilGenerator.Emit(OpCodes.Call, objToValueMethod);

                        // 存储到变量（获取或创建局部变量）
                        var varLocal = local.GetOrCreateLocalVar(ilGenerator, selectCase.VariableName,
                            typeof(LangValueType));
                        ilGenerator.Emit(OpCodes.Stloc, varLocal);
                    }
                }

                // 执行 case 块
                selectCase.BlockStatement.Accept(this);

                // 跳转到循环结束（退出 select）
                ilGenerator.Emit(OpCodes.Br, loopEnd);
            }
            else
            {
                // 发送 case: 尝试非阻塞发送
                // if (ResourceManager.TrySendChannel(channelId, value, 0)) { 执行块; return; }

                // 加载 channelId
                selectCase.ChannelExpression.Accept(this);

                // 加载要发送的值
                selectCase.SendValueExpression!.Accept(new CompilerVisitor(ilGenerator, local));
                var sendValueType = selectCase.SendValueExpression.Accept(new TypeInferenceVisitor(local));
                if (sendValueType is { IsValueType: true })
                {
                    ilGenerator.Emit(OpCodes.Box, sendValueType);
                }

                // 加载超时时间 0（非阻塞）
                ilGenerator.Emit(OpCodes.Ldc_I4_0);

                // 调用 ResourceManager.TrySendChannel(int, object, int)
                var trySendMethod = typeof(ResourceManager).GetMethod(
                    nameof(ResourceManager.TrySendChannel),
                    [typeof(int), typeof(object), typeof(int)]);
                if (trySendMethod != null) ilGenerator.Emit(OpCodes.Call, trySendMethod);

                // 如果返回 false，跳过此 case
                ilGenerator.Emit(OpCodes.Brfalse, caseBlockEnd);

                // 返回 true: 执行 case 块
                selectCase.BlockStatement.Accept(this);

                // 跳转到循环结束（退出 select）
                ilGenerator.Emit(OpCodes.Br, loopEnd);
            }

            // case 块结束标签
            ilGenerator.MarkLabel(caseBlockEnd);
        }

        // 所有 case 都未就绪
        if (node.DefaultCase is not null)
        {
            // 有 default 分支：执行 default 并退出
            ilGenerator.MarkLabel(defaultLabel);
            node.DefaultCase.Accept(this);
            ilGenerator.Emit(OpCodes.Br, loopEnd);
        }
        else
        {
            // 无 default 分支：休眠 1ms 后继续轮询
            ilGenerator.MarkLabel(sleepLabel);

            // Thread.Sleep(1)
            ilGenerator.Emit(OpCodes.Ldc_I4_1);
            var sleepMethod = typeof(Thread).GetMethod(
                nameof(Thread.Sleep),
                [typeof(int)]);
            if (sleepMethod != null) ilGenerator.Emit(OpCodes.Call, sleepMethod);

            // 继续循环
            ilGenerator.Emit(OpCodes.Br, loopStart);
        }

        // 循环结束标签
        ilGenerator.MarkLabel(loopEnd);
    
    
        return null;
    }

    /// <summary>
    /// 访问 EnumInit 节点（编译器）
    /// </summary>
    public object? VisitEnumInit(EnumInit node)
    {

        // 检查枚举是否已经存在
        if (local.ClassVar.ContainsKey(node.EnumName))
        {
            return null;
        }

        // 确保有动态程序集和模块
        if (local.DynamicAssembly is null || local.DynamicModule is null)
        {
            var assemblyName = new AssemblyName("Old8LangDynamicAssembly");
            local.DynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            local.DynamicModule = local.DynamicAssembly.DefineDynamicModule("Old8LangDynamicModule");
        }

        var moduleBuilder = local.DynamicModule;

        // 定义枚举类型
        var enumBuilder = moduleBuilder.DefineEnum(
            node.EnumName,
            TypeAttributes.Public,
            typeof(int)); // 枚举基础类型为 int

        // 计算枚举成员的实际值
        int currentValue = 0;
        var enumValues = new Dictionary<string, int>();

        foreach (var (memberName, memberValueExpr) in node.Members)
        {
            if (memberValueExpr is not null)
            {
                // 有显式赋值，计算表达式的值
                // 注意：IL 模式下，表达式必须是常量
                if (memberValueExpr is ILiteralSyntax { Kind: LiteralKind.Int, LiteralValue: int intValue })
                {
                    currentValue = intValue;
                }
                else
                {
                    throw new SyntaxError(node.Position, $"枚举成员 '{memberName}' 的值必须是整数常量");
                }
            }

            // 定义枚举成员
            enumBuilder.DefineLiteral(memberName, currentValue);
            enumValues[memberName] = currentValue;
            currentValue++; // 下一个未赋值的成员值自动递增
        }

        // 创建枚举类型
        var createdEnumType = enumBuilder.CreateType();

        // 将枚举类型添加到 LocalManager
        local.ClassVar[node.EnumName] = createdEnumType;
    
    
        return null;
    }

    /// <summary>
    /// 访问 ExternStatement 节点（编译器）
    /// </summary>
    public object? VisitExternStatement(ExternStatement node)
    {

        // 使用工厂创建对应的提供者
        var provider = ExternProviderFactory.CreateProvider(node.ExternType);

        // 检查是否支持 IL 模式
        if (!provider.SupportsCompilation)
        {
            throw new InvalidOperationError(node,
                $"{node.ExternType} 类型的 extern 函数不支持 IL 模式，仅支持解释模式执行。");
        }

        // 委托给提供者生成 IL 代码
        provider.GenerateIL(node.DllName, node.Functions, node.DefaultCallingConvention, ilGenerator, local);

        return null;
    
    }

    /// <summary>
    /// 访问 FileHeaderDirective 节点（编译器）
    /// </summary>
    public object? VisitFileHeaderDirective(FileHeaderDirective node)
    {
        // 文件头指令在 IL 模式下不需要生成代码
        return null;
    }

    /// <summary>
    /// 访问 UsingStatement 节点（编译器）
    /// </summary>
    public object? VisitUsingStatement(UsingStatement node)
    {

        // 1. 创建局部变量存储资源
        Label? suspendedDisposeSkipLabel = null;
        node.ResourceExpression.Accept(this);
        var resourceType = node.ResourceExpression.Accept(new TypeInferenceVisitor(local));
        if (resourceType != null)
        {
            var resourceLocal = ilGenerator.DeclareLocal(resourceType);
            ilGenerator.Emit(OpCodes.Stloc, resourceLocal);

            // 2. 如果有变量名，添加到局部变量
            if (node.VariableName is not null)
            {
                local.AddLocalVar(node.VariableName, resourceLocal);
                local.LocalVarTypes[node.VariableName] = resourceType;
            }

            // 3. 生成try-finally
            ilGenerator.BeginExceptionBlock();

            // 异步状态机：using 体也是可恢复的受保护区域，首指令处发射派发链
            if (local.AsyncStateMachineGenerator is not null)
            {
                local.AsyncStateMachineGenerator.EnterProtectedRegion(ilGenerator, node);
            }

            // try块：执行BlockStatement
            node.BlockStatement.Accept(this);

            // finally块
            ilGenerator.BeginFinallyBlock();

            // 挂起返回时不要提前释放资源（与用户 finally 同理）
            if (local.AsyncStateMachineGenerator is not null)
            {
                suspendedDisposeSkipLabel = ilGenerator.DefineLabel();
                local.AsyncStateMachineGenerator.EmitSuspendedFinallyGuard(
                    ilGenerator, suspendedDisposeSkipLabel.Value);
            }

            // 加载资源
            ilGenerator.Emit(OpCodes.Ldloc, resourceLocal);
        }

        // 根据资源类型调用不同的 Dispose 方法
        if (resourceType == typeof(int))
        {
            // 1. 整数资源：调用 ResourceManager.TryDispose(id)
            var disposeMethod = typeof(ResourceManager).GetMethod(nameof(ResourceManager.TryDispose));
            if (disposeMethod != null) ilGenerator.Emit(OpCodes.Call, disposeMethod);
        }
        else if (typeof(AnyLangValue).IsAssignableFrom(resourceType))
        {
            // 2. AnyLangValue 资源：调用 AnyLangValue.TryDispose()
            var tryDisposeMethod = typeof(AnyLangValue).GetMethod(
                nameof(AnyLangValue.TryDispose),
                Type.EmptyTypes);
            if (tryDisposeMethod is not null)
            {
                ilGenerator.Emit(OpCodes.Callvirt, tryDisposeMethod);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Pop);
            }
        }
        else
        {
            // 3. IDisposable 资源：调用 IDisposable.Dispose
            var disposableType = typeof(IDisposable);
            if (disposableType.IsAssignableFrom(resourceType))
            {
                var disposeMethod = disposableType.GetMethod(nameof(IDisposable.Dispose));
                if (disposeMethod is not null)
                {
                    if (resourceType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Box, resourceType);
                    }

                    ilGenerator.Emit(OpCodes.Callvirt, disposeMethod);
                }
            }
            else
            {
                // 不支持Dispose的类型，丢弃栈顶值
                ilGenerator.Emit(OpCodes.Pop);
            }
        }

        if (suspendedDisposeSkipLabel is not null)
        {
            ilGenerator.MarkLabel(suspendedDisposeSkipLabel.Value);
        }

        ilGenerator.EndExceptionBlock();

        return null;
    
    }

    /// <summary>
    /// 访问 FuncRunStatement 节点（表达式语句，按原实现销毁调用结果）
    /// </summary>
    public object? VisitFuncRunStatement(FuncRunStatement node)
    {
        // 迁移自 FuncRunStatement.GenerateIl()
        if (node.Expression is null) return null;

        node.Expression.Accept(this);
        var outputType = node.Expression is Operation operation
            ? OperationTypeCache.Get(operation) ?? node.Expression.Accept(new TypeInferenceVisitor(local))
            : node.Expression.Accept(new TypeInferenceVisitor(local));
        if (outputType != typeof(void)) ilGenerator.Emit(OpCodes.Pop);
        return null;
    }
}
