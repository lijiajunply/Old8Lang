using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;
using Old8Lang.Interpreter;
using Old8Lang.TypeSystem;

namespace Old8Lang.AST.Statement;

/// <summary>
/// 赋值语句类，用于处理Old8Lang中的赋值操作
/// 支持普通变量赋值、成员访问赋值和索引访问赋值
/// </summary>
public class SetStatement : OldStatement
{
    /// <summary>
    /// 变量标识符（用于普通变量赋值）
    /// </summary>
    public readonly LangId? Id;

    /// <summary>
    /// 左侧表达式（用于成员访问或索引访问赋值）
    /// </summary>
    private readonly LangExpression? LeftExpression;

    /// <summary>
    /// 赋值表达式
    /// </summary>
    public readonly LangExpression Value;

    /// <summary>
    /// 构造函数：创建普通变量赋值语句
    /// </summary>
    /// <param name="id">变量标识符</param>
    /// <param name="value">赋值表达式</param>
    /// <param name="position">源代码位置信息，用于错误报告</param>
    public SetStatement(LangId id, LangExpression value, SourcePosition position = default) : base(position)
    {
        Id = id;
        LeftExpression = null;
        Value = value;
    }

    /// <summary>
    /// 构造函数：创建成员访问或索引访问赋值语句
    /// </summary>
    /// <param name="leftExpression">左侧表达式（成员访问或索引访问）</param>
    /// <param name="value">赋值表达式</param>
    /// <param name="position">源代码位置信息，用于错误报告</param>
    public SetStatement(LangExpression leftExpression, LangExpression value, SourcePosition position = default) :
        base(position)
    {
        Id = null;
        LeftExpression = leftExpression;
        Value = value;
    }

    /// <summary>
    /// 在解释模式下执行赋值语句
    /// </summary>
    /// <param name="manager">变量管理器，用于管理变量的赋值和访问</param>
    /// <exception cref="TypeError">当类型不匹配时抛出</exception>
    /// <exception cref="NameError">当找不到指定名称时抛出</exception>
    public override void Run(VariateManager manager)
    {
        var result = Value.Run(manager);

        // 检查是否为首次赋值
        bool isInitialAssignment = false;
        if (Id != null)
        {
            var existingVariable = manager.GetAny(Id);
            isInitialAssignment = (existingVariable == null);

            // 如果有类型注解，进行类型检查
            if (!string.IsNullOrEmpty(Id.AssumptionType))
            {
                TypeChecker.ValidateVariableAssignment(Id.AssumptionType, result, this, Id.IdName, isInitialAssignment);
            }
            else if (!isInitialAssignment)
            {
                // 没有类型注解但是修改已存在的变量，检查是否为 const 变量
                if (TypeChecker.IsConstVariable(Id.IdName))
                {
                    throw new Error.TypeError(this, "any", TypeChecker.GetLangValueType(result), $"不能修改 const 变量 '{Id.IdName}'");
                }
            }
        }

        // 处理嵌套索引访问赋值：array[0][0] <- value
        if (LeftExpression is NestedIndexAccess nestedIndexAccess)
        {
            // 运行基础索引访问，获取容器对象
            var baseResult = nestedIndexAccess.BaseIndex.Run(manager);

            // 获取嵌套索引值
            var nestedIndexValue = nestedIndexAccess.NestedIndex.Run(manager);

            // 检查基础结果是否支持索引访问
            if (baseResult is ILangList baseCollection)
            {
                baseCollection.Set(nestedIndexValue, result);
                return;
            }

            throw new InvalidOperationError(this, $"不支持的嵌套索引赋值类型: {baseResult?.GetType().Name ?? "null"}");
        }

        // 处理成员访问赋值：this.name <- value, person.name <- value
        if (LeftExpression is Operation operation)
        {
            // 处理数组解构赋值：[a, b] <- [1, 2]
            if (operation is { Left: LangId { IdName: "array_destruct" }, Opera: LangTokenType.LeftBracket })
            {
                // 从字符串中解析解构的标识符列表
                var identifiersStr = operation.Right?.ToString() ??
                                     throw new InvalidOperationError(this, "数组解构赋值需要有效的标识符列表");

                // 解析标识符列表字符串，格式："a,b,null"
                var identStrs = identifiersStr.Split(',');

                // 获取右侧数组值
                if (result is ListLangValue listValue)
                {
                    // 列表类型
                    for (int i = 0; i < identStrs.Length; i++)
                    {
                        var identStr = identStrs[i].Trim();
                        if (identStr == "null") continue; // 跳过空元素

                        // 获取列表元素
                        if (i < listValue.Values.Count)
                        {
                            var elementValue = listValue.Values[i];
                            // 执行赋值
                            manager.Set(new LangId(identStr), elementValue);
                        }
                    }
                }
                else if (result is ArrayLangValue arrayValue)
                {
                    // 数组类型
                    var items = arrayValue.GetItems().ToList();
                    for (int i = 0; i < identStrs.Length; i++)
                    {
                        var identStr = identStrs[i].Trim();
                        if (identStr == "null") continue; // 跳过空元素

                        // 获取数组元素
                        if (i < items.Count)
                        {
                            var elementValue = items[i];
                            // 执行赋值
                            manager.Set(new LangId(identStr), elementValue);
                        }
                    }
                }
                else
                {
                    throw new TypeError(this, "数组解构赋值右侧必须是数组或列表类型");
                }

                return;
            }

            // 处理对象解构赋值：{name, age} <- person
            if (operation is { Left: LangId { IdName: "object_destruct" }, Opera: LangTokenType.LeftBrace })
            {
                // 从字符串中解析解构的属性列表
                var propertiesStr = operation.Right?.ToString() ??
                                    throw new InvalidOperationError(this, "对象解构赋值需要有效的属性列表");

                // 获取右侧对象值
                if (result is not AnyLangValue objectValue)
                {
                    throw new TypeError(this, "对象解构赋值右侧必须是对象类型");
                }

                // 解析属性列表字符串，格式："name,age,id:userId"
                var propStrs = propertiesStr.Split(',');

                // 执行解构赋值
                foreach (var propStr in propStrs)
                {
                    var propStrTrimmed = propStr.Trim();
                    if (propStrTrimmed.Contains(":"))
                    {
                        // 处理带别名的对象解构：name: newName
                        var parts = propStrTrimmed.Split(':');
                        var propName = parts[0].Trim();
                        var aliasName = parts[1].Trim();

                        // 获取属性值
                        if (objectValue.Result.TryGetValue(propName, out var propValue))
                        {
                            // 执行赋值
                            manager.Set(new LangId(aliasName), propValue);
                        }
                        else
                        {
                            throw new NameError(this, propName);
                        }
                    }
                    else
                    {
                        // 处理基本对象解构：name
                        var propName = propStrTrimmed;

                        // 获取属性值
                        if (objectValue.Result.TryGetValue(propName, out var propValue))
                        {
                            // 执行赋值
                            manager.Set(new LangId(propName), propValue);
                        }
                        else
                        {
                            throw new NameError(this, propName);
                        }
                    }
                }

                return;
            }

            // 检查是否是 DOT 操作（成员访问或嵌套索引）
            if (operation.Opera == LangTokenType.Dot)
            {
                // 首先检查是否是嵌套索引赋值：matrix[0][1] <- value
                // 左侧应该是LangListItem（第一层索引），右侧应该是索引表达式
                if (operation.Left is LangListItem outerListItem &&
                    operation.Right is LangExpression finalIndex)
                {
                    // 获取外层索引的值：matrix[0] 返回内层数组
                    var outerCollectionValue = manager.GetValue(outerListItem.ListId);
                    var outerIndexValue = outerListItem.Key.Run(manager);

                    if (outerCollectionValue is ArrayLangValue outerArray)
                    {
                        // 获取内层数组
                        var innerArray = outerArray.Get(outerIndexValue as IntLangValue ??
                                                     throw new TypeError(this, "IntLangValue", outerIndexValue.GetType().Name));

                        // 检查内层数组是否是ILangList类型
                        if (innerArray is ILangList innerListCollection)
                        {
                            // 获取最终索引
                            var finalIndexValue = finalIndex.Run(manager);

                            // 设置内层数组的元素
                            innerListCollection.Set(finalIndexValue, result);
                            return;
                        }
                    }
                    else if (outerCollectionValue is ListLangValue outerList)
                    {
                        // 获取内层数组
                        var innerArray = outerList.Get(outerIndexValue as IntLangValue ??
                                                    throw new TypeError(this, "IntLangValue", outerIndexValue.GetType().Name));

                        // 检查内层数组是否是ILangList类型
                        if (innerArray is ILangList innerListCollection)
                        {
                            // 获取最终索引
                            var finalIndexValue = finalIndex.Run(manager);

                            // 设置内层数组的元素
                            innerListCollection.Set(finalIndexValue, result);
                            return;
                        }
                    }
                }

                // 处理 this.member <- value 形式的赋值
                if (operation is { Left: LangId { IdName: "this" }, Right: LangId memberName })
                {
                    // 查找当前实例
                    if (manager.GetValue(new LangId("this")) is AnyLangValue anyValue)
                    {
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyValue.Result[memberName.IdName] = result;
                        // 同时更新实例的Manager中的值，确保后续访问能获取到最新值
                        anyValue.Manager.Set(new LangId(memberName.IdName), result);
                        // 更新当前manager中的值，确保在同一个方法中后续访问能获取到最新值
                        manager.Set(new LangId(memberName.IdName), result);
                        return;
                    }

                    // 如果没有找到，可能是在init方法中，此时需要检查manager.IsFunc标志
                    if (manager.IsFunc)
                    {
                        // 在init方法中，当前实例应该是manager.AnyInfo中的第一个AnyLangValue
                        anyValue = manager.GetValue(new LangId("this")) as AnyLangValue ??
                                   throw new NameError(this, "this");
                        // 清除缓存，确保下次访问获取最新值
                        anyValue.ClearFunctionLookupCache();
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyValue.Result[memberName.IdName] = result;
                        // 同时更新实例的Manager中的值，确保后续访问能获取到最新值
                        anyValue.Manager.Set(new LangId(memberName.IdName), result);
                        // 更新当前manager中的值，确保在同一个方法中后续访问能获取到最新值
                        manager.Set(new LangId(memberName.IdName), result);
                        return;
                    }
                }
                // 处理普通对象成员访问：person.name <- value
                else if (operation is { Left: { } leftExpr, Right: LangId memberNameObj })
                {
                    // 获取左侧对象的值
                    var leftValue = leftExpr.Run(manager);
                    if (leftValue is AnyLangValue anyObj)
                    {
                        // 清除缓存，确保下次访问获取最新值
                        anyObj.ClearFunctionLookupCache();

                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyObj.Result[memberNameObj.IdName] = result;
                        // 同时更新对象的管理器中的值
                        anyObj.Manager.Set(new LangId(memberNameObj.IdName), result);
                        return;
                    }
                }
            }
        }
        // 处理切片赋值：array[start:end] <- values
        else if (LeftExpression is SliceLangValue sliceValue)
        {
            // 获取集合对象
            var collectionValue = sliceValue.Id.Run(manager);

            // 检查集合是否是ILangList类型
            if (collectionValue is not ILangList listCollection)
            {
                throw new InvalidOperationError(this,
                    $"类型 '{collectionValue.GetType().Name}' 不支持切片赋值操作");
            }

            // 计算切片参数
            var length = listCollection.GetLength();
            var start1 = sliceValue.Start?.Run(manager);
            var end1 = sliceValue.End?.Run(manager);
            var step1 = sliceValue.Step?.Run(manager);

            var stepValue = step1?.GetValue<int>() ?? 1;

            if (stepValue == 0)
                throw new InvalidOperationError(this, "切片步长不能为0");

            if (stepValue != 1)
            {
                throw new InvalidOperationError(this,
                    "切片赋值不支持步长参数。如果需要使用步长，请使用循环逐个赋值");
            }

            // 计算起始和结束索引
            int startValue, endValue;
            if (stepValue > 0)
            {
                startValue = start1?.GetValue<int>() ?? 0;
                endValue = end1?.GetValue<int>() ?? length;
            }
            else
            {
                startValue = start1?.GetValue<int>() ?? length - 1;
                endValue = end1?.GetValue<int>() ?? -1;
            }

            // 获取要赋值的值列表
            IEnumerable<LangValueType> valuesList;
            if (result is ILangList resultList)
            {
                valuesList = resultList.GetItems();
            }
            else
            {
                throw new TypeError(this, "ILangList", result.GetType().Name,
                    "切片赋值的右侧必须是列表、数组或其他可迭代类型");
            }

            // 调用SetSlice方法
            listCollection.SetSlice(startValue, endValue, valuesList);
            return;
        }
        // 处理索引访问赋值：array[index] <- value, list[index] <- value, dict[key] <- value
        else if (LeftExpression is LangListItem listItem)
        {
            // 获取集合对象
            var collectionValue = manager.GetValue(listItem.ListId);
            // 获取索引或键
            var indexValue = listItem.Key.Run(manager);

            // 检查集合是否是ILangList类型，如果是则调用其Set方法
            if (collectionValue is ILangList listCollection)
            {
                listCollection.Set(indexValue, result);
                return;
            }
        }

        // 处理普通变量赋值：name <- value
        if (Id != null && !string.IsNullOrEmpty(Id.IdName))
        {
            manager.Set(Id, result);
        }
    }

    /// <summary>
    /// 在编译模式下生成赋值语句的IL代码
    /// </summary>
    /// <param name="ilGenerator">IL指令生成器</param>
    /// <param name="local">局部变量管理器，用于管理变量的赋值和访问</param>
    /// <exception cref="InvalidOperationError">当尝试对字符串进行索引赋值时抛出</exception>
    public override void GenerateIl(ILGenerator ilGenerator, LocalManager local)
    {
        if (Id != null && !string.IsNullOrEmpty(Id.IdName))
        {
            // 普通变量赋值: name <- value
            Value.SetValueToIl(ilGenerator, local, Id.IdName);
        }
        else if (LeftExpression != null)
        {
            if (LeftExpression is Operation operation)
            {
                // 处理数组解构赋值: [a, b, c] <- array
                if (operation is { Left: LangId { IdName: "array_destruct" }, Opera: LangTokenType.LeftBracket })
                {
                    // 解析解构的标识符列表
                    var identifiersStr = operation.Right?.ToString() ??
                                         throw new InvalidOperationError(this, "数组解构赋值需要有效的标识符列表");
                    var identStrs = identifiersStr.Split(',');

                    // 生成右侧数组值的IL
                    Value.LoadIlValue(ilGenerator, local);

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

                    return;
                }

                // 处理对象解构赋值: {name, age} <- person
                if (operation is { Left: LangId { IdName: "object_destruct" }, Opera: LangTokenType.LeftBrace })
                {
                    // 解析解构的属性列表
                    var propertiesStr = operation.Right?.ToString() ??
                                        throw new InvalidOperationError(this, "对象解构赋值需要有效的属性列表");
                    var propStrings = propertiesStr.Split(',');

                    // 生成右侧对象值的IL
                    Value.LoadIlValue(ilGenerator, local);

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
                                                throw new InvalidOperationError(this,
                                                    "AnyLangValue类型缺少GetPropertyValue方法");

                        // 加载属性名
                        ilGenerator.Emit(OpCodes.Ldstr, propName);

                        // 调用GetPropertyValue方法
                        ilGenerator.Emit(OpCodes.Callvirt, getPropertyMethod);

                        // 将属性值赋值给对应的变量
                        var localVar = local.GetOrCreateLocalVar(ilGenerator, aliasName, typeof(object));
                        ilGenerator.Emit(OpCodes.Stloc, localVar);
                    }

                    return;
                }

                // 处理成员访问或索引访问赋值: left.right <- value 或 left[right] <- value
                if (operation is { Opera: LangTokenType.Dot, Right: LangId memberId })
                {
                    // 成员访问赋值: left.right <- value

                    // 检查是否是this访问（如this.name <- value）
                    if (operation.Left is LangId { IdName: "this" } && local.InClassEnv != null)
                    {
                        // 加载 this（参数0）
                        ilGenerator.Emit(OpCodes.Ldarg_0);

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
                            while (baseType != null && baseType != typeof(object))
                            {
                                fieldInfo = baseType.GetField(memberId.IdName,
                                    BindingFlags.Public | BindingFlags.Instance);
                                if (fieldInfo != null) break;
                                baseType = baseType.BaseType;
                            }
                        }
                        else
                        {
                            // 对于已创建的类型，直接获取字段
                            fieldInfo = local.InClassEnv.GetField(memberId.IdName,
                                BindingFlags.Public | BindingFlags.Instance);
                        }

                        if (fieldInfo != null)
                        {
                            // 加载右值
                            Value.LoadIlValue(ilGenerator, local);

                            // 检查值类型是否与字段类型匹配
                            var valueType = Value.OutputType(local);
                            if (valueType != null && fieldInfo.FieldType != valueType)
                            {
                                // 如果值类型与字段类型不匹配，进行类型转换
                                if (fieldInfo.FieldType == typeof(object) && valueType.IsValueType)
                                {
                                    // 值类型到object，需要装箱
                                    ilGenerator.Emit(OpCodes.Box, valueType);
                                }
                                else if (fieldInfo.FieldType.IsValueType && valueType == typeof(object))
                                {
                                    // object到值类型，需要拆箱
                                    ilGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
                                }
                            }

                            ilGenerator.Emit(OpCodes.Stfld, fieldInfo);
                        }

                        return;
                    }

                    // 非this访问，正常处理
                    // 加载左对象
                    operation.Left!.LoadIlValue(ilGenerator, local);
                    // 加载右值
                    Value.LoadIlValue(ilGenerator, local);
                    // 获取字段或属性
                    var leftType = operation.Left.OutputType(local)!;

                    // 检查leftType是否是TypeBuilder或typeof(object)（表示this访问）
                    if (leftType is not TypeBuilder && leftType != typeof(object))
                    {
                        var field = leftType.GetField(memberId.IdName);
                        if (field != null)
                        {
                            ilGenerator.Emit(OpCodes.Stfld, field);
                        }
                        else
                        {
                            var property = leftType.GetProperty(memberId.IdName);
                            if (property != null && property.GetSetMethod() != null)
                            {
                                ilGenerator.Emit(OpCodes.Callvirt, property.GetSetMethod()!);
                            }
                        }
                    }
                    // 如果是TypeBuilder或typeof(object)，跳过此操作
                }
                else if (operation.Opera == LangTokenType.Dot)
                {
                    // 索引访问赋值: left[right] <- value
                    // 加载左对象
                    operation.Left!.LoadIlValue(ilGenerator, local);
                    // 加载索引
                    operation.Right!.LoadIlValue(ilGenerator, local);
                    // 加载右值
                    Value.LoadIlValue(ilGenerator, local);
                    // 获取左对象类型
                    var leftType = operation.Left.OutputType(local)!;

                    if (leftType.IsArray)
                    {
                        // 数组索引赋值
                        var elementType = leftType.GetElementType()!;
                        // 获取值的类型
                        var valueType = Value.OutputType(local)!;
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
                        var valueType = Value.OutputType(local)!;
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
            else if (LeftExpression is LangListItem listItem)
            {
                // 处理LangListItem索引赋值: listId[key] <- value
                // 获取集合类型
                var listType = listItem.ListId.OutputType(local);

                // 加载集合对象
                listItem.ListId.LoadIlValue(ilGenerator, local);

                // 对于字典，需要在加载键和值时进行类型转换
                if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    var genericArgs = listType.GetGenericArguments();
                    var dictKeyType = genericArgs[0];
                    var dictValueType = genericArgs[1];

                    // 加载键并进行类型转换
                    listItem.Key.LoadIlValue(ilGenerator, local);
                    var keyType = listItem.Key.OutputType(local);
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
                    Value.LoadIlValue(ilGenerator, local);
                    var valueType = Value.OutputType(local);
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
                    return;
                }

                // 对于非字典类型，按原来的方式加载
                listItem.Key.LoadIlValue(ilGenerator, local);
                Value.LoadIlValue(ilGenerator, local);

                if (listType == typeof(string))
                {
                    // 字符串是不可变的，不支持赋值
                    throw new InvalidOperationError(this, "字符串是不可变的，不支持索引赋值");
                }
                else if (listType.IsArray)
                {
                    // 数组索引赋值
                    var elementType = listType.GetElementType()!;
                    // 获取值的类型
                    var valueType = Value.OutputType(local)!;
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
                    var valueType = Value.OutputType(local)!;
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
    }

    /// <summary>
    /// 获取指定索引处的语句（实现OldStatement接口）
    /// </summary>
    /// <param name="index">语句索引</param>
    /// <returns>返回当前语句本身，因为SetStatement是单个语句</returns>
    public override OldStatement this[int index] => this;

    /// <summary>
    /// 获取语句数量（实现OldStatement接口）
    /// </summary>
    /// <returns>返回0，因为SetStatement是单个语句</returns>
    public override int Count => 0;

    /// <summary>
    /// 将赋值语句转换为字符串表示
    /// </summary>
    /// <returns>赋值语句的字符串表示</returns>
    public override string ToString()
    {
        if (LeftExpression != null)
        {
            return $"{LeftExpression} <- {Value}";
        }

        // 如果 Id 为空或 IdName 为空，只显示右值（用于块表达式的返回值）
        if (Id == null || string.IsNullOrEmpty(Id.IdName))
        {
            return $" <- {Value}";
        }

        return $"{Id} <- {Value}";
    }
}