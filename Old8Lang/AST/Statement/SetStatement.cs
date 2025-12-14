using Old8Lang.LangParser;
using System.Reflection;
using System.Reflection.Emit;
using Old8Lang.AST.Expression;
using Old8Lang.AST.Expression.Value;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.Compiler;
using Old8Lang.Error;

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
    public readonly LangExpression? LeftExpression;
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

        // 如果有类型注解，进行类型检查
        if (Id != null && !string.IsNullOrEmpty(Id.AssumptionType))
        {
            var expectedType = Id.AssumptionType.ToLower();
            var actualType = result.TypeToString().ToLower();

            // 解析泛型类型注解，如 "list<int>" 或 "array<string>"
            var isGeneric = expectedType.Contains('<') && expectedType.EndsWith('>');
            string baseExpectedType;
            string genericArg = "";

            if (isGeneric)
            {
                // 提取泛型类型名称
                var genericIndex = expectedType.IndexOf('<');
                baseExpectedType = expectedType[..genericIndex].Trim();
                genericArg = expectedType[(genericIndex + 1)..^1].Trim();
            }
            else
            {
                baseExpectedType = expectedType;
            }

            // 建立类型匹配映射
            var typeMap = new Dictionary<string, List<string>>
            {
                { "int", ["int"] },
                { "double", ["double"] },
                { "string", ["string"] },
                { "bool", ["bool"] },
                { "char", ["char"] },
                { "array", ["array"] },
                { "dictionary", ["dictionary"] },
                { "list", ["list"] },
                { "tuple", ["tuple"] },
                { "type", ["type"] },
                { "function", ["function"] }
            };

            // 检查基础类型是否匹配
            if (typeMap.TryGetValue(baseExpectedType, out var allowedTypes))
            {
                if (!allowedTypes.Contains(actualType))
                {
                    throw new TypeError(Id, expectedType, actualType);
                }
            }

            // 如果是泛型类型，检查元素类型是否匹配
            if (isGeneric && (baseExpectedType == "list" || baseExpectedType == "array" ||
                              baseExpectedType == "dictionary"))
            {
                // 对于列表和数组，检查元素类型
                if (result is ListLangValue listValue)
                {
                    // 检查列表中的所有元素类型是否匹配泛型参数
                    foreach (var item in listValue.Values)
                    {
                        var itemType = item.TypeToString().ToLower();
                        if (itemType != genericArg)
                        {
                            throw new TypeError(Id, expectedType, actualType,
                                $"列表元素类型不匹配：期望 {genericArg}，实际 {itemType}");
                        }
                    }
                }
                else if (result is ArrayLangValue arrayValue)
                {
                    // 检查数组中的所有元素类型是否匹配泛型参数
                    foreach (var item in arrayValue.GetItems())
                    {
                        var itemType = item.TypeToString().ToLower();
                        if (itemType != genericArg)
                        {
                            throw new TypeError(Id, expectedType, actualType,
                                $"数组元素类型不匹配：期望 {genericArg}，实际 {itemType}");
                        }
                    }
                }
            }
        }

        // 处理成员访问赋值：this.name <- value, person.name <- value
        if (LeftExpression is Operation operation)
        {
            // 检查是否是 DOT 操作（成员访问）
            if (operation.Opera == LangTokenType.Dot)
            {
                // 处理 this.member <- value 形式的赋值
                if (operation is { Left: LangId { IdName: "this" }, Right: LangId memberName })
                {
                    // 查找当前实例
                    if (manager.GetValue(new LangId("this")) is AnyLangValue anyValue)
                    {
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyValue.Result[memberName.IdName] = result;
                        // 同时更新VariateManager中的值，确保后续访问能获取到最新值
                        anyValue.Manager.Set(new LangId(memberName.IdName), result);
                        // 同时更新当前manager中的值，确保在同一个方法中后续访问能获取到最新值
                        manager.Set(new LangId(memberName.IdName), result);
                        return;
                    }

                    // 如果没有找到，可能是在init方法中，此时需要检查manager.IsFunc标志
                    if (manager.IsFunc)
                    {
                        // 在init方法中，当前实例应该是manager.AnyInfo中的第一个AnyLangValue
                        anyValue = manager.GetValue(new LangId("this")) as AnyLangValue ??
                                   throw new NameError(this, "this");
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyValue.Result[memberName.IdName] = result;
                        // 同时更新VariateManager中的值，确保后续访问能获取到最新值
                        anyValue.Manager.Set(new LangId(memberName.IdName), result);
                        // 同时更新当前manager中的值，确保在同一个方法中后续访问能获取到最新值
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
                        // 将结果添加到实例的Result字典中，覆盖原来的值
                        anyObj.Result[memberNameObj.IdName] = result;
                        // 同时更新对象的管理器中的值
                        anyObj.Manager.Set(new LangId(memberNameObj.IdName), result);
                        return;
                    }
                }
            }
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
            if (LeftExpression is Operation { Opera: LangTokenType.Dot } operation)
            {
                // 处理成员访问或索引访问赋值: left.right <- value 或 left[right] <- value
                if (operation.Right is LangId memberId)
                {
                    // 成员访问赋值: left.right <- value

                    // 检查是否是this访问（如this.name <- value）
                    if (operation.Left is LangId { IdName: "this" } && local.InClassEnv != null)
                    {
                        // 加载 this（参数0）
                        ilGenerator.Emit(OpCodes.Ldarg_0);

                        FieldInfo? fieldInfo = null;

                        // 从 FieldVar 中获取字段信息
                        if (local.FieldVar.TryGetValue(memberId.IdName, out fieldInfo))
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
                                fieldInfo = baseType.GetField(memberId.IdName, BindingFlags.Public | BindingFlags.Instance);
                                if (fieldInfo != null) break;
                                baseType = baseType.BaseType;
                            }
                        }
                        else
                        {
                            // 对于已创建的类型，直接获取字段
                            fieldInfo = local.InClassEnv.GetField(memberId.IdName, BindingFlags.Public | BindingFlags.Instance);
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
                else
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