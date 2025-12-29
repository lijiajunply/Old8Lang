using Old8Lang.AST.Expression.AnyValues;
using Old8Lang.AST.Expression.Intermediates;
using Old8Lang.AST.Expression.Value;
using Old8Lang.Error;
using Old8Lang.Interpreter;

namespace Old8Lang.AST.Expression;

/// <summary>
/// 语言值类型抽象基类，所有Old8Lang表达式值都继承自此类
/// </summary>
/// <remarks>
/// 该类是Old8Lang表达式系统的核心，定义了所有值类型必须实现的基本操作
/// 包括算术运算、比较运算、类型转换、点操作等
/// </remarks>
/// <param name="position">源代码位置信息，用于错误报告</param>
public abstract class LangValueType(SourcePosition position = default) : LangExpression(position)
{
    /// <summary>
    /// 将值转换为字符串表示
    /// </summary>
    /// <returns>值的字符串表示</returns>
    public override string ToString() => GetValue().ToString()!;

    #region 算术运算

    /// <summary>
    /// 加法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>加法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持加法运算时抛出</exception>
    public virtual LangValueType Plus(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的加法操作");

    /// <summary>
    /// 减法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>减法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持减法运算时抛出</exception>
    public virtual LangValueType Minus(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的减法操作");

    /// <summary>
    /// 乘法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>乘法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持乘法运算时抛出</exception>
    public virtual LangValueType Times(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的乘法操作");

    /// <summary>
    /// 除法运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>除法运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持除法运算时抛出</exception>
    public virtual LangValueType Divide(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的除法操作");

    /// <summary>
    /// 取模运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>取模运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持取模运算时抛出</exception>
    public virtual LangValueType Mod(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的取模操作");

    /// <summary>
    /// 幂运算
    /// </summary>
    /// <param name="otherLangValueType">另一个操作数</param>
    /// <returns>幂运算结果</returns>
    /// <exception cref="InvalidOperationError">当不支持幂运算时抛出</exception>
    public virtual LangValueType Power(LangValueType otherLangValueType) => throw new InvalidOperationError(this,
        $"不支持类型 '{GetType().Name}' 和 '{otherLangValueType.GetType().Name}' 的幂运算");

    #endregion

    /// <summary>
    /// 点操作，用于访问对象的属性或方法
    /// </summary>
    /// <param name="dotExpression">点后面的表达式</param>
    /// <param name="manager">变量管理器，用于运行表达式</param>
    /// <returns>点操作结果</returns>
    /// <exception cref="AttributeError">当访问不存在的属性时抛出</exception>
    /// <exception cref="InvalidOperationError">当不支持点操作时抛出</exception>
    public virtual LangValueType Dot(LangExpression dotExpression, VariateManager manager)
    {
        if (dotExpression is LangId id)
        {
            if (id.IdName == "XAUAT")
                return new StringLangValue("西建大还我血汗钱我要回家");
            throw new AttributeError(this, id.IdName, GetType().Name);
        }

        if (dotExpression is Instance instance)
        {
            return instance.FromClassToResult(this);
        }

        throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 的点操作");
    }

    #region 比较运算

    /// <summary>
    /// 相等比较
    /// </summary>
    /// <param name="otherValueType">另一个值</param>
    /// <returns>比较结果，相等返回true，否则返回false</returns>
    public virtual bool Equal(LangValueType? otherValueType) => false;

    /// <summary>
    /// 小于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，小于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持小于比较时抛出</exception>
    public virtual bool Less(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持Less操作");

    /// <summary>
    /// 大于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，大于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持大于比较时抛出</exception>
    public virtual bool Greater(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持Greater操作");

    /// <summary>
    /// 小于等于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，小于等于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持小于等于比较时抛出</exception>
    public virtual bool LessEqual(LangValueType? otherValue) => throw new InvalidOperationError(this, "不支持LessEqual操作");

    /// <summary>
    /// 大于等于比较
    /// </summary>
    /// <param name="otherValue">另一个值</param>
    /// <returns>比较结果，大于等于返回true，否则返回false</returns>
    /// <exception cref="InvalidOperationError">当不支持大于等于比较时抛出</exception>
    public virtual bool GreaterEqual(LangValueType? otherValue) =>
        throw new InvalidOperationError(this, "不支持GreaterEqual操作");

    #endregion

    /// <summary>
    /// 类型转换
    /// </summary>
    /// <param name="otherLangValueType">目标类型值</param>
    /// <param name="manager">变量管理器</param>
    /// <returns>转换后的结果</returns>
    /// <exception cref="InvalidOperationError">当不支持类型转换时抛出</exception>
    public virtual LangValueType Converse(LangValueType otherLangValueType, VariateManager manager) =>
        throw new InvalidOperationError(this, $"不支持类型 '{GetType().Name}' 转换为 '{otherLangValueType.GetType().Name}'");

    /// <summary>
    /// 执行表达式，返回自身
    /// </summary>
    /// <param name="manager">变量管理器</param>
    /// <returns>自身实例</returns>
    public override LangValueType Run(VariateManager manager) => this;

    /// <summary>
    /// 将值类型转换为字符串表示
    /// </summary>
    /// <returns>值类型的字符串表示</returns>
    public virtual string TypeToString()
    {
        return this switch
        {
            AnyLangValue a => a.ClassId.IdName,
            ArrayLangValue => "Array",
            BoolLangValue => "Bool",
            CharLangValue => "Char",
            DictionaryLangValue => "Dictionary",
            DoubleLangValue => "Double",
            FuncLangValue func => $"Function {func.Id}({Apis.ListToString(func.Ids)})",
            Instance instance => $"Instance {instance}",
            IntLangValue => "Int",
            NullLangValue => "Null",
            LangListItem item => $"Item {item}",
            ListLangValue => "List",
            StringLangValue => "String",
            TypeLangValue => "Type",
            TupleLangValue => "Tuple",
            _ => "Value"
        };
    }

    /// <summary>
    /// 获取值的实际.NET对象
    /// </summary>
    /// <returns>值对应的.NET对象</returns>
    public virtual object GetValue() => new();

    /// <summary>
    /// 获取值的实际.NET对象，并转换为指定类型
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>转换后的.NET对象</returns>
    /// <exception cref="InvalidCastException">当无法转换为指定类型时抛出</exception>
    public T GetValue<T>()
    {
        return (T)GetValue();
    }

    /// <summary>
    /// 获取值的显示字符串，用于调试和输出
    /// </summary>
    /// <returns>值的显示字符串</returns>
    public virtual string ToDisplayString() => ToString(); // 默认使用ToString()，子类可以重写

    /// <summary>
    /// 将.NET对象转换为Old8Lang值类型
    /// </summary>
    /// <param name="value">要转换的.NET对象</param>
    /// <returns>对应的Old8Lang值类型</returns>
    /// <exception cref="InvalidOperationError">当不支持转换的类型时抛出</exception>
    /// <remarks>
    /// 支持的转换类型包括：
    /// - int → IntLangValue
    /// - string → StringLangValue
    /// - double → DoubleLangValue
    /// - char → CharLangValue
    /// - List&lt;object&gt; → ListLangValue
    /// - List&lt;string&gt; → ListLangValue
    /// - List&lt;int&gt; → ListLangValue
    /// - List&lt;double&gt; → ListLangValue
    /// - object[] → ArrayLangValue
    /// - long → IntLangValue
    /// - bool → BoolLangValue
    /// - Dictionary&lt;object, object&gt; → DictionaryLangValue
    /// - Tuple&lt;object, object&gt; → TupleLangValue
    /// - ValueTuple&lt;object, object&gt; → TupleLangValue
    /// - Task 或 Task&lt;T&gt; → 等待结果后递归转换
    /// </remarks>
    public static LangValueType ObjToValue(object? value)
    {
        if (value == null)
        {
            return NullLangValue.Instance;
        }

        // 处理 Task 和 Task<T> 类型
        var valueType = value.GetType();
        if (valueType.Name is "Task`1" or "Task")
        {
            try
            {
                // 使用反射获取 Result 属性或等待 Task
                var resultProperty = valueType.GetProperty("Result");
                if (resultProperty != null)
                {
                    // 对于 Task<T>，获取 Result 属性
                    var taskResult = resultProperty.GetValue(value);
                    return ObjToValue(taskResult);
                }

                // 对于 Task (无返回值)，返回 VoidLangValue
                return new VoidLangValue();
            }
            catch
            {
                // 如果无法获取 Task 结果，返回 VoidLangValue
                return new VoidLangValue();
            }
        }

        return value switch
        {
            int a => IntLangValue.Create(a),
            string a => StringLangValue.Create(a),
            double a => DoubleLangValue.Create(a),
            char a => CharLangValue.Create(a),
            List<object> a => new ListLangValue(a),
            List<string> a => new ListLangValue(a.Cast<object>().ToList()),
            List<int> a => new ListLangValue(a.Cast<object>().ToList()),
            List<double> a => new ListLangValue(a.Cast<object>().ToList()),
            object[] a => new ArrayLangValue(a.ToList()),
            long a => IntLangValue.Create((int)a),
            bool a => BoolLangValue.Create(a),
            Dictionary<object, object> a => new DictionaryLangValue(a.Select(x =>
            {
                var key = ObjToValue(x.Key);
                var val = ObjToValue(x.Value);
                return new KeyValuePair<LangExpression, LangExpression>(key, val);
            }).ToList()),
            Tuple<object, object> a => new TupleLangValue(ObjToValue(a.Item1), ObjToValue(a.Item2)),
            ValueTuple<object, object> a => new TupleLangValue(ObjToValue(a.Item1), ObjToValue(a.Item2)),
            _ => TryEnhancedConversion(value, new SourcePosition())
        };
    }

    /// <summary>
    /// 将 .NET 对象转换为 Old8Lang 值（增强版本，带位置信息）
    /// </summary>
    /// <param name="value">要转换的 .NET 对象</param>
    /// <param name="position">源代码位置，用于错误报告</param>
    /// <returns>转换后的 Old8Lang 值</returns>
    public static LangValueType ObjToValue(object? value, SourcePosition position)
    {
        if (value == null)
        {
            return NullLangValue.Instance;
        }

        var valueType = value.GetType();

        // 处理可空类型
        if (Nullable.GetUnderlyingType(valueType) != null)
        {
            return value switch
            {
                null => NullLangValue.Instance,
                _ => ObjToValue(Convert.ChangeType(value, Nullable.GetUnderlyingType(valueType)!), position)
            };
        }

        // 处理 Task 和 Task<T> 类型
        if (valueType.Name is "Task`1" or "Task")
        {
            return HandleTaskConversionEnhanced(value, position);
        }

        // 处理数组
        if (valueType.IsArray)
        {
            return HandleArrayConversionEnhanced(value, position);
        }

        // 处理枚举
        if (valueType.IsEnum)
        {
            return StringLangValue.Create(value.ToString());
        }

        // 处理集合类型
        if (IsGenericCollectionTypeEnhanced(valueType, out var elementType))
        {
            return HandleCollectionConversionEnhanced(value, elementType, position);
        }

        // 处理字典类型
        if (IsDictionaryTypeEnhanced(valueType, out var keyType, out var valueTypeArg))
        {
            return HandleDictionaryConversionEnhanced(value, keyType, valueTypeArg, position);
        }

        // 处理元组
        if (IsTupleTypeEnhanced(valueType))
        {
            return HandleTupleConversionEnhanced(value, position);
        }

        // 处理增强的基本类型
        return value switch
        {
            int a => IntLangValue.Create(a),
            long a => HandleLongConversionEnhanced(a, position),
            short a => IntLangValue.Create(a),
            byte a => IntLangValue.Create(a),
            float a => DoubleLangValue.Create(Math.Round(a, 10)),
            double a => DoubleLangValue.Create(a),
            decimal a => DoubleLangValue.Create(Math.Round((double)a, 15)),
            string a => StringLangValue.Create(a),
            char a => CharLangValue.Create(a),
            bool a => BoolLangValue.Create(a),
            DateTime a => StringLangValue.Create(a.ToString("O")),
            DateTimeOffset a => StringLangValue.Create(a.ToString("O")),
            Guid a => StringLangValue.Create(a.ToString()),
            byte[] a => new ListLangValue(a.Cast<object>().ToList()),
            List<object> a => new ListLangValue(a),
            List<string> a => new ListLangValue(a.Cast<object>().ToList()),
            List<int> a => new ListLangValue(a.Cast<object>().ToList()),
            List<double> a => new ListLangValue(a.Cast<object>().ToList()),
            object[] a => new ArrayLangValue(a.ToList()),
            Dictionary<object, object> a => HandleDictionaryConversion(a, position),
            Tuple<object, object> a => new TupleLangValue(ObjToValue(a.Item1, position), ObjToValue(a.Item2, position)),
            ValueTuple<object, object> a => new TupleLangValue(ObjToValue(a.Item1, position),
                ObjToValue(a.Item2, position)),
            _ => TryCustomConversionEnhanced(value, position)
        };
    }

    /// <summary>
    /// 处理 long 类型转换，避免溢出
    /// </summary>
    private static LangValueType HandleLongConversionEnhanced(long value, SourcePosition position)
    {
        if (value >= int.MinValue && value <= int.MaxValue)
        {
            return IntLangValue.Create((int)value);
        }

        // 如果超出 int 范围，转换为字符串
        return StringLangValue.Create(value.ToString());
    }

    /// <summary>
    /// 处理 Task 类型转换
    /// </summary>
    private static LangValueType HandleTaskConversionEnhanced(object task, SourcePosition position)
    {
        try
        {
            var taskType = task.GetType();
            var resultProperty = taskType.GetProperty("Result");

            if (resultProperty != null)
            {
                var taskResult = resultProperty.GetValue(task);
                return ObjToValue(taskResult, position);
            }

            return new VoidLangValue();
        }
        catch (Exception ex)
        {
            // Task 转换失败，返回包含错误信息的字符串
            return StringLangValue.Create($"Task转换失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理数组转换
    /// </summary>
    private static LangValueType HandleArrayConversionEnhanced(object array, SourcePosition position)
    {
        try
        {
            var list = new List<object>();
            foreach (var item in (System.Collections.IEnumerable)array)
            {
                list.Add(item);
            }

            return new ArrayLangValue(list);
        }
        catch (Exception ex)
        {
            return StringLangValue.Create($"数组转换失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理集合类型转换
    /// </summary>
    private static LangValueType HandleCollectionConversionEnhanced(object collection, Type elementType,
        SourcePosition position)
    {
        try
        {
            var list = new List<object>();
            foreach (var item in (System.Collections.IEnumerable)collection)
            {
                list.Add(item);
            }

            return new ListLangValue(list);
        }
        catch (Exception ex)
        {
            return StringLangValue.Create($"集合转换失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理字典类型转换
    /// </summary>
    private static LangValueType HandleDictionaryConversionEnhanced(object dictionary, Type keyType, Type valueTypeArg,
        SourcePosition position)
    {
        try
        {
            var pairs = new List<KeyValuePair<LangExpression, LangExpression>>();

            foreach (var item in (System.Collections.IDictionary)dictionary)
            {
                var entry = (System.Collections.DictionaryEntry)item;
                var key = ObjToValue(entry.Key, position);
                var value = ObjToValue(entry.Value, position);
                pairs.Add(new KeyValuePair<LangExpression, LangExpression>(key, value));
            }

            return new DictionaryLangValue(pairs);
        }
        catch (Exception ex)
        {
            return StringLangValue.Create($"字典转换失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理字典转换（向后兼容）
    /// </summary>
    private static LangValueType HandleDictionaryConversion(Dictionary<object, object> dict, SourcePosition position)
    {
        return new DictionaryLangValue(dict.Select(x =>
        {
            var key = ObjToValue(x.Key, position);
            var val = ObjToValue(x.Value, position);
            return new KeyValuePair<LangExpression, LangExpression>(key, val);
        }).ToList());
    }

    /// <summary>
    /// 处理元组类型转换
    /// </summary>
    private static LangValueType HandleTupleConversionEnhanced(object tuple, SourcePosition position)
    {
        try
        {
            var tupleType = tuple.GetType();

            // 处理 ValueTuple
            if (tupleType.FullName?.StartsWith("System.ValueTuple") == true)
            {
                var fields = tupleType.GetFields();
                if (fields.Length >= 2)
                {
                    var item1 = fields[0].GetValue(tuple);
                    var item2 = fields[1].GetValue(tuple);
                    return new TupleLangValue(ObjToValue(item1, position), ObjToValue(item2, position));
                }
            }

            // 处理 Tuple
            if (tupleType.FullName?.StartsWith("System.Tuple") == true)
            {
                var properties = tupleType.GetProperties();
                if (properties.Length >= 2)
                {
                    var item1 = properties[0].GetValue(tuple);
                    var item2 = properties[1].GetValue(tuple);
                    return new TupleLangValue(ObjToValue(item1, position), ObjToValue(item2, position));
                }
            }

            return StringLangValue.Create($"不支持的元组类型: {tupleType.Name}");
        }
        catch (Exception ex)
        {
            return StringLangValue.Create($"元组转换失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 尝试自定义类型转换
    /// 对于 C# 自定义类和未识别的类型，使用 NativeObjectLangValue 包装
    /// </summary>
    private static LangValueType TryCustomConversionEnhanced(object value, SourcePosition position)
    {
        try
        {
            var valueType = value.GetType();

            // 跳过 .NET 基础类型和常见系统类型
            if (valueType.Namespace?.StartsWith("System") == true &&
                !valueType.IsClass || valueType.IsPrimitive)
            {
                // 对于系统类型，尝试转换为字符串
                return StringLangValue.Create(value.ToString() ?? "null");
            }

            // 对于自定义类型，使用 NativeObjectLangValue 包装
            // 这保留了类型信息，允许访问成员和调用方法
            return new Intermediates.NativeObjectLangValue(value, position);
        }
        catch (Exception ex)
        {
            // 最后的备选方案：转换为字符串
            return StringLangValue.Create($"[{value.GetType().Name} 转换失败: {ex.Message}]");
        }
    }

    /// <summary>
    /// 尝试增强的类型转换（用于原方法的 fallback）
    /// </summary>
    private static LangValueType TryEnhancedConversion(object value, SourcePosition position)
    {
        try
        {
            // 尝试新的转换逻辑
            return ObjToValue(value, position);
        }
        catch
        {
            // 如果新方法也失败，返回字符串表示
            try
            {
                return StringLangValue.Create(value.ToString() ?? "null");
            }
            catch
            {
                return StringLangValue.Create($"[{value.GetType().Name}]");
            }
        }
    }

    /// <summary>
    /// 检查是否为泛型集合类型
    /// </summary>
    private static bool IsGenericCollectionTypeEnhanced(Type type, out Type elementType)
    {
        elementType = null!;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        if (type.GetInterface("System.Collections.Generic.IEnumerable`1") != null)
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否为字典类型
    /// </summary>
    private static bool IsDictionaryTypeEnhanced(Type type, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var args = type.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
        }

        if (type.GetInterface("System.Collections.Generic.IDictionary`2") != null)
        {
            var args = type.GetGenericArguments();
            keyType = args[0];
            valueType = args[1];
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查是否为元组类型
    /// </summary>
    private static bool IsTupleTypeEnhanced(Type type)
    {
        return type.FullName?.StartsWith("System.Tuple") == true ||
               type.FullName?.StartsWith("System.ValueTuple") == true;
    }

    #region ValueToObj - Old8Lang 值到 .NET 对象的统一转换

    /// <summary>
    /// 将 Old8Lang 值类型转换为 .NET 对象（统一转换方法）
    /// </summary>
    /// <param name="value">要转换的 Old8Lang 值</param>
    /// <returns>对应的 .NET 对象</returns>
    /// <remarks>
    /// 这是 ObjToValue 的镜像方法，确保双向转换的对称性。
    /// 支持的转换类型包括：
    /// - IntLangValue → int
    /// - DoubleLangValue → double
    /// - StringLangValue → string
    /// - CharLangValue → char
    /// - BoolLangValue → bool
    /// - NullLangValue → null
    /// - ListLangValue → List&lt;object&gt;
    /// - ArrayLangValue → object[]
    /// - DictionaryLangValue → Dictionary&lt;object, object&gt;
    /// - TupleLangValue → Tuple&lt;object, object&gt;
    /// - VoidLangValue → null
    /// - FuncLangValue → Delegate (保留引用)
    /// - Instance → 保留实例引用
    /// - 其他类型 → 调用 GetValue() 或返回字符串表示
    /// </remarks>
    public static object? ValueToObj(LangValueType? value)
    {
        if (value == null)
        {
            return null;
        }

        return value switch
        {
            // 基本类型
            NullLangValue => null,
            VoidLangValue => null,
            IntLangValue intVal => intVal.Value,
            DoubleLangValue doubleVal => doubleVal.Value,
            StringLangValue strVal => strVal.Value,
            CharLangValue charVal => charVal.Value,
            BoolLangValue boolVal => boolVal.Value,

            // 集合类型
            ListLangValue listVal => ValueToObjList(listVal),
            ArrayLangValue arrayVal => ValueToObjArray(arrayVal),
            DictionaryLangValue dictVal => ValueToObjDictionary(dictVal),
            TupleLangValue tupleVal => ValueToObjTuple(tupleVal),

            // 函数和实例类型（保留引用）
            FuncLangValue funcVal => funcVal,
            AsyncFuncLangValue asyncFuncVal => asyncFuncVal,
            Instance instance => instance,

            // 任意类型（自定义类实例）
            AnyLangValue anyVal => anyVal,

            // Native 类型包装
            NativeObjectLangValue nativeObj => nativeObj.NativeObject,
            NativeAnyLangValue nativeAny => nativeAny,
            NativeStaticAny nativeStatic => nativeStatic,

            // 类型值
            TypeLangValue typeVal => typeVal,

            // 异步类型
            TaskLangValue taskVal => taskVal,
            ThreadLangValue threadVal => threadVal,
            CancellationTokenLangValue tokenVal => tokenVal,

            // 其他类型，尝试使用 GetValue() 或返回自身
            _ => ValueToObjFallback(value)
        };
    }

    /// <summary>
    /// 将 ListLangValue 转换为 List&lt;object&gt;
    /// </summary>
    private static List<object> ValueToObjList(ListLangValue listVal)
    {
        var result = new List<object>();

        // 优先使用 Values 字段（已运行的结果）
        if (listVal.Values is { Count: > 0 })
        {
            foreach (var item in listVal.Values)
            {
                result.Add(ValueToObj(item) ?? new object());
            }
        }

        return result;
    }

    /// <summary>
    /// 将 ArrayLangValue 转换为 object[]
    /// </summary>
    private static object[] ValueToObjArray(ArrayLangValue arrayVal)
    {
        var runResult = arrayVal.RunResult;
        if (runResult == null || runResult.Length == 0)
        {
            return [];
        }

        var result = new object[runResult.Length];
        for (int i = 0; i < runResult.Length; i++)
        {
            result[i] = ValueToObj(runResult[i]) ?? new object();
        }

        return result;
    }

    /// <summary>
    /// 将 DictionaryLangValue 转换为 Dictionary&lt;object, object&gt;
    /// </summary>
    private static Dictionary<object, object> ValueToObjDictionary(DictionaryLangValue dictVal)
    {
        var result = new Dictionary<object, object>();

        // 使用 Value 字段（已运行的键值对）
        foreach (var (key, value) in dictVal.Value)
        {
            var objKey = ValueToObj(key) ?? new object();
            var objValue = ValueToObj(value) ?? new object();
            result[objKey] = objValue;
        }

        return result;
    }

    /// <summary>
    /// 将 TupleLangValue 转换为 Tuple&lt;object, object&gt;
    /// </summary>
    private static Tuple<object, object> ValueToObjTuple(TupleLangValue tupleVal)
    {
        var (key, value) = tupleVal.Value;

        // 检查 Value 是否已初始化（Run() 是否已被调用）
        // 如果 Value.Item1 或 Item2 为 null，尝试从 V1, V2 字段获取值
        if (key == null && tupleVal.V1 is LangValueType v1)
        {
            key = v1;
        }

        if (value == null && tupleVal.V2 is LangValueType v2)
        {
            value = v2;
        }

        var objKey = ValueToObj(key) ?? new object();
        var objValue = ValueToObj(value) ?? new object();

        return new Tuple<object, object>(objKey, objValue);
    }

    /// <summary>
    /// 回退转换方法，用于处理无法直接转换的类型
    /// </summary>
    private static object ValueToObjFallback(LangValueType value)
    {
        try
        {
            // 尝试调用 GetValue() 方法
            var result = value.GetValue();

            // 如果返回的是默认的空 object，尝试返回字符串表示
            if (result.GetType() == typeof(object) && result.Equals(new object()))
            {
                return value.ToDisplayString();
            }

            return result;
        }
        catch
        {
            // 如果失败，返回字符串表示
            return value.ToDisplayString();
        }
    }

    #endregion
}